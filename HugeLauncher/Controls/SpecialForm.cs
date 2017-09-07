using System.Drawing;
using System.Windows.Forms;

namespace HugeLauncher.Controls
{
    public partial class SpecialForm : Form
    {
        protected static frmMain mainIns;

        internal int HeightMargin;

        internal Point Position
        {
            get
            {
                Point p = frmMain.mainInstance.Location;
                return new Point(p.X + 9, p.Y + HeightMargin);
            }
        }

        protected static void Init(frmMain ins, HugeFormType type)
        {
            mainIns = ins;
            if (type == HugeFormType.frmDescription)
                frmDescription.instance = new frmDescription();
            else
                frmTutorial.instance = new frmTutorial();
            //instance =  ? (SpecialForm) new frmDescription() : new frmTutorial();
        }

        public SpecialForm()
        {
        }

        public SpecialForm(int heightMargin)
        {
            HeightMargin = heightMargin;
            InitializeComponent();

            Point p = mainIns.Location;

            TopMost = true;

            Show();
            Shown += (sender, e) =>
            {
                Location = Position;
            };

            Activated += (sender, e) =>
            {
                if (Visible)
                    mainIns.Activate();
            };

            FormClosing += (sender, e) =>
            {
                if (Visible)
                    mainIns.Close();
            };
        }
    }
}