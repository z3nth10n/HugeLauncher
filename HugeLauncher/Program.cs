using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Run(new frmMain());
        }

        private static void Run(frmMain main)
        {
            Console.WriteLine("Running program!");

            //Run LiteLerped-WF-API
            LiteProgram.Run("hugelauncher_", main);

            Application.Run(main);
        }
    }
}