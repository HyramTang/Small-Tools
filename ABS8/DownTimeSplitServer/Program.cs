using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace DownTimeSplitService
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool create;
            using (Mutex mu = new Mutex(true, Application.ProductName, out create))
            {
                if (create)
                {
                    Run();
                }
                else
                {
                    MessageBox.Show("ABS8 Report Service程序已经在运行中了!");
                }
            }
        }

        private static void Run()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmService());
        }
    }
}
