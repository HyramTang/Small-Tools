using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using AJ.Andon.Entity;
using AJ.Andon.Entity.Dictionary;
using AJ.Andon.Entity.Production;
using AJ.Andon.Entity.Report;
using DMES.Utility;

namespace DownTimeSplitService
{
    public class ServiceThread
    {
        public static ServiceThread m_instance;
        public bool isRun = false;
        public DowntimeQueryHelper queryhelper;
        public TimeSplitHelper timehelper;
        public ReportHelper reporthelper;
        DownTimeSplitLogic dtsLogic;
        public ServiceThread()
        {
            queryhelper = new DowntimeQueryHelper();
            timehelper = new TimeSplitHelper();
            reporthelper = new ReportHelper();
            dtsLogic = new DownTimeSplitLogic();
        }
        public static ServiceThread GetInstance()
        {
            if (m_instance == null)
            {
                m_instance = new ServiceThread();
            }
            return m_instance;
        }
        public void Begin()
        {
            isRun = true;
            try
            {
                Doit();
            }
            catch (Exception ex)
            {
                DMES.Utility.Logger.Log4netHelper.Error(ex);
            }
        }

        public void End()
        {
            isRun = false;
        }

        private void Doit()
        {
            while (isRun)
            {
                DateTime dtNow = DateTime.Now;
                DateTime m__dtBegin = new DateTime();
                DateTime dtNull = new DateTime();
                if (dtNow.Hour == 7 && (dtNow.Minute > 45 && dtNow.Minute < 50))
                {
                    m__dtBegin = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dtNow.Hour, 0, 0);
                    dtNull = new DateTime(dtNow.AddDays(-1).Year, dtNow.AddDays(-1).Month, dtNow.AddDays(-1).Day, 19, 0, 0);
                }
                else if (dtNow.Hour == 19 && (dtNow.Minute > 45 && dtNow.Minute < 50))
                {
                    m__dtBegin = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dtNow.Hour, 0, 0);
                    dtNull = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 7, 0, 0);
                }
                else
                {
                    Thread.Sleep(10000);
                    continue;
                }

