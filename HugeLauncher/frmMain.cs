using Lerp2Web;
using LiteLerped_WF_API.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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
        }

        private void btnDelClient_Click(object sender, EventArgs e)
        {
        }

        private void btnRunisServer_Click(object sender, EventArgs e)
        {
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
            if (IsVersionSetup(comboBox1.SelectedIndex, PackType.Client))
            {
                btnRuninsClient.Text = "Abrir Launcher";
            }
            else
            {
                btnRuninsClient.Text = "Instalar versión";
                btnDelClient.Visible = false;
            }
        }

        private void menuStrip1_ItemClicked(object sender, System.Windows.Forms.ToolStripItemClickedEventArgs e)
        {
        }

        private void establecerRutaDeInstalaciónToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private static void StartingTutorial()
        { //Inject a frame form
            //string title = "Starting tutorial";
            //MessageBox.Show(title, "Welcome to HugeLauncher! With this launcher you");
            frmTutorial tutorial = new frmTutorial() { TopMost = true };
            tutorial.Show();
            instance.LocationChanged += (sender, e) =>
            {
                Point p = instance.Location;
                tutorial.Location = new Point(p.X + 9, p.Y + 55);
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

        private void mostrarTutorialInicialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartingTutorial();
        }

        public static bool IsVersionSetup(int index, PackType type)
        {
            string folder = Path.Combine(ModpackFolderPath, ModpackName, GetVersionStr(index, type));
            return Directory.Exists(folder) && !folder.IsDirectoryEmpty();
        }

        public static string GetVersionStr(int index, PackType type)
        {
            JObject obj = type == PackType.Client ? clientVersions : serverVersions;
            return obj["Versions"].Children().ElementAt(index)["Version"].ToObject<string>();
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
}