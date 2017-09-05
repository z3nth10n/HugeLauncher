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

        private static bool runClient,
                            runServer;

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
            int index = comboBox1.SelectedIndex;
            string path = GetFolderPathVer(index, type);
            JToken verObj = GetVersion(index, type);
            JEnumerable<JToken> dl = verObj["Files"].Children();

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
        private static Queue<DownloadPath> _downloads = new Queue<DownloadPath>();
        private static Form form;

        private static Label _lblFProg;

        private static Label lblFileProgress
        {
            get
            {
                if (_lblFProg == null) _lblFProg = (Label) form.Controls.Find("lblFileProgress", true)[0];
                return _lblFProg;
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

        public static void StartDownload(this Form frm, IEnumerable<DownloadPath> downloads)
        {
            form = frm;

            foreach (DownloadPath download in downloads)
                _downloads.Enqueue(download);

            NextDownload(_downloads.Dequeue());
        }

        private static void NextDownload(DownloadPath dl)
        {
            Thread thread = new Thread(() =>
            {
                string fol = Path.GetDirectoryName(dl.Path);
                if (!Directory.Exists(fol))
                    Directory.CreateDirectory(fol);

                using (WebClient client = new WebClient())
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
                    double percentage = (double) e.BytesReceived / e.TotalBytesToReceive * 100;
                    lblFileProgress.Text = string.Format("Downloaded {0} of {1} ({2:F2}%) ", e.BytesReceived.BytesToString(), e.TotalBytesToReceive.BytesToString(), percentage);// + e.BytesReceived + " of " + e.TotalBytesToReceive ;
                    pbFileProgress.Value = (int) Math.Truncate(percentage);
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
                NextDownload(_downloads.Dequeue());
            });
        }

        /*private static void downloadFile(IEnumerable<string> urls)
        {
            foreach (var url in urls)
            {
                _downloadUrls.Enqueue(url);
            }

            // Starts the download
            btnGetDownload.Text = "Downloading...";
            btnGetDownload.Enabled = false;
            progressBar1.Visible = true;
            lblFileName.Visible = true;

            DownloadFile();
        }

        private void DownloadFile()
        {
            if (_downloadUrls.Any())
            {
                WebClient client = new WebClient();
                client.DownloadProgressChanged += client_DownloadProgressChanged;
                client.DownloadFileCompleted += client_DownloadFileCompleted;

                var url = _downloadUrls.Dequeue();
                string FileName = url.Substring(url.LastIndexOf("/") + 1,
                            (url.Length - url.LastIndexOf("/") - 1));

                client.DownloadFileAsync(new Uri(url), "C:\\Test4\\" + FileName);
                lblFileName.Text = url;
                return;
            }

            // End of the download
            btnGetDownload.Text = "Download Complete";
        }

        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // handle error scenario
                throw e.Error;
            }
            if (e.Cancelled)
            {
                // handle cancelled scenario
            }
            DownloadFile();
        }

        private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            progressBar1.Value = int.Parse(Math.Truncate(percentage).ToString());
        }*/
    }
}