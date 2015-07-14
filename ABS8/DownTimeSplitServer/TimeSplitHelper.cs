using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using AJ.Andon.Entity.Dictionary;
using AJ.Andon.Entity.Production;

namespace DownTimeSplitService
{
    public class TimeSplitHelper
    {
        private static TimeSplitHelper m_timesplithelper;
        public static TimeSplitHelper GetInstance()
        {
            if (m_timesplithelper == null)
            {
                m_timesplithelper = new TimeSplitHelper();
            }
            return m_timesplithelper;
        }

        /// <summary>
        /// 把所有时间放在一个DataTable里然后排序，然后再分割出开始结束时间
        /// </summary>
        /// <param name="lstScheduleDowntime"></param>
        /// <param name="lstUnscheduleTime"></param>
        /// <param name="p_starttime"></param>
        /// <param name="p_endtime"></param>
        /// <returns></returns>
        private DataTable PrepareDateTimeTable(List<ScheduleDownTime> lstScheduleDowntime, List<UnScheduleDownTime> lstUnscheduleTime, DateTime p_starttime, DateTime p_endtime)
        {
            DataTable dtTimeTable = new DataTable();
            dtTimeTable.Columns.Add("Time", typeof(DateTime));

            DataRow p_startrow = dtTimeTable.NewRow();
            DataRow p_endrow = dtTimeTable.NewRow();
            p_startrow["Time"] = p_starttime;
            p_endrow["Time"] = p_endtime;

            dtTimeTable.Rows.Add(p_startrow);
            dtTimeTable.Rows.Add(p_endrow);

            foreach (ScheduleDownTime sd in lstScheduleDowntime)
            {
                DataRow rowstarttime = dtTimeTable.NewRow();
                rowstarttime["Time"] = sd.StartTime;

                DataRow rowendtime = dtTimeTable.NewRow();
                rowendtime["time"] = sd.EndTime;

                dtTimeTable.Rows.Add(rowstarttime);
                dtTimeTable.Rows.Add(rowendtime);
            }

            foreach (UnScheduleDownTime ud in lstUnscheduleTime)
            {
                DataRow rowstarttime = dtTimeTable.NewRow();
                rowstarttime["Time"] = ud.StartTime;

                DataRow rowendtime = dtTimeTable.NewRow();
                rowendtime["time"] = ud.EndTime;

                dtTimeTable.Rows.Add(rowstarttime);
                dtTimeTable.Rows.Add(rowendtime);
            }

            DataTable dtTimeTableClone = dtTimeTable.Clone();
            DataRow[] sortedrows = dtTimeTable.Select("", "Time asc");
            foreach (DataRow row in sortedrows)
                dtTimeTableClone.ImportRow(row);

            dtTimeTable = dtTimeTableClone;

            DataTable dtComareTime = new DataTable();
            dtComareTime.Columns.Add("StartTime", typeof(DateTime));
            dtComareTime.Columns.Add("EndTime", typeof(DateTime));

            for (int i = 0; i < dtTimeTable.Rows.Count; i++)
            {
                if (i == 0)
                    continue;

                DataRow prerow = dtTimeTable.Rows[i - 1];
                DataRow currentrow = dtTimeTable.Rows[i];
                DateTime _Starttime = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(prerow["Time"]);
                DateTime _EndTime = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(currentrow["Time"]);
                if (_Starttime == _EndTime)
                    continue;

                DataRow row = dtComareTime.NewRow();
                row["StartTime"] = _Starttime;
                row["EndTime"] = _EndTime;
                dtComareTime.Rows.Add(row);
            }
            return dtComareTime;
        }


