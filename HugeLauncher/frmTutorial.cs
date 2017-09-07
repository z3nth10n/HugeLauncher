using HugeLauncher.Controls;
using System;
using System.Drawing;

namespace HugeLauncher
{
    public partial class frmTutorial : SpecialForm
    {
        public static frmTutorial instance;

        public frmTutorial()
            : base(new Point(2, 25), new Size(20, 65))
        {
            InitializeComponent();
            if (frmDescription.instance != null)
                frmDescription.instance.Hide();
        }

        public static void Init(frmMain ins)
        {
            mainIns = ins;
            Init(new frmTutorial());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TopMost = false;
            Hide();
            if (!frmDescription.wasOpened)
                Init(frmDescription.instance != null ? frmDescription.instance : new frmDescription());
        }

        private void frmTutorial_Load(object sender, EventArgs e)
        {
            webBrowser1.Navigate(Program.GetHugeFormUri(this));
        }
    }
}