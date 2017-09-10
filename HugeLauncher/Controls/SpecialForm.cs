using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HugeLauncher.Controls
{
    public partial class SpecialForm : Form
    {
        protected static frmMain mainIns;

        internal Point MarginPosition;

        internal Size MarginSize;
        internal Panel PanelInstance;

        internal static SpecialForm specialInstance;

        protected static void Init<T>(T val) where T : SpecialForm
        {
            if (typeof(T).Name == "frmDescription")
                frmDescription.instance = (frmDescription) (object) val;
            else
                frmTutorial.instance = (frmTutorial) (object) val;

            specialInstance.Loading();
        }

        public SpecialForm()
        {
        }

        public SpecialForm(Point mPos, Size siz)
        {
            specialInstance = this;
            MarginPosition = mPos;
            MarginSize = siz;
        }

        private void Loading()
        {
            Panel pnl = new Panel();

            pnl.Location = MarginPosition;
            pnl.Size = new Size(mainIns.Width - MarginSize.Width, mainIns.Height - MarginSize.Height);
            pnl.BackColor = Color.Red;

            pnl.Controls.AddRange(Controls.Cast<Control>().ToArray());

            mainIns.Controls.Add(pnl);

            pnl.BringToFront();

            FixHeight(pnl);
            //Bringer(pnl);

            PanelInstance = pnl;
        }

        private void FixHeight(Panel pnl)
        {
            Size siz = mainIns.Size - MarginSize;

            pnl.Height = siz.Height;

            Control web = pnl.Controls.Cast<Control>().SingleOrDefault(x => x is WebBrowser);

            if (web == null) return;

            web.Height = siz.Height - 41;

            if (pnl.Controls != null && pnl.Controls.Count > 0)
                foreach (Control control in pnl.Controls)
                    if (!(control is WebBrowser))
                        control.Top = siz.Height - 33;
        }

        private void Bringer(Panel pnl)
        {
            Console.WriteLine(pnl.Controls.Count);
            foreach (Control c in pnl.Controls)
            {
                Console.WriteLine("Bringing {0}", c.Name);
                c.BringToFront();
                c.Refresh();
            }
        }

        public new void Show()
        {
            PanelInstance.Visible = true;
        }

        public new void Hide()
        {
            PanelInstance.Visible = false;
        }
    }
}