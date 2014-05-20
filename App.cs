//#define DEMO

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32.TaskScheduler;

namespace handsoff
{
    class App : ApplicationContext /*Form*/
    {
        private const string HID_GUID = "{745a17a0-74d3-11d0-b6fe-00a0c90f57da}";

        private NotifyIcon appIcon;
        private ContextMenuStrip appMenu;
        private ToolStripMenuItem controlledDeviceMenuItem;
        private ToolStripMenuItem launchOnStartupMenuItem;
        private ToolStripMenuItem aboutMenuItem;
        private ToolStripMenuItem quitMenuItem;
        private List<Device> devices;
        private bool displayTutorial;
        private Random rng;

        public App()
        {
            /* TODO:
             *  - Demo version
             *  - Isolate/cleanup toggle/initial state (It may be trying to be too smart right now. Consider only acting on a session basis, no persistent data)
             *  - Handle devices change system event
             */

            Application.ApplicationExit += OnApplicationExit;

            /*this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.ShowInTaskbar = false; 
            this.Load += OnLoad;*/

            displayTutorial = String.IsNullOrWhiteSpace(controlledDeviceID);

            rng = new Random();

            InitializeComponent();
            appIcon.Visible = true;

            DisplayHelp();
        }

        /*private void OnLoad(object sender, EventArgs e)
        {
            Size = new Size(0, 0);
        }


        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000; // system detected a new device
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004; //device was removed
        private const int DBT_DEVNODES_CHANGED = 0x0007; //device changed

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_DEVICECHANGE && m.WParam.ToInt32() == DBT_DEVNODES_CHANGED)
            {
                ScheduleUpdate();
            }

            base.WndProc(ref m);
        }

        private void ScheduleUpdate()
        {

        }*/

        private void InitializeComponent()
        {
            appIcon = new NotifyIcon();

            appIcon.Text = Application.ProductName;
            appIcon.MouseClick += OnAppClick;

            appMenu = new ContextMenuStrip();

            controlledDeviceMenuItem = new ToolStripMenuItem();
            launchOnStartupMenuItem = new ToolStripMenuItem();
            aboutMenuItem = new ToolStripMenuItem();
            quitMenuItem = new ToolStripMenuItem();
            appMenu.SuspendLayout();

            appMenu.Items.AddRange(new ToolStripItem[] { controlledDeviceMenuItem, launchOnStartupMenuItem, aboutMenuItem, new ToolStripSeparator(), quitMenuItem });

            controlledDeviceMenuItem.Text = "Controlled device";
            controlledDeviceMenuItem.DropDown.Opening += OnControlledDeviceOpening;
            UpdateDevicesList();

#if (DEMO)
            launchOnStartupMenuItem.Text = "Launch on startup (full version only)";
#else
            launchOnStartupMenuItem.Text = "Launch on startup";
            launchOnStartupMenuItem.Checked = launchOnStartup;
#endif
            launchOnStartupMenuItem.MouseDown += OnStartupClick;

            aboutMenuItem.Text = "About...";
            aboutMenuItem.MouseDown += OnAboutClick;

            quitMenuItem.Text = "Quit";
            quitMenuItem.MouseDown += OnQuitClick;

            appMenu.ResumeLayout(false);
            appIcon.ContextMenuStrip = appMenu;

            if (IsDeviceEnabled(controlledDeviceID) != null) 
            {
                SaveDeviceInitialState();
            }
            RestoreDeviceToggleState();

            UpdateIcon();
        }

        private void UpdateDevicesList()
        {
            ListDevices();

            DetectControlledDevice();

            controlledDeviceMenuItem.DropDown.Items.Clear();

            foreach(Device device in devices) 
            {
                ToolStripMenuItem deviceMenuItem = new ToolStripMenuItem(device.name);
                deviceMenuItem.Name = device.instancePath;
                deviceMenuItem.MouseDown += OnDeviceClick;

                if (device.name.IndexOf("touch", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Font itemFont = deviceMenuItem.Font;
                    deviceMenuItem.Font = new Font(itemFont, itemFont.Style | FontStyle.Bold);
                }

                if (device.instancePath == controlledDeviceID)
                {
                    deviceMenuItem.Checked = true;
                }

                controlledDeviceMenuItem.DropDown.Items.Add(deviceMenuItem);
            }
        }

        private void DetectControlledDevice()
        {
            if (String.IsNullOrWhiteSpace(controlledDeviceID))
            {
                Device defaultDevice = devices.FirstOrDefault(s => (s.name.Contains("touch") && (s.name.Contains("screen") || s.name.Contains("display"))));

                if (defaultDevice != null)
                {
                    controlledDeviceID = defaultDevice.instancePath;
                }
            }
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
                    devices.Add(new Device((String)queryObj["DeviceID"], (String)queryObj["Name"]/* + " (" + ((String)queryObj["Status"] == "OK" ? "Enabled" : "Disabled") + ")"*/));
                }

                /*if (devices.FirstOrDefault(s => s.instancePath == controlledDeviceID) == null) 
                {
                    // Clear the controleld device if it isn't found
                    controlledDeviceID = "";
                }*/

                devices.Sort();

            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred while querying for WMI data: " + e.Message);
            }

        }