        public DataTable GetTimeTableForABS8(List<ScheduleDownTime> lstScheduleDowntime, List<UnScheduleDownTime> lstUnscheduleTime, Line p_line, DateTime p_dtStart, DateTime p_dtEnd)
        {
            //这里的非计划停机时间是去除掉所有的提醒类的非计划停机和计划停机 换型是自动算出来的
            if (lstScheduleDowntime == null) { lstScheduleDowntime = new List<ScheduleDownTime>(); }
            if (lstUnscheduleTime == null) { lstUnscheduleTime = new List<UnScheduleDownTime>(); }


            DataTable dtResult = new DataTable();
            dtResult.Columns.Add("downcode", typeof(string));
            dtResult.Columns.Add("starttime", typeof(DateTime));
            dtResult.Columns.Add("endtime", typeof(DateTime));
            dtResult.Columns.Add("linename", typeof(string)); //linename
            dtResult.Columns.Add("stationname", typeof(string));//stationname
            dtResult.Columns.Add("lineid", typeof(Int32));
            dtResult.Columns.Add("stationid", typeof(Int32));
            dtResult.Columns.Add("reactiontime", typeof(DateTime));
            dtResult.Columns.Add("actiontime", typeof(DateTime));
            dtResult.Columns.Add("defectcategoryid", typeof(Int32));
            dtResult.Columns.Add("employeeid", typeof(Int32));
            dtResult.Columns.Add("ischangedpart", typeof(bool));
            dtResult.Columns.Add("id", typeof(Int32));
            dtResult.Columns.Add("stationstep", typeof(Int32));
            dtResult.Columns.Add("workstationid", typeof(Int32));
            dtResult.Columns.Add("scheduledowntypeid", typeof(Int32));
            dtResult.Columns.Add("downreason", typeof(string));

            //1.准备好这段期间内的时间顺序

            DataTable dtTimeTable = PrepareDateTimeTable(lstScheduleDowntime, lstUnscheduleTime, p_dtStart, p_dtEnd);

            List<Line> lstLines = AJ.Andon.Entity.DALLib<Line>.DataAccess.GetSome(null);
            List<Station> lstStations = AJ.Andon.Entity.DALLib<Station>.DataAccess.GetSome(null);
            List<ScheduleDownType> lstScheduleDowntimeType = AJ.Andon.Entity.DALLib<ScheduleDownType>.DataAccess.GetSome(null);
            List<DefectCategory> lstDefectCategory = AJ.Andon.Entity.DALLib<DefectCategory>.DataAccess.GetSome(null);


            //2.比较每个时间段内的停机事件

            for (int i = 0; i < dtTimeTable.Rows.Count; i++)
            {
                DataRow timerow = dtTimeTable.Rows[i];
                DateTime _starttime = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(timerow["StartTime"]);
                DateTime _endtime = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(timerow["EndTime"]);

                // 先去查找计划停机

                bool isok = false;
                foreach (ScheduleDownTime sd in lstScheduleDowntime)
                {
                    if (sd.StartTime <= _starttime && sd.EndTime >= _endtime)
                    {
                        isok = true;
                        Station station = lstStations.Find(p => p.Id == sd.StationId);
                        if (station == null) { station = new Station(); }
                        Line line = lstLines.Find(p => p.Id == station.LineId);

                        DataRow row = dtResult.NewRow();
                        row["downcode"] = lstScheduleDowntimeType.Find(p => p.Id == sd.ScheduleDownTypeId).ScheduleDownTypeName;
                        row["starttime"] = _starttime;
                        row["endtime"] = _endtime;
                        if (line != null)
                        {
                            row["linename"] = line.LineName;
                            row["lineid"] = line.Id;
                        }
                        if (station != null)
                        {
                            row["stationname"] = station.StationName;
                            row["stationid"] = station.Id;
                        }

                        row["scheduledowntypeid"] = sd.ScheduleDownTypeId;
                        row["downreason"] = sd.DownReason;
                        row["id"] = sd.Id;
                        dtResult.Rows.Add(row);
                        break;
                    }
                }


                //接着查找非计划停机

                foreach (UnScheduleDownTime ud in lstUnscheduleTime)
                {

                    if (ud.StartTime <= _starttime && ud.EndTime >= _endtime)
                    {
                        isok = true;
                        Station station = lstStations.Find(p => p.Id == ud.StationId);
                        if (station == null) { station = new Station(); }
                        Line line = lstLines.Find(p => p.Id == station.LineId);

                        DataRow row = dtResult.NewRow();

                        row["downcode"] = lstDefectCategory.Find(p => p.Id == ud.DefectCategoryId).DefectCategoryName;
                        row["starttime"] = _starttime;
                        row["endtime"] = _endtime;
                        if (line != null)
                        {
                            row["linename"] = line.LineName;
                            row["lineid"] = line.Id;
                        }
                        if (station != null)
                        {
                            row["stationname"] = station.StationName;
                            row["stationid"] = station.Id;
                            row["stationstep"] = station.StationStep;
                            row["workstationid"] = station.WorkStationId;
                        }

                        row["reactiontime"] = ud.ReactionTime;
                        row["actiontime"] = ud.ActionTime;
                        row["defectcategoryid"] = ud.DefectCategoryId;
                        row["employeeid"] = ud.EmployeeId;
                        row["ischangedpart"] = ud.IsChangedPart;
                        row["id"] = ud.Id;

                        dtResult.Rows.Add(row);
                        //break;
                    }
                }

                if (isok == false)
                {
                    //表示这段时间是在生产中
                    if (_endtime == _starttime)
                    {
                        continue;
                    }
                    DataRow row = dtResult.NewRow();
                    row["downcode"] = "product";
                    row["starttime"] = _starttime;
                    row["endtime"] = _endtime;
                    row["linename"] = p_line.LineName;
                    row["lineid"] = p_line.Id;
                    dtResult.Rows.Add(row);
                }
            }
            return dtResult;
        }

        public DataTable GetABS8SpiltTimeFromTable(DataTable dtSpilt)
        {
            Dictionary<string, DataRow> dic_spilt = new Dictionary<string, DataRow>();
            if (dtSpilt == null)
            {
                dtSpilt = new DataTable();
            }
            DataTable TabSplit = dtSpilt.Clone();
            foreach (DataRow row in dtSpilt.Rows)
            {
                string key = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(row["starttime"]).ToString("yyyy-MM-dd_HH_mm_ss");
                if (dic_spilt.ContainsKey(key))
                {
                    //dic_spilt[key].Add(row);
                    continue;
                }
                else
                {
                    DataRow r = TabSplit.NewRow();
                    r = row;
                    dic_spilt.Add(key, r);
                    TabSplit.Rows.Add(r.ItemArray);
                }
            }
            return TabSplit;
        }
    }
}
