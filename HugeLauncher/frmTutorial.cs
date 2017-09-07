using HugeLauncher.Controls;
using System;
using System.Drawing;

namespace HugeLauncher
{
    public partial class frmTutorial : SpecialForm
    {
        public static frmTutorial instance;

        public frmTutorial()
            : base(55)
        {
            InitializeComponent();
            Shown += (sender, e) =>
            {
                Size = new Size(mainIns.Width - 20, mainIns.Height - 65);
            };
            Program.FixHeight(this);
        }

        public static void Init(frmMain ins)
        {
            //Console.WriteLine(new System.Diagnostics.StackTrace().ToString());
            Init(ins, HugeFormType.frmTutorial);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TopMost = false;
            Hide();
            if (!frmDescription.wasOpened)
                Init(mainIns, HugeFormType.frmDescription);
        }

        private void frmTutorial_Load(object sender, EventArgs e)
        {
            webBrowser1.Url = Program.GetHugeFormUri(HugeFormType.frmTutorial);
        }
    }
}