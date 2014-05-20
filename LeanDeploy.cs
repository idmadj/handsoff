using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace handsoff
{
    // LeanDeploy is a super lean utility that lets your app self-install itself without user input.
    class LeanDeploy
    {
        private const string UNINSTALL_PARAM = "uninstall";
        private const string SILENT_PARAM = "silent";

        public delegate void LDEvent();

        public static event LDEvent BeforeInstall;
        public static event LDEvent AfterInstall;
        public static event LDEvent BeforeUninstall;
        public static event LDEvent ProcessesKilled;

        private static string assemblyName;
        private static string executablePath;
        private static string installFolder;
        public static string installPath { get { return Path.Combine(installFolder, assemblyName + ".exe"); } }

        static LeanDeploy()
        {
            assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            executablePath = Assembly.GetExecutingAssembly().Location;
            installFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Application.ProductName);
        }

        public static void Check()
        {
            if (CheckParam(UNINSTALL_PARAM))
            {
                try
                {
                    Uninstall();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Uninstallation failed: " + e.Message, Application.ProductName + " Uninstall", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                Environment.Exit(0);
            }
            else if (!isInstalledExecutable)
            {
                try
                {
                    Install();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Installation failed: " + e.Message, Application.ProductName + " Install", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Uninstall();
                    Environment.Exit(0);
                }

                LaunchExecutable();
            }
        }

        private static void Install()
        {
            if (!isInstalled) {
                if (BeforeInstall != null) {
                    BeforeInstall();
                }

                AddExecutable();
                AddShortcut();
                AddUninstallEntry();

                if (AfterInstall != null)
                {
                    AfterInstall();
                }
            }
        }

        private static void Uninstall()
        {
            if (CheckParam(SILENT_PARAM) ||
                MessageBox.Show("Are you sure you want to uninstall " + Application.ProductName + "?",
                                Application.ProductName + " Uninstall",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                if (BeforeUninstall != null)
                {
                    BeforeUninstall();
                }

                RemoveSettings();
                RemoveShortcut();
                RemoveUninstallEntry();
                RemoveExecutable();
            }
        }

        private static bool isInstalled
        {
            get
            {
                return System.IO.File.Exists(installPath);
            }
        }

        private static bool isInstalledExecutable
        {
            get
            {
                return isInstalled && Path.Equals(executablePath, installPath);
            }
        }

        private static void AddExecutable()
        {
            Directory.CreateDirectory(installFolder);
            System.IO.File.Copy(executablePath, installPath, true);
        }

        private static void RemoveExecutable()
        {
            KillProcesses();

            ProcessStartInfo info = new ProcessStartInfo();
            info.Arguments = "/C choice /C Y /N /D Y /T 3 & rmdir \"" + installFolder + "\" /s /q";
            info.WindowStyle = ProcessWindowStyle.Hidden;
            info.CreateNoWindow = true;
            info.FileName = "cmd.exe";
            Process.Start(info);

            // Make sure to quit if we are running the installed executable so it gets deleted by the timed action above
            if (isInstalledExecutable) {
                Environment.Exit(0);
            }
        }

        private static void AddShortcut()
        {
            string shortcutFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), Application.ProductName);

            WshShell shell = new WshShell();
            Directory.CreateDirectory(shortcutFolder);
            IWshShortcut shortcut = shell.CreateShortcut(Path.Combine(shortcutFolder, Application.ProductName + ".lnk"));
            shortcut.Description = Application.ProductName;
            shortcut.TargetPath = installPath;
            shortcut.Save();
        }

        private static void RemoveShortcut()
        {
            string shortcutFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), Application.ProductName);

            if (Directory.Exists(shortcutFolder)) 
            {
                Directory.Delete(shortcutFolder, true);
            }
        }

        private static void AddUninstallEntry()
        {
            Version version = new Version(Application.ProductVersion);

            RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + Application.ProductName);

            registryKey.SetValue("DisplayIcon", "\"" + installPath + "\"");
            registryKey.SetValue("DisplayName", Application.ProductName);
            registryKey.SetValue("DisplayVersion", version.Major + "." + version.Minor);
            registryKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
            registryKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            registryKey.SetValue("Publisher", Application.CompanyName);
            registryKey.SetValue("EstimatedSize", Math.Round(new System.IO.FileInfo(executablePath).Length / 1024f), RegistryValueKind.DWord);
            registryKey.SetValue("UninstallString", "\"" + installPath + "\" uninstall");
            registryKey.SetValue("VersionMajor", version.Major, RegistryValueKind.DWord);
            registryKey.SetValue("VersionMinor", version.Minor, RegistryValueKind.DWord);

            registryKey.Close();
        }

        private static void RemoveUninstallEntry()
        {
            Registry.CurrentUser.DeleteSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + Application.ProductName, false);
        }

        private static void LaunchExecutable()
        {
            Process.Start(installPath);
            Environment.Exit(0);
        }

        private static void KillProcesses()
        {
            Process currentProcess = Process.GetCurrentProcess();

            Process[] processes = Process.GetProcessesByName(assemblyName);
            foreach (Process process in processes)
            {
                if (process.Id != currentProcess.Id)
                {
                    process.Kill();
                }
            }

            if (ProcessesKilled != null)
            {
                ProcessesKilled();
            }
        }

        private static void RemoveSettings()
        {
            string settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Application.CompanyName);

            if (Directory.Exists(settingsFolder)) 
            {
                Directory.Delete(settingsFolder, true);
            }
        }

        private static bool CheckParam(string param)
        {
            return Array.Find(Environment.GetCommandLineArgs(), e => String.Equals(e, param, StringComparison.OrdinalIgnoreCase)) != null;
        }
    }
}
