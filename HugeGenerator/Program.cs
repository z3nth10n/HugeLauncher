using Fclp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HugeGenerator
{
    internal class Program
    {
        /*

            TODO:

            Especificar -update por cli para poner como false o true la bool realtimeVersion

             */

        private static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected

        // Pinvoke
        private delegate bool ConsoleEventDelegate(int eventType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        public const string TagList = "https://gitlab.com/api/v3/projects/{0}/repository/tags",
                            TreeList = "https://gitlab.com/api/v3/projects/{0}/repository/tree?ref={1}&recursive=1",
                            FileRepoInfo = "https://gitlab.com/api/v3/projects/{0}/repository/files?ref={1}&file_path={2}",
                            _ModpackName = "HugeCraft";

        public const int _RepoID = 3820415;

        public static List<TagData> tagData = new List<TagData>();
        public static List<TreeFile> treeFile = new List<TreeFile>();
        public static List<FileInfo> fileInfo = new List<FileInfo>();

        public static bool realtimeVersion;

        public static string AppPath
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }

        public static string FileInfoPath
        {
            get
            {
                return Path.Combine(AppPath, "Result", string.Format("fileInfo_{0}.json", appArgs.RepoID));
            }
        }

        public static string AppResultPath
        {
            get
            {
                return Path.Combine(Path.GetFullPath(appArgs.Path).Replace(Environment.CurrentDirectory, ""), string.Format("{0}_Data.json", appArgs.ModpackName));
            }
        }

        internal static AppArgs appArgs = new AppArgs();

        private static void Main(string[] args)
        {
            var p = new FluentCommandLineParser();

            p.Setup<bool>('u', "update")
                .Callback(x => appArgs.Update = x)
                .WithDescription("Specify if you want to get the version from the local machine or from GitLab.");

            if (appArgs.Update == default(bool))
                appArgs.Update = false;

            p.Setup<int>("id")
                .Callback(x => appArgs.RepoID = x)
                .WithDescription("Specify the ID of the repository.");

            if (appArgs.RepoID == default(int))
                appArgs.RepoID = _RepoID;

            p.Setup<string>('n', "name")
                .Callback(x => appArgs.ModpackName = x)
                .WithDescription("Specify the name of the modpack.");

            if (appArgs.ModpackName == default(string) || string.IsNullOrEmpty(appArgs.ModpackName))
                appArgs.ModpackName = _ModpackName;

            p.Setup<string>('p', "path")
                .Callback(x => appArgs.Path = x)
                .WithDescription("Specify the path of the <modpack_name>_Data.json file, that is the result of this program.");

            if (appArgs.Path == default(string) || string.IsNullOrEmpty(appArgs.Path))
                appArgs.Path = AppPath;

            p.SetupHelp("?", "help")
                .Callback(text => Console.WriteLine(text));

            if (!p.Parse(args).HasErrors)
                Run();
            else
                p.HelpOption.ShowHelp(p.Options);
        }

        private static void Run()
        {
            realtimeVersion = appArgs.Update;

            string _fol = Path.Combine(AppPath, "Result");
            if (!Directory.Exists(_fol))
                Directory.CreateDirectory(_fol);

            Stopwatch sw = new Stopwatch();
            long ellapsed = 0;

            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);

            sw = Stopwatch.StartNew();

            if (!realtimeVersion && File.Exists(FileInfoPath))
                fileInfo = JsonConvert.DeserializeObject<FileInfo[]>(File.ReadAllText(FileInfoPath)).ToList();

            tagData = GetTagData();

            sw.Stop();

            Console.WriteLine("[GetTagData] First phase finished in {0} ms!", sw.ElapsedMilliseconds);

            ellapsed += sw.ElapsedMilliseconds;

            AppData data = new AppData();
            data.Name = appArgs.ModpackName;

            foreach (TagData tag in tagData)
            {
                string reff = tag.Commit["id"].ToObject<string>();

                treeFile = GetRepoTree(reff);

                int i = 0,
                    count = treeFile.Count;

                long curEllapsed = 0;

                foreach (TreeFile file in treeFile)
                {
                    if (file.Type == FileType.Blob && !fileInfo.Any(x => x.FilePath == file.FilePath))
                    {
                        sw = Stopwatch.StartNew();
                        Console.WriteLine("Adding {0}... Processed {1} of {2} files ({3:F2}%)", file.FilePath, i, count, i * 100d / count);

                        FileInfo fil = GetFileInfo(file.FilePath, reff);

                        if (fil != null)
                            fileInfo.Add(fil);
                        else
                            Console.WriteLine("Exception ocurred in {0}th file!", i);

                        sw.Stop();

                        Console.WriteLine("Ended in {0} ms!\n", sw.ElapsedMilliseconds);

                        ellapsed += sw.ElapsedMilliseconds;
                        curEllapsed += sw.ElapsedMilliseconds;
                    }
                    else
                        Console.WriteLine("[{0}: {1}] Skipping file {2}!\n", file.Type, file.FilePath, i);
                    ++i;
                }

                Console.WriteLine("[GetTreeFile] Second phase finished in {0} ms!", curEllapsed);

                sw = Stopwatch.StartNew();

                List<FileData> fileData = new List<FileData>();
                foreach (FileInfo file in fileInfo)
                    if (!fileData.Any(x => x.FileName == file.FilePath))
                        fileData.Add(new FileData(file.BlobId, file.CommitId, file.FilePath, file.Size));

                ModpackData modpack = new ModpackData();
                modpack.Files = fileData.ToArray();
                modpack.TotalSize = fileData.Sum(x => x.Size);
                modpack.Version = tag.Name;

                data.AddVersion(modpack);

                sw.Stop();

                Console.WriteLine("[GetFileData] Third phase finished in {0} ms!", sw.ElapsedMilliseconds);

                ellapsed += sw.ElapsedMilliseconds;
            }

            File.WriteAllText(AppResultPath, JsonConvert.SerializeObject(data, Formatting.Indented));

            Console.WriteLine("Finished everything in {0} ms!", ellapsed);
            Console.Read();
        }

        private static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                if (!realtimeVersion && fileInfo != null && fileInfo.Count > 0)
                    File.WriteAllText(FileInfoPath, JsonConvert.SerializeObject(fileInfo.ToArray(), Formatting.Indented));
            }
            return false;
        }

        public static string ExportList(string filename, string url)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        string filePath = Path.Combine(AppPath, "Result", filename);
                        bool realtime = realtimeVersion || !File.Exists(filePath);
                        string content = realtime ? client.DownloadString(url) : File.ReadAllText(filePath);
                        if (realtime)
                            File.WriteAllText(filePath, content);
                        return content;
                    }
                    catch
                    {
                        //Not found file
                        return null;
                    }
                }
            }
            catch
            {
                //Internet connection lost
                NoInternet();
                return null;
            }
        }

        public static List<TagData> GetTagData()
        {
            return JsonConvert.DeserializeObject<TagData[]>(ExportList(string.Format("tagData_{0}.json", appArgs.RepoID), string.Format(TagList, appArgs.RepoID))).ToList();
        }

        public static List<TreeFile> GetRepoTree(string reff)
        {
            return JsonConvert.DeserializeObject<TreeFile[]>(ExportList(string.Format("treeFile_{0}.json", appArgs.RepoID), string.Format(TreeList, appArgs.RepoID, reff))).ToList();
        }

        public static FileInfo GetFileInfo(string path, string reff)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        string content = client.DownloadString(string.Format(FileRepoInfo, appArgs.RepoID, reff, path));
                        FileInfo file = JsonConvert.DeserializeObject<FileInfo>(content);
                        file.Content = ""; //Para ahorrar espacio
                        return file;
                    }
                    catch
                    {
                        //Not found file
                        return null;
                    }
                }
            }
            catch
            {
                //Internet connection lost
                NoInternet();
                return null;
            }
        }

        internal static void NoInternet()
        {
            Console.WriteLine("Internet lost connection! Force program exit...");
            Console.WriteLine("");
            Console.WriteLine("Press any key to exit application...");
            Console.Read();
            ForceAppShutdown();
        }

        internal static void ForceAppShutdown()
        {
            Environment.Exit(0);
        }
    }

    public enum FileType { Tree, Blob }

    public class TreeFile
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("name")]
        public string FileName;

        [JsonProperty("path")]
        public string FilePath;

        [JsonProperty("type")]
        public FileType Type;

        [JsonProperty("mode")]
        public int Mode;
    }

    public class FileInfo
    {
        [JsonProperty("file_name")]
        public string FileName;

        [JsonProperty("file_path")]
        public string FilePath;

        [JsonProperty("encoding")]
        public string Encoding;

        [JsonProperty("content")]
        public string Content;

        [JsonProperty("ref")]
        public string Ref;

        [JsonProperty("blob_id")]
        public string BlobId;

        [JsonProperty("commit_id")]
        public string CommitId;

        [JsonProperty("last_commit_id")]
        public string LastCommitId;

        [JsonProperty("size")]
        public int Size;
    }

    public class TagData
    {
        [JsonProperty("release")]
        public object Release;

        [JsonProperty("commit")]
        public JObject Commit;

        [JsonProperty("message")]
        public string Message;

        [JsonProperty("name")]
        public string Name;
    }

    //This needs to go to the API
    public class FileData
    {
        public const string RawUrl = "https://gitlab.com/{0}/{1}/raw/{2}/{3}";
        public static string Author = "ikillnukes1", RepoName = "HugeCraft-Client";

        public string Id,
                      CommitId,
                      FileRelPath;

        public string FileName
        {
            get
            {
                return Path.GetFileName(FileRelPath);
            }
        }

        public string FileUrl
        {
            get
            {
                return string.Format(RawUrl, Author, RepoName, CommitId, FileRelPath);
            }
        }

        public int Size;

        public FileData(string id, string cid, string path, int siz)
        {
            Id = id;
            CommitId = cid;
            FileRelPath = path;
            Size = siz;
        }
    }

    public class ModpackData
    {
        public string Version;
        public FileData[] Files;
        public int TotalSize;
    }

    public class AppData
    {
        public string Name;
        public ModpackData[] Versions;

        public void AddVersion(ModpackData data)
        {
            ArrayExtensions.Append(ref Versions, data);
            /*List<ModpackData> mData = Versions != null ? Versions.ToList() : new List<ModpackData>();
            mData.Add(data);
            Versions = mData.ToArray();*/
        }
    }

    public static class ArrayExtensions
    {
        public static void Append<T>(ref T[] array, T append)
        {
            if (array == null) array = new T[0];
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = append;    // < Adds an extra element to my array
        }
    }

    public class AppArgs
    {
        public bool Update { get; set; }
        public int RepoID { get; set; }

        //public string Ref { get; set; } //If you want to catch only one value
        public string ModpackName { get; set; }

        public string Path { get; set; }

        public AppArgs()
        {
            Update = false;
            RepoID = Program._RepoID;
            ModpackName = Program._ModpackName;
            Path = Program.AppPath;
        }
    }
}