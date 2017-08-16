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
using System.Threading;

namespace HugeGenerator
{
    public enum LocationData
    {
        TagData,
        TreeData
    }

    internal class Program
    {
        /*

            TODO:

            Tengo que obtener las distintas versiones, e ir guardando el treeFile y el fileInfo segun la version, ya que, no nos interesa volver a descargar antiguas versiones
            simplemente queremos actualizar el json para obtener las nuevas
            En vez de hacer tres listas voy a unificar todo en un archivo, además de que no voy a usar clases personalizadas, si no, JObjects
            Quiero hacer funcionar esto pa github

             */

        private static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected

        // Pinvoke
        private delegate bool ConsoleEventDelegate(CtrlType eventType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        public const string TagList = "https://gitlab.com/api/v3/projects/{0}/repository/tags",
                            TreeList = "https://gitlab.com/api/v3/projects/{0}/repository/tree?ref={1}&recursive=1",
                            FileRepoInfo = "https://gitlab.com/api/v3/projects/{0}/repository/files?ref={1}&file_path={2}",
                            _ModpackName = "HugeCraft";

        public const int _RepoID = 3820415;

        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        public static bool realtimeVersion;

        internal static Timer internalTimer;

        public static string AppPath
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }

        public static string AppResultPath
        {
            get
            {
                return Path.Combine(Path.GetFullPath(Path.Combine(AppPath, appArgs.Path)), string.Format("{0}_Data.json", appArgs.ModpackName));
            }
        }

        public static string RepoResultPath
        {
            get
            {
                return Path.Combine(AppPath, "Result", string.Format("RepoResult_{0}.json", appArgs.RepoID));
            }
        }

        internal static RepoData repoData = new RepoData();
        internal static AppArgs appArgs = new AppArgs();
        internal static int executingTime = 0;
        internal static bool finished;

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

            internalTimer = new Timer(timer_Elapsed);
            internalTimer.Change(60000, 60000);

            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            executingTime = (int) t.TotalSeconds;

            string _fol = Path.Combine(AppPath, "Result");
            if (!Directory.Exists(_fol))
                Directory.CreateDirectory(_fol);

            Stopwatch sw = new Stopwatch();
            long ellapsed = 0;

            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);

            sw = Stopwatch.StartNew();

            if (!realtimeVersion && File.Exists(RepoResultPath))
                repoData = JsonConvert.DeserializeObject<RepoData>(File.ReadAllText(RepoResultPath));

            repoData.tagData = GetTagData();

            sw.Stop();

            Console.WriteLine("[GetTagData] First phase finished in {0} ms!", sw.ElapsedMilliseconds);

            ellapsed += sw.ElapsedMilliseconds;

            AppData data = new AppData();
            data.Name = appArgs.ModpackName;

            foreach (JObject tag in repoData.tagData)
            {
                string ver = tag["name"].ToObject<string>();

                if (!repoData.IsLatest(ver) && repoData.ContainsVersion(ver))
                    continue; //Si no estamos actualizando o si lo estamos haciendo, pero no es la ultima version y dicha version está ya completa entonces skipeamos

                string reff = tag["commit"]["id"].ToObject<string>();

                repoData.GetVersion(ver).treeData = GetRepoTree(reff, ver);

                int i = 0,
                    count = repoData.GetVersion(ver).treeData.Length;

                long curEllapsed = 0;

                foreach (JObject file in repoData.GetVersion(ver).treeData)
                {
                    try
                    {
                        string type = file["type"].ToObject<string>();
                        if (type == "blob" && (repoData.GetVersion(ver).fileData == null || !repoData.GetVersion(ver).fileData.Any(x => x["file_path"].ToObject<string>() == file["path"].ToObject<string>())))
                        {
                            sw = Stopwatch.StartNew();
                            Console.WriteLine("Adding {0}... Processed {1} of {2} files ({3:F2}%)", file["path"].ToObject<string>(), i, count, i * 100d / (count - 1));

                            JObject fil = GetFileInfo(file["path"].ToObject<string>(), reff);

                            if (fil != null)
                                ArrayExtensions.Append(ref repoData.GetVersion(ver).fileData, fil);
                            else
                                Console.WriteLine("Exception ocurred in {0}th file!", i);

                            sw.Stop();

                            Console.WriteLine("Ended in {0} ms!\n", sw.ElapsedMilliseconds);

                            ellapsed += sw.ElapsedMilliseconds;
                            curEllapsed += sw.ElapsedMilliseconds;
                        }
                        else
                            Console.WriteLine("[{0}: {1}] Skipping file {2}!\n", type, file["path"].ToObject<string>(), i);
                        ++i;
                    }
                    catch
                    {
                        Console.WriteLine("Value null!");
                        sw.Stop();
                    }
                }

                Console.WriteLine("[GetTreeFile] Second phase finished in {0} ms!", curEllapsed);

                sw = Stopwatch.StartNew();

                List<FileData> fileData = new List<FileData>();
                foreach (JObject file in repoData.GetVersion(ver).fileData)
                    if (!fileData.Any(x => x.FileName == file["file_path"].ToObject<string>()))
                        fileData.Add(new FileData(file["blob_id"].ToObject<string>(), file["commit_id"].ToObject<string>(), file["file_path"].ToObject<string>(), file["size"].ToObject<int>()));

                ModpackData modpack = new ModpackData();
                modpack.Files = fileData.ToArray();
                modpack.TotalSize = fileData.Sum(x => x.Size);
                modpack.Version = ver;

                data.AddVersion(modpack);

                sw.Stop();

                Console.WriteLine("[GetFileData] Third phase finished in {0} ms!", sw.ElapsedMilliseconds);

                ellapsed += sw.ElapsedMilliseconds;
            }

