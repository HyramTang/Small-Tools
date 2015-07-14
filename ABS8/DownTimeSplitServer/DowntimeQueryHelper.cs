using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AJ.Andon.Entity;
using AJ.Andon.Entity.Dictionary;
using AJ.Andon.Entity.Production;

namespace DownTimeSplitService
{
    /// <summary>
    /// 查询【计划】和【非计划】的停机时间
    /// </summary>
    public class DowntimeQueryHelper
    {
        /// <summary>
        /// 查询【计划】停机时间
        /// </summary>
        /// <param name="p_line"></param>
        /// <param name="p_startTime"></param>
        /// <param name="p_endTime"></param>
        /// <returns></returns>
        public List<ScheduleDownTime> QueryScheduleDowntime(Line p_line, DateTime p_startTime, DateTime p_endTime)
        {
            //1.查询分四个步骤 开始时间、结束时间、大于开始小于结束 、结束时间为null

            string sqlstarttime = @"select [tbPro_ScheduleDownTime].* from [dbo].[tbPro_ScheduleDownTime] 
                where StationId in(select Id from [dbo].[tbDic_Station] where LineId={0})
                 and StartTime>='{1}' and StartTime<='{2}'";
            sqlstarttime = string.Format(sqlstarttime, p_line.Id, p_startTime, p_endTime);

            List<ScheduleDownTime> lstStartime = DALLib<ScheduleDownTime>.DataAccess.GetSome(sqlstarttime);


            string sqlendtime = @"select [tbPro_ScheduleDownTime].* from [dbo].[tbPro_ScheduleDownTime]
              where StationId in(select Id from [dbo].[tbDic_Station] where LineId={0})
             and EndTime>='{1}' and EndTime<='{2}'";

            sqlendtime = string.Format(sqlendtime, p_line.Id, p_startTime, p_endTime);


            List<ScheduleDownTime> lstEndtime = DALLib<ScheduleDownTime>.DataAccess.GetSome(sqlendtime);


            string sqllessstartlargerthanend = @"select [tbPro_ScheduleDownTime].* from [dbo].[tbPro_ScheduleDownTime] 
             where StationId in(select Id from [dbo].[tbDic_Station] where LineId={0})
                       and EndTime>='{1}'   and  StartTime<='{2}'";

            sqllessstartlargerthanend = string.Format(sqllessstartlargerthanend, p_line.Id, p_endTime, p_startTime);

            List<ScheduleDownTime> lstLessLarge = DALLib<ScheduleDownTime>.DataAccess.GetSome(sqllessstartlargerthanend);


            string sqlEndtimeisnull = @"select [tbPro_ScheduleDownTime].* from [dbo].[tbPro_ScheduleDownTime]
                          where StationId in(select Id from [dbo].[tbDic_Station] where LineId={0})
                           and  EndTime  is null ";


            sqlEndtimeisnull = string.Format(sqlEndtimeisnull, p_line.Id);

            List<ScheduleDownTime> lstEndTimeinull = DALLib<ScheduleDownTime>.DataAccess.GetSome(sqlEndtimeisnull);


            Dictionary<int, ScheduleDownTime> dic_sd = new Dictionary<int, ScheduleDownTime>();

            if (lstStartime == null) { lstStartime = new List<ScheduleDownTime>(); }
            if (lstEndtime == null) { lstEndtime = new List<ScheduleDownTime>(); }
            if (lstLessLarge == null) { lstLessLarge = new List<ScheduleDownTime>(); }
            if (lstEndTimeinull == null) { lstEndTimeinull = new List<ScheduleDownTime>(); }

            foreach (ScheduleDownTime sd in lstStartime)
            {
                if (dic_sd.ContainsKey(sd.Id))
                {

                    continue;
                }
                else
                {

                    dic_sd.Add(sd.Id, sd);
                }

            }

            foreach (ScheduleDownTime sd in lstEndtime)
            {
                if (dic_sd.ContainsKey(sd.Id))
                {

                    continue;
                }
                else
                {

                    dic_sd.Add(sd.Id, sd);
                }

            }


            foreach (ScheduleDownTime sd in lstEndTimeinull)
            {
                if (sd.StartTime >= p_endTime)
                {
                    continue;

                }


                if (dic_sd.ContainsKey(sd.Id))
                {

                    continue;
                }
                else
                {

                    dic_sd.Add(sd.Id, sd);
                }

            }


            foreach (ScheduleDownTime sd in lstLessLarge)
            {
                if (dic_sd.ContainsKey(sd.Id))
                {

                    continue;
                }
                else
                {

                    dic_sd.Add(sd.Id, sd);
                }

            }



            // 去除头尾 

            List<ScheduleDownTime> lstResult = new List<ScheduleDownTime>();

            foreach (int sdid in dic_sd.Keys)
            {
                ScheduleDownTime sd = dic_sd[sdid];
                if (sd.StartTime < p_startTime)
                {

                    sd.StartTime = p_startTime;
                }

                if (sd.EndTime > p_endTime || sd.EndTime==DateTime.MinValue)
                {

                    sd.EndTime = p_endTime;
                }
                lstResult.Add(sd);
            }

            return lstResult;

        }

