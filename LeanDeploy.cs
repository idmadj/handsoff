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

        public delegate void LDEvent();

        public static event LDEvent BeforeInstall;
        public static event LDEvent AfterInstall;
        public static event LDEvent BeforeUninstall;

        private static string assemblyName;
        private static string executablePath;
        private static string installFolder;
        public static string installPath;

        /* TODO
         *  - Error catching
         *  - Convert installPath to getter setter
         *  - Uninstall confirmation MsgBox?
         */

        static LeanDeploy()
        {
            assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            executablePath = Assembly.GetExecutingAssembly().Location;
            installFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Application.ProductName);
            installPath = Path.Combine(installFolder, assemblyName + ".exe");
        }

        public static void Check()
        {
            if (Array.Find(Environment.GetCommandLineArgs(), e => String.Equals(e, UNINSTALL_PARAM, StringComparison.OrdinalIgnoreCase)) != null)
            {
                Uninstall();
                Environment.Exit(0);
            }
            else if (!isInstalledExecutable)
            {
                Install();
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
            if (BeforeUninstall != null)
            {
                BeforeUninstall();
            }

            RemoveSettings();
            RemoveShortcut();
            RemoveUninstallEntry();
            RemoveExecutable();
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
            KillExecutable();

            ProcessStartInfo info = new ProcessStartInfo();
            info.Arguments="/C choice /C Y /N /D Y /T 3 & rmdir \"" + installFolder + "\" /s /q";
            info.WindowStyle=ProcessWindowStyle.Hidden;
            info.CreateNoWindow=true;
            info.FileName="cmd.exe";
            Process.Start(info);

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

        private static void KillExecutable()
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
        }

        private static void RemoveSettings()
        {
            string settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Application.CompanyName);

            if (Directory.Exists(settingsFolder)) 
            {
                Directory.Delete(settingsFolder, true);
            }
        }
    }
}
