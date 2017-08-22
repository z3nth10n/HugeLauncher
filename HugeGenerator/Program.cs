using CsQuery;
using CsQuery.ExtensionMethods;
using Fclp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace HugeGenerator
{
    public enum LocationData
    {
        TagData,
        TreeData
    }

    public enum RepoType
    {
        Github,
        Gitlab
    }

    public class Program
    {
        /*

            TODO:

            Creo que los datos no se cargan del disco duro... (Hace una cosa rara, cuando hay datos de internet y se tienen que coger del hdd (realtimeVersion: false))

             */

        private static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected

        // Pinvoke
        private delegate bool ConsoleEventDelegate(CtrlType eventType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

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
                return Path.Combine(Path.GetFullPath(Path.Combine(AppPath, appArgs.SavePath)), string.Format("{0}_Data.json", appArgs.ModpackName));
            }
        }

        public static string RepoResultPath
        {
            get
            {
                return Path.Combine(AppPath, "Result", string.Format("RepoResult_{0}.json", appArgs.RepoName));
            }
        }

        public const string _ModpackName = "HugeCraft",
                            _DefRepoUrl = "http://gitlab.com/ikillnukes1/HugeCraft-Client";

        public static bool realtimeVersion;

        internal static Timer internalTimer;
        internal static RepoData repoData = new RepoData();
        internal static AppArgs appArgs = new AppArgs();

        internal static int executingTime = 0,
                            loadedTrees = 0,
                            loadedFiles = 0;

        internal static bool finished;
        internal static RepoAPI repoApi;

        private static void Main(string[] args)
        {
            var p = new FluentCommandLineParser();

            p.Setup<string>("url")
                .Callback(x => appArgs.RepoUrl = x)
                .WithDescription("Specify the ID of the repository.");

            // Console.WriteLine(appArgs.RepoUrl);

            //if (appArgs.RepoUrl == default(string))
            //    appArgs.RepoUrl = _DefRepoUrl;

            p.Setup<bool>('u', "update")
                .Callback(x => appArgs.Update = x)
                .WithDescription("Specify if you want to get the version from the local machine or from GitLab/Github.");

            if (appArgs.Update == default(bool))
                appArgs.Update = false;

            //Debug purpouses (don't work at saving, so, don't use if you won't fix this)
            p.Setup<bool>('f', "force")
                .Callback(x => appArgs.ForceUdateOldReleases = x)
                .WithDescription("Specify if you want to update Old Releases on the HDD.");

            if (appArgs.ForceUdateOldReleases == default(bool))
                appArgs.ForceUdateOldReleases = false;

            p.Setup<string>('n', "name")
                .Callback(x => appArgs.ModpackName = x)
                .WithDescription("Specify the name of the modpack.");

            if (appArgs.ModpackName == default(string) || string.IsNullOrEmpty(appArgs.ModpackName))
                appArgs.ModpackName = _ModpackName;

            p.Setup<string>('p', "path")
                .Callback(x => appArgs.SavePath = x)
                .WithDescription("Specify the path of the <modpack_name>_Data.json file, that is the result of this program.");

            if (appArgs.SavePath == default(string) || string.IsNullOrEmpty(appArgs.SavePath))
                appArgs.SavePath = AppPath;

            p.SetupHelp("?", "help")
                .Callback(text => Console.WriteLine(text));

            var res = p.Parse(args);

            if (!res.HasErrors)
                Run();
            else
                p.HelpOption.ShowHelp(p.Options);
        }

        private static void Run()
        {
            realtimeVersion = appArgs.Update;

            if (!appArgs.RepoUrl.ValidUrl())
                throw new Exception(string.Format("{0} is a valid Url for --url parameter!", appArgs.RepoUrl));

            Uri appUrl = new Uri(appArgs.RepoUrl);
            string dom = appUrl.Host;

            if (dom.Contains("github.com"))
                repoApi = new GithubRepo();
            else if (dom.Contains("gitlab.com"))
                repoApi = new GitlabRepo();
            else
                throw new Exception(string.Format("[{0}] Unsuppoted repository used! Please, use Github or Gitlab.", dom));

            internalTimer = new Timer(timer_Elapsed);
            internalTimer.Change(60000, 60000);

            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            executingTime = (int) t.TotalSeconds;

            string _fol = Path.Combine(AppPath, "Result");
            if (!Directory.Exists(_fol))
                Directory.CreateDirectory(_fol);

            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);

            repoApi.Run();

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

        internal static JToken[] GetData(LocationData data, string ver)
        {
            if (repoData == null) return null;
            try
            {
                switch (data)
                {
                    case LocationData.TreeData:
                        return repoData.GetVersion(ver).treeData; //Mas especificamente en alguno de estos lados

                    case LocationData.TagData:
                        return repoData.tagData;
                }
            }
            catch
            { // Fail creating a new value... (Yes, we know can happen...)
                return null;
            }
            return null;
        }

        internal static void UpdateData(LocationData data, JToken[] obj, string ver = "")
        {
            switch (data)
            {
                case LocationData.TreeData:
                    //Console.WriteLine("aaaa11");
                    repoData.GetVersion(ver).treeData = obj;
                    break;

                case LocationData.TagData:
                    repoData.tagData = obj;
                    break;
            }
        }

        public static JToken[] ExportList(string url, LocationData data, string ver = "")
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        //Console.WriteLine(RepoAPI.repoType.ToString());
                        JToken[] obj = GetData(data, ver); //El problema está aqui
                        //Console.WriteLine(ver + " " + (obj == null || obj != null && obj.Count == 0));
                        bool realtime = realtimeVersion || (obj == null || obj != null && obj.Length == 0);
                        object content = realtime ? (RepoAPI.repoType == RepoType.Gitlab ? (object) JsonConvert.DeserializeObject<JArray>(client.DownloadString(url)) : JsonConvert.DeserializeObject<JObject>(client.DownloadString(url))) : (object) obj;
                        JToken[] ret = realtime ? (RepoAPI.repoType == RepoType.Gitlab ? ((JArray) content).Children().ToArray() : ((GithubRepo) repoApi).GetArray((JObject) content)) : (JToken[]) content;
                        if (!realtime)
                        {
                            if (data == LocationData.TreeData)
                                loadedTrees = ret.Length;
                            else
                                loadedFiles = ret.Length;
                        }
                        //Console.WriteLine("Ret NUL: {0}; Obj NUL: {1}", ret == null, obj == null);
                        //Console.WriteLine(RepoAPI.repoType.ToString());
                        //Console.WriteLine(JsonConvert.SerializeObject(content));
                        if (realtime) //Si hemos traido los datos de internet actualizar (TB SI SON DEL DISCO, PORQUE AUN ASI HAY QUE PASARLOS A MEMORIA)
                            UpdateData(data, ret, ver);
                        return ret;
                    }
                    catch (Exception ex)
                    {
                        //Not found file
                        Console.WriteLine("There was a problem exporting the list, message:\n\n{0}", ex.ToString());
                        return null;
                    }
                }
            }
            catch (Exception ex)
            { //Si se llega hasta aqui esq en el primer catch puede haber una excepcion...
                //Internet connection lost
                NoInternet(ex.ToString());
                return null;
            }
        }

        internal static void NoInternet(string ex)
        {
            Console.WriteLine("Internet lost connection! Force program exit...\nMessage: {0}", ex);
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

        internal static void SaveRepoData()
        {
            if (repoData != null && !repoData.IsLatestNull() && repoData.latestRelease.ContainsData() || appArgs.ForceUdateOldReleases)
            { //He quitado el !realtimeVersion, porque aunque sea en tiempo real siempre interesará guardar una copia por si se quiere retomar desde ahí
                Console.WriteLine("\nSuccessfully saved repo data into IO!\n");
                File.WriteAllText(RepoResultPath, JsonConvert.SerializeObject(repoData.GetSerializedData(), Formatting.Indented));
            }
        }
    }

    public class RepoData
    {
        public ObjectData[] objData;
        public JToken[] tagData; //All available releases with their commits

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

        public SerializedRepoData GetSerializedData()
        {
            return GetSerializedData(this);
        }

        public SerializedRepoData GetSerializedData(RepoData r)
        {
            if (r.objData == null || r.tagData == null)
                return null;

            int c = r.objData.Length - 1;

            JArray _tagData = new JArray(r.tagData);
            //r.tagData.ForEach(x => Console.WriteLine(JsonConvert.SerializeObject(x))); //_tagData.Add(x));
            //Console.Read();
            //_tagData = new JObject(); //r.tagData.Select(x => new JObject(x)).ToArray();

            if (r.objData.Any(x => x != null && x.treeData == null) || RepoAPI.repoType == RepoType.Gitlab && r.objData.Any(x => x != null && x.fileData == null))
                return null;

            SerializedObjectData[] _objData = new SerializedObjectData[c];
            _objData = r.objData.Select(x =>
                       new SerializedObjectData(x.Version,
                           RepoAPI.repoType == RepoType.Gitlab
                           ? (JContainer) new JArray(x.treeData)
                           : new JObject(x.treeData),
                           RepoAPI.repoType == RepoType.Gitlab ?
                           new JArray(x.fileData) : null))
                       .ToArray();

            /*for (int i = 0; i < c; ++i)
            {
                if (objData[i].treeData != null) objData[i].treeData = r.objData[i].treeData.Select(x => new JObject(x)).ToArray();
                if (RepoAPI.repoType == RepoType.Gitlab && objData[i].fileData != null)
                    objData[i].fileData = r.objData[i].fileData.Select(x => new JObject(x)).ToArray();
            }*/

            SerializedRepoData repo = new SerializedRepoData(_tagData, _objData);
            return repo;
        }

        public ObjectData GetVersion(string key)
        {
            bool isNNull = objData != null && objData.Length > 0; // && !objData.Any(x => x == null);
            ObjectData data = isNNull ? objData.SingleOrDefault(x => x != null && x.Version == key) : null;
            if (isNNull && data != null)
                return data;
            else if (data == null || !isNNull)
            {
                Console.WriteLine("Creating a new version: {0}...", key);
                ObjectData obj = new ObjectData() { Version = key };
                ArrayExtensions.Append(ref objData, obj);
                return objData[objData.Length - 1];
            }
            Console.WriteLine("Something went wrong creating a new version for {0}...", key);
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

        public JToken[] treeData, //All files of a repo of a current commit
                        fileData; //Individual file data (SOLO DISPONIBLE EN GITLAB)

        public ObjectData()
        {
            if (treeData == null) treeData = new JToken[0];
            if (fileData == null && RepoAPI.repoType == RepoType.Gitlab) fileData = new JToken[0];
        }

        public bool ContainsData()
        {
            return treeData != null && treeData.Length > 0
                || fileData != null && fileData.Length > 0;
        }
    }

    public class SerializedRepoData
    {
        public SerializedObjectData[] objData;
        public JArray tagData; //All available releases with their commits

        public SerializedRepoData(JArray t, SerializedObjectData[] o)
        {
            tagData = t;
            objData = o;
        }

        public RepoData GetDeserializedData()
        {
            RepoData repo = new RepoData();
            int c = objData.Length - 1;
            //Console.WriteLine(JsonConvert.SerializeObject(tagData.Children()[0]));
            //Console.Read();
            repo.tagData = tagData.Children().ToArray();
            repo.objData = new ObjectData[c];
            repo.objData.ForEach((x, i) =>
            {
                x = new ObjectData() { Version = objData[i].Version };
                //int ctr = objData[i].treeData.Children().Count();
                //x.treeData = new JToken[ctr];
                x.treeData = objData[i].treeData.Children().ToArray();
                if (RepoAPI.repoType == RepoType.Gitlab)
                    x.fileData = objData[i].fileData.Children().ToArray();
            });
            return repo;
        }
    }

    public class SerializedObjectData
    {
        public string Version;

        public JContainer treeData, //All files of a repo of a current commit
                       fileData; //Individual file data (SOLO DISPONIBLE EN GITLAB)

        public SerializedObjectData(string v, JContainer t, JContainer f)
        {
            Version = v;
            treeData = t;
            fileData = f;
        }
    }

    //This needs to go to the API
    public class FileData
    {
        public static string RawUrl,
                             Author = "ikillnukes1", RepoName = "HugeCraft-Client";

        public string Id,
                      CommitId,
                      FileRelPath;

        [JsonIgnore]
        public string FileName
        {
            get
            {
                return Path.GetFileName(FileRelPath);
            }
        }

        public int Size;

        //Github
        public FileData(string url, int siz)
        {
            RawUrl = url;
            Size = siz;
        }

        //Gitlab
        public FileData(string id, string cid, string path, int siz)
        {
            Id = id;
            CommitId = cid;
            FileRelPath = path;
            Size = siz;
            RawUrl = ((GitlabRepo) Program.repoApi).GetRawUrl(new NameValueCollection()
            {
                { "author", Author },
                { "name", RepoName },
                { "cid", CommitId },
                { "path", FileRelPath }
            });
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

    public static class UriExtensions
    {
        public static bool ValidUrl(this string Url)
        {
            Uri uriResult;
            return Uri.TryCreate(Url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }

    public static class WebExtensions
    {
        public static string DownloadString(string add)
        { //DownloadString for https
            try
            {
                using (var client = new WebClient())
                {
                    try
                    {
                        client.Headers.Add("user-agent", "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.2.15) Gecko/20110303 Firefox/3.6.15");
                        return client.DownloadString(add);
                    }
                    catch
                    {
                        Console.WriteLine("File not found!");
                        return "";
                    }
                }
            }
            catch
            {
                Console.WriteLine("No internet connection!");
                return "";
            }
        }
    }

    public class AppArgs
    {
        public bool Update { get; set; }

        public string RepoUrl { get; set; }

        public bool ForceUdateOldReleases { get; set; }

        //private int i = 0;

        /*private string _rep = "";

        public string RepoUrl
        {
            get
            {
                return _rep;
            }
            set
            {
                StackTrace t = new StackTrace();
                Console.WriteLine(i + ": " + t.ToString());
                ++i;
                _rep = value;
            }
        }*/

        //public string Ref { get; set; } //If you want to catch only one value
        public string ModpackName { get; set; }

        public string SavePath { get; set; }

        public string RepoOwner
        {
            get
            {
                /*StackTrace t = new StackTrace();
                Console.WriteLine(i + ": " + t.ToString());
                ++i;*/

                Uri uri = new Uri(RepoUrl); //RepoUrl
                string str = uri.Host + uri.PathAndQuery + uri.Fragment;
                return str.Split('/')[1];
            }
        }

        public string RepoName
        {
            get
            {
                Uri uri = new Uri(RepoUrl);
                string str = uri.Host + uri.PathAndQuery + uri.Fragment;
                return str.Split('/')[2];
            }
        }

        public AppArgs()
        {
            Update = false;
            RepoUrl = Program._DefRepoUrl;
            ModpackName = Program._ModpackName;
            SavePath = Program.AppPath;
        }
    }

    public abstract class RepoAPI
    { //Las propiedades deberian ser protected? Yo creo que si...
        internal static Stopwatch watch;
        internal static long ellapsed = 0;
        internal static AppData data = new AppData();

        public static RepoType repoType;

        //private RepoType repoType;

        public RepoAPI(RepoType type)
        {
            repoType = type;
        }

        public abstract string treeUrl { get; set; }
        public abstract string tagUrl { get; set; }

        protected abstract string GetTagUrl();

        protected abstract string GetTreeUrl(string reff);

        public JToken[] GetTagData()
        {
            //Console.WriteLine(GetTagUrl());
            //Console.WriteLine(Program.appArgs.RepoUrl);
            return Program.ExportList(GetTagUrl(), LocationData.TagData);
        }

        public JToken[] GetTreeData(string reff, string ver = "")
        {
            //Console.WriteLine(GetTreeUrl(reff));
            /*Console.WriteLine("bbb");
            var r = Program.ExportList(GetTreeUrl(reff), LocationData.TreeData, !string.IsNullOrEmpty(ver) ? ver : reff);
            Console.WriteLine("NUL: " + (r == null));*/
            //Console.WriteLine(r.Count);
            return Program.ExportList(GetTreeUrl(reff), LocationData.TreeData, !string.IsNullOrEmpty(ver) ? ver : reff); //r
        }

        public void Run()
        {
            data.Name = Program.appArgs.ModpackName;

            //Is this needed as callback?
            GetTags(() =>
            {
                //Is this really needed?
                /*if (repoType == RepoType.Github)
                    ((GithubRepo) repoApi).GetTree();
                else
                    ((GitlabRepo) repoApi).GetTree();*/

                Program.repoApi.GetTree();

                string _fol1 = Path.GetDirectoryName(Program.AppResultPath);
                if (!Directory.Exists(_fol1))
                    Directory.CreateDirectory(_fol1);

                File.WriteAllText(Program.AppResultPath, JsonConvert.SerializeObject(data, Formatting.Indented));
                Program.finished = true;

                Console.WriteLine("Finished everything in {0} ms!", ellapsed);
            });
        }

        protected void GetTags(Action fin)
        {
            watch = Stopwatch.StartNew();

            if (!Program.realtimeVersion && File.Exists(Program.RepoResultPath))
                Program.repoData = JsonConvert.DeserializeObject<SerializedRepoData>(File.ReadAllText(Program.RepoResultPath)).GetDeserializedData();

            //Lo que si me choca esq si arriba nos traemos todo de un mismo archivo aqui no va a hacer falta devolver na...
            Program.repoData.tagData = Program.repoApi.GetTagData(); //Reminder: Esto ya se encarga el solito de obtener una version de internet o del disco segun proceda

            watch.Stop();
            ellapsed += watch.ElapsedMilliseconds;

            Console.WriteLine("[GetTagData] First phase finished in {0} ms!", watch.ElapsedMilliseconds);

            fin();
        }

        protected abstract void GetTree();

        protected abstract List<FileData> MakeFileDataConversion(string ver);

        protected void CreateModpack(string ver)
        {
            watch = Stopwatch.StartNew();

            List<FileData> fileData = new List<FileData>();

            //Is this really needed?
            /*if (repoType == RepoType.Github)
                fileData = ((GithubRepo) repoApi).MakeFileDataConversion(ver);
            else
                fileData = ((GitlabRepo) repoApi).MakeFileDataConversion(ver);*/
            fileData = Program.repoApi.MakeFileDataConversion(ver);

            ModpackData modpack = new ModpackData();
            modpack.Files = fileData.ToArray();
            modpack.TotalSize = fileData.Sum(x => x.Size);
            modpack.Version = ver;

            data.AddVersion(modpack);

            watch.Stop();
            ellapsed += watch.ElapsedMilliseconds;

            Console.WriteLine("[CreatingModpack] Fourth phase finished in {0} ms! (Ver: {1})", watch.ElapsedMilliseconds, ver);
        }
    }

    public class GithubRepo : RepoAPI
    {
        public override string treeUrl { get; set; }
        public override string tagUrl { get; set; }

        internal static string _realUrl = "";

        internal static string RealUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(_realUrl))
                    return _realUrl;
                string localhost = "localhost/lerp2php/hugelauncherphp",
                       lerp2dev = "lerp2dev.com/misc/Lerp2PHP/HugeLauncherPHP",
                       concatUrl = "http://{0}/Check.php",
                       localhostUrl = string.Format(concatUrl, localhost);
                try
                {
                    var request = WebRequest.Create(localhostUrl);
                    using (var response = request.GetResponse())
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            // Process the stream
                            using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                            {
                                if (reader.ReadToEnd() == "OK")
                                    _realUrl = localhost;
                                else
                                    _realUrl = lerp2dev;
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    _realUrl = localhost;
                }
                return _realUrl;
            }
        }

        public GithubRepo()
            : base(RepoType.Github)
        {
            treeUrl = string.Format("http://{0}/{1}", RealUrl, "Core.php?action=get-tree&owner={0}&repo={1}&sha={2}&recursive=1");
            tagUrl = string.Format("http://{0}/{1}", RealUrl, "Core.php?action=get-tags&owner={0}&repo={1}");
        }

        public JToken[] GetArray(JObject obj)
        {
            //Console.WriteLine(obj.Properties().Count());
            //obj.Properties().ForEach(x => Console.WriteLine(x.Name));
            //Console.WriteLine(JsonConvert.SerializeObject(obj["data"]));
            //Console.WriteLine(obj["data"].Count());
            try
            {
                //Console.WriteLine(JsonConvert.SerializeObject(new JToken[](obj["data"])[0]));
                return obj["data"].Children().ToArray(); //No se puede castear
            }
            catch (Exception ex)
            {
                Console.WriteLine("EX: {0}; String: {1}", ex.ToString(), obj.ToString().Substring(0, 1000)); //.ToString().Substring(0, 1000)
                return null;
            }
        }

        protected override string GetTagUrl()
        {
            return string.Format(tagUrl, Program.appArgs.RepoOwner, Program.appArgs.RepoName);
        }

        protected override string GetTreeUrl(string reff)
        {
            return string.Format(treeUrl, Program.appArgs.RepoOwner, Program.appArgs.RepoName, reff);
        }

        protected override void GetTree()
        {
            //var test = Program.repoData.tagData;
            //Console.WriteLine(JsonConvert.SerializeObject(test));
            //test.ForEach(x => Console.WriteLine(x.Path));
            //test[0].ToObject<JObject>().Properties().ForEach(x => Console.WriteLine(x.Name));
            //Program.repoData.tagData.ForEach(x => Console.WriteLine(x.Path));
            //Console.WriteLine(JsonConvert.SerializeObject(Program.repoData.tagData[0]));
            int i = 0,
                count = Program.repoData.tagData.Length - 1;
            foreach (JToken tag in Program.repoData.tagData)
            {
                watch = Stopwatch.StartNew();
                Console.WriteLine("[{0}] Parsing version {1} of {2}...", i < Program.loadedTrees ? "Loading" : "Creating", i, count);
                //Console.WriteLine(JsonConvert.SerializeObject(tag, Formatting.Indented));
                //Console.WriteLine(JsonConvert.SerializeObject(tag, Formatting.Indented));
                string reff = tag["object"]["sha"].ToObject<string>();

                if (!Program.repoData.IsLatest(reff) && Program.repoData.ContainsVersion(reff))
                    continue; //Si no estamos actualizando o si lo estamos haciendo, pero no es la ultima version y dicha version está ya completa entonces skipeamos

                //Console.WriteLine("aaaa");
                //Console.WriteLine(JsonConvert.SerializeObject(Program.repoApi.GetTreeData(reff)));

                //Console.WriteLine("NUL2: " + (Program.repoApi == null));
                Program.repoData.GetVersion(reff).treeData = Program.repoApi.GetTreeData(reff);

                watch.Stop();
                ellapsed += watch.ElapsedMilliseconds;

                Console.WriteLine("[LoadingTags] Loaded tag in {0} ms!", watch.ElapsedMilliseconds);

                CreateModpack(reff);
                ++i;
                Console.WriteLine();
                //Program.SaveRepoData();
            }
            //throw new NotImplementedException();
        }

        protected override List<FileData> MakeFileDataConversion(string ver)
        {
            List<FileData> fileData = new List<FileData>();
            //Console.WriteLine("aaskfkgkaa");
            //Console.WriteLine(Program.repoData.GetVersion(ver).fileData == null);
            //Console.WriteLine(JsonConvert.SerializeObject(Program.repoData.GetVersion(ver).treeData[2], Formatting.Indented));
            //Console.WriteLine(Program.repoData.GetVersion(ver).treeData[2].Children()[0].Count());
            foreach (JToken file in Program.repoData.GetVersion(ver).treeData[2].Children()[0])
            {
                //Console.WriteLine(JsonConvert.SerializeObject(file));
                //Console.Read();
                //Console.WriteLine(file.Count());
                if (!fileData.Any(x => x.FileName == file["path"].ToObject<string>()))
                    fileData.Add(new FileData(file["url"].ToObject<string>(), file["size"].ToObject<int>()));
            }
            return fileData;
        }
    }

    public class GitlabRepo : RepoAPI
    {
        public override string treeUrl { get; set; }
        public override string tagUrl { get; set; }
        public string fileUrl { get; set; }

        public int RepoID = 3820415;

        public GitlabRepo()
            : base(RepoType.Gitlab)
        {
            try
            {
                treeUrl = "https://gitlab.com/api/v3/projects/{0}/repository/tree?ref={1}&recursive=1";
                tagUrl = "https://gitlab.com/api/v3/projects/{0}/repository/tags";
                fileUrl = "https://gitlab.com/api/v3/projects/{0}/repository/files?ref={1}&file_path={2}";
                string html = WebExtensions.DownloadString(Program.appArgs.RepoUrl);
                if (string.IsNullOrEmpty(html))
                    throw new Exception("There was a problem downloading html from the Gitlab repo!");
                //HtmlDocument doc = new HtmlDocument();
                //doc.LoadHtml(html);
                //doc.DocumentNode.Descendants().Where(x => x.Attributes["name"] != null).ForEach(x => Console.WriteLine(x.Attributes["name"].Value));
                /*var element = doc.DocumentNode
                    .Descendants()
                    .Where(x => x.Attributes["name"] != null)
                    .SingleOrDefault(node => node.Attributes["name"].Value == "project_id");*/
                //var element = doc.DocumentNode.SelectSingleNode("//input[@name='project_id']");
                CQ doc = CQ.Create(html),
                   element = doc["input[name=project_id]"];
                if (element == null)
                    throw new Exception(
                        string.Format(
                            "[{0}] You hadn't passed a valid Gitlab project url! Please, specify only the root: http://gitlab.com/Author/Repository_name/, if the problem persists, please tell it to the owner!",
                            Program.appArgs.RepoUrl));
                RepoID = int.Parse(element[0].Attributes["value"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public JObject GetFileInfo(string path, string reff)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        string content = client.DownloadString(string.Format(fileUrl, RepoID, reff, path));
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
            catch (Exception ex)
            {
                //Internet connection lost
                Program.NoInternet(ex.ToString());
                return null;
            }
        }

        protected override string GetTagUrl()
        {
            return string.Format(tagUrl, RepoID);
        }

        protected override string GetTreeUrl(string reff)
        {
            return string.Format(treeUrl, RepoID, reff);
        }

        protected override void GetTree()
        {
            foreach (JToken tag in Program.repoData.tagData)
            {
                string ver = tag["name"].ToObject<string>(),
                       reff = tag["commit"]["id"].ToObject<string>();

                if (!Program.repoData.IsLatest(ver) && Program.repoData.ContainsVersion(ver))
                    continue; //Si no estamos actualizando o si lo estamos haciendo, pero no es la ultima version y dicha version está ya completa entonces skipeamos

                Program.repoData.GetVersion(ver).treeData = Program.repoApi.GetTreeData(reff, ver);

                GetFile(reff, ver);

                CreateModpack(ver);
            }
        }

        protected void GetFile(string reff, string ver)
        {
            int i = 0,
                count = Program.repoData.GetVersion(ver).treeData.Length;

            long curEllapsed = 0;

            foreach (JToken file in Program.repoData.GetVersion(ver).treeData)
            {
                try
                {
                    string type = file["type"].ToObject<string>();
                    if (type == "blob" && (Program.repoData.GetVersion(ver).fileData == null || !Program.repoData.GetVersion(ver).fileData.Any(x => x["file_path"].ToObject<string>() == file["path"].ToObject<string>())))
                    {
                        watch = Stopwatch.StartNew();
                        Console.WriteLine("Adding {0}... Processed {1} of {2} files ({3:F2}%)", file["path"].ToObject<string>(), i, count, i * 100d / (count - 1));

                        JObject fil = ((GitlabRepo) Program.repoApi).GetFileInfo(file["path"].ToObject<string>(), reff);

                        JArray arr = new JArray(Program.repoData.GetVersion(ver).fileData);
                        arr.Add(fil);

                        if (fil != null)
                            Program.repoData.GetVersion(ver).fileData = arr.Children().ToArray(); //ArrayExtensions.Append(ref Program.repoData.GetVersion(ver).fileData, fil);
                        else
                            Console.WriteLine("Exception ocurred in {0}th file!", i);

                        watch.Stop();

                        Console.WriteLine("Ended in {0} ms!\n", watch.ElapsedMilliseconds);

                        ellapsed += watch.ElapsedMilliseconds;
                        curEllapsed += watch.ElapsedMilliseconds;
                    }
                    else
                        Console.WriteLine("[{0}: {1}] Skipping file {2}!\n", type, file["path"].ToObject<string>(), i);
                    //Program.SaveRepoData();
                    ++i;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Value null!, message: {0}", ex.ToString());
                    watch.Stop();
                }
            }

            Console.WriteLine("[GetFileByFile] Third phase finished in {0} ms!", curEllapsed);
        }

        protected override List<FileData> MakeFileDataConversion(string ver)
        {
            List<FileData> fileData = new List<FileData>();
            foreach (JToken file in Program.repoData.GetVersion(ver).fileData)
                if (!fileData.Any(x => x.FileName == file["file_path"].ToObject<string>()))
                    fileData.Add(new FileData(file["blob_id"].ToObject<string>(), file["commit_id"].ToObject<string>(), file["file_path"].ToObject<string>(), file["size"].ToObject<int>()));
            return fileData;
        }

        public string GetRawUrl(NameValueCollection col)
        {
            return string.Format("https://gitlab.com/{0}/{1}/raw/{2}/{3}", col["author"], col["name"], col["cid"], col["path"]);
        }
    }

    /*public class JData : JObject
    {
        public JObject[] data;

        public JData(JObject[] d)
        {
            data = d;
        }

        public static implicit operator JObject[] (JData dat)
        {
            return dat.data;
        }

        public static explicit operator JData(JObject[] dat)
        {
            return new JData(dat);
        }
    }*/
}