        /// <summary>
        /// 查询【非计划】停机时间
        /// </summary>
        /// <param name="p_line"></param>
        /// <param name="p_startTime"></param>
        /// <param name="p_endTime"></param>
        /// <returns></returns>
        public List<UnScheduleDownTime> QueryUnScheduleDowntime(Line p_line, DateTime p_startTime, DateTime p_endTime)
        {
            //去除所有的换型提醒(不能把换型提醒计入报表)
            string SqlWhere = " AND DefectCategoryId NOT IN (SELECT Id FROM tbDic_DefectCategory WHERE ClientDisplay LIKE N'%提醒%')";

            string sqlstarttime = @"select * from [dbo].[tbPro_UnScheduleDownTime] 
            where StationId in(select Id from [dbo].[tbDic_Station] where LineId={0})       
            and [tbPro_UnScheduleDownTime].StartTime >='{1}' and [tbPro_UnScheduleDownTime].StartTime<='{2}'"+SqlWhere+"";
            sqlstarttime = string.Format(sqlstarttime, p_line.Id, p_startTime, p_endTime);

            List<UnScheduleDownTime> lstStarttime = DALLib<UnScheduleDownTime>.DataAccess.GetSome(sqlstarttime);

            string sqlendtime = @"select * from [dbo].[tbPro_UnScheduleDownTime] 
            where StationId in(select Id from [dbo].[tbDic_Station] where LineId={0})          
            and [tbPro_UnScheduleDownTime].EndTime >='{1}' and [tbPro_UnScheduleDownTime].EndTime<='{2}'" + SqlWhere + "";
            sqlendtime = string.Format(sqlendtime, p_line.Id, p_startTime, p_endTime);
            List<UnScheduleDownTime> lstEndtime = DALLib<UnScheduleDownTime>.DataAccess.GetSome(sqlendtime);



            string sqllesslarge = @"select * from [dbo].[tbPro_UnScheduleDownTime] 
            where StationId in(select Id from [dbo].[tbDic_Station] where LineId={0})
            and [tbPro_UnScheduleDownTime].StartTime >='{1}' and [tbPro_UnScheduleDownTime].EndTime<='{2}'" + SqlWhere + "";
            sqllesslarge = string.Format(sqllesslarge, p_line.Id, p_startTime, p_endTime);
            List<UnScheduleDownTime> lstLessLarge = DALLib<UnScheduleDownTime>.DataAccess.GetSome(sqllesslarge);

            string sqlendtimeisnull = @"select * from [dbo].[tbPro_UnScheduleDownTime] 
            where StationId in(select Id from [dbo].[tbDic_Station] where LineId={0})      
            and  [tbPro_UnScheduleDownTime].EndTime is null " + SqlWhere + "";

            sqlendtimeisnull = string.Format(sqlendtimeisnull, p_line.Id);
            List<UnScheduleDownTime> lstEndtimeisnull = DALLib<UnScheduleDownTime>.DataAccess.GetSome(sqlendtimeisnull);

            if (lstStarttime == null) { lstStarttime = new List<UnScheduleDownTime>(); }
            if (lstEndtime == null) { lstEndtime = new List<UnScheduleDownTime>(); }
            if (lstEndtimeisnull == null) { lstEndtimeisnull = new List<UnScheduleDownTime>(); }
            if (lstLessLarge == null) { lstLessLarge = new List<UnScheduleDownTime>(); }

            Dictionary<int, UnScheduleDownTime> dic_ud = new Dictionary<int, UnScheduleDownTime>();

            foreach (UnScheduleDownTime ud in lstStarttime)
            {
                if (dic_ud.ContainsKey(ud.Id))
                {
                    continue;
                }
                else
                {

                    dic_ud.Add(ud.Id, ud);
                }
            }


            foreach (UnScheduleDownTime ud in lstEndtime)
            {
                if (dic_ud.ContainsKey(ud.Id))
                {
                    continue;
                }
                else
                {

                    dic_ud.Add(ud.Id, ud);
                }
            }



            foreach (UnScheduleDownTime ud in lstEndtimeisnull)
            {
                if (ud.StartTime >= p_endTime)
                {
                    continue;
                }
                if (dic_ud.ContainsKey(ud.Id))
                {
                    continue;
                }
                else
                {

                    dic_ud.Add(ud.Id, ud);
                }
            }

            foreach (UnScheduleDownTime ud in lstLessLarge)
            {
                if (dic_ud.ContainsKey(ud.Id))
                {
                    continue;
                }
                else
                {

                    dic_ud.Add(ud.Id, ud);
                }
            }


            List<UnScheduleDownTime> lstResult = new List<UnScheduleDownTime>();
            foreach (int udid in dic_ud.Keys)
            {
                UnScheduleDownTime ud = dic_ud[udid];
                if (ud.StartTime < p_startTime)
                {
                    ud.StartTime = p_startTime;
                }

                if (ud.EndTime > p_endTime)
                {
                    ud.EndTime = p_endTime;
                }
                //如果停机还未结束，把当前的时间设置为结束时间
                if (ud.EndTime == DateTime.MinValue)
                {
                    ud.EndTime = p_endTime;
                    //GlobalVars.IsUndoneFlowProduction = GlobalVars.LastFlowProduction;
                    if (!GlobalVars.dicIsUndoneFlowProduction.ContainsKey(p_line.Id))
                        GlobalVars.dicIsUndoneFlowProduction.Add(p_line.Id, GlobalVars.LastFlowProduction);
                }
                lstResult.Add(ud);

            }
            return lstResult;
        }
    }
}
