using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using AJ.Andon.Entity.Production;

namespace DownTimeSplitService
{
    public class DateTimeCalcHelper
    {
        public static DataTable GetSortTable(List<ScheduleDownTime> lstsd, List<UnScheduleDownTime> lstud, DateTime dtstart, DateTime dtend)
        {
            DataTable dtSortTable = new DataTable();
            dtSortTable.Columns.Add("Id", typeof(int));
            dtSortTable.Columns.Add("dtstart", typeof(DateTime));
            dtSortTable.Columns.Add("dtend", typeof(DateTime));
            dtSortTable.Columns.Add("type", typeof(string));

            //计划停机为plan  非计划停机为unplan
            if (lstsd == null)
            {
                lstsd = new List<ScheduleDownTime>();
            }
            if (lstud == null)
            {
                lstud = new List<UnScheduleDownTime>();
            }

            #region 把时间统一调整为这个区间段内
            for (int i = 0; i < lstud.Count; i++)
            {
                DateTime _dtStart = lstud[i].StartTime;
                DateTime _dtEnd = lstud[i].EndTime;
                if (_dtEnd > dtend)
                {
                    _dtEnd = dtend;

                }

                if (_dtStart < dtstart)
                {
                    _dtStart = dtstart;
                }

                if (_dtStart > dtend)
                {
                    _dtStart = dtend;

                }

                if (_dtEnd < dtstart)
                {
                    _dtEnd = dtstart;

                }


                lstud[i].StartTime = _dtStart;
                lstud[i].EndTime = _dtEnd;


            }

            for (int i = 0; i < lstsd.Count; i++)
            {

                DateTime _dtStart = lstsd[i].StartTime;
                DateTime _dtEnd = lstsd[i].EndTime;
                if (_dtEnd > dtend)
                {
                    _dtEnd = dtend;

                }

                if (_dtStart < dtstart)
                {
                    _dtStart = dtstart;
                }

                if (_dtStart > dtend)
                {
                    _dtStart = dtend;

                }

                if (_dtEnd < dtstart)
                {
                    _dtEnd = dtstart;

                }


                lstsd[i].StartTime = _dtStart;
                lstsd[i].EndTime = _dtEnd;


            }


            #endregion


            for (int i = 0; i < lstud.Count; i++)
            {
                UnScheduleDownTime ud = lstud[i];
                DataRow row = dtSortTable.NewRow();
                row["Id"] = ud.Id;
                row["dtstart"] = ud.StartTime;
                row["dtend"] = ud.EndTime;
                row["type"] = "unplan";
                dtSortTable.Rows.Add(row);
            }


            for (int i = 0; i < lstsd.Count; i++)
            {
                ScheduleDownTime sd = lstsd[i];
                DataRow row = dtSortTable.NewRow();
                row["Id"] = sd.Id;
                row["dtstart"] = sd.StartTime;
                row["dtend"] = sd.EndTime;
                row["type"] = "plan";
                dtSortTable.Rows.Add(row);
            }

            #region 正式进行排序
            DataRow[] sortedRows = dtSortTable.Select("", " dtstart asc");

            DateTime _dtpreStart = DateTime.MinValue;
            DateTime _dtpreEnd = DateTime.MinValue;

            DateTime _dtcurrentStart = DateTime.MinValue;
            DateTime _dtcurrentEnd = DateTime.MinValue;

            DataTable dtnewsort = dtSortTable.Clone();

            for (int i = 0; i < sortedRows.Length; i++)
            {
                if (i == 0)
                {
                    _dtpreStart = Convert.ToDateTime(sortedRows[i]["dtstart"]);
                    _dtpreEnd = Convert.ToDateTime(sortedRows[i]["dtend"]);
                    dtnewsort.ImportRow(sortedRows[i]);
                    continue;
                }

                _dtcurrentStart = Convert.ToDateTime(sortedRows[i]["dtstart"]);
                _dtcurrentEnd = Convert.ToDateTime(sortedRows[i]["dtend"]);



                if (_dtpreEnd > _dtcurrentStart)//上次结束时间大于本次开始时间
                {

                    if (_dtpreEnd >= _dtcurrentEnd)//上次纪录完全包括了本次纪录的时间，所以不需要纪录该条纪录了。
                    {
                        _dtpreStart = _dtcurrentEnd;
                        continue;
                    }

                    _dtcurrentStart = _dtpreEnd;

                    sortedRows[i]["dtstart"] = _dtcurrentStart;

                    _dtpreStart = _dtcurrentStart;
                    _dtpreEnd = _dtcurrentEnd;
                    dtnewsort.ImportRow(sortedRows[i]);

                }
                else
                { //上次结束时间小于等于本次的开始时间 直接取这条纪录就好了
                    _dtpreEnd = _dtcurrentEnd;
                    _dtpreStart = _dtcurrentStart;
                    dtnewsort.ImportRow(sortedRows[i]);
                }
            }
            #endregion



            return dtnewsort;
        }


        public static void SortDowntimeList(List<ScheduleDownTime> sds, List<UnScheduleDownTime> uds, DateTime dtStart, DateTime dtEnd, out List<ScheduleDownTime> outsd, out List<UnScheduleDownTime> outud)
        {

            DataTable SortedTable = GetSortTable(sds, uds, dtStart, dtEnd);
            List<UnScheduleDownTime> lstoutUd = new List<UnScheduleDownTime>();
            List<ScheduleDownTime> lstoutSd = new List<ScheduleDownTime>();

            for (int i = 0; i < SortedTable.Rows.Count; i++)
            {
                DataRow row = SortedTable.Rows[i];
                int Id = Convert.ToInt32(row["Id"]);
                DateTime _dtstart = Convert.ToDateTime(row["dtstart"]);
                DateTime _dtend = Convert.ToDateTime(row["dtend"]);
                string typename = Convert.ToString(row["type"]);

                switch (typename)
                {
                    case "plan":
                        ScheduleDownTime sd = sds.Find(p => p.Id == Id);
                        sd.StartTime = _dtstart;
                        sd.EndTime = _dtend;
                        lstoutSd.Add(sd);

                        break;
                    case "unplan":
                        UnScheduleDownTime ud = uds.Find(p => p.Id == Id);
                        ud.StartTime = _dtstart;
                        ud.EndTime = _dtend;
                        lstoutUd.Add(ud);
                        break;

                }

            }
            outsd = lstoutSd;
            outud = lstoutUd;
        }
    }
}
