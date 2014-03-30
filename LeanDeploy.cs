using IWshRuntimeLibrary;
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
                return false;
            }
        }

        public static string installPath
        {
            get
            {
                // TODO: Combine path with that .NET function. Do it for all paths
                // MainModule.FileName returns the launcher's filename
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Application.ProductName, Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName));
            }
        }

        private static bool isInstalledExecutable
        {
            get
            {
                return isInstalled && false;
            }
        }

        private static void AddExecutable()
        {
            // Install executable in program files
        }

        private static void RemoveExecutable()
        {
            KillExecutable();

            // TODO: remove folder too
            ProcessStartInfo info = new ProcessStartInfo();
            info.Arguments="/C choice /C Y /N /D Y /T 3 & Del " + installPath;
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
            //TODO: Create folder : (shortcutFolder)
            IWshShortcut shortcut = shell.CreateShortcut(Path.Combine(shortcutFolder, Application.ProductName + ".lnk"));
            shortcut.Description = Application.ProductName;
            shortcut.TargetPath = installPath;
            shortcut.Save();
        }

        private static void RemoveShortcut()
        {
            // Delete(Environment.GetFolderPath(Environment.SpecialFolder.Programs) + "\\" + Application.ProductName)
        }

        private static void AddUninstallEntry()
        {
            
        }

        private static void RemoveUninstallEntry()
        {

        }

        private static void LaunchExecutable()
        {
            Process.Start(installPath);
            Environment.Exit(0);
        }

        private static void KillExecutable()
        {
            Process currentProcess = Process.GetCurrentProcess();

            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            foreach (Process process in processes)
            {
                if (process != currentProcess) {
                    process.Kill();
                }
            }
        }

        private static void RemoveSettings()
        {
            //Delete Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Application.CompanyName)
        }
    }
}
