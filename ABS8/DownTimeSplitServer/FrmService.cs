using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using AJ.Andon.Entity;
using AJ.Andon.Entity.Dictionary;
using AJ.Andon.Entity.Production;
using DownTimeSplitService;
using DownTimeSplitService.Properties;

namespace DownTimeSplitService
{
    public partial class FrmService : Form
    {
        public FrmService()
        {
            InitializeComponent();
            notifyIcon1.Icon = Resources.ReportOffLine;
            notifyIcon1.Text = this.Text;
        }
        private Thread m_Thread;
        public DowntimeQueryHelper queryhelper;
        public TimeSplitHelper timehelper;

        private void btnStart_Click(object sender, EventArgs e)
        {
            //notifyIcon1.Icon = Resources.ReportOnLine;
            //txtLog.AppendText(DateTime.Now.ToString() + ":  服务已经启动\n");
            //btnStart.Enabled = false;
            //btnStop.Enabled = true;
            //m_Thread = new Thread(new ThreadStart(ServiceThread.GetInstance().Begin));
            //m_Thread.IsBackground = true;
            //m_Thread.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            //if (!btnStart.Enabled)
            //{
            //    notifyIcon1.Icon = Resources.ReportOffLine;
            //    txtLog.AppendText(DateTime.Now.ToString() + ":  服务已经关闭\n");
            //    btnStart.Enabled = true;
            //    btnStop.Enabled = false;
            //    ServiceThread.GetInstance().End();
            //}
            //else
            //    MessageBox.Show("服务未启动！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void FrmService_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.ShowInTaskbar = false;
            this.Opacity = 0;
            this.Hide();
            e.Cancel = true;
        }

        private void ShowMe()
        {
            this.Opacity = 100;
            this.Show();
            this.ShowInTaskbar = true;
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            ShowMe();
        }

        private void showToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowMe();
        }

        private void timerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (new Frmpwd().ShowDialog() == DialogResult.OK)
            {
                Application.Exit();
            }
            else
            {
                Hide();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            #region
            //DateTime dtStart = new DateTime(2014, 12, 01, 7, 0, 0, 0);
            //DateTime dtEnd = new DateTime(2014, 12, 01, 19, 0, 0, 0);

            //DateTime dtStart = new DateTime(2014, 12, 01, 19, 0, 0, 0);
            //DateTime dtEnd = new DateTime(2014, 12, 02, 7, 0, 0, 0);

            //DateTime dtStart = new DateTime(2014, 12, 02, 07, 0, 0, 0);
            //DateTime dtEnd = new DateTime(2014, 12, 02, 19, 0, 0, 0);

            //DateTime dtStart = new DateTime(2014, 12, 02, 19, 0, 0, 0);
            //DateTime dtEnd = new DateTime(2014, 12, 03, 7, 0, 0, 0);

            //DateTime dtStart = new DateTime(2014, 12, 03, 07, 0, 0, 0);
            //DateTime dtEnd = new DateTime(2014, 12, 03, 19, 0, 0, 0);

            //DateTime dtStart = new DateTime(2014, 12, 03, 19, 0, 0, 0);
            //DateTime dtEnd = new DateTime(2014, 12, 04, 7, 0, 0, 0);

            //DateTime dtStart = new DateTime(2014, 12, 04, 07, 0, 0, 0);
            //DateTime dtEnd = new DateTime(2014, 12, 04, 19, 0, 0, 0);

            //DateTime dtStart = new DateTime(2014, 12, 04, 19, 0, 0, 0);
            //DateTime dtEnd = new DateTime(2014, 12, 05, 7, 0, 0, 0);

            //DateTime dtStart = new DateTime(2014, 12, 05, 07, 0, 0, 0);
            //DateTime dtEnd = new DateTime(2014, 12, 05, 19, 0, 0, 0);

            //DateTime dtStart = new DateTime(2014, 12, 05, 19, 0, 0, 0);
            //DateTime dtEnd = new DateTime(2014, 12, 06, 7, 0, 0, 0);


            //ReportHelper helper = new ReportHelper();
            //helper.StartServer(dtStart, dtEnd);
            #endregion
            DateTime dtnow = DateTime.Now;
            if (dtnow.Hour >= 7 && dtnow.Hour < 19)
            {
                dtnow = new DateTime(dtnow.Year, dtnow.Month, dtnow.Day, 7, 0, 0, 0);
            }
            else
            {
                if (dtnow.Hour >= 19 && dtnow.Hour <= 24)
                {
                    dtnow = new DateTime(dtnow.Year, dtnow.Month, dtnow.Day, 19, 0, 0, 0);
                }
                else if (dtnow.Hour >= 0 && dtnow.Hour < 7)
                {
                    dtnow = new DateTime(dtnow.AddDays(-1).Year, dtnow.AddDays(-1).Month, dtnow.AddDays(-1).Day, 19, 0, 0, 0);
                }
            }


            DateTime dtStart = new DateTime(dtnow.Year, dtnow.Month, 4, 7, 0, 0, 0);
            DateTime dtEnd = new DateTime(dtnow.Year, dtnow.Month, 4, 19, 0, 0, 0);

            while (true)
            {
                ReportHelper helper = new ReportHelper();
                helper.StartServer(dtStart, dtEnd);

                dtStart=dtStart.AddHours(12);
                dtEnd = dtEnd.AddHours(12);

                if (dtStart >= dtnow)
                {
                    MessageBox.Show("导入成功！");
                    timer1.Enabled = false;
                    break;
                }
            }

            #region
            //if (dtnow.Hour == 7)
            //{
            //    if (dtnow.Minute > 40 && dtnow.Minute < 45)
            //    {
            //        DateTime dtStart = new DateTime(dtnow.AddDays(-1).Year, dtnow.AddDays(-1).Month, dtnow.AddDays(-1).Day, 19, 0, 0, 0);
            //        DateTime dtEnd = new DateTime(dtnow.Year, dtnow.Month, dtnow.Day, 7, 0, 0, 0);

            //        ReportHelper helper = new ReportHelper();
            //        helper.StartServer(dtStart, dtEnd);
            //    }
            //}
            //else if (dtnow.Hour == 19)
            //{
            //    if (dtnow.Minute > 40 && dtnow.Minute < 45)
            //    {
            //        DateTime dtStart = new DateTime(dtnow.Year, dtnow.Month, dtnow.Day, 7, 0, 0, 0);
            //        DateTime dtEnd = new DateTime(dtnow.Year, dtnow.Month, dtnow.Day, 19, 0, 0, 0);

            //        ReportHelper helper = new ReportHelper();
            //        helper.StartServer(dtStart, dtEnd);
            //    }
            //}
            #endregion
        }

        private void FrmService_Load(object sender, EventArgs e)
        {
            GlobalVars.lines = DALLib<Line>.DataAccess.GetSome(null);

            //btnStart_Click(new object(), new EventArgs());
        }
    }
}
