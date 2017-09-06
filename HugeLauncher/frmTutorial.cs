using System;
using System.Drawing;
using System.Windows.Forms;

namespace HugeLauncher
{
    public partial class frmTutorial : Form
    {
        public static frmTutorial instance;
        private static frmMain mainIns;

        internal Point Position
        {
            get
            {
                Point p = frmMain.instance.Location;
                return new Point(p.X + 9, p.Y + 55);
            }
        }

        public frmTutorial()
        {
            InitializeComponent();
        }

        public static void Init(frmMain ins)
        {
            instance = new frmTutorial()
            {
                TopMost = true,
                Size = new Size(ins.Width - 20, ins.Height - 65) //Arreglar el height y top de sus controles
            };

            Program.FixHeight(instance);

            instance.Show();
            instance.Shown += (sender, e) =>
            {
                instance.Location = instance.Position;
            };

            mainIns = ins;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TopMost = false;
            Hide();
            if (!frmDescription.wasOpened)
                frmDescription.Init(mainIns);
        }

        private void frmTutorial_Load(object sender, EventArgs e)
        {
            webBrowser1.Url = Program.GetHugeFormUri(HugeFormType.frmTutorial);
        }
    }
}