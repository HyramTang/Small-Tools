using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using AJ.Andon.Entity;
using AJ.Andon.Entity.Dictionary;
using AJ.Andon.Entity.Production;
using DMES.Utility;

namespace DownTimeSplitService
{
    public class DownTimeSplitLogic
    {
        TimeSplitHelper timehelper = null;
        DowntimeQueryHelper queryhelper = null;
        DataTable SplitTab = null;
        DataTable UnschduleTab = null;
        DataTable ScheduleTab = null;

        public DownTimeSplitLogic()
        {
            timehelper = new TimeSplitHelper();
            queryhelper = new DowntimeQueryHelper();
        }
        public DataTable GetDownTime(int LineId, DateTime begin, DateTime end, out List<ScheduleDownTime> lstSchdule, out List<UnScheduleDownTime> lstUnschedule)
        {
            string SqlGetLine = "SELECT TOP 1 * FROM tbDic_Line WHERE Id=" + LineId + "";
            Line line = DALLib<Line>.DataAccess.GetOneBySQL(SqlGetLine);
            List<Station> lstStation = DALLib<Station>.DataAccess.GetSome(string.Format("select * from tbDic_Station where LineId={0}", line.Id));

            List<ScheduleDownTime> lstScheduleDowntime = new List<ScheduleDownTime>(); //所有的计划停机
            List<UnScheduleDownTime> lstUnscheDowntime = new List<UnScheduleDownTime>();//所有的非计划停机

            lstScheduleDowntime = queryhelper.QueryScheduleDowntime(line, begin, end);
            lstUnscheDowntime = queryhelper.QueryUnScheduleDowntime(line, begin, end);


            //第一次不假如计划停机进行时间切分：因为非计划需要先计算后再切分，然后再分割时间
            SplitTab = timehelper.GetTimeTableForABS8(null, lstUnscheDowntime, line, begin, end);

            UnschduleTab = SplitTab.Clone();
            ScheduleTab = SplitTab.Clone();
            foreach (DataRow row in SplitTab.Rows)
            {
                if (row["downcode"].ToString() != string.Empty && row["downcode"].ToString().Contains("U"))
                    UnschduleTab.Rows.Add(row.ItemArray);
            }

            List<UnScheduleDownTime> lstUnscheduleSplit = new List<UnScheduleDownTime>();
            try
            {
                foreach (DataRow row in UnschduleTab.Rows)
                {
                    //string SqlGetProduct = "SELECT TOP 1 * FROM tbPro_Production WHERE LineId=" + line.Id + " AND RealStartTime<='" + row["starttime"].ToString() + "' ORDER BY RealStartTime DESC";
                    //此处为了获取此时正在生产什么产品，然后通过ProductId才能查找到GroupCT或StationCT
                    //改为查询HistoryProduction，时间：2014-12-30 11:52:03
                    string SqlGetProduct = "SELECT TOP 1 * FROM tbPro_HistoryProduction WHERE LineId=" + line.Id + " AND RealStartTime<='" + row["starttime"].ToString() + "' ORDER BY RealStartTime DESC";
                    Production production = DALLib<Production>.DataAccess.GetOneBySQL(SqlGetProduct);
                    string SqlGetStation = "SELECT * FROM tbDic_Station WHERE Id=" + row["stationid"] + "";
                    Station station = DALLib<Station>.DataAccess.GetOneBySQL(SqlGetStation);
                    string SqlGetStationRefDT = "SELECT TOP 1 * FROM tbDic_StationRefDT WHERE StationId=" + station.Id + "";
                    StationRefDT stationRefDT = DALLib<StationRefDT>.DataAccess.GetOneBySQL(SqlGetStationRefDT);
                    DateTime StartTime = (DateTime)row["starttime"];
                    DateTime EndTime = (DateTime)row["endtime"];
                    string downcode = row["downcode"].ToString();
                    decimal downtimeseconds = 0;
                    if (lstStation.FindAll(p => (p.StationStep == station.StationStep && p.WorkStationId == station.WorkStationId)).Count > 1)
                    {//并联设备
                        DataRow[] unschdulerow = UnschduleTab.Select("stationstep='" + station.StationStep + "' and workstationid='" + station.WorkStationId + "' and starttime='" + StartTime + "' and endtime='" + EndTime + "'");
                        //停机了多少台设备
                        int DownCount = 1;
                        if (unschdulerow != null && unschdulerow.Length >= 1)
                            DownCount = unschdulerow.Length;
                        //查找算子
                        string SqlGetGroupOperator = "";
                        Product_Group_CT pgc = null;
                        if (production != null)
                        {
                            SqlGetGroupOperator = "SELECT Top 1 * FROM tbDic_Product_Group_CT WHERE WorkStationId=" + station.WorkStationId + " AND StationStep=" + station.StationStep + " AND GroupDownCount=" + DownCount + " AND ProductId=" + production.ProductId + "";
                            pgc = DALLib<Product_Group_CT>.DataAccess.GetOneBySQL(SqlGetGroupOperator);
                        }
                        downtimeseconds = (decimal)EndTime.Subtract(StartTime).TotalSeconds;
                        if (pgc != null && downcode != "U6")//换型不用切
                            downtimeseconds = downtimeseconds * pgc.GroupOperator;
                    }
                    else
                    {//串联设备
                        //string SqlGetStationOperator = "SELECT Top 1 * FROM tbDic_Product_Station_CT WHERE StationId=" + station.Id + " AND ProductId=" + production.ProductId + "";
                        //Product_Station_CT psc = DALLib<Product_Station_CT>.DataAccess.GetOneBySQL(SqlGetStationOperator);
                        downtimeseconds = (decimal)EndTime.Subtract(StartTime).TotalSeconds;
                        //if (psc != null && downcode != "U6")//换型不用切
                        //    downtimeseconds = downtimeseconds * psc.Operator;
                        //else
                        //    downtimeseconds = downtimeseconds * 1;
                        ////string SqlGetWorkFlow = "SELECT * FROM tbDic_WorkFlow WHERE Id IN (SELECT TOP 1 WorkFlowId FROM tbDic_WorkFlow_Line WHERE LineId=" + line.Id + ")";
                        ////WorkFlow workflow = DALLib<WorkFlow>.DataAccess.GetOneBySQL(SqlGetWorkFlow);
                        ////if (workflow != null && stationRefDT != null && downcode != "U6" && (workflow.WorkFlowName.ToLower().Contains("au") || workflow.WorkFlowName.ToLower().Contains("fa")))
                        ////{
                        ////    if (downtimeseconds <= stationRefDT.ReferenceDT)
                        ////        downtimeseconds = 0;
                        ////    else
                        ////        downtimeseconds = downtimeseconds - stationRefDT.ReferenceDT;
                        ////}
                        if (stationRefDT != null && downcode != "U6")
                        {
                            decimal agotime = downtimeseconds;
                            downtimeseconds = downtimeseconds - stationRefDT.ReferenceDT;
                            if (downtimeseconds <= 0)
                                downtimeseconds = 0;
                            else if (downtimeseconds > 0 && stationRefDT.ReferenceDT == 0)
                                downtimeseconds = agotime;
                        }
                    }
                    EndTime = StartTime.AddSeconds((double)downtimeseconds);
                    if (lstUnscheduleSplit != null && lstUnscheduleSplit.Count > 0)
                    {
                        UnScheduleDownTime ud = new UnScheduleDownTime
                        {
                            Id = CommonMethod.SafeGetIntFromObj(row["id"], 0),
                            DefectCategoryId = CommonMethod.SafeGetIntFromObj(row["defectcategoryid"], 0),
                            StartTime = StartTime,
                            EndTime = EndTime,
                            ReactionTime = CommonMethod.SafeGetDateTimeFromObj(row["reactiontime"]),
                            ActionTime = CommonMethod.SafeGetDateTimeFromObj(row["actiontime"]),
                            EmployeeId = CommonMethod.SafeGetIntFromObj(row["employeeid"], 0),
                            StationId = CommonMethod.SafeGetIntFromObj(row["stationid"], 0),
                            IsChangedPart = CommonMethod.SafeGetBooleanFromObj(row["ischangedpart"]),
                        };
                        #region
                        //for (int i = 0; i < lstUnscheduleSplit.Count; i++)
                        //{
                        //    if (ud.StartTime == lstUnscheduleSplit[i].StartTime)
                        //    {
                        //        decimal downtimecount = (decimal)lstUnscheduleSplit[i].EndTime.Subtract(lstUnscheduleSplit[i].StartTime).TotalSeconds;
                        //        if (downtimecount > timecount)
                        //        {
                        //            lstUnscheduleSplit.Remove(lstUnscheduleSplit[i]);
                        //            lstUnscheduleSplit.Add(ud);
                        //        }
                        //    }
                        //}
                        #endregion
                        lstUnscheduleSplit.Add(ud);
                        List<UnScheduleDownTime> lstSameStartTime = lstUnscheduleSplit.FindAll(p => p.StartTime == ud.StartTime);
                        if (lstSameStartTime.Count > 1)
                        {
                            decimal timecount = (decimal)ud.EndTime.Subtract(ud.StartTime).TotalSeconds;
                            for (int i = 0; i < lstSameStartTime.Count; i++)
                            {
                                decimal downtimecount = (decimal)lstSameStartTime[i].EndTime.Subtract(lstSameStartTime[i].StartTime).TotalSeconds;
                                if (timecount > downtimecount)
                                {
                                    lstUnscheduleSplit.Remove(lstUnscheduleSplit.Find(p => p.EndTime == lstSameStartTime[i].EndTime));
                                    break;
                                }
                                else if (timecount <= downtimecount)
                                {
                                    //不去除换型(保住换型)
                                    lstUnscheduleSplit.Remove(lstUnscheduleSplit.Find(p => p.EndTime == lstSameStartTime[i].EndTime && p.DefectCategoryId != 57));
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        lstUnscheduleSplit.Add(new UnScheduleDownTime
                        {
                            Id = CommonMethod.SafeGetIntFromObj(row["id"], 0),
                            DefectCategoryId = CommonMethod.SafeGetIntFromObj(row["defectcategoryid"], 0),
                            StartTime = StartTime,
                            EndTime = EndTime,
                            ReactionTime = CommonMethod.SafeGetDateTimeFromObj(row["reactiontime"]),
                            ActionTime = CommonMethod.SafeGetDateTimeFromObj(row["actiontime"]),
                            EmployeeId = CommonMethod.SafeGetIntFromObj(row["employeeid"], 0),
                            StationId = CommonMethod.SafeGetIntFromObj(row["stationid"], 0),
                            IsChangedPart = CommonMethod.SafeGetBooleanFromObj(row["ischangedpart"]),
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DMES.Utility.Logger.Log4netHelper.Error(ex);
                lstSchdule = null;
                lstUnschedule = null;
                return null;
            }
            //经过和算子计算的停机时间，停机时间会有所变动，变动后再次放入方法中排序
            SplitTab = timehelper.GetTimeTableForABS8(lstScheduleDowntime, lstUnscheduleSplit, line, begin, end);
            //去掉重复时间停机段
            //SplitTab = timehelper.GetABS8SpiltTimeFromTable(SplitTab);

            lstSchdule = GetScheduleLst(SplitTab);
            lstUnschedule = GetUnscheduleLst(SplitTab);

            return SplitTab;
        }
        public List<ScheduleDownTime> GetScheduleLst(DataTable SplitTab)
        {
            List<ScheduleDownTime> lstSchedule = new List<ScheduleDownTime>();
            int Id = 1;
            ScheduleTab = null;
            ScheduleTab = SplitTab.Clone();
            //获取已切分好的【计划停机】
            foreach (DataRow row in SplitTab.Rows)
            {
                if (row["downcode"].ToString() != string.Empty && row["downcode"].ToString().Contains("P"))
                    ScheduleTab.Rows.Add(row.ItemArray);
            }

            foreach (DataRow row in ScheduleTab.Rows)
            {
                lstSchedule.Add(new ScheduleDownTime
                {
                    Id = CommonMethod.SafeGetIntFromObj(row["id"], 0),
                    ScheduleDownTypeId = CommonMethod.SafeGetIntFromObj(row["scheduledowntypeid"], 0),
                    StartTime = CommonMethod.SafeGetDateTimeFromObj(row["starttime"]),
                    EndTime = CommonMethod.SafeGetDateTimeFromObj(row["endtime"]),
                    EmployeeId = CommonMethod.SafeGetIntFromObj(row["employeeid"], 0),
                    StationId = CommonMethod.SafeGetIntFromObj(row["stationid"], 0),
                });
                Id++;
            }

            return lstSchedule;
        }
        public List<UnScheduleDownTime> GetUnscheduleLst(DataTable SplitTab)
        {
            List<UnScheduleDownTime> lstUnschedule = new List<UnScheduleDownTime>();
            int Id = 1;
            UnschduleTab = null;
            UnschduleTab = SplitTab.Clone();
            //获取已切分好的【非计划停机】
            foreach (DataRow row in SplitTab.Rows)
            {
                if (row["downcode"].ToString() != string.Empty && row["downcode"].ToString().Contains("U"))
                    UnschduleTab.Rows.Add(row.ItemArray);
            }

            foreach (DataRow row in UnschduleTab.Rows)
            {
                lstUnschedule.Add(new UnScheduleDownTime
                {
                    Id = CommonMethod.SafeGetIntFromObj(row["id"], 0),
                    DefectCategoryId = CommonMethod.SafeGetIntFromObj(row["defectcategoryid"], 0),
                    StartTime = CommonMethod.SafeGetDateTimeFromObj(row["starttime"]),
                    EndTime = CommonMethod.SafeGetDateTimeFromObj(row["endtime"]),
                    ReactionTime = CommonMethod.SafeGetDateTimeFromObj(row["reactiontime"]),
                    ActionTime = CommonMethod.SafeGetDateTimeFromObj(row["actiontime"]),
                    EmployeeId = CommonMethod.SafeGetIntFromObj(row["employeeid"], 0),
                    StationId = CommonMethod.SafeGetIntFromObj(row["stationid"], 0),
                    IsChangedPart = CommonMethod.SafeGetBooleanFromObj(row["ischangedpart"]),
                });
                Id++;
            }

            return lstUnschedule;
        }
    }
}
