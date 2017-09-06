using Lerp2Web;
using LiteLerped_WF_API.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using LWeb = Lerp2Web.Lerp2Web;
using WebAPI = Lerp2Web.API;

namespace HugeLauncher
{
    public enum PackType
    {
        Client,
        Server
    }

    public partial class frmMain : LerpedForm
    {
        public static frmMain instance;

        public const string clientDataUrl = "https://gitlab.com/ikillnukes1/HugeCraft-Client/raw/master/HugeCraft_Data.json",
                            serverDataUrl = "https://gitlab.com/ikillnukes1/HugeCraft-Server/raw/master/HugeCraft_Data.json",
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
            instance = this;
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            Console.WriteLine("frmMain_Load");

            //Check the first execution
            if (WebAPI.InitializatedConfigSession)
            {
                //Do special thing here for this session??
                //Yep, we have to say to the user in several msgbox what to do here...
                //For example that they can change the path of execution by default
                StartingTutorial();
            }

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
        }

        private static void StartingTutorial()
        { //Inject a frame form
            //string title = "Starting tutorial";
            //MessageBox.Show(title, "Welcome to HugeLauncher! With this launcher you");
            Point p = instance.Location;
            frmTutorial tutorial = new frmTutorial()
            {
                TopMost = true,
                Location = new Point(p.X + 9, p.Y + 55),
                Size = new Size(instance.Width - 20, instance.Height - 65)
            };
            tutorial.Show();
            instance.LocationChanged += (sender, e) =>
            {
                Point p1 = instance.Location;
                tutorial.Location = new Point(p1.X + 9, p1.Y + 55);
            };
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

            this.StartDownload(dl.Select(token =>
            {
                return new DownloadPath(new Uri(token["FileUrl"].ToObject<string>()), Path.Combine(path, token["FileRelPath"].ToObject<string>()));
            }));

            //https://stackoverflow.com/questions/9459225/asynchronous-file-download-with-progress-bar
        }

        private void mostrarTutorialInicialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartingTutorial();
        }

        public static bool IsVersionSetup(int index, PackType type)
        {
            string folder = GetFolderPathVer(index, type);
            //Console.WriteLine("Foool: {0}", folder);
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

    public class ModpackData
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

    public class DownloadPath
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
        private static int FileLength;
        private static Queue<DownloadPath> _downloads = new Queue<DownloadPath>();
        private static Form form;
        private static DownloadPath curDownload;
        private static bool cancellingDownload;
        private static WebClient client;
        private static PackType packType;

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

        public static void StartDownload(this Form frm, IEnumerable<DownloadPath> downloads, PackType type)
        {
            form = frm;
            FileLength = downloads.Count();
            packType = type;

            foreach (DownloadPath download in downloads)
                _downloads.Enqueue(download);

            NextDownload(_downloads.Dequeue());
        }

        public static void CancelDownload()
        {
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
            thread.Start();
        }

        private static void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            form.BeginInvoke((MethodInvoker) delegate
            {
                try
                {
                    if (cancellingDownload)
                    {
                        client.DownloadProgressChanged -= null;
                        client.DownloadFileCompleted -= null;
                        client.Dispose();
                        _downloads = null;
                        return;
                    }
                    double phasePercentage = (double) CurFilesCount / FileLength * 100,
                           totalPercentage = phasePercentage * 1,
                           percentage = (double) e.BytesReceived / e.TotalBytesToReceive * 100;
                    string fileName = Path.GetFileName(curDownload.Path);
                    lblFileProgress.Text = string.Format("Downloaded {0} of {1} ({2:F2}%) ", e.BytesReceived.BytesToString(), e.TotalBytesToReceive.BytesToString(), percentage);
                    pbFileProgress.Value = (int) Math.Truncate(percentage);
                    lblTotalProgress.Text = string.Format("Fase {0}: {1} de {2} archivos descargados ({3:F2}%)\nArchivo: {4} (Total: {5:F2}%)", "1", CurFilesCount, FileLength, phasePercentage, GetFileStr(fileName), totalPercentage);
                    pbTotalProgress.Value = (int) Math.Truncate(totalPercentage);
                }
                catch
                {
                    Console.WriteLine("Exception occurred!");
                }
            });
        }

        private static void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            form.BeginInvoke((MethodInvoker) delegate
            {
                DownloadPath dl = _downloads.Dequeue();
                if (dl != null) NextDownload(dl);
                else
                {
                    //Check if files are complete??
                    if (packType == PackType.Client)
                        frmMain.runClient = true;
                    else
                        frmMain.runServer = true;
                    frmMain.downloadingData = false;
                }
            });
        }

        private static string GetFileStr(string name)
        {
            string withoutExt = Path.GetFileNameWithoutExtension(name);
            return withoutExt.Substring(0, 12) + "..." + name.Replace(withoutExt, "");
        }
    }
}