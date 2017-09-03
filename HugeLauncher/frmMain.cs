using Lerp2Web;
using LiteLerped_WF_API.Controls;
using Newtonsoft.Json.Linq;
using System;
using LWeb = Lerp2Web.Lerp2Web;

namespace HugeLauncher
{
    public partial class frmMain : LerpedForm
    {
        public const string clientDataUrl = "https://gitlab.com/ikillnukes1/HugeCraft-Client/raw/master/HugeCraft_Data.json",
                            serverDataUrl = "https://gitlab.com/ikillnukes1/HugeCraft-Server/raw/master/HugeCraft_Data.json";

        internal static JObject clientVersions, serverVersions;

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            Console.WriteLine("frmMain_Load");

            //Get versions
            clientVersions = LWeb.JGet(null, clientDataUrl, true);
            clientVersions["Versions"].Children().ForEach(x => comboBox1.Items.Add(x["Version"].ToObject<string>()));
            comboBox1.Text = comboBox1.Items[0].ToString();
        }
    }
}