                Dictionary<int, DateTime> dic_time = new Dictionary<int, DateTime>();
                foreach (Line line in GlobalVars.lines)
                {
                    DateTime dtCurrentTime = m__dtBegin;

                    List<ScheduleDownTime> lstScheduleDowntime = new List<ScheduleDownTime>(); //所有的计划停机
                    List<UnScheduleDownTime> lstUnscheDowntime = new List<UnScheduleDownTime>();//所有的非计划停机

                    List<FlowDowntime> lstFlowDowntime = new List<FlowDowntime>();//所有的停机事件
                    List<FlowProduction> lstFlowProduction = new List<FlowProduction>(); //所有的生产事件

                    List<Station> lstStation = new List<Station>();
                    lstStation = DALLib<Station>.DataAccess.GetSome(string.Format("select * from tbDic_Station where LineId={0}", line.Id));

                    //获取最后一次的纪录停机时间
                    FlowProduction flowproduction = DALLib<FlowProduction>.DataAccess.GetOneBySQL(string.Format("select top 1  * from tbFlowProduction where LineId={0}  order by RealStartTime desc ", line.Id));
                    if (flowproduction == null)
                    {
                        flowproduction = new FlowProduction();
                        //flowproduction.RealEndTime = DateTime.Now.AddHours(-12);
                        flowproduction.RealEndTime = dtNull;
                    }
                    GlobalVars.LastFlowProduction = flowproduction;
                    if (dic_time.ContainsKey(line.Id))
                    {
                        dic_time[line.Id] = flowproduction.RealEndTime;
                    }
                    else
                    {
                        dic_time.Add(line.Id, flowproduction.RealEndTime);
                    }

                    //lstScheduleDowntime = queryhelper.QueryScheduleDowntime(line, flowproduction.RealEndTime, dtCurrentTime);
                    //lstUnscheDowntime = queryhelper.QueryUnScheduleDowntime(line, flowproduction.RealEndTime, dtCurrentTime);

                    DataTable IsSplitedTab = new DataTable();
                    FlowProduction lastFlowProduction;
                    if (GlobalVars.dicIsUndoneFlowProduction != null && GlobalVars.dicIsUndoneFlowProduction.TryGetValue(line.Id, out lastFlowProduction) && lastFlowProduction != null)
                    {
                        string SqlDeleteFlowProduction = "DELETE FROM tbFlowProduction WHERE LineId=" + line.Id + " AND RealStartTime>='" + lastFlowProduction.RealEndTime + "' AND RealStartTime<='" + dtCurrentTime + "'";
                        DALLib<object>.DataAccess.ExecuteNonQuery(SqlDeleteFlowProduction);
                        GlobalVars.dicIsUndoneFlowProduction.Remove(line.Id);
                        IsSplitedTab = dtsLogic.GetDownTime(line.Id, lastFlowProduction.RealEndTime, dtCurrentTime, out lstScheduleDowntime, out  lstUnscheDowntime);
                    }
                    else
                        IsSplitedTab = dtsLogic.GetDownTime(line.Id, flowproduction.RealEndTime, dtCurrentTime, out lstScheduleDowntime, out  lstUnscheDowntime);


                    DataTable dtResult = new DataTable();
                    dtResult.Columns.Add("Id", typeof(Int32));
                    dtResult.Columns.Add("dtStart", typeof(DateTime));
                    dtResult.Columns.Add("dtEnd", typeof(DateTime));
                    dtResult.Columns.Add("dtype", typeof(String));

                    for (int i = 0; i < lstScheduleDowntime.Count; i++)
                    {
                        DataRow row = dtResult.NewRow();
                        row["Id"] = lstScheduleDowntime[i].Id;
                        row["dtStart"] = lstScheduleDowntime[i].StartTime;
                        row["dtEnd"] = lstScheduleDowntime[i].EndTime;
                        row["dtype"] = "p";
                        dtResult.Rows.Add(row);
                    }

                    for (int i = 0; i < lstUnscheDowntime.Count; i++)
                    {
                        DataRow row = dtResult.NewRow();
                        row["Id"] = lstUnscheDowntime[i].Id;
                        row["dtStart"] = lstUnscheDowntime[i].StartTime;
                        row["dtEnd"] = lstUnscheDowntime[i].EndTime;
                        row["dtype"] = "u";
                        dtResult.Rows.Add(row);
                    }

                    DataRow[] sortedrows = dtResult.Select("", "dtStart asc");

                    #region 注释预留
                    //DateTime dtLastStart = DateTime.Now;
                    //DateTime dtLastEnd = DateTime.Now;
                    //DateTime dtCurrentStart = DateTime.Now;
                    //DateTime dtCurrentEnd = DateTime.Now;

                    //for (int i = 0; i < sortedrows.Length; i++)
                    //{
                    //    if (i == 0)
                    //    {
                    //        dtLastStart = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(sortedrows[i]["dtStart"]);
                    //        dtLastEnd = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(sortedrows[i]["dtEnd"]);
                    //        continue;
                    //    }

                    //    dtCurrentStart = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(sortedrows[i]["dtStart"]);
                    //    dtCurrentEnd = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(sortedrows[i]["dtEnd"]);

                    //    if (dtCurrentStart > dtLastEnd) //本次开始时间大于上次结束时间
                    //    {
                    //        dtLastStart = dtCurrentStart;
                    //        dtLastEnd = dtCurrentEnd;
                    //        continue;
                    //    }
                    //    if (dtCurrentStart < dtLastEnd)
                    //    {
                    //        if (dtCurrentEnd < dtLastEnd)
                    //        {
                    //            sortedrows[i]["Id"] = 0;
                    //            dtLastStart = dtCurrentEnd;
                    //        }
                    //        else
                    //        {
                    //            sortedrows[i]["dtStart"] = dtLastEnd;
                    //            dtLastStart = dtLastEnd;
                    //            dtLastEnd = dtCurrentEnd;
                    //        }
                    //    }
                    //}
                    #endregion

                    DataTable _dtResultTable = dtResult.Clone();

                    #region 存储FlowDownTime内容
                    for (int i = 0; i < sortedrows.Length; i++)
                    {
                        int DowntimeId = DMES.Utility.CommonMethod.SafeGetIntFromObj(sortedrows[i]["Id"], 0);
                        DateTime dtStart = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(sortedrows[i]["dtStart"]);
                        DateTime dtEnd = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(sortedrows[i]["dtEnd"]);

                        if (dtEnd <= new DateTime(1901, 1, 1, 0, 0, 0))
                        {
                            dtEnd = DateTime.Now;
                            sortedrows[i]["dtEnd"] = dtEnd;
                        }
                        string dtye = DMES.Utility.CommonMethod.SafeGetStringFromObj(sortedrows[i]["dtype"]);

                        if (DowntimeId == 0)
                        {
                            continue;
                        }
                        FlowDowntime _flowdowntime = new FlowDowntime();

                        if (dtye == "p")
                        {
                            _flowdowntime.DowntimeType = "p";
                            _flowdowntime.StartTime = dtStart;
                            _flowdowntime.EndTime = dtEnd;
                            _flowdowntime.LineId = line.Id;

                            double span = _flowdowntime.EndTime.Subtract(_flowdowntime.StartTime).TotalMinutes;
                            _flowdowntime.PlandDowntimeSpan = DMES.Utility.CommonMethod.SafeGetDecimalFromObject(span, 0);
                            _flowdowntime.LineName = line.LineName;
                            ScheduleDownTime sd = lstScheduleDowntime.Find(p => p.Id == DowntimeId);

                            ScheduleDownType sdtype = DALLib<ScheduleDownType>.DataAccess.GetOne("Id", sd.ScheduleDownTypeId);
                            Station station = lstStation.Find(p => p.Id == sd.StationId);
                            _flowdowntime.DowntimeStationId = sd.StationId;
                            _flowdowntime.DowntimeStation = station.StationCode;
                        }
                        else if (dtye == "u")
                        {
                            _flowdowntime.DowntimeType = "u";
                            _flowdowntime.StartTime = dtStart;
                            _flowdowntime.EndTime = dtEnd;
                            _flowdowntime.LineId = line.Id;
                            _flowdowntime.LineName = line.LineName;
                            UnScheduleDownTime ud = lstUnscheDowntime.Find(p => p.Id == DowntimeId);
                            DefectCategory defect = DALLib<DefectCategory>.DataAccess.GetOne("Id", ud.DefectCategoryId);
                            Station station = lstStation.Find(p => p.Id == ud.StationId);
                            _flowdowntime.DowntimeStationId = station.Id;
                            double span = _flowdowntime.EndTime.Subtract(_flowdowntime.StartTime).TotalMinutes;
                            _flowdowntime.UnPlandDowntimeSpan = DMES.Utility.CommonMethod.SafeGetDecimalFromObject(span, 0);
                            _flowdowntime.DowntimeStation = station.StationCode;
                        }
                        lstFlowDowntime.Add(_flowdowntime);
                        _dtResultTable.ImportRow(sortedrows[i]);
                    }

                    foreach (FlowDowntime dttime in lstFlowDowntime)
                    {
                        DALLib<FlowDowntime>.DataAccess.SaveOne(dttime);
                    }
                    #endregion

                    #region 存储FlowProduction内容
                    FlowProduction LastOneProduction = null;
                    for (int i = 0; i < IsSplitedTab.Rows.Count; i++)
                    {
                        int DowntimeId = DMES.Utility.CommonMethod.SafeGetIntFromObj(IsSplitedTab.Rows[i]["id"], 0);

                        DateTime dtStart = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(IsSplitedTab.Rows[i]["starttime"]);
                        DateTime dtEnd = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(IsSplitedTab.Rows[i]["endtime"]);
                        string dtye = DMES.Utility.CommonMethod.SafeGetStringFromObj(IsSplitedTab.Rows[i]["downcode"]);

                        FlowProduction currentproduction = new FlowProduction();
                        currentproduction.RealStartTime = dtStart;
                        currentproduction.RealEndTime = dtEnd;
                        currentproduction.LineId = line.Id;

                        if (dtye.Contains("P"))
                        {
                            ScheduleDownTime sd = lstScheduleDowntime.Find(p => p.Id == DowntimeId);
                            ScheduleDownType scheduletype = DALLib<ScheduleDownType>.DataAccess.GetOne("Id", sd.ScheduleDownTypeId);
                            Station station = lstStation.Find(p => p.Id == sd.StationId);
                            currentproduction.PlanedDownTimeCode = scheduletype.ScheduleDownTypeName;
                            currentproduction.PlanedDowntimeDesc = scheduletype.ClientDisplay;
                            currentproduction.PlanedDownTimeID = scheduletype.Id;
                            currentproduction.Person = "P";
                            currentproduction.PlanedDowntimeSpan = DMES.Utility.CommonMethod.SafeGetDecimalFromObject(dtEnd.Subtract(dtStart).TotalMinutes, 4);
                            currentproduction.ProductOutput = 0;
                            currentproduction.ProductionOEE = 0;
                            currentproduction.DownTimeStationId = station.Id;
                            currentproduction.DownTimeStation = station.StationCode;
                            currentproduction.LineId = line.Id;
                        }
                        else if (dtye.Contains("U"))
                        {
                            UnScheduleDownTime ud = lstUnscheDowntime.Find(p => p.Id == DowntimeId);
                            DefectCategory defect = DALLib<DefectCategory>.DataAccess.GetOne("Id", ud.DefectCategoryId);
                            Station station = lstStation.Find(p => p.Id == ud.StationId);
                            currentproduction.Person = "U";
                            currentproduction.UnplanDowntimeId = ud.Id;
                            currentproduction.UnplanedDowntimeDesc = defect.ClientDisplay;
                            currentproduction.UnplanDowntimeCode = defect.DefectCategoryName;
                            currentproduction.UnplanedDowntimeSpan = DMES.Utility.CommonMethod.SafeGetDecimalFromObject(dtEnd.Subtract(dtStart).TotalMinutes, 4);
                            currentproduction.DownTimeStation = station.StationCode;
                            currentproduction.DownTimeStationId = station.Id;
                            currentproduction.LineId = line.Id;
                        }
                        else if (dtye.Contains("product"))
                        {
                            //写入一条产量纪录
                            currentproduction.Person = "product";
                            currentproduction.RealProductTime = CommonMethod.SafeGetDecimalFromObject(dtEnd.Subtract(dtStart).TotalMinutes, 4);
                            lstFlowProduction.Add(currentproduction);
                        }
                        LastOneProduction = currentproduction;

                        lstFlowProduction.Add(currentproduction);
                    }

                    for (int i = 0; i < lstFlowProduction.Count; i++)
                    {
                        #region
                        //lstFlowProduction[i].LineId = line.Id;

                        //DateTime _dtBegin = lstFlowProduction[i].RealStartTime;
                        //DateTime _dtEnd = lstFlowProduction[i].RealEndTime;

                        //if (_dtBegin.Hour != _dtEnd.Hour)
                        //{
                        //    int hous = _dtEnd.Hour - _dtBegin.Hour;
                        //    DateTime _dtStarttime = _dtBegin;

                        //    if (hous < 0)
                        //    {
                        //        hous += 24;
                        //    }

                        //    for (int j = 0; j <= hous; j++)
                        //    {
                        //        DateTime dttempEnd = new DateTime(_dtStarttime.AddHours(1).Year, _dtStarttime.AddHours(1).Month,
                        //            _dtStarttime.AddHours(1).Day, _dtStarttime.AddHours(1).Hour, 0, 0);
                        //        if (dttempEnd >= _dtEnd)
                        //        {
                        //            dttempEnd = _dtEnd;
                        //        }

                        //        FlowProduction t_production = lstFlowProduction[i];
                        //        t_production.Id = 0;
                        //        t_production.RealStartTime = _dtStarttime;
                        //        t_production.RealEndTime = dttempEnd;
                        //        _dtStarttime = dttempEnd;
                        //        if (t_production.Person == "P")
                        //        {
                        //            t_production.PlanedDowntimeSpan = Math.Round(DMES.Utility.CommonMethod.SafeGetDecimalFromObject(t_production.RealEndTime.Subtract(t_production.RealStartTime).TotalMinutes, 0), 4);
                        //        }
                        //        else if (t_production.Person == "U")
                        //        {
                        //            t_production.UnplanedDowntimeSpan = Math.Round(DMES.Utility.CommonMethod.SafeGetDecimalFromObject(t_production.RealEndTime.Subtract(t_production.RealStartTime).TotalMinutes, 0), 4);
                        //        }
                        //        DALLib<FlowProduction>.DataAccess.SaveOne(t_production);
                        //    }
                        //    continue;
                        //}
                        #endregion
                        DALLib<FlowProduction>.DataAccess.SaveOne(lstFlowProduction[i]);
                    }
                    #endregion
                }