            string _fol1 = Path.GetDirectoryName(AppResultPath);
            if (!Directory.Exists(_fol1))
                Directory.CreateDirectory(_fol1);

            File.WriteAllText(AppResultPath, JsonConvert.SerializeObject(data, Formatting.Indented));
            finished = true;

            Console.WriteLine("Finished everything in {0} ms!", ellapsed);
            Console.Read();
        }

        private static bool ConsoleEventCallback(CtrlType eventType)
        {
            switch (eventType)
            {
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                default:
                    SaveRepoData();
                    return false;
            }
        }

        internal static JObject[] GetData(LocationData data, string ver)
        {
            if (repoData == null) return null;
            switch (data)
            {
                case LocationData.TreeData:
                    return repoData.GetVersion(ver).treeData;

                case LocationData.TagData:
                    return repoData.tagData;
            }
            return null;
        }

        internal static void UpdateData(LocationData data, JObject[] obj, string ver = "")
        {
            switch (data)
            {
                case LocationData.TreeData:
                    repoData.GetVersion(ver).treeData = obj;
                    break;

                case LocationData.TagData:
                    repoData.tagData = obj;
                    break;
            }
        }

        public static JObject[] ExportList(string url, LocationData data, string ver = "")
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        JObject[] obj = GetData(data, ver);
                        bool realtime = realtimeVersion || obj == null;
                        JObject[] content = realtime ? JsonConvert.DeserializeObject<JObject[]>(client.DownloadString(url)) : obj;
                        if (realtime) //Si hemos traido los datos de internet actualizar
                            UpdateData(data, obj, ver);
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

        public static JObject[] GetTagData()
        {
            return ExportList(string.Format(TagList, appArgs.RepoID), LocationData.TagData);
        }

        public static JObject[] GetRepoTree(string reff, string ver)
        {
            return ExportList(string.Format(TreeList, appArgs.RepoID, reff), LocationData.TreeData, ver);
        }

        public static JObject GetFileInfo(string path, string reff)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        string content = client.DownloadString(string.Format(FileRepoInfo, appArgs.RepoID, reff, path));
                        JObject file = JsonConvert.DeserializeObject<JObject>(content);
                        file["content"] = null; //Para ahorrar espacio
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

        private static void timer_Elapsed(object o)
        {
            // do stuff every minute
            if (!finished)
                SaveRepoData();
        }

        private static void SaveRepoData()
        {
            if (repoData != null && repoData.latestRelease.ContainsData())
            { //He quitado el !realtimeVersion, porque aunque sea en tiempo real siempre interesará guardar una copia por si se quiere retomar desde ahí
                Console.WriteLine("\nSuccessfully saved repo data into IO!\n");
                File.WriteAllText(RepoResultPath, JsonConvert.SerializeObject(repoData, Formatting.Indented));
            }
        }
    }

    public class RepoData
    {
        public ObjectData[] objData;
        public JObject[] tagData; //All available releases with their commits

        [JsonIgnore]
        public ObjectData latestRelease
        {
            get
            {
                if (objData != null && objData.Length > 0)
                    return objData[0]; //Supuestamente la ultima release es el primer valor de la array
                return null;
            }
        }

        public ObjectData GetVersion(string key)
        {
            bool isNNull = objData != null && objData.Length > 0;
            ObjectData data = isNNull ? objData.SingleOrDefault(x => x.Version == key) : null;
            if (isNNull)
                return data;
            else if (data == null || !isNNull)
            {
                ObjectData obj = new ObjectData() { Version = key };
                ArrayExtensions.Append(ref objData, obj);
                return obj;
            }
            return null;
        }

        public bool IsLatestNull()
        {
            return latestRelease == null;
        }

        public bool IsLatest(string ver)
        {
            return latestRelease != null && latestRelease.Version == ver;
        }

        public bool ContainsVersion(string ver)
        {
            ObjectData data = GetVersion(ver);
            return data != null
                && (data.treeData != null && data.treeData.Length > 0
                || data.fileData != null && data.fileData.Length > 0);
        }
    }

    public class ObjectData
    {
        public string Version;

        public JObject[] treeData, //All files of a repo of a current commit
                         fileData; //Individual file data

        public bool ContainsData()
        {
            return treeData != null && treeData.Length > 0
                || fileData != null && fileData.Length > 0;
        }
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