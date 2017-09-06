using System;
using System.Drawing;
using System.Windows.Forms;

namespace HugeLauncher
{
    public partial class frmDescription : Form
    {
        internal static bool wasOpened;
        public static frmDescription instance;

        internal Point Position
        {
            get
            {
                Point p = frmMain.instance.Location;
                return new Point(p.X + 9, p.Y + 85);
            }
        }

        public frmDescription()
        {
            InitializeComponent();
        }

        public static void Init(frmMain ins)
        {
            Point p = ins.Location;
            instance = new frmDescription()
            {
                TopMost = true
            };

            instance.Show();
            instance.Shown += (sender, e) =>
            {
                instance.Location = instance.Position;
            };
        }

        private void frmDescription_Load(object sender, EventArgs e)
        {
            wasOpened = true;
            webBrowser1.Url = Program.GetHugeFormUri(HugeFormType.frmDescription);
        }
    }
}