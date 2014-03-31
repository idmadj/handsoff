using Microsoft.Win32;
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
        private const string HID_GUID = "{745a17a0-74d3-11d0-b6fe-00a0c90f57da}";

        private NotifyIcon appIcon;
        private ContextMenuStrip appMenu;
        private ToolStripMenuItem optionsMenuItem;
        private ToolStripMenuItem quitMenuItem;
        private Options optionsForm;
        private List<Device> devices;

        public Main()
        {
            /* TODO:
             *  - Installer/Uninstaller
             *  - Bind launchonstartup to options
             *  - Rework firstrun scenario
             *  - Device auto-detect
             *  - Launch on startup
             *  - Move config to tray menu
             *  - Clear startup registry entry on uninstall
             */

            Application.ApplicationExit += OnApplicationExit;

            InitializeComponent();
            appIcon.Visible = true;

            appIcon.ShowBalloonTip(3, Application.ProductName, "Simply click or tap this icon to toggle your touchscreen.", ToolTipIcon.Info);
        }

        private void UpdateIcon()
        {
            if (IsDeviceEnabled(Properties.Settings.Default.controlledDevice))
            {
                appIcon.Icon = new Icon(Properties.Resources.TouchOn, SystemInformation.SmallIconSize);
            }
            else
            {
                appIcon.Icon = new Icon(Properties.Resources.TouchOff, SystemInformation.SmallIconSize);
            }
        }

        private void InitializeComponent()
        {
            appIcon = new NotifyIcon();

            appIcon.BalloonTipClicked += OnBalloonClick;

            appIcon.Text = Application.ProductName;
            appIcon.MouseClick += OnAppClick;

            appMenu = new ContextMenuStrip();
            optionsMenuItem = new ToolStripMenuItem();
            quitMenuItem = new ToolStripMenuItem();
            appMenu.SuspendLayout();

            appMenu.Items.AddRange(new ToolStripItem[] { optionsMenuItem, quitMenuItem });
            appMenu.Name = "appMenu";

            optionsMenuItem.Name = "quitMenuItem";
            optionsMenuItem.Text = "Configuration...";
            optionsMenuItem.Click += new EventHandler(OnOptionsClick);

            quitMenuItem.Name = "quitMenuItem";
            quitMenuItem.Text = "Quit";
            quitMenuItem.Click += new EventHandler(OnQuitClick);

            appMenu.ResumeLayout(false);
            appIcon.ContextMenuStrip = appMenu;

            UpdateIcon();
        }

        private void ListDevices()
        {
            devices = new List<Device>();

            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity Where ClassGuid = '" + HID_GUID + "'");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    devices.Add(new Device((String)queryObj["DeviceID"], (String)queryObj["Name"] + " (" + ((String)queryObj["Status"] == "OK" ? "Enabled" : "Disabled") + ")"));
                }

                devices.Sort();

            }
            catch (ManagementException e)
            {
                MessageBox.Show("An error occurred while querying for WMI data: " + e.Message);
            }

        }

        private bool IsDeviceEnabled(string deviceID)
        {
            bool returnValue = false;

            if (!String.IsNullOrWhiteSpace(deviceID)) 
            {
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity Where DeviceID = '" + deviceID.Replace("\\", "\\\\") + "'");

                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        if ((String)queryObj["Status"] == "OK")
                        {
                            returnValue = true;
                        }
                        break;
                    }
                }
                catch (ManagementException e)
                {
                    Console.WriteLine("An error occurred while querying for WMI data: " + e.Message);
                }
            }

            return returnValue;
        }

        private void EnableDevice(string deviceID, bool enabled = true)
        {
            if (!String.IsNullOrWhiteSpace(deviceID))
            {
                DisableHardware.DisableDevice(n => n.ToUpperInvariant().Contains(deviceID), !enabled);
            }
        }

        private bool ToggleDevice(string deviceID)
        {
            bool deviceEnabled = !IsDeviceEnabled(deviceID);

            EnableDevice(deviceID, deviceEnabled);

            return deviceEnabled;
        }

        private void OpenOptions()
        {
            ListDevices();

            if (optionsForm == null || Application.OpenForms[optionsForm.Name] == null)
            {
                optionsForm = new Options();
                optionsForm.OKClicked += OnOptionsOK;
                optionsForm.CancelClicked += OnOptionsCancel;
                optionsForm.devices = devices;
                optionsForm.selectedDevice = Properties.Settings.Default.controlledDevice;
                optionsForm.Show();
            }
            else
            {
                optionsForm.Focus();
            }
        }

        private void OnAppClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) 
            {
                ToggleDevice(Properties.Settings.Default.controlledDevice);
                UpdateIcon();
            }
        }

        private void OnOptionsClick(object sender, EventArgs e)
        {
            OpenOptions();
        }

        private void OnBalloonClick(object sender, EventArgs e)
        {
            OpenOptions();
        }

        private void OnQuitClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void OnOptionsOK(object _e)
        {
            Properties.Settings.Default.Save();
            UpdateIcon();
        }

        private void OnOptionsCancel(object _e)
        {
           Properties.Settings.Default.Reload();
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            //Cleanup so that the icon will be removed when the application is closed
            appIcon.Visible = false;
        }



        private bool launchOnStartup
        {
            get 
            {
                bool returnValue = false;
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");

                if (registryKey.GetValue(Application.ProductName) != null)
                {
                    returnValue = true;
                }

                registryKey.Close();

                return returnValue;
            }

            set 
            {
                RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");

                if (value) 
                {
                    registryKey.SetValue(Application.ProductName, LeanDeploy.installPath);
                } 
                else 
                {
                    registryKey.DeleteValue(Application.ProductName, false);
                }

                registryKey.Close();
            }
        }
    }

    public delegate void BasicEvent(Object _e);

    public class Device : IComparable<Device>
    {
        public string instancePath { get; set; }
        public string name { get; set; }

        public Device(string _instancePath, string _name)
        {

            instancePath = _instancePath;
            name = _name;
        }

        public int CompareTo(Device other)
        {
            return name.CompareTo(other.name);
        }
    }
}