        private static bool? IsDeviceEnabled(string deviceID)
        {
            bool? returnValue = null;

            if (!String.IsNullOrWhiteSpace(deviceID)) 
            {
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity Where DeviceID = '" + deviceID.Replace("\\", "\\\\") + "'");

                    ManagementObjectCollection foundDevices = searcher.Get();

                    if (foundDevices.Count > 0) 
                    {
                        returnValue = false;
                    }

                    foreach (ManagementObject queryObj in foundDevices)
                    {
                        if ((String)queryObj["Status"] == "OK")
                        {
                            returnValue = true;
                        }
                        break;
                    }
                }
                catch (Exception e)
                {
                    returnValue = null;
                    Console.WriteLine("An error occurred while querying for WMI data: " + e.Message);
                }
            }

            return returnValue;
        }

        private static bool? IsDevicePresent(string deviceID)
        {
            bool? returnValue = false;

            if (!String.IsNullOrWhiteSpace(deviceID))
            {
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity Where DeviceID = '" + deviceID.Replace("\\", "\\\\") + "'");
                    
                    if (searcher.Get().Count > 0) 
                    {
                        returnValue = true;
                    }
                }
                catch (Exception e)
                {
                    returnValue = null;
                    Console.WriteLine("An error occurred while querying for WMI data: " + e.Message);
                }
            }

            return returnValue;
        }

        private static void EnableDevice(string deviceID, bool enabled = true)
        {
            if (!String.IsNullOrWhiteSpace(deviceID))
            {
                try
                {
                    DisableHardware.DisableDevice(n => n.ToUpperInvariant().Contains(deviceID), !enabled);
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred while enabling/disabling a device: " + e.Message);
                }
            }
        }

        private static bool ToggleDevice(string deviceID)
        {
            bool deviceEnabled = !(IsDeviceEnabled(deviceID) ?? false);

            EnableDevice(deviceID, deviceEnabled);

            return deviceEnabled;
        }

        private void DisplayHelp()
        {
            if (String.IsNullOrWhiteSpace(controlledDeviceID) || !(IsDevicePresent(controlledDeviceID) ?? false))
            {
                appIcon.ShowBalloonTip(5, Application.ProductName, "Touchscreen not found. Click or tap here to select the controlled device manually.", ToolTipIcon.Warning);
            }
            else if (displayTutorial)
            {
                appIcon.ShowBalloonTip(5, Application.ProductName, "Simply click or tap this icon to toggle your touchscreen. Right-click for options.", ToolTipIcon.Info);
                displayTutorial = false;
            }
        }

        private void UpdateIcon()
        {
            if (String.IsNullOrWhiteSpace(controlledDeviceID) || !(IsDevicePresent(controlledDeviceID) ?? false))
            {
                appIcon.Icon = new Icon(Properties.Resources.icon_warn, SystemInformation.SmallIconSize);
            }
            else if (IsDeviceEnabled(controlledDeviceID) ?? false)
            {
                appIcon.Icon = new Icon(Properties.Resources.icon_on, SystemInformation.SmallIconSize);
            }
            else
            {
                appIcon.Icon = new Icon(Properties.Resources.icon_off, SystemInformation.SmallIconSize);
            }
        }

        public static void SaveDeviceInitialState()
        {
            Properties.Settings defaultSettings = Properties.Settings.Default;

            string currentDevice = defaultSettings.controlledDevice;

            if (!String.IsNullOrWhiteSpace(currentDevice))
            {
                defaultSettings.controlledDeviceInitialState = IsDeviceEnabled(currentDevice);
                defaultSettings.Save();
            }
        }

        public static void RestoreDeviceInitialState()
        {
            Properties.Settings defaultSettings = Properties.Settings.Default;

            string currentDevice = defaultSettings.controlledDevice;

            if (!String.IsNullOrWhiteSpace(currentDevice))
            {
                bool? deviceState = defaultSettings.controlledDeviceInitialState;

                if (deviceState != null)
                {
                    EnableDevice(currentDevice, (deviceState ?? false));
                }
            }
        }

        public static void SaveDeviceToggleState()
        {
            Properties.Settings defaultSettings = Properties.Settings.Default;

            string currentDevice = defaultSettings.controlledDevice;

            if (!String.IsNullOrWhiteSpace(currentDevice))
            {
                defaultSettings.controlledDeviceToggleState = IsDeviceEnabled(currentDevice);
                defaultSettings.Save();
            }
        }

        public static void RestoreDeviceToggleState()
        {
            Properties.Settings defaultSettings = Properties.Settings.Default;

            string currentDevice = defaultSettings.controlledDevice;

            if (!String.IsNullOrWhiteSpace(currentDevice))
            {
                bool? deviceState = defaultSettings.controlledDeviceToggleState;

                if (deviceState != null)
                {
                    EnableDevice(currentDevice, (deviceState ?? false));
                }
            }
        }

        private void ShowAppMenu()
        {
            MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
            mi.Invoke(appIcon, null);
        }

