using System;
using System.Collections.Generic;
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

        protected static void Init<T>(T val, Action act = null) where T : SpecialForm
        {
            if (typeof(T).Name == "frmDescription")
                frmDescription.instance = (frmDescription) (object) val;
            else
                frmTutorial.instance = (frmTutorial) (object) val;

            specialInstance.Loading();

            act?.Invoke();
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

            pnl.AddRange(Controls.Cast<Control>());

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

    public enum EventType
    {
        Loaded
    }

    public class AttachedControl
    {
        internal readonly static Dictionary<string, AttachedControl> controls = new Dictionary<string, AttachedControl>();

        private readonly Dictionary<EventType, Action<object, EventArgs>> attachedActions = new Dictionary<EventType, Action<object, EventArgs>>();

        public Action<object, EventArgs> this[EventType ev]
        {
            get
            {
                if (!attachedActions.ContainsKey(ev))
                    attachedActions.Add(ev, null);
                return attachedActions[ev];
            }
            set
            {
                if (attachedActions.ContainsKey(ev))
                    attachedActions[ev] = value;
                else
                    attachedActions.Add(ev, value);
            }
        }

        private AttachedControl()
        {
        }

        public AttachedControl(Control c)
        {
            controls.Add(c.Name, this);
        }
    }
}