using HugeLauncher.Controls;
using HugeLauncher.Properties;
using Lerp2Web;
using LiteLerped_WF_API.Classes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.Control;
using LiteProgram = LiteLerped_WF_API.Program;

namespace HugeLauncher
{
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

        private static string GetHugeFormName(SpecialForm frm)
        {
            switch (frm.GetType().Name)
            {
                case "frmDescription":
                    return "get-desc";

                case "frmTutorial":
                    return "get-tutorial";
            }
            return "";
        }

        internal static Uri GetHugeFormUri(SpecialForm frm)
        {
            NameValueCollection col = new NameValueCollection()
            {
                { "action", GetHugeFormName(frm) },
                { "lang", string.Format("{0,-3}", Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName) }
            };
            return string.Concat(WebUrl, "Core.php").GetBuildedUri(col);
        }
    }

    public static class API
    {
        public static bool IsDirectoryEmpty(this string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }

        public static void AddRange(this Control con, IEnumerable<Control> col)
        {
            IEnumerable<Control> cc = new List<Control>(col);
            con.Controls.AddRange(cc.ToArray());

            foreach (Control c in cc)
                AttachedControl.controls[c.Name][EventType.Loaded](c, null);
        }
    }

    public class HugeConfigKeys : LerpedConfigKeys
    {
    }
}