using HugeLauncher.Properties;
using LiteLerped_WF_API.Classes;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LiteProgram = LiteLerped_WF_API.Program;

namespace HugeLauncher
{
    internal static class Program
    {
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