                #region 把这个时间内的产量纪录替换成为真正的产量纪录，里面可能包含换型。
                //foreach (Line line in GlobalVars.lines)
                //{

                //    DateTime dtCurrentTime = m__dtBegin;

                //    //string sqlflowproducts = @"select * from [dbo].[tbFlowProduction] where LineId={2}
                //    //and RealStartTime between '{0}' and  '{1}'
                //    //and Person='product' order by  RealStartTime asc ";
                //    sqlflowproducts = string.Format(sqlflowproducts, dic_time[line.Id], dtCurrentTime, line.Id);

                //    List<FlowProduction> lstWaitSpitProution = DALLib<FlowProduction>.DataAccess.GetSome(sqlflowproducts);
                //    if (lstWaitSpitProution == null)
                //    {
                //        lstWaitSpitProution = new List<FlowProduction>();
                //    }

                //    //获取上次最后一条产量纪录
                //    //string sqllast_productime = @"SELECT top 1  *  
                //    //      FROM [dbo].[tbFlowProduction]
                //    //      where Person='product' and LineId={0} order by RealStartTime desc ";

                //    sqllast_productime = string.Format(sqllast_productime, line.Id);

                //    FlowProduction production = DALLib<FlowProduction>.DataAccess.GetOneBySQL(sqllast_productime);
                //    DateTime dt_last = DateTime.MinValue;
                //    if (production != null)
                //    {
                //        dt_last = production.RealEndTime;
                //    }

                //    for (int i = 0; i < lstWaitSpitProution.Count; i++)
                //    {
                //        string stationlocations = reporthelper.GetStationLocations(line.Id);
                //        //MesFactory.GetInstance().DoProduction(lstWaitSpitProution[i].RealStartTime, lstWaitSpitProution[i].RealEndTime, line.Id, line.LineName);
                //    }

                //    for (int i = 0; i < lstWaitSpitProution.Count; i++)
                //    {
                //        DALLib<FlowProduction>.DataAccess.DeleteOne(lstWaitSpitProution[i].Id);
                //        Thread.Sleep(10);
                //    }
                //}
                #endregion

                //查看下当前时间，如果为19:30 、7:30
                Thread.Sleep(1000 * 60 * 5);
                //Thread.Sleep(10000);

            }
        }
    }
}
