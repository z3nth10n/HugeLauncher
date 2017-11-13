using HugeLauncher.Properties;
using Lerp2Web;
using LiteLerped_WF_API.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using LWeb = Lerp2Web.Lerp2Web;
using WebAPI = Lerp2Web.API;
using Timer = System.Windows.Forms.Timer;
using System.Text.RegularExpressions;

namespace HugeLauncher
{
    public enum PackType
    {
        Client,
        Server
    }

    public partial class frmMain : LerpedForm
    {
        public static frmMain mainInstance;

        public const string clientDataUrl = "https://gitlab.com/ikillnukes1/HugeCraft-Client/raw/master/HugeCraft_Data.json",
                            serverDataUrl = "https://gitlab.com/ikillnukes1/HugeCraft-Server/raw/master/HugeCraft_Data.json",
                            TLauncherData = "https://1drv.ms/u/s!Ajlvp9AoY-eJhqJOpFkCpX42mYJgJA",
                            TLauncherConfig = "https://1drv.ms/u/s!Ajlvp9AoY-eJhqJNBWjyioEA9_Hd8Q",
                            ModpackName = "HugeCraft";

        private static JObject _clientVers;

        internal static bool runClient, //Make this also a property (to set the text)
                             runServer;

        private static bool _downData;

        internal static bool downloadingData
        {
            set
            {
                _downData = value;
                if (!runClient || !runServer)
                {
                    if (_downData)
                    {
                        //Set btnRunis click event as cancel
                    }
                    else
                    {
                        //Set btnRunis click event as install
                    }
                }
                else
                {
                    //Set btnRunis click event as open
                }
            }
        }

        private static List<ModpackData> modpackData = new List<ModpackData>();

        internal static JObject clientVersions
        {
            get
            {
                if (_clientVers == null)
                    _clientVers = modpackData.Count == 0 ?
                        LWeb.JGet(null, clientDataUrl, true) :
                        modpackData.SingleOrDefault(x => x.Name == ModpackName).ClientData;
                return _clientVers;
            }
        }

        private static JObject _serverVers;

        internal static JObject serverVersions
        {
            get
            {
                if (_serverVers == null)
                    _serverVers = modpackData.Count == 0 ?
                        LWeb.JGet(null, clientDataUrl, true) :
                        modpackData.SingleOrDefault(x => x.Name == ModpackName).ServerData;
                return _serverVers;
            }
        }

        public static IEnumerable<string> cVersions
        {
            get
            {
                return clientVersions["Versions"].Children().Select(x => x["Version"].ToObject<string>());
            }
        }

        public static string AppPath
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }

        public static string ModpackFolderPath
        {
            get
            {
                return Path.Combine(AppPath, "Modpacks");
            }
        }

        public static string DataFolderPath
        {
            get
            {
                return Path.Combine(AppPath, "Data");
            }
        }

        public static string DataFilePath
        {
            get
            {
                return Path.Combine(DataFolderPath, "ModpacksData.json");
            }
        }

        public static string LauncherFolderPath
        {
            get
            {
                return Path.Combine(AppPath, "Launcher");
            }
        }

        public static string StartUpPath
        {
            get
            {
                return Settings.Default.StartupPath;
            }
        }

