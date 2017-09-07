using HugeLauncher.Properties;
using Lerp2Web;
using LiteLerped_WF_API.Classes;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using LiteProgram = LiteLerped_WF_API.Program;

namespace HugeLauncher
{
    public enum HugeFormType
    {
        frmDescription,
        frmTutorial
    }

    internal static class Program
    {
        public static string WebUrl
        {
            get
            {
                return string.Concat(Lerp2Web.Lerp2Web.APIServer, "/hugelauncherphp/");
            }
        }

        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Console.WriteLine("Running program!");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            LiteProgram.Run("hugelauncher_", new frmMain(), () =>
            {
                Settings.Default.StartupPath = frmMain.AppPath;
                Settings.Default.Save();
            });
        }

        private static string GetHugeFormName(HugeFormType type)
        {
            switch (type)
            {
                case HugeFormType.frmDescription:
                    return "get-desc";

                case HugeFormType.frmTutorial:
                    return "get-tutorial";
            }
            return "";
        }

        internal static Uri GetHugeFormUri(HugeFormType type)
        {
            NameValueCollection col = new NameValueCollection()
            {
                { "action", GetHugeFormName(type) },
                { "lang", string.Format("{0,-3}", Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName) }
            };
            return string.Concat(WebUrl, "Core.php").GetBuildedUri(col);
        }

        internal static void FixHeight<T>(T frm) where T : Form
        {
            Control webBrowser = frm.Controls.Find("webBrowser1", true)[0],
                    button = frm.Controls.Find("button1", true)[0];

            webBrowser.Height = frm.Height - 41;
            button.Top = frm.Height - 41 - 7;
        }
    }

    public static class API
    {
        public static bool IsDirectoryEmpty(this string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }
    }

    public class HugeConfigKeys : LerpedConfigKeys
    {
    }
}