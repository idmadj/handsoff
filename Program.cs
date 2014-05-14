using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;


namespace handsoff
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            LeanDeploy.BeforeUninstall += App.OnUninstall;
            LeanDeploy.Check();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new App());
        }
    }
}
