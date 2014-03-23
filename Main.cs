using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace handsoff
{
    class Main : ApplicationContext
    {
        private NotifyIcon appIcon;
        private ContextMenuStrip appMenu;
        private ToolStripMenuItem optionsMenuItem;
        private ToolStripMenuItem quitMenuItem;
        private Options optionsForm;
        private List<string> devices;

        public Main()
        {
            Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
            InitializeComponent();
            appIcon.Visible = true;
        }

        private void InitializeComponent()
        {
            appIcon = new NotifyIcon();

            appIcon.BalloonTipIcon = ToolTipIcon.Info;
            appIcon.BalloonTipText = "Click this icon to toggle your touchscreen on and off.";
            appIcon.Text = Application.ProductName;
            appIcon.Icon = Properties.Resources.TouchOn;
            appIcon.Click += OnAppClick;

            appMenu = new ContextMenuStrip();
            optionsMenuItem = new ToolStripMenuItem();
            quitMenuItem = new ToolStripMenuItem();
            appMenu.SuspendLayout();

            // 
            // appMenu
            // 
            appMenu.Items.AddRange(new ToolStripItem[] { optionsMenuItem, quitMenuItem });
            appMenu.Name = "appMenu";

            // 
            // quitMenuItem
            // 
            optionsMenuItem.Name = "quitMenuItem";
            optionsMenuItem.Text = "Options";
            optionsMenuItem.Click += new EventHandler(OnOptionsClick);

            // 
            // quitMenuItem
            // 
            quitMenuItem.Name = "quitMenuItem";
            quitMenuItem.Text = "Quit";
            quitMenuItem.Click += new EventHandler(OnQuitClick);

            appMenu.ResumeLayout(false);
            appIcon.ContextMenuStrip = appMenu;
        }

        private void listDevices()
        {
            devices = new List<string>();

            Guid HIDGUID;
            HidD_GetHidGuid(out HIDGUID);

            Console.WriteLine(HIDGUID);

            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_PnPEntity Where ClassGuid = '{745a17a0-74d3-11d0-b6fe-00a0c90f57da}'");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    devices.Add((String)queryObj["Name"]);
                }
            }
            catch (ManagementException e)
            {
                MessageBox.Show("An error occurred while querying for WMI data: " + e.Message);
            }

            DisableHardware.DisableDevice(n => n.ToUpperInvariant().Contains("VEN_10DE&DEV_0373&SUBSYS_CB841043&REV_A2"), true);

        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            //Cleanup so that the icon will be removed when the application is closed
            appIcon.Visible = false;
        }

        private void OnAppClick(object sender, EventArgs e)
        {
            appIcon.Icon = Properties.Resources.TouchOff;
            //appIcon.ShowBalloonTip(10000);
        }

        private void OnOptionsClick(object sender, EventArgs e)
        {
            listDevices();
            
            if (optionsForm == null || Application.OpenForms[optionsForm.Name] == null)
            {
                optionsForm = new Options();
                optionsForm.devices = devices;
                optionsForm.Show();
            }
            else
            {
                optionsForm.Focus();
            }

        }

        private void OnQuitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        [DllImport(@"hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void HidD_GetHidGuid(out Guid gHid);
    }
}
