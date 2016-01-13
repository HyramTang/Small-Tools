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

            //string sql5 = @"ALTER TABLE tbFlowProduction ALTER Column PlanedCT decimal(8,2)";
            //ExcuteSQL(sql5);

            //string sql6 = @"alter table tbPro_Production add StaticCT decimal(18,2)";
            //ExcuteSQL(sql6);

            //DateTime dtNow=DateTime.Now;
            //DateTime dtStart = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 7, 0, 0);
            //string sql7 = @"update tbPro_Production set 
            //StaticCT=(select CycleTime from tbDic_Product_Line_CT where LineId=tbPro_Production.LineId and ProductId=tbPro_Production.ProductId)";
            //ExcuteSQL(sql7);

            //            string sql8 = @"update tbPro_Production set 
            //            StaticCT=
            //            (case when 
            //            (select CycleTime from tbDic_Product_Line_CT where LineId=tbPro_Production.LineId and ProductId=tbPro_Production.ProductId)!='0'
            //            then
            //            (select CycleTime from tbDic_Product_Line_CT where LineId=tbPro_Production.LineId and ProductId=tbPro_Production.ProductId)
            //            else
            //            (select DefaultCT from tb_Rpt_Performance_Line_Target where LineId=tbPro_Production.LineId) end) WHERE StaticCT IS null";
            //ExcuteSQL(sql8);

            //string sql9 = @"alter table tbDic_COTRecord add IsDefaultTarget bit";
            //ExcuteSQL(sql9);

            //string sql10 = @"CREATE TABLE [dbo].[tbDic_COTargetDefault](
            //[Id] [int] IDENTITY(1,1) NOT NULL,
            //[LineId] [int] NULL,
            //[DefaultValue] [decimal](18, 2) NULL,
            //CONSTRAINT [PK_tbDic_COTargetDefault] PRIMARY KEY CLUSTERED 
            //([Id] ASC)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]) ON [PRIMARY]";
            //ExcuteSQL(sql10);

            //string sql11 = "update tbDic_COTRecord set IsDefaultTarget='true' where COTarget=-1";

            //string sql12 = "alter table tbDailyShiftInfo add MTD decimal(18, 4)";
            //ExcuteSQL(sql12);

            //string sql13 = @"CREATE TABLE [dbo].[tb_Alarm_OtherAlarmInterface](
            //[Id] [int] IDENTITY(1,1) NOT NULL,
            //[StartTime] [datetime] NULL,
            //[HandlerStatus] [nvarchar](50) NULL,
            //[CreateTime] [datetime] NULL,
            //[LineName] [nvarchar](50) NULL,
            //[StationName] [nvarchar](50) NULL,
            //[DowntimeId] [int] NULL,
            //CONSTRAINT [PK_tb_Alarm_OtherAlarmInterface] PRIMARY KEY CLUSTERED 
            //(
            //[Id] ASC
            //)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            //) ON [PRIMARY]
            //            
            //ALTER TABLE [dbo].[tb_Alarm_OtherAlarmInterface] ADD  CONSTRAINT [DF_tb_Alarm_OtherAlarmInterface_CreateTime]  DEFAULT (getdate()) FOR [CreateTime]
            //            
            //CREATE TABLE [dbo].[tb_Rpt_RegularTarget](
            //[Id] [int] IDENTITY(1,1) NOT NULL,
            //[RegularName] [nvarchar](50) NULL,
            //[RegularTargetInfo] [decimal](18, 4) NULL,
            //CONSTRAINT [PK_tb_Rpt_RegularTarget] PRIMARY KEY CLUSTERED 
            //(
            //[Id] ASC
            //)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            //) ON [PRIMARY]
            //            
            //alter table [dbo].[tbPro_UnScheduleDownTime]
            //add  EmpConfirm nvarchar(50)
            //            
            //alter table [tbDic_DownCodeHandleInfo]
            //add  [NeedLineLeaderConfirm] nvarchar(50)
            //            
            //update tbDic_Station set WorkStationId=
            //(select top 1 Id From tbDic_WorkStation where WorkStationName='ST220' and LineId=4) 
            //where StationName='ST220' AND LineId=4
            //            
            //            
            //update tbDic_Station set WorkStationId=
            //(select top 1 Id From tbDic_WorkStation where WorkStationName='ST205' and LineId=4) 
            //where StationName='ST205' AND LineId=4";

            //string sql14 = @"alter table [AndonForABS8].[dbo].tbPro_UnScheduleDownTime add ResetTime datetime
            //alter table [DBAJAndon].[dbo].tbPro_UnScheduleDownTime add ResetTime datetime";

            //string sql15 = @"alter table tbDic_DefectCategory add IsRemind bool";
            //ExcuteSQL(sql15);

            //string sql16 = @"Update tbPro_Production SET RealProductOutput=24 WHERE RealStartTime>='2015-11-22 17:11:00.000' AND RealEndTime<='2015/11/22 17:14:30' AND LineId=29";
            //if (ExcuteSQL(sql16))
            //    MessageBox.Show("Success！\nEffect [" + Count + "] Rows!");

            //string sql17 = "delete from tb_Alarm_Trigger_Voice where TriggerSystem='SpotAlarm'";

            //string sql18 = "INSERT tbDic_Spalte2 (Spalte2Letter,Spalte2Value,ECUProductId)values('B','023','3')";
            //ExcuteSQL(sql18);

            string sql19 = @"delete from tbPro_Production where ProductId in (select Id from tbDic_Product where ECU='1277' or ECU like 'A%')
            delete  from tbDic_Product_Line_CT where ProductId in (select Id from tbDic_Product where ECU='1277' or ECU like 'A%')
            delete  from tbDic_Product_Group_CT where ProductId in (select Id from tbDic_Product where ECU='1277' or ECU like 'A%')
            delete  from tbDic_Product_Station_CT where ProductId in (select Id from tbDic_Product where ECU='1277' or ECU like 'A%')
            delete from tbDic_Product where ECU='1277' or ECU like 'A%'";
            ExcuteSQL(sql19);


            MessageBox.Show("Success！");
            button2.Enabled = false;
        }
    }
}
