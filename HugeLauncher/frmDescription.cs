using HugeLauncher.Controls;
using System;
using System.Drawing;

namespace HugeLauncher
{
    public partial class frmDescription : SpecialForm
    {
        public static frmDescription instance;
        internal static bool wasOpened;

        public frmDescription()
            : base(new Point(0, 25), new Size(0, 190))
        {
            InitializeComponent();
        }

        public static void Init(frmMain ins)
        {
            mainIns = ins;
            Init(new frmDescription());
        }

        private void frmDescription_Load(object sender, EventArgs e)
        {
            wasOpened = true;
            webBrowser1.Navigate(Program.GetHugeFormUri(this));
        }
    }
}