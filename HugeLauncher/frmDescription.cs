using HugeLauncher.Controls;
using System;

namespace HugeLauncher
{
    public partial class frmDescription : SpecialForm
    {
        public static frmDescription instance;
        internal static bool wasOpened;

        public frmDescription()
            : base(85)
        {
            InitializeComponent();
        }

        public static void Init(frmMain ins)
        {
            Init(ins, HugeFormType.frmDescription);
        }

        private void frmDescription_Load(object sender, EventArgs e)
        {
            wasOpened = true;
            webBrowser1.Url = Program.GetHugeFormUri(HugeFormType.frmDescription);
            //Hide();
        }
    }
}