        public static string MCLPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".tlauncher", "mcl.properties");
            }
        }

        private void btnRuninsClient_Click(object sender, EventArgs e)
        {
            if (runClient)
            {
            }
            else
                DownloadData(PackType.Client);
        }

        private void btnDelClient_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure?", "Application message", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                new DirectoryInfo(GetFolderPathVer(comboBox1.SelectedIndex, PackType.Client)).Empty();
                runClient = false;
                RutineVersionInstalled(PackType.Client);
            }
        }

        private void btnRunisServer_Click(object sender, EventArgs e)
        {
            if (runServer)
            {
            }
            else
                DownloadData(PackType.Server);
        }

        private void btnDelServer_Click(object sender, EventArgs e)
        {
        }

        public frmMain()
        {
            mainInstance = this;
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            Console.WriteLine("frmMain_Load");

            //Check the first execution
            if (WebAPI.InitializatedConfigSession)
                frmTutorial.Init(mainInstance);
            else
                frmDescription.Init(mainInstance);

            //Create a folder for the modpacks & data & launcher
            if (!Directory.Exists(ModpackFolderPath))
                Directory.CreateDirectory(ModpackFolderPath);

            if (!Directory.Exists(LauncherFolderPath))
                Directory.CreateDirectory(LauncherFolderPath);

            if (!Directory.Exists(DataFolderPath))
            {
                Directory.CreateDirectory(DataFolderPath);
                SaveModpackData();
            }
            else if (File.Exists(DataFilePath)) //Try to load the data from the client and server
                LoadModpackData();

            //Make a static method for this

            //Get versions
            cVersions.ForEach(x => comboBox1.Items.Add(x));
            //comboBox1.Text = comboBox1.Items[0].ToString();
            comboBox1.SelectedIndex = 0; //El 0 se va a convertir en el last played version

            //Once we selected the version of the client, then check if version is installed
            RutineVersionInstalled(PackType.Client);
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
        }

        private void establecerRutaDeInstalaciónToolStripMenuItem_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = AppPath;
            DialogResult res = folderBrowserDialog1.ShowDialog();
            if (res == DialogResult.OK)
            {
                Settings.Default.StartupPath = folderBrowserDialog1.SelectedPath;
                Settings.Default.Save();
            }
        }

        public static void LoadModpackData()
        {
            modpackData = JsonConvert.DeserializeObject<ModpackData[]>(File.ReadAllText(DataFilePath)).ToList();
        }

        public static void SaveModpackData()
        {
            modpackData.Add(new ModpackData(ModpackName, clientVersions, serverVersions));
            File.WriteAllText(DataFilePath, JsonConvert.SerializeObject(modpackData.ToArray()));
        }

        private void DownloadData(PackType type)
        {
            downloadingData = true;

            frmDescription.instance.Hide();

            int index = comboBox1.SelectedIndex;
            string path = GetFolderPathVer(index, type);
            JToken verObj = GetVersion(index, type);
            JEnumerable<JToken> dl = verObj["Files"].Children();

            if (type == PackType.Client)
                btnRuninsClient.Text = "Cancelar instalacion";
            else
                btnRunisServer.Text = "Cancelar instalacion";

            //Set maximums
            pbFileProgress.Maximum = 100; //verObj["TotalSize"].ToObject<int>();
            pbTotalProgress.Maximum = 100;

            PhaseManager.PhaseCompleted += (i) =>
            {
                switch (i)
                { //Do something special when...
                    case 0: //... Modpack downloaded
                        break;

                    case 1: //... TLauncher Files downloaded
                        break;

                    case 2: //... TLauncher Config downloaded
                        ModifyMCLProperties(LauncherFolderPath);
                        break;
                }
            };

            PhaseManager phase = new PhaseManager(type,
                new DownloadPack(verObj["TotalSize"].ToObject<long>(),
                    dl.Select(token =>
                            {
                                return new DownloadPath(new Uri(token["FileUrl"].ToObject<string>()), Path.Combine(path, token["FileRelPath"].ToObject<string>()));
                            })
                    ),
                new DownloadPack(TLauncherData, LauncherFolderPath));

            //Check if appdata folder exists and if not add download and then do the following or if it exists modify the value from the config to another one

            if (!File.Exists(MCLPath))
                PhaseManager.instance.AddDownload(new DownloadPack(TLauncherConfig, Path.GetDirectoryName(MCLPath)));
            else
                ModifyMCLProperties(LauncherFolderPath);

            phase.Download(1);
        }

        private void ModifyMCLProperties(string path)
        {
            string filecontent = File.ReadAllText(MCLPath),
                   content = Regex.Replace(filecontent, "minecraft.gamedir=.+?\n", string.Format("minecraft.gamedir={0}" + Environment.NewLine, path.Replace(":", @"\:").Replace(@"\", @"\\")));
            File.WriteAllText(MCLPath, filecontent);
        }

        private void mostrarTutorialInicialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmTutorial.Init(this);
            //frmDescription.instance.Hide();
        }

        public static bool IsVersionSetup(int index, PackType type)
        {
            string folder = GetFolderPathVer(index, type);
            return Directory.Exists(folder) && !folder.IsDirectoryEmpty();
        }

        public static string GetFolderPathVer(int index, PackType type)
        {
            return Path.Combine(ModpackFolderPath, ModpackName, type.ToString(), GetVersionStr(index, type));
        }

        public static string GetVersionStr(int index, PackType type)
        {
            JObject obj = type == PackType.Client ? clientVersions : serverVersions;
            return obj["Versions"].Children().ElementAt(index)["Version"].ToObject<string>();
        }

        private static JToken GetVersion(int index, PackType type)
        {
            JObject obj = type == PackType.Client ? clientVersions : serverVersions;
            return obj["Versions"].Children().ElementAt(index);
        }

        private void RutineVersionInstalled(PackType type)
        {
            Button btnRunins = type == PackType.Client ? btnRuninsClient : btnRunisServer,
                   btnDel = type == PackType.Client ? btnDelClient : btnDelServer;

            if (IsVersionSetup(comboBox1.SelectedIndex, type))
            {
                btnRunins.Text = "Abrir Launcher";
                runClient = true;
            }
            else
            {
                btnRunins.Text = "Instalar versión";
                btnDel.Visible = false;
            }
        }
    }

    public sealed class ModpackData
    {
        public string Name;

        public JObject ClientData,
                       ServerData;

        public ModpackData(string n, JObject cli, JObject ser)
        {
            Name = n;
            ClientData = cli;
            ServerData = ser;
        }
    }

    public sealed class DownloadPath
    {
        public Uri Url;
        public string Path;

        public DownloadPath(Uri u, string p)
        {
            Url = u;
            Path = p;
        }
    }

    public static class DownloadManager
    {
        public delegate void CustomEventHandler(bool completed);

        public static event CustomEventHandler DownloadCompleted;

        private static int FileLength;
        private static Queue<DownloadPath> _downloads = new Queue<DownloadPath>();
        private static Form form;
        private static DownloadPath curDownload;
        private static bool cancellingDownload;
        private static WebClient client;
        private static PackType packType;
        private static long TotalBytes;
        private static double LastByteValue, CurByteValue, AvByteValue;
        private static ulong loop;

        //private static Stopwatch watch;
        private static Timer timer;

        private static int CurFilesCount
        {
            get
            {
                return FileLength - _downloads.Count;
            }
        }

        private static Label _lblFProg;

        private static Label lblFileProgress
        {
            get
            {
                if (_lblFProg == null) _lblFProg = (Label) form.Controls.Find("lblFileProgress", true)[0];
                return _lblFProg;
            }
        }

        private static Label _lblTProg;

        private static Label lblTotalProgress
        {
            get
            {
                if (_lblTProg == null) _lblTProg = (Label) form.Controls.Find("lblTotalProgress", true)[0];
                return _lblTProg;
            }
        }

        private static ProgressBar _pbFProg;

        private static ProgressBar pbFileProgress
        {
            get
            {
                if (_pbFProg == null) _pbFProg = (ProgressBar) form.Controls.Find("pbFileProgress", true)[0];
                return _pbFProg;
            }
        }

        private static ProgressBar _pbTProg;

        private static ProgressBar pbTotalProgress
        {
            get
            {
                if (_pbTProg == null) _pbTProg = (ProgressBar) form.Controls.Find("pbTotalProgress", true)[0];
                return _pbTProg;
            }
        }

        private static Label _lblMetrics;

        private static Label lblMetrics
        {
            get
            {
                if (_lblMetrics == null) _lblMetrics = (Label) form.Controls.Find("lblMetrics", true)[0];
                return _lblMetrics;
            }
        }

        public static void StartDownload(this Form frm, IEnumerable<DownloadPath> downloads, PackType type, long totalBytes)
        {
            cancellingDownload = false;
            form = frm;
            FileLength = downloads.Count();
            packType = type;
            TotalBytes = totalBytes;

            foreach (DownloadPath download in downloads)
                _downloads.Enqueue(download);

            NextDownload(_downloads.Dequeue());
        }

        public static void CancelDownload()
        {
            frmDescription.instance.Show(); //No funca esto o q?
            cancellingDownload = true;
        }

        private static void NextDownload(DownloadPath dl)
        {
            Thread thread = new Thread(() =>
            {
                curDownload = dl;
                string fol = Path.GetDirectoryName(dl.Path);
                if (!Directory.Exists(fol))
                    Directory.CreateDirectory(fol);

                using (client = new WebClient())
                {
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                    client.DownloadFileAsync(dl.Url, dl.Path);
                }
            });
            if (!cancellingDownload)
                thread.Start();
        }

        private static void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            form.BeginInvoke((MethodInvoker) delegate
            {
                if (timer == null)
                {
                    timer = new Timer();
                    timer.Tick += (sender1, e1) =>
                    {
                        double downloadRate = CurByteValue - LastByteValue;
                        LastByteValue = CurByteValue;
                        AvByteValue += Math.Abs(downloadRate);
                        ++loop;

                        double currentRate = AvByteValue / loop;

                        lblMetrics.Text = string.Format("ETA: {0}; Download rate: {1}/s", currentRate > 0 ? TimeSpan.FromSeconds((int) Math.Truncate(TotalBytes / currentRate)).ToString(@"hh\h\ mm\m\ ss\s") : "Inf", currentRate.BytesToString());
                    };
                    timer.Interval = 1000;
                    timer.Start();
                }

                try
                {
                    if (cancellingDownload)
                    {
                        client.DownloadProgressChanged -= new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                        client.DownloadFileCompleted -= new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                        client.Dispose();
                        _downloads = null;
                        return;
                    }
                    double phasePercentage = (double) CurFilesCount / FileLength * 100,
                           totalPercentage = PhaseManager.GetSummedValue(),
                           percentage = (double) e.BytesReceived / e.TotalBytesToReceive * 100;

                    string fileName = Path.GetFileName(curDownload.Path);

                    lblFileProgress.Text = string.Format("Downloaded {0} of {1} ({2:F2}%) ", e.BytesReceived.BytesToString(), e.TotalBytesToReceive.BytesToString(), percentage);
                    pbFileProgress.Value = (int) Math.Truncate(percentage);
                    lblTotalProgress.Text = string.Format("Fase {0}: {1} de {2} archivos descargados ({3:F2}%)\nArchivo: {4} (Total: {5:F2}%)", PhaseManager.PackIndex, CurFilesCount, FileLength, phasePercentage, fileName.GetFileStr(), totalPercentage);
                    pbTotalProgress.Value = (int) Math.Truncate(totalPercentage);

                    CurByteValue = e.BytesReceived;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception occurred! Message:\n\n{0}", ex.ToString());
                }
            });
        }

        private static void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            form.BeginInvoke((MethodInvoker) delegate
            {
                DownloadPath dl = _downloads.Count > 0 ? _downloads.Dequeue() : null;
                if (dl != null && !cancellingDownload) NextDownload(dl);
                else
                {
                    //Check if files are complete??
                    if (packType == PackType.Client)
                        frmMain.runClient = true;
                    else
                        frmMain.runServer = true;
                    frmMain.downloadingData = false;
                }

                DownloadCompleted(dl == null);
            });
        }
    }

    public sealed class PhaseManager
    {
        public delegate void CustomEventHandler(int index);

        public static event CustomEventHandler PhaseCompleted;

        private static DownloadPack curPack;

        internal static int PackIndex;
        internal static PhaseManager instance;
        internal static Action<DownloadPack> NextDownload;

        public Queue<DownloadPack> Packs = new Queue<DownloadPack>();
        private readonly PackType _Type;

        private PhaseManager()
        {
        }

        public PhaseManager(PackType t, params DownloadPack[] dls)
        {
            instance = this;
            _Type = t;

            foreach (DownloadPack dl in dls)
                Packs.Enqueue(dl);

            NextDownload = (dl) =>
            {
                DownloadManager.StartDownload(frmMain.mainInstance, dl.col, _Type, dl.TotalBytes);
            };

            DownloadManager.DownloadCompleted += (comp) =>
            {
                if (comp)
                {
                    NextDownload?.Invoke(Dequeue());
                    Console.WriteLine("Download queue finished, starting a new phase!");
                }
                else
                    Console.WriteLine("Finished a download, waiting to until the end.");
            };
        }

        public void AddDownload(DownloadPack dl)
        {
            Packs.Enqueue(dl);
        }

        public void Download(int startIndex = 0)
        {
            Console.WriteLine(Packs.Count);
            if (startIndex > 0) Packs.SetFirstTo(startIndex);
            NextDownload?.Invoke(Dequeue());
        }

        public DownloadPack Dequeue()
        {
            PhaseCompleted(PackIndex);
            ++PackIndex;
            curPack = Packs.Dequeue();
            return curPack;
        }

        public static double GetSummedValue()
        {
            double ret = 0;
            for (int i = PackIndex; i > 0; --i)
                ret += GetPhaseValue(i);
            return ret;
        }

        internal static double GetPhaseValue(int ind = -1)
        {
            DownloadPack dl = ind == -1 ? instance.Packs.ElementAt(ind) : curPack;
            return (double) dl.TotalBytes / DownloadPack.TotalPackSize;
        }
    }

    public sealed class DownloadPack
    {
        private readonly static List<DownloadPack> dlPack = new List<DownloadPack>();

        public static long TotalPackSize
        {
            get
            {
                return dlPack.Select(x => x.TotalBytes).Sum();
            }
        }

        public long TotalBytes;
        public IEnumerable<DownloadPath> col;

        private DownloadPack()
        {
        }

        public DownloadPack(string Url, string Path)
        {
            //Download a unique file
            TotalBytes = (long) WebExtensions.GetFileLength(Url);
            col = new DownloadPath[] { new DownloadPath(new Uri(Url), Path) };
        }

        public DownloadPack(long tb, IEnumerable<DownloadPath> paths)
        {
            TotalBytes = tb;
            col = paths;

            dlPack.Add(this);
        }
    }
}