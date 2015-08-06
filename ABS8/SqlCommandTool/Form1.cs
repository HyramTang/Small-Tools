using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SqlCommandTool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private int Count = 0;

        //新增SpotVoice字段
        private void btnAction1_Click(object sender, EventArgs e)
        {
            try
            {
                string Sql = ConfigurationManager.ConnectionStrings["SqlCommand"].ConnectionString;
                Sql = @"alter table tb_Rel_SpotRecord add SpotVoice nvarchar(max)";
                if (ExcuteSQL(Sql))
                    MessageBox.Show("Success!\nEffect [" + Count + "] Rows!");
                else
                    MessageBox.Show("No Effect Any Rows!");
            }
            catch (Exception eex)
            {
                MessageBox.Show(eex.Message);
            }
            btnAction1.Enabled = false;
        }
        //删除U6重复记录
        private void btnAction2_Click(object sender, EventArgs e)
        {
            try
            {
                string Sql = ConfigurationManager.ConnectionStrings["SqlCommand"].ConnectionString;
                Sql = @"delete from tbPro_UnScheduleDownTime
                where Id in (select d.Id from tbPro_UnScheduleDownTime d
                LEFT JOIN tbDic_Station s on s.Id=d.StationId
                left join tbDic_Line l on l.Id=s.LineId
                 where StartTime in (select StartTime from tbPro_UnScheduleDownTime group by StartTime,EndTime having COUNT(*)>1) 
                and DefectCategoryId=57 and LineId not in (8,9,10)
                ) and Id not in (select min(Id) from tbPro_UnScheduleDownTime 
                where StartTime>='2015-04-25 00:07:18.000' group by StartTime,EndTime having count(*)>1) ";
                if (ExcuteSQL(Sql))
                    MessageBox.Show("Success!\nEffect [" + Count + "] Rows!");
                else
                    MessageBox.Show("No Effect Any Rows!");
            }
            catch (Exception eex)
            {
                MessageBox.Show(eex.Message);
            }
            btnAction2.Enabled = false;
        }
        //删除COTRecord报表重复数据
        private void btnAction3_Click(object sender, EventArgs e)
        {
            try
            {
                string Sql = ConfigurationManager.ConnectionStrings["SqlCommand"].ConnectionString;
                Sql = @"delete from tbDic_COTRecord 
                where StartTime in (select StartTime from tbDic_COTRecord group by StartTime having COUNT(*)>1) 
                and Id not in (select min(Id) from tbDic_COTRecord group by StartTime,LineId,EndTime having count(*)>1)";
                if (ExcuteSQL(Sql))
                    MessageBox.Show("Success!\nEffect [" + Count + "] Rows!");
                else
                    MessageBox.Show("No Effect Any Rows!");
            }
            catch (Exception eex)
            {
                MessageBox.Show(eex.Message);
            }
            btnAction3.Enabled = false;
        }
        //删除修改工具产生的错误U6数据
        private void btnAction4_Click(object sender, EventArgs e)
        {
            try
            {
                string Sql = ConfigurationManager.ConnectionStrings["SqlCommand"].ConnectionString;
                Sql = @"delete from tbPro_UnScheduleDownTime 
                where Id in (select d.Id from tbPro_UnScheduleDownTime d
                LEFT JOIN tbDic_Station s on s.Id=d.StationId
                left join tbDic_Line l on l.Id=s.LineId
                where StationId not IN(5,16,35,44,57,78) 
                and LineId not in (7,8,9,10,11,12) 
                and StartTime>'2015-04-23 7:00:00.000'
                and DefectCategoryId=57)";
                if (ExcuteSQL(Sql))
                    MessageBox.Show("Success!\nEffect [" + Count + "] Rows!");
                else
                    MessageBox.Show("No Effect Any Rows!");
            }
            catch (Exception eex)
            {
                MessageBox.Show(eex.Message);
            }
            btnAction4.Enabled = false;
        }
        private bool ExcuteSQL(string strSQL)
        {
            bool result = false;

            string conn = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
            SqlConnection sqlConnection = new SqlConnection(conn);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = sqlConnection;
            cmd.CommandText = strSQL;

            sqlConnection.Open();
            Count = cmd.ExecuteNonQuery();
            result = Count > 0 ? true : false;
            sqlConnection.Close();

            return result;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string Sql = ConfigurationManager.ConnectionStrings["SqlCommand"].ConnectionString;
                Sql = @"delete from tbPro_HistoryProduction where ProductId is null";
                if (ExcuteSQL(Sql))
                    MessageBox.Show("Success!\nEffect [" + Count + "] Rows!");
                else
                    MessageBox.Show("No Effect Any Rows!");
            }
            catch (Exception eex)
            {
                MessageBox.Show(eex.Message);
            }
            btnAction4.Enabled = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //string sql1 = "update tbPro_HistoryProduction set RealEndTime='2015-06-03 03:22:00' where Id=32168";
            //string sql2 = "update tbPro_Production set RealEndTime='2015-06-03 03:22:00' where Id=34626";

            //if (ExcuteSQL(sql1))
            //    MessageBox.Show("Success！\nEffect [" + Count + "] Rows!");
            //if (ExcuteSQL(sql2))
            //    MessageBox.Show("Success！\nEffect [" + Count + "] Rows!");

            //string sql3 = "update tbPro_UnScheduleDownTime set StartTime='2015-06-03 03:22:00' where Id=57881";
            //if (ExcuteSQL(sql3))
            //    MessageBox.Show("Success！\nEffect [" + Count + "] Rows!");
            //button2.Enabled = false;

            //string sql4 = @"update tbPro_HistoryProduction set RealEndTime='2015-06-24 23:22:00' where Id=36391
            //update tbPro_HistoryProduction set RealEndTime='2015-06-25 06:59:59' where Id=36417";
            //if (ExcuteSQL(sql4))
            //    MessageBox.Show("Success！\nEffect [" + Count + "] Rows!");

            string sql5 = @"ALTER TABLE tbFlowProduction ALTER Column PlanedCT decimal(8,2)";
            ExcuteSQL(sql5);
            MessageBox.Show("Success！");
            button2.Enabled = false;
        }
    }
}