#if (DEMO)
        private void ShowFullVersionPopup()
        {
            appMenu.Close();
            aboutMenuItem.Enabled = false;

            if (MessageBox.Show("Enjoying HandsOff's trial version?\n\nPlease consider purchasing the full version, which is free of this annoying popup and can also be setup to run on startup.\n\nIt's only $1.89, and you'll help support the development of awesome software!\n\nReady for the full version?", "HandsOff Full", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                Process.Start("http://www.secretfloor.com");
            }

            aboutMenuItem.Enabled = true;
        }
#endif

        private void OnAppClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) 
            {
                if (String.IsNullOrWhiteSpace(controlledDeviceID) || !(IsDevicePresent(controlledDeviceID) ?? false))
                {
                    ShowAppMenu();
                }
                else
                {
                    ToggleDevice(controlledDeviceID);
                    SaveDeviceToggleState();

#if (DEMO)
                    if (rng.Next(5) < 1) 
                    {
                        ShowFullVersionPopup();
                    }
#endif
                }

                UpdateIcon();
            }
        }

        private void OnControlledDeviceOpening(object sender, CancelEventArgs e)
        {
            UpdateDevicesList();
        }

        private void OnStartupClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
#if (DEMO)
                ShowFullVersionPopup();
#else
                launchOnStartup = !launchOnStartupMenuItem.Checked;
                launchOnStartupMenuItem.Checked = launchOnStartup;
#endif
            }
        }

        private void OnAboutClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                appMenu.Close();
                aboutMenuItem.Enabled = false;

                Version version = new Version(Application.ProductVersion);
                int year = DateTime.Now.Year;

                MessageBox.Show(Application.ProductName + " " + version.Major + "." + version.Minor + " © 2014" + (year > 2014 ? " - " + year : "") + " Abdelmadjid Hammou.\nAll Rights Reserved.", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
                aboutMenuItem.Enabled = true;
            }
        }

        private void OnQuitClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Application.Exit();
            }
        }

        private void OnDeviceClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                controlledDeviceID = ((ToolStripMenuItem)sender).Name;
                DisplayHelp();
            }
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            //Cleanup so that the icon will be removed when the application is closed
            appIcon.Visible = false;

            SaveDeviceToggleState();
            RestoreDeviceInitialState();
        }

        public static void OnUninstall()
        {
            RestoreDeviceInitialState();
#if (!DEMO)
            launchOnStartup = false;
#endif
        }

        public static void OnProcessesKilled()
        {
            TrayIconBuster.TrayIconBuster.RemovePhantomIcons();
        }

        private string controlledDeviceID
        {
            get
            {
                return Properties.Settings.Default.controlledDevice;
            }

            set
            {
                Properties.Settings defaultSettings = Properties.Settings.Default;

                string currentDevice = defaultSettings.controlledDevice;

                if (currentDevice != value && (IsDevicePresent(value) ?? false))
                {
                    RestoreDeviceInitialState();

                    defaultSettings.controlledDevice = value;
                    defaultSettings.Save();

                    SaveDeviceInitialState();
                    SaveDeviceToggleState();
                }

                UpdateIcon();
            }
        }

#if (!DEMO)
        private static bool launchOnStartup
        {
            get
            {
                bool returnValue = false;

                TaskService taskService = new TaskService();

                try
                {
                    if (taskService.GetTask(Application.ProductName) != null)
                    {
                        returnValue = true;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occured while trying to read a scheduled task: " + e.Message);
                }

                return returnValue;
            }

            set
            {
                TaskService taskService = new TaskService();

                if (value)
                {
                    TaskDefinition taskDefinition = taskService.NewTask();
                    taskDefinition.RegistrationInfo.Description = "Launches " + Application.ProductName + " on startup.";

                    taskDefinition.Triggers.Add(new LogonTrigger());

                    taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;

                    taskDefinition.Settings.DisallowStartIfOnBatteries = false;
                    taskDefinition.Settings.StopIfGoingOnBatteries = false;
                    taskDefinition.Settings.AllowHardTerminate = false;
                    taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;

                    taskDefinition.Actions.Add(new ExecAction("\"" + LeanDeploy.installPath + "\""));

                    try 
                    {
                        taskService.RootFolder.RegisterTaskDefinition(Application.ProductName, taskDefinition);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("An error occured while trying to register a scheduled task: " + e.Message);
                    }
                }
                else
                {
                    try
                    {
                        if (taskService.GetTask(Application.ProductName) != null)
                        {
                            taskService.RootFolder.DeleteTask(Application.ProductName);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("An error occured while trying to delete a scheduled task: " + e.Message);
                    }
                }

            }
        }
#endif
    }

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
