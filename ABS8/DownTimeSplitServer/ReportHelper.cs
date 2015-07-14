using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using AJ.Andon.Entity;
using AJ.Andon.Entity.Dictionary;
using AJ.Andon.Entity.Production;
using AJ.Andon.Entity.Report;
using AJ.Andon.Entity.User;
using DMES.Utility;

namespace DownTimeSplitService
{
    public class ReportHelper
    {
        DownTimeSplitLogic dtsLogic = new DownTimeSplitLogic();
        public enum EDayShiftFlag
        {
            DayShift = 1,
            NightShift = 2

        }

        public void StartServer(DateTime dtStart, DateTime dtEnd)
        {
            List<Line> lstline = DALLib<Line>.DataAccess.GetSome(null);
            string daystr = "";
            EDayFlag dayflag = EDayFlag.Day;

            if (dtStart.Hour == 7 && dtEnd.Hour == 19)
            {
                daystr = dtStart.ToString("yyyyMMdd");
                dayflag = EDayFlag.Day;
            }

            if (dtStart.Hour == 19 && dtEnd.Hour == 7)
            {
                daystr = dtStart.ToString("yyyyMMdd");
                dayflag = EDayFlag.Night;
            }

            try
            {
                foreach (Line line in lstline)
                {
                    LineSynTime syntime = AJ.Andon.Entity.DALLib<LineSynTime>.DataAccess.GetOneBySQL(string.Format(@"select * from tbLineSynTime where  LineId={0} and  DayStr='{1}'", line.Id, daystr));
                    if (syntime == null)
                    {
                        syntime = new LineSynTime();
                        syntime.LineId = line.Id;
                        syntime.LineName = line.LineName;
                    }
                    bool isfinished = false;

                    switch (dayflag)
                    {
                        case EDayFlag.Day:
                            if (!string.IsNullOrEmpty(syntime.DayShift))
                            {
                                isfinished = true;
                            }
                            else
                            {
                                syntime.DayShift = "ok";
                                syntime.DayStr = daystr;
                            }
                            break;
                        case EDayFlag.Night:

                            if (!string.IsNullOrEmpty(syntime.NightShift))
                            {
                                isfinished = true;
                            }
                            else
                            {
                                syntime.NightShift = "ok";
                                syntime.DayStr = daystr;
                            }

                            break;
                    }

                    if (isfinished)
                        continue;

                    DALLib<LineSynTime>.DataAccess.SaveOne(syntime);
                    //先把这里面的换型纪录理出来
                    //写入实际的换型时间
                    List<UnScheduleDownTime> lstCOUds = AnalyzeCOTimWithProduction(line.Id, dtStart, dtEnd);

                    //DateTimeCalcHelper.SortDowntimeList(lstscheduldonwtime, lstunscheduledowntime, dtStart, dtEnd, out outsd, out outud);

                    List<ScheduleDownTime> lstScheduleDowntime = new List<ScheduleDownTime>();
                    List<UnScheduleDownTime> lstUnscheduleDowntime = new List<UnScheduleDownTime>();
                    List<UnScheduleDownTime> UnSlitDT = new List<UnScheduleDownTime>();
                    DataTable IsSplitedTab = dtsLogic.GetDownTime(line.Id, dtStart, dtEnd, out lstScheduleDowntime, out  lstUnscheduleDowntime);
                    UnSlitDT = GetUnSplitDT(line.Id, dtStart, dtEnd);

                    CalcOneLineShiftInfo(dtStart, dtEnd, line, lstScheduleDowntime, lstUnscheduleDowntime, UnSlitDT);
                }
            }
            catch (Exception ee)
            {
                DMES.Utility.Logger.Log4netHelper.Error(ee);
            }
        }
        //核心逻辑
        public void CalcOneLineShiftInfo(DateTime _dtStart, DateTime _dtEnd, Line line, List<ScheduleDownTime> Sds, List<UnScheduleDownTime> Uds, List<UnScheduleDownTime> UnSplitDT)
        {
            EDayFlag dayflag = EDayFlag.Day;
            DayInfo dayinfo = null;

            string daystr = "";
            string monthstr = "";
            if (_dtStart.Hour == 7 && _dtEnd.Hour == 19)
            {
                //白班     
                dayflag = EDayFlag.Day;
                dayinfo = GetDayInfo(_dtEnd, line);
                daystr = _dtEnd.ToString("yyyyMMdd");
                monthstr = _dtEnd.ToString("yyyyMM");
            }

            if (_dtStart.Hour == 19 && _dtEnd.Hour == 7)
            {
                //晚班     
                dayflag = EDayFlag.Night;
                dayinfo = GetDayInfo(_dtEnd, line);
                daystr = _dtStart.ToString("yyyyMMdd");
                monthstr = _dtStart.ToString("yyyyMM");

            }


            //找出这段时间内的产量纪录

            List<Production> lsProduction = GetProduction(line.Id, _dtStart, _dtEnd);
            decimal cotime = DMES.Utility.CommonMethod.SafeGetDecimalFromObject(CalcCOTime(lsProduction), 0);

            //检查下当天的数据是否有生成过

            //如果有，先把他删除掉
            //重新生成当天的指标 

            DailyShiftInfo shiftInfo = new DailyShiftInfo();
            decimal NetProductionTime = lsProduction.Sum(p => p._RealCT * p.RealProductOutput / 60);//【Excel】：PlannedProductionTime(秒除60变成分钟)

            List<ScheduleDownType> lstScheduletype = DALLib<ScheduleDownType>.DataAccess.GetSome(null);
            List<DefectCategory> lstDefectCategary = DALLib<DefectCategory>.DataAccess.GetSome(null);

            Dictionary<string, int> dic_schedultype = new Dictionary<string, int>();
            Dictionary<string, int> dic_defectcategary = new Dictionary<string, int>();

            Dictionary<int, decimal> dic_sd_times = new Dictionary<int, decimal>();
            Dictionary<int, decimal> dic_ud_times = new Dictionary<int, decimal>();


            for (int i = 0; i < lstScheduletype.Count; i++)
            {
                dic_schedultype.Add(lstScheduletype[i].ScheduleDownTypeName.ToLower(), lstScheduletype[i].Id);
                dic_sd_times.Add(lstScheduletype[i].Id, 0);
            }

            for (int i = 0; i < lstDefectCategary.Count; i++)
            {
                dic_defectcategary.Add(lstDefectCategary[i].DefectCategoryName.ToLower(), lstDefectCategary[i].Id);
                dic_ud_times.Add(lstDefectCategary[i].Id, 0);
            }

            for (int i = 0; i < Uds.Count; i++)
            {
                dic_ud_times[Uds[i].DefectCategoryId] += DMES.Utility.CommonMethod.SafeGetDecimalFromObject(Uds[i].EndTime.Subtract(Uds[i].StartTime).TotalMinutes, 0);
            }

            for (int i = 0; i < Sds.Count; i++)
            {
                dic_sd_times[Sds[i].ScheduleDownTypeId] += DMES.Utility.CommonMethod.SafeGetDecimalFromObject(Sds[i].EndTime.Subtract(Sds[i].StartTime).TotalMinutes, 0);

            }

            #region 所有U
            decimal P1 = dic_sd_times[dic_schedultype["p1"]];
            decimal P2 = dic_sd_times[dic_schedultype["p2"]];
            decimal P5 = dic_sd_times[dic_schedultype["p5"]];
            decimal P7 = dic_sd_times[dic_schedultype["p7"]];

            decimal P3 = dic_sd_times[dic_schedultype["p3"]];
            decimal P4 = dic_sd_times[dic_schedultype["p4"]];
            decimal P6 = dic_sd_times[dic_schedultype["p6"]];
            decimal P8 = dic_sd_times[dic_schedultype["p8"]];
            decimal P9 = dic_sd_times[dic_schedultype["p9"]];
            //decimal P10 = dic_sd_times[dic_schedultype["p10"]];

            decimal U1 = dic_ud_times[dic_defectcategary["u1"]];
            decimal U2 = dic_ud_times[dic_defectcategary["u2"]];
            decimal U3 = dic_ud_times[dic_defectcategary["u3"]];
            decimal U4 = dic_ud_times[dic_defectcategary["u4"]];
            decimal U5 = dic_ud_times[dic_defectcategary["u5"]];//这个可以用来当成是换型的时间
            decimal U6 = cotime; //这个就是真正的换型时间
            //decimal U6 = dic_ud_times[dic_defectcategary["u6"]];//换型
            // cotime = U6;
            //cotime = U5;
            //decimal U7 = dic_ud_times[dic_defectcategary["u7"]];
            //decimal U8 = dic_ud_times[dic_defectcategary["u8"]];
            //decimal U9 = dic_ud_times[dic_defectcategary["u9"]];
            decimal U10 = dic_ud_times[dic_defectcategary["u10"]];
            decimal U11 = dic_ud_times[dic_defectcategary["u11"]];
            //decimal U12 = dic_ud_times[dic_defectcategary["u12"]];
            //decimal U13 = dic_ud_times[dic_defectcategary["u13"]];
            //decimal U14 = dic_ud_times[dic_defectcategary["u14"]];
            //decimal U15 = dic_ud_times[dic_defectcategary["u15"]];
            //decimal U16 = dic_ud_times[dic_defectcategary["u16"]];
            decimal U17 = dic_ud_times[dic_defectcategary["u17"]];
            decimal U18 = dic_ud_times[dic_defectcategary["u18"]];
            decimal U19 = dic_ud_times[dic_defectcategary["u19"]];
            decimal U21 = dic_ud_times[dic_defectcategary["u21"]];
            decimal U22 = dic_ud_times[dic_defectcategary["u22"]];
            decimal U23 = dic_ud_times[dic_defectcategary["u23"]];
            decimal U24 = dic_ud_times[dic_defectcategary["u24"]];
            decimal U25 = dic_ud_times[dic_defectcategary["u25"]];
            decimal U26 = dic_ud_times[dic_defectcategary["u26"]];
            decimal U28 = dic_ud_times[dic_defectcategary["u28"]];
            decimal U29 = dic_ud_times[dic_defectcategary["u29"]];
            decimal U30 = dic_ud_times[dic_defectcategary["u30"]];
            decimal U31 = dic_ud_times[dic_defectcategary["u31"]];
            decimal U32 = dic_ud_times[dic_defectcategary["u32"]];
            decimal U33 = dic_ud_times[dic_defectcategary["u33"]];
            decimal U34 = dic_ud_times[dic_defectcategary["u34"]];
            decimal U35 = dic_ud_times[dic_defectcategary["u35"]];
            decimal U36 = dic_ud_times[dic_defectcategary["u36"]];
            decimal U37 = dic_ud_times[dic_defectcategary["u37"]];
            decimal U38 = dic_ud_times[dic_defectcategary["u38"]];
            decimal U40 = dic_ud_times[dic_defectcategary["u40"]];
            decimal U41 = dic_ud_times[dic_defectcategary["u41"]];
            decimal U42 = dic_ud_times[dic_defectcategary["u42"]];
            decimal U44 = dic_ud_times[dic_defectcategary["u44"]];
            decimal U45 = dic_ud_times[dic_defectcategary["u45"]];
            decimal U46 = dic_ud_times[dic_defectcategary["u46"]];
            decimal U47 = dic_ud_times[dic_defectcategary["u47"]];
            decimal U48 = dic_ud_times[dic_defectcategary["u48"]];
            decimal U49 = dic_ud_times[dic_defectcategary["u49"]];
            decimal U50 = dic_ud_times[dic_defectcategary["u50"]];
            decimal U51 = dic_ud_times[dic_defectcategary["u51"]];
            decimal U52 = dic_ud_times[dic_defectcategary["u52"]];
            decimal U53 = dic_ud_times[dic_defectcategary["u53"]];
            decimal U54 = dic_ud_times[dic_defectcategary["u54"]];
            decimal U56 = dic_ud_times[dic_defectcategary["u56"]];
            decimal U57 = dic_ud_times[dic_defectcategary["u57"]];
            decimal U58 = dic_ud_times[dic_defectcategary["u58"]];
            decimal U59 = dic_ud_times[dic_defectcategary["u59"]];
            decimal U60 = dic_ud_times[dic_defectcategary["u60"]];
            decimal U61 = dic_ud_times[dic_defectcategary["u61"]];
            decimal U62 = dic_ud_times[dic_defectcategary["u62"]];
            decimal U63 = dic_ud_times[dic_defectcategary["u63"]];
            #endregion

            decimal Breaktime = P1 + P2;
            decimal workinghour = 0;
            if (NetProductionTime != 0)
                workinghour = 720 - P5 - P7;
            decimal planedtime = P1 + P2 + P3 + P4 + P5 + P6 + P7 + P8 + P9;
            decimal actualproductTime = 720 - planedtime; //实际的生产时间


            //decimal unplanedtime = U1 + U2 + U3 + U4 + U5 + U7 + U9 + U10 + U11 + U12 + U13 + U14 + U15 + U16;//提醒类是否放入？
            decimal unplanedtime = U1 + U2 + U3 + U4 + U6 + U10 + U11 + U17 + U18 + U19 + U21 + U22 + U23 + U24 +
                U25 + U26 + U28 + U29 + U30 + U31 + U32 + U33 + U34 + U35 + U36 + U37 + U40 + U41 + U42 + U44 +
                U45 + U46 + U47 + U48 + U49 + U50 + U51 + U52 + U53 + U56 + U61 + U59 + U38 + U54 + U57 + U58 + U60 + U62 + U63;
            //decimal __atutaltime = actualproductTime - unplanedtime;
            //if (__atutaltime < NetProductionTime)
            //{
            //    NetProductionTime = __atutaltime;
            //}


            decimal perloss = workinghour != 0 ? workinghour - NetProductionTime - planedtime + P5 + P7 - unplanedtime : 0;

            decimal avect = 0;
            int totaloutput = lsProduction.Sum(p => p.RealProductOutput);
            decimal _tempoutput = 0;
            for (int i = 0; i < lsProduction.Count; i++)
            {
                _tempoutput += lsProduction[i].RealProductOutput * lsProduction[i]._RealCT;

            }
            avect = totaloutput > 0 ? _tempoutput / totaloutput : 0;


            Performance_Line_Target linetarget = DALLib<Performance_Line_Target>.DataAccess.GetOne("LineId", line.Id);
            if (linetarget == null)
                linetarget = new Performance_Line_Target();
            decimal targetoutput = avect > 0 ? (workinghour - Breaktime) * 60 * linetarget.RelUtiTarget / avect : 0;


            DailyShiftInfo shiftinfo = DALLib<DailyShiftInfo>.DataAccess.GetOneBySQL(string.Format("select * from   tbDailyShiftInfo  where DayInfoId={0}", dayinfo.Id));
            if (shiftinfo == null)
            {
                shiftinfo = new DailyShiftInfo();

            }

            shiftinfo.TargetOutput = DMES.Utility.CommonMethod.SafeGetIntFromObj(targetoutput, 0);
            shiftinfo.ActualOutput = totaloutput;
            shiftinfo.ActualRelUT = (workinghour - Breaktime) != 0 ? NetProductionTime / (workinghour - Breaktime) : 0;
            shiftinfo.AVECT = avect;
            shiftinfo.BreakTime = Breaktime;
            shiftinfo.ActualOEE = actualproductTime != 0 ? NetProductionTime / actualproductTime : 0;
            shiftinfo.TargetRelUt = linetarget.RelUtiTarget;
            shiftinfo.PerLoss = perloss;
            shiftinfo.ShiftTime = workinghour / 60;
            shiftinfo.TargetProductivity = 0;
            shiftinfo.cotime = cotime;//换型时间
            shiftinfo.COLoss = (int)Math.Round(cotime, 0, MidpointRounding.AwayFromZero);//四舍五入，换型显示的时间
            shiftinfo.DayInfoId = dayinfo.Id;
            shiftinfo.workinghour = workinghour;
            shiftinfo.netproductiontime = NetProductionTime;
            shiftinfo.totalplantime = planedtime;
            shiftinfo.totalunplantime = unplanedtime;
            if (shiftinfo.ActualOEE != 0 && shiftinfo.ActualOEE.ToString().Contains(".") && shiftinfo.ActualOEE.ToString().IndexOf(".") >= 14)
                shiftinfo.ActualOEE = CommonMethod.SafeGetDecimalFromObject(shiftinfo.ActualOEE.ToString().Substring(0, 14), 0);
            DALLib<DailyShiftInfo>.DataAccess.SaveOne(shiftinfo);


            ShiftBreakTime _shiftbreaktime = DALLib<ShiftBreakTime>.DataAccess.GetOneBySQL("select * from tb_Rpt_ShiftBreakTime where DayInfoId=" + dayinfo.Id);
            if (_shiftbreaktime == null)
            {
                _shiftbreaktime = new AJ.Andon.Entity.Report.ShiftBreakTime();
                _shiftbreaktime.DayInfoId = dayinfo.Id;
            }
            _shiftbreaktime.Value = Breaktime;
            DALLib<ShiftBreakTime>.DataAccess.SaveOne(_shiftbreaktime);

            ShiftCOLoss _shiftcoloss = DALLib<ShiftCOLoss>.DataAccess.GetOneBySQL("select * from tb_Rpt_ShiftCOLoss where DayInfoId=" + dayinfo.Id); ;
            ShiftOutput _shiftoutput = DALLib<ShiftOutput>.DataAccess.GetOneBySQL("select * from tb_Rpt_ShiftOutput where DayInfoId=" + dayinfo.Id);
            ShiftPerLoss _shiftperloss = DALLib<ShiftPerLoss>.DataAccess.GetOneBySQL("select * from tb_Rpt_ShiftPerLoss where DayInfoId=" + dayinfo.Id);
            ShiftProductivity _shiftperoductivity = DALLib<ShiftProductivity>.DataAccess.GetOneBySQL("select * from tb_Rpt_ShiftProductivity where DayInfoId=" + dayinfo.Id);
            ShiftTime _shifttime = DALLib<ShiftTime>.DataAccess.GetOneBySQL("select * from tb_Rpt_ShiftTime where DayInfoId=" + dayinfo.Id);
            ShiftUT _shiftut = DALLib<ShiftUT>.DataAccess.GetOneBySQL("select * from tb_Rpt_ShiftUT where DayInfoId=" + dayinfo.Id);

            //开始往每天的报表里面插入数据
            //分析

            if (_shiftcoloss == null)
            {
                _shiftcoloss = new AJ.Andon.Entity.Report.ShiftCOLoss();
                _shiftcoloss.DayInfoId = dayinfo.Id;
            }

            if (_shiftoutput == null)
            {
                _shiftoutput = new AJ.Andon.Entity.Report.ShiftOutput();
                _shiftoutput.DayInfoId = dayinfo.Id;

            }
            if (_shiftperloss == null)
            {
                _shiftperloss = new AJ.Andon.Entity.Report.ShiftPerLoss();
                _shiftperloss.DayInfoId = dayinfo.Id;
            }


            if (_shiftperoductivity == null)
            {
                _shiftperoductivity = new AJ.Andon.Entity.Report.ShiftProductivity();
                _shiftperoductivity.DayInfoId = dayinfo.Id;
            }

            if (_shifttime == null)
            {
                _shifttime = new AJ.Andon.Entity.Report.ShiftTime();
                _shifttime.DayInfoId = dayinfo.Id;
            }
            if (_shiftut == null)
            {
                _shiftut = new AJ.Andon.Entity.Report.ShiftUT();
                _shiftut.DayInfoId = dayinfo.Id;
            }


            _shiftcoloss.Value = cotime;
            DALLib<ShiftCOLoss>.DataAccess.SaveOne(_shiftcoloss);
            _shiftoutput.Value = shiftinfo.ActualOutput;
            DALLib<ShiftOutput>.DataAccess.SaveOne(_shiftoutput);
            _shiftperloss.Value = (shiftinfo.workinghour - shiftinfo.BreakTime) != 0 ? shiftinfo.PerLoss / (shiftinfo.workinghour - shiftinfo.BreakTime) : 0;
            //Excel中OEE表不减，Excel的DB中是减得【修改：Hyram，Time：2014-12-9 11:25:20。Reasone：根据【Excel】公式修改，分母只是720-P5-P7，不需要再减P1和P2】
            //_shiftperloss.Value = shiftinfo.PerLoss / (shiftinfo.workinghour-shiftinfo.BreakTime);
            DALLib<ShiftPerLoss>.DataAccess.SaveOne(_shiftperloss);
            _shiftperoductivity.Value = shiftinfo.ActualProductivity;
            DALLib<ShiftProductivity>.DataAccess.SaveOne(_shiftperoductivity);
            _shifttime.Value = shiftinfo.ShiftTime;
            DALLib<ShiftTime>.DataAccess.SaveOne(_shifttime);
            _shiftut.Value = shiftinfo.ActualRelUT;
            DALLib<ShiftUT>.DataAccess.SaveOne(_shiftut);


            Dictionary<int, decimal> dic_stationdowntime = new Dictionary<int, decimal>();
            Dictionary<int, int> dic_stationdowntimes = new Dictionary<int, int>();

            Dictionary<int, decimal> dic_stationdowntime_unsplit = new Dictionary<int, decimal>();
            Dictionary<int, int> dic_stationdowntimes_unsplit = new Dictionary<int, int>();



            //如果现在是白班，那么白班的dayperformance的纪录要删除掉 重新添加  否则如果是晚班的话，就在原来的基础上面进行增加
            if (dayflag == EDayFlag.Day)
            {
                //删除掉原来的纪录 然后再重新添加记录
                string sqldayperformance = @"SELECT  *
                  FROM [tb_Rpt_DayPerformanceSummary]
                  where [Date]='{0}' and LineId={1}";

                sqldayperformance = string.Format(sqldayperformance, daystr, line.Id);
                DaySummaryTemp daysumery = DALLib<DaySummaryTemp>.DataAccess.GetOneBySQL(sqldayperformance);
                if (daysumery != null)
                    DALLib<DayPerformanceStationDown>.DataAccess.ExecuteNonQuery(string.Format("delete from tb_Rpt_DayPerformanceStationDown where DayPerformanceId={0}", daysumery.Id));
                else
                    daysumery = new AJ.Andon.Entity.Report.DaySummaryTemp();

                #region 所有U
                daysumery.P1 = P1;
                daysumery.P2 = P2;
                daysumery.P3 = P3;
                daysumery.P4 = P4;
                daysumery.P5 = P5;
                daysumery.P6 = P6;
                daysumery.P7 = P7;
                daysumery.P8 = P8;
                daysumery.P9 = P9;
                //daysumery.P10 = P10;

                daysumery.U1 = U1;
                daysumery.U2 = U2;
                daysumery.U3 = U3;
                daysumery.U4 = U4;
                daysumery.U5 = U5;
                //daysumery.U6 = cotime;
                daysumery.U6 = U6;
                //daysumery.U7 = U7;
                //daysumery.U8 = U8;
                //daysumery.U9 = U9;
                daysumery.U10 = U10;
                daysumery.U11 = U11;
                //daysumery.U12 = U12;
                //daysumery.U13 = U13;
                //daysumery.U14 = U14;
                //daysumery.U15 = U15;
                //daysumery.U16 = U16;
                daysumery.U17 = U17;
                daysumery.U18 = U18;
                daysumery.U19 = U19;
                daysumery.U21 = U21;
                daysumery.U22 = U22;
                daysumery.U23 = U23;
                daysumery.U24 = U24;
                daysumery.U25 = U25;
                daysumery.U26 = U26;
                daysumery.U28 = U28;
                daysumery.U29 = U29;
                daysumery.U30 = U30;
                daysumery.U31 = U31;
                daysumery.U32 = U32;
                daysumery.U33 = U33;
                daysumery.U34 = U34;
                daysumery.U35 = U35;
                daysumery.U36 = U36;
                daysumery.U37 = U37;
                daysumery.U38 = U38;
                daysumery.U40 = U40;
                daysumery.U41 = U41;
                daysumery.U42 = U42;
                daysumery.U44 = U44;
                daysumery.U45 = U45;
                daysumery.U46 = U46;
                daysumery.U47 = U47;
                daysumery.U48 = U48;
                daysumery.U49 = U49;
                daysumery.U50 = U50;
                daysumery.U51 = U51;
                daysumery.U52 = U52;
                daysumery.U53 = U53;
                daysumery.U54 = U54;
                daysumery.U56 = U56;
                daysumery.U57 = U57;
                daysumery.U58 = U58;
                daysumery.U59 = U59;
                daysumery.U60 = U60;
                daysumery.U61 = U61;
                daysumery.U62 = U62;
                daysumery.U63 = U63;
                #endregion

                daysumery.WorkingHour = workinghour;
                daysumery.NetProductiontime = NetProductionTime;
                daysumery.OEE = shiftinfo.ActualOEE;
                daysumery.PerLoss = perloss;
                daysumery.BreakTime = Breaktime;
                daysumery.RelUti = shiftinfo.ActualRelUT;
                daysumery.Date = dayinfo.Day;
                daysumery.LineId = line.Id;
                daysumery.Output = shiftinfo.ActualOutput;
                daysumery.AbsUti = (shiftinfo.workinghour) != 0 ? shiftinfo.netproductiontime / shiftinfo.workinghour : 0;//除P5、P7的所有P
                decimal dayplanedtime = daysumery.P1 + daysumery.P2 + daysumery.P3 + daysumery.P4 + daysumery.P6 + daysumery.P8 + daysumery.P9;
                daysumery.OEELoss = daysumery.WorkingHour != 0 ? (daysumery.WorkingHour - dayplanedtime) - daysumery.NetProductiontime : 0;
                DALLib<DaySummaryTemp>.DataAccess.SaveOne(daysumery);

                //把非计划停机的纪录填写到数据库里面去
                //【已经切分】的【每台设备停机的时间、次数】
                for (int i = 0; i < Uds.Count; i++)
                {
                    if (dic_stationdowntime.ContainsKey(Uds[i].StationId))
                    {
                        dic_stationdowntime[Uds[i].StationId] += DMES.Utility.CommonMethod.SafeGetDecimalFromObject(Uds[i].EndTime.Subtract(Uds[i].StartTime).TotalMinutes, 0);
                        dic_stationdowntimes[Uds[i].StationId] += 1;
                    }
                    else
                    {
                        dic_stationdowntime.Add(Uds[i].StationId, DMES.Utility.CommonMethod.SafeGetDecimalFromObject(Uds[i].EndTime.Subtract(Uds[i].StartTime).TotalMinutes, 0));
                        dic_stationdowntimes.Add(Uds[i].StationId, 1);
                    }
                }
                //【未切分】的【每台设备的停机时间、次数】
                for (int i = 0; i < UnSplitDT.Count; i++)
                {
                    if (dic_stationdowntime_unsplit.ContainsKey(UnSplitDT[i].StationId))
                    {
                        dic_stationdowntime_unsplit[UnSplitDT[i].StationId] += DMES.Utility.CommonMethod.SafeGetDecimalFromObject(UnSplitDT[i].EndTime.Subtract(UnSplitDT[i].StartTime).TotalMinutes, 0);
                        dic_stationdowntimes_unsplit[UnSplitDT[i].StationId] += 1;
                    }
                    else
                    {
                        dic_stationdowntime_unsplit.Add(UnSplitDT[i].StationId, DMES.Utility.CommonMethod.SafeGetDecimalFromObject(UnSplitDT[i].EndTime.Subtract(UnSplitDT[i].StartTime).TotalMinutes, 0));
                        dic_stationdowntimes_unsplit.Add(UnSplitDT[i].StationId, 1);
                    }
                }
                //【已切分】---------------------------------------------------
                List<DayPerformanceStationDown> lstDayperformanceStationDown = new List<DayPerformanceStationDown>();
                foreach (int stationid in dic_stationdowntime.Keys)
                {
                    DayPerformanceStationDown performancedown = new AJ.Andon.Entity.Report.DayPerformanceStationDown();
                    performancedown.DayPerformanceId = daysumery.Id;
                    performancedown.StationId = stationid;
                    performancedown.DownTime = dic_stationdowntime[stationid];
                    performancedown.DownTimes = dic_stationdowntimes[stationid];

                    lstDayperformanceStationDown.Add(performancedown);
                    DALLib<DayPerformanceStationDown>.DataAccess.SaveOne(performancedown);
                }
                //【未切分】+++++++++++++++++++++++++++++++++++++++++++++++++++
                List<DayPerformanceStationDownUnSplit> lstDayperformanceStationDownUnSplit = new List<DayPerformanceStationDownUnSplit>();
                foreach (int stationid in dic_stationdowntime_unsplit.Keys)
                {
                    DayPerformanceStationDownUnSplit performancedown = new DayPerformanceStationDownUnSplit();
                    performancedown.DayPerformanceId = daysumery.Id;
                    performancedown.StationId = stationid;
                    performancedown.DownTime = dic_stationdowntime_unsplit[stationid];
                    performancedown.DownTimes = dic_stationdowntimes_unsplit[stationid];

                    lstDayperformanceStationDownUnSplit.Add(performancedown);
                    DALLib<DayPerformanceStationDownUnSplit>.DataAccess.SaveOne(performancedown);
                }
            }
            else
            {
                //获取到原来的纪录，然后在上面累加

                //删除掉原来的纪录 然后再重新添加记录
                string sqldayperformance = @"SELECT  *
                  FROM [tb_Rpt_DayPerformanceSummary]
                  where [Date]='{0}' and LineId={1}";
                sqldayperformance = string.Format(sqldayperformance, daystr, line.Id);
                DaySummaryTemp daysumery = DALLib<DaySummaryTemp>.DataAccess.GetOneBySQL(sqldayperformance);
                List<DayPerformanceStationDown> lstdowntimestation = new List<AJ.Andon.Entity.Report.DayPerformanceStationDown>();
                List<DayPerformanceStationDownUnSplit> lstdowntimestationunsplit = new List<DayPerformanceStationDownUnSplit>();

                if (daysumery != null)
                {
                    lstdowntimestation = DALLib<DayPerformanceStationDown>.DataAccess.
                       GetSome(string.Format(" select * from [dbo].[tb_Rpt_DayPerformanceStationDown] where DayPerformanceId={0}", daysumery.Id));
                    lstdowntimestationunsplit = DALLib<DayPerformanceStationDownUnSplit>.DataAccess.
                        GetSome(string.Format(" select * from [dbo].[tb_Rpt_DayPerformanceStationDownUnSplit] where DayPerformanceId={0}", daysumery.Id));
                }
                else
                    daysumery = new AJ.Andon.Entity.Report.DaySummaryTemp();

                #region 所有U
                daysumery.P1 += P1;
                daysumery.P2 += P2;
                daysumery.P3 += P3;
                daysumery.P4 += P4;
                daysumery.P5 += P5;
                daysumery.P6 += P6;
                daysumery.P7 += P7;
                daysumery.P8 += P8;
                daysumery.P9 += P9;
                //daysumery.P10 += P10;

                daysumery.U1 += U1;
                daysumery.U2 += U2;
                daysumery.U3 += U3;
                daysumery.U4 += U4;
                daysumery.U5 += U5;
                //daysumery.U6 += cotime;
                daysumery.U6 += U6;
                //daysumery.U7 += U7;
                //daysumery.U8 += U8;
                //daysumery.U9 += U9;
                daysumery.U10 += U10;
                daysumery.U11 += U11;
                //daysumery.U12 += U12;
                //daysumery.U13 += U13;
                //daysumery.U14 += U14;
                //daysumery.U15 += U15;
                //daysumery.U16 += U16;
                daysumery.U17 += U17;
                daysumery.U18 += U18;
                daysumery.U19 += U19;
                daysumery.U21 += U21;
                daysumery.U22 += U22;
                daysumery.U23 += U23;
                daysumery.U24 += U24;
                daysumery.U25 += U25;
                daysumery.U26 += U26;
                daysumery.U28 += U28;
                daysumery.U29 += U29;
                daysumery.U30 += U30;
                daysumery.U31 += U31;
                daysumery.U32 += U32;
                daysumery.U33 += U33;
                daysumery.U34 += U34;
                daysumery.U35 += U35;
                daysumery.U36 += U36;
                daysumery.U37 += U37;
                daysumery.U38 += U38;
                daysumery.U40 += U40;
                daysumery.U41 += U41;
                daysumery.U42 += U42;
                daysumery.U44 += U44;
                daysumery.U45 += U45;
                daysumery.U46 += U46;
                daysumery.U47 += U47;
                daysumery.U48 += U48;
                daysumery.U49 += U49;
                daysumery.U50 += U50;
                daysumery.U51 += U51;
                daysumery.U52 += U52;
                daysumery.U53 += U53;
                daysumery.U54 += U54;
                daysumery.U56 += U56;
                daysumery.U57 += U57;
                daysumery.U58 += U58;
                daysumery.U59 += U59;
                daysumery.U60 += U60;
                daysumery.U61 += U61;
                daysumery.U62 += U62;
                daysumery.U63 += U63;
                #endregion

                daysumery.WorkingHour += workinghour;
                daysumery.NetProductiontime += NetProductionTime;

                daysumery.PerLoss += perloss;
                daysumery.BreakTime += Breaktime;
                daysumery.Date = dayinfo.Day;
                daysumery.LineId = line.Id;
                daysumery.Output += shiftinfo.ActualOutput;
                daysumery.AbsUti = (daysumery.WorkingHour) != 0 ? daysumery.NetProductiontime / (daysumery.WorkingHour) : 0;
                //decimal dayplanedtime = daysumery.P1 + daysumery.P2 + daysumery.P3 + daysumery.P4 + daysumery.P6 + daysumery.P8 + daysumery.P9 + daysumery.P10;
                //除P5、P7的所有P
                decimal dayplanedtime = daysumery.P1 + daysumery.P2 + daysumery.P3 + daysumery.P4 + daysumery.P6 + daysumery.P8 + daysumery.P9;
                daysumery.OEE = (daysumery.WorkingHour - dayplanedtime) != 0 ? daysumery.NetProductiontime / (daysumery.WorkingHour - dayplanedtime) : 0;
                daysumery.OEELoss = daysumery.WorkingHour != 0 ? (daysumery.WorkingHour - dayplanedtime) - daysumery.NetProductiontime : 0;
                daysumery.RelUti = (daysumery.WorkingHour - daysumery.BreakTime) != 0 ? daysumery.NetProductiontime / (daysumery.WorkingHour - daysumery.BreakTime) : 0;

                DALLib<DaySummaryTemp>.DataAccess.SaveOne(daysumery);

                //把非计划停机的纪录填写到数据库里面去
                //【已经切分】的【每台设备停机的时间、次数】
                for (int i = 0; i < Uds.Count; i++)
                {
                    if (dic_stationdowntime.ContainsKey(Uds[i].StationId))
                    {

                        dic_stationdowntime[Uds[i].StationId] += DMES.Utility.CommonMethod.SafeGetDecimalFromObject(Uds[i].EndTime.Subtract(Uds[i].StartTime).TotalMinutes, 0);
                        dic_stationdowntimes[Uds[i].StationId] += 1;
                    }
                    else
                    {
                        dic_stationdowntime.Add(Uds[i].StationId, DMES.Utility.CommonMethod.SafeGetDecimalFromObject(Uds[i].EndTime.Subtract(Uds[i].StartTime).TotalMinutes, 0));
                        dic_stationdowntimes.Add(Uds[i].StationId, 1);
                    }
                }
                //【未切分】的【每台设备的停机时间、次数】
                for (int i = 0; i < UnSplitDT.Count; i++)
                {
                    if (dic_stationdowntime_unsplit.ContainsKey(UnSplitDT[i].StationId))
                    {
                        dic_stationdowntime_unsplit[UnSplitDT[i].StationId] += DMES.Utility.CommonMethod.SafeGetDecimalFromObject(UnSplitDT[i].EndTime.Subtract(UnSplitDT[i].StartTime).TotalMinutes, 0);
                        dic_stationdowntimes_unsplit[UnSplitDT[i].StationId] += 1;
                    }
                    else
                    {
                        dic_stationdowntime_unsplit.Add(UnSplitDT[i].StationId, DMES.Utility.CommonMethod.SafeGetDecimalFromObject(UnSplitDT[i].EndTime.Subtract(UnSplitDT[i].StartTime).TotalMinutes, 0));
                        dic_stationdowntimes_unsplit.Add(UnSplitDT[i].StationId, 1);
                    }
                }
                //【已切分】
                foreach (int stationid in dic_stationdowntime.Keys)
                {
                    DayPerformanceStationDown performancedown = lstdowntimestation.Find(p => p.StationId == stationid && p.DayPerformanceId == daysumery.Id);
                    if (performancedown == null)
                        performancedown = new AJ.Andon.Entity.Report.DayPerformanceStationDown();
                    performancedown.DayPerformanceId = daysumery.Id;
                    performancedown.StationId = stationid;
                    performancedown.DownTime += dic_stationdowntime[stationid];
                    performancedown.DownTimes += dic_stationdowntimes[stationid];

                    DALLib<DayPerformanceStationDown>.DataAccess.SaveOne(performancedown);
                }
                //【未切分】
                foreach (int stationid in dic_stationdowntime_unsplit.Keys)
                {
                    DayPerformanceStationDownUnSplit performancedown = lstdowntimestationunsplit.Find(p => p.StationId == stationid && p.DayPerformanceId == daysumery.Id);
                    if (performancedown == null)
                        performancedown = new DayPerformanceStationDownUnSplit();
                    performancedown.DayPerformanceId = daysumery.Id;
                    performancedown.StationId = stationid;
                    performancedown.DownTime += dic_stationdowntime_unsplit[stationid];
                    performancedown.DownTimes += dic_stationdowntimes_unsplit[stationid];

                    DALLib<DayPerformanceStationDownUnSplit>.DataAccess.SaveOne(performancedown);
                }
            }

            // 开始往每月的报表里面插入数据
            string sqlmonthstr = @"select * from [dbo].[tb_Rpt_MonthlyPerformanceSummary]
                where LineId={0} and [Month]='{1}'";
            sqlmonthstr = string.Format(sqlmonthstr, line.Id, monthstr);
            MonthSummaryTemp monthTemp = DALLib<MonthSummaryTemp>.DataAccess.GetOneBySQL(sqlmonthstr);
            if (monthTemp == null)
            {
                monthTemp = new AJ.Andon.Entity.Report.MonthSummaryTemp();
            }

            #region 所有U

            monthTemp.P1 += P1;
            monthTemp.P2 += P2;
            monthTemp.P3 += P3;
            monthTemp.P4 += P4;
            monthTemp.P5 += P5;
            monthTemp.P6 += P6;
            monthTemp.P7 += P7;
            monthTemp.P8 += P8;
            monthTemp.P9 += P9;
            //monthTemp.P10 += P10;

            monthTemp.U1 += U1;
            monthTemp.U2 += U2;
            monthTemp.U3 += U3;
            monthTemp.U4 += U4;
            monthTemp.U5 += U5;
            //monthTemp.U6 += cotime;
            monthTemp.U6 += U6;
            //monthTemp.U7 += U7;
            //monthTemp.U8 += U8;
            //monthTemp.U9 += U9;
            monthTemp.U10 += U10;
            monthTemp.U11 += U11;
            //monthTemp.U12 += U12;
            //monthTemp.U13 += U13;
            //monthTemp.U14 += U14;
            //monthTemp.U15 += U15;
            //monthTemp.U16 += U16;
            monthTemp.U17 += U17;
            monthTemp.U18 += U18;
            monthTemp.U19 += U19;
            monthTemp.U21 += U21;
            monthTemp.U22 += U22;
            monthTemp.U23 += U23;
            monthTemp.U24 += U24;
            monthTemp.U25 += U25;
            monthTemp.U26 += U26;
            monthTemp.U28 += U28;
            monthTemp.U29 += U29;
            monthTemp.U30 += U30;
            monthTemp.U31 += U31;
            monthTemp.U32 += U32;
            monthTemp.U33 += U33;
            monthTemp.U34 += U34;
            monthTemp.U35 += U35;
            monthTemp.U36 += U36;
            monthTemp.U37 += U37;
            monthTemp.U38 += U38;
            monthTemp.U40 += U40;
            monthTemp.U41 += U41;
            monthTemp.U42 += U42;
            monthTemp.U44 += U44;
            monthTemp.U45 += U45;
            monthTemp.U46 += U46;
            monthTemp.U47 += U47;
            monthTemp.U48 += U48;
            monthTemp.U49 += U49;
            monthTemp.U50 += U50;
            monthTemp.U51 += U51;
            monthTemp.U52 += U52;
            monthTemp.U53 += U53;
            monthTemp.U54 += U54;
            monthTemp.U56 += U56;
            monthTemp.U57 += U57;
            monthTemp.U58 += U58;
            monthTemp.U59 += U59;
            monthTemp.U60 += U60;
            monthTemp.U61 += U61;
            monthTemp.U62 += U62;
            monthTemp.U63 += U63;

            #endregion


            monthTemp.WorkingHour += workinghour;
            monthTemp.BreakTime += Breaktime;
            monthTemp.LineId = line.Id;
            monthTemp.NetProductiontime += NetProductionTime;
            monthTemp.Month = monthstr;
            //monthTemp.OEELoss += NetProductionTime;
            monthTemp.Output += shiftinfo.ActualOutput;


            //monthTemp.AbsUti = monthTemp.NetProductiontime / (monthTemp.WorkingHour);
            //公式修改，分母不是WorkingHour，是1440*总的天数，因为WorkingHour是减去了P5，P7，所以加上月的P5，P7就等于1440*总的天数
            monthTemp.AbsUti = (monthTemp.WorkingHour + monthTemp.P5 + monthTemp.P7) != 0 ? monthTemp.NetProductiontime / (monthTemp.WorkingHour + monthTemp.P5 + monthTemp.P7) : 0;
            monthTemp.RelUti = (monthTemp.WorkingHour - monthTemp.BreakTime) != 0 ? monthTemp.NetProductiontime / (monthTemp.WorkingHour - monthTemp.BreakTime) : 0;
            //decimal monthplancedtime = monthTemp.P1 + monthTemp.P2 + monthTemp.P3 + monthTemp.P4 + monthTemp.P6 + monthTemp.P8 + monthTemp.P9 + monthTemp.P10;
            //除P5、P7的所有P
            decimal monthplancedtime = monthTemp.P1 + monthTemp.P2 + monthTemp.P3 + monthTemp.P4 + monthTemp.P6 + monthTemp.P8 + monthTemp.P9;

            monthTemp.OEE = (monthTemp.WorkingHour - monthplancedtime) != 0 ? monthTemp.NetProductiontime / (monthTemp.WorkingHour - monthplancedtime) : 0;
            monthTemp.PerLoss += perloss;
            monthTemp.OEELoss = monthTemp.WorkingHour != 0 ? (monthTemp.WorkingHour - monthplancedtime - monthTemp.NetProductiontime) : 0;

            DALLib<MonthSummaryTemp>.DataAccess.SaveOne(monthTemp);


            //月的纪录就是直接往里面添加就好了

            List<MonthlyPerformanceStationDown> lstmonthlyStationDown = DALLib<MonthlyPerformanceStationDown>.DataAccess.
                GetSome(string.Format("select * from tb_Rpt_MonthlyPerformanceStationDown where  MonthlyPerformanceId={0}", monthTemp.Id));
            List<MonthlyPerformanceStationDownUnSplit> lstmonthlyStationDownUnSplit = DALLib<MonthlyPerformanceStationDownUnSplit>.DataAccess.
                GetSome(string.Format("select * from tb_Rpt_MonthlyPerformanceStationDownUnSplit where  MonthlyPerformanceId={0}", monthTemp.Id));
            if (lstmonthlyStationDown == null)
                lstmonthlyStationDown = new List<MonthlyPerformanceStationDown>();
            if (lstmonthlyStationDownUnSplit == null)
                lstmonthlyStationDownUnSplit = new List<MonthlyPerformanceStationDownUnSplit>();
            //【已切分】
            foreach (int stationid in dic_stationdowntime.Keys)
            {
                MonthlyPerformanceStationDown monthrperformancedown = lstmonthlyStationDown.Find(p => p.StationId == stationid && p.MonthlyPerformanceId == monthTemp.Id);
                if (monthrperformancedown == null)
                    monthrperformancedown = new AJ.Andon.Entity.Report.MonthlyPerformanceStationDown();
                monthrperformancedown.MonthlyPerformanceId = monthTemp.Id;
                monthrperformancedown.StationId = stationid;
                monthrperformancedown.DownTime += dic_stationdowntime[stationid];
                monthrperformancedown.DownTimes += dic_stationdowntimes[stationid];

                DALLib<MonthlyPerformanceStationDown>.DataAccess.SaveOne(monthrperformancedown);
            }
            //【未切分】
            foreach (int stationid in dic_stationdowntime_unsplit.Keys)
            {
                MonthlyPerformanceStationDownUnSplit monthrperformancedown = lstmonthlyStationDownUnSplit.Find(p => p.StationId == stationid && p.MonthlyPerformanceId == monthTemp.Id);
                if (monthrperformancedown == null)
                    monthrperformancedown = new MonthlyPerformanceStationDownUnSplit();
                monthrperformancedown.MonthlyPerformanceId = monthTemp.Id;
                monthrperformancedown.StationId = stationid;
                monthrperformancedown.DownTime += dic_stationdowntime_unsplit[stationid];
                monthrperformancedown.DownTimes += dic_stationdowntimes_unsplit[stationid];

                DALLib<MonthlyPerformanceStationDownUnSplit>.DataAccess.SaveOne(monthrperformancedown);
            }

        }

        /// <summary>
        /// 获取【指定的(停机U2，质量U3，短时U10，CCSU61)停机记录】【并不切分】
        /// </summary>
        /// <param name="LineId"></param>
        /// <param name="dtStart"></param>
        /// <param name="dtEnd"></param>
        /// <returns></returns>
        private List<UnScheduleDownTime> GetUnSplitDT(int LineId, DateTime dtStart, DateTime dtEnd)
        {
            string SqlGetUnScheduleDT = @"SELECT und.* FROM tbPro_UnScheduleDownTime und
            INNER JOIN tbDic_DefectCategory defect ON defect.Id=und.DefectCategoryId 
            INNER JOIN tbDic_Station station ON station.Id=und.StationId 
            INNER JOIN tbDic_Line line ON line.Id=station.LineId 
            WHERE (StartTime>='" + dtStart + "' AND EndTime<'" + dtEnd + "') AND line.Id=" + LineId + " AND defect.DefectCategoryName IN ('U3','U2','U10','U61')";

            List<UnScheduleDownTime> UnSplitDT = DALLib<UnScheduleDownTime>.DataAccess.GetSome(SqlGetUnScheduleDT);

            if (UnSplitDT == null && UnSplitDT.Count > 0)
                return UnSplitDT = new List<UnScheduleDownTime>();
            else
                return UnSplitDT;
        }

        /// <summary>
        /// 解析Production中实际换型的时间
        /// </summary>
        /// <param name="lineid"></param>
        /// <param name="dtStart"></param>
        /// <param name="dtEnd"></param>
        /// <returns></returns>
        private List<UnScheduleDownTime> AnalyzeCOTimWithProduction(int lineid, DateTime dtStart, DateTime dtEnd)
        {
            Station station = DALLib<Station>.DataAccess.GetOneBySQL("select top 1  * from tbDic_Station where  LineId=" + lineid);
            station = DALLib<Station>.DataAccess.GetOneBySQL(@"SELECT station.* FROM tbDic_Station station
                         INNER JOIN tbDic_WorkStation ws ON ws.Id=station.WorkStationId
                         INNER JOIN tbDic_Line line ON line.Id=station.LineId
                         INNER JOIN tbDic_SpecialLocation sl ON sl.StationId=station.Id
                         INNER JOIN tb_Dic_SpecialParameter sp ON sp.Id=sl.ParameterId
                         WHERE ws.IsLineOutput='true' AND sl.IsOutput='true' AND line.Id=" + lineid + "");
            if (lineid == 7)
            {
                station = DALLib<Station>.DataAccess.GetOneBySQL(@"SELECT station.* FROM tbDic_Station station
                INNER JOIN tbDic_WorkStation ws ON ws.Id=station.WorkStationId
                INNER JOIN tbDic_Line line ON line.Id=station.LineId
                INNER JOIN tbDic_Location sl ON sl.StationId=station.Id
                WHERE ws.IsLineOutput='true' AND sl.IsOutput='true' AND line.Id=" + lineid + "");
            }
            List<Production> lsProduction = GetProduction(lineid, dtStart, dtEnd);
            List<UnScheduleDownTime> lstUnscheduleDowntime = new List<UnScheduleDownTime>();
            DefectCategory u6defect = DALLib<DefectCategory>.DataAccess.GetOneBySQL("select * from tbDic_DefectCategory where DefectCategoryName='U6'");
            int productid = 0;
            DateTime _dtTemp = DateTime.Now;

            for (int i = 0; i < lsProduction.Count; i++)
            {
                if (i == 0)
                {
                    productid = lsProduction[i].ProductId;
                    continue;
                }

                if (productid != lsProduction[i].ProductId)
                {
                    DateTime dtLastEnd = lsProduction[i - 1].RealEndTime;
                    DateTime dtCurrentStart = lsProduction[i].RealStartTime;
                    double _cotime = dtCurrentStart.Subtract(dtLastEnd).TotalMinutes;
                    if (_cotime > 60) //换型时间不可能需要一个小时
                    {
                        continue;
                    }
                    else
                    {
                        //写入一条换型纪录 stationid 就为第一台station为基准
                        UnScheduleDownTime ud = new UnScheduleDownTime();
                        ud.DefectCategoryId = u6defect.Id;
                        ud.StationId = station.Id;
                        ud.StartTime = dtLastEnd;
                        ud.EndTime = dtCurrentStart;
                        lstUnscheduleDowntime.Add(ud);
                        productid = lsProduction[i].ProductId;
                    }
                }
            }

            for (int i = 0; i < lstUnscheduleDowntime.Count; i++)
            {
                DALLib<UnScheduleDownTime>.DataAccess.SaveOne(lstUnscheduleDowntime[i]);
            }

            return lstUnscheduleDowntime;
        }

        public DayInfo GetDayInfo(DateTime dtNow, Line line)
        {
            DayInfo dayDayInfo = null;


            if (dtNow.Hour == 19) //白班
            {
                string strday = dtNow.ToString("yyyyMMdd");
                dayDayInfo = DALLib<DayInfo>.DataAccess.GetOneBySQL(string.Format("select * from tbDayInfo where LineId={0}  and  [day]='{1}' and DayShiftFlag={2}",
                    line.Id, strday, (int)EDayShiftFlag.DayShift));

                if (dayDayInfo == null)
                {
                    dayDayInfo = new DayInfo();
                    dayDayInfo.LineId = line.Id;
                    dayDayInfo.DayShiftFlag = (int)EDayShiftFlag.DayShift;
                    dayDayInfo.Day = strday;
                    dayDayInfo.Shift = DALLib<ShiftType>.DataAccess.GetOne("ShiftName", ShiftCalendarInfo.GetInstance().GetShiftInfo(dtNow.AddHours(-1))).Id;
                    DALLib<DayInfo>.DataAccess.SaveOne(dayDayInfo);
                }
                dayDayInfo._dtBegin = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 7, 0, 0);
                dayDayInfo._dtEnd = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 19, 0, 0);

            }
            else if (dtNow.Hour == 7) //晚班
            {
                string strday = dtNow.AddDays(-1).ToString("yyyyMMdd");

                dayDayInfo = DALLib<DayInfo>.DataAccess.GetOneBySQL(string.Format("select * from tbDayInfo where LineId={0}  and  [day]='{1}' and DayShiftFlag={2}",
                    line.Id, strday, (int)EDayShiftFlag.NightShift));

                if (dayDayInfo == null)
                {
                    dayDayInfo = new DayInfo();
                    dayDayInfo.LineId = line.Id;
                    dayDayInfo.Day = strday;
                    dayDayInfo.DayShiftFlag = (int)EDayShiftFlag.NightShift;
                    dayDayInfo.Shift = DALLib<ShiftType>.DataAccess.GetOne("ShiftName",
                    ShiftCalendarInfo.GetInstance().GetShiftInfo(new DateTime(dtNow.AddDays(-1).Year, dtNow.AddDays(-1).Month, dtNow.AddDays(-1).Day, 20, 0, 0))).Id;
                    DALLib<DayInfo>.DataAccess.SaveOne(dayDayInfo);
                }
                dayDayInfo._dtBegin = new DateTime(dtNow.AddDays(-1).Year, dtNow.AddDays(-1).Month, dtNow.AddDays(-1).Day, 19, 0, 0);
                dayDayInfo._dtEnd = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 7, 0, 0);


            }

            return dayDayInfo;
        }

        private List<Production> GetProduction(int lineid, DateTime _dtStart, DateTime _dtEnd)
        {
            //string sql = @"select * from [dbo].[tbPro_Production]
            //where RealStartTime>='{0}' and RealStartTime<'{1}'
            //                    and LineId={2}  order by RealStartTime asc";

            //改为查询HistoryProduction，时间：2014-12-30 11:27:33
            string sql = @"select * from [dbo].[tbPro_HistoryProduction]
            where RealStartTime>='{0}' and RealStartTime<'{1}'
            and LineId={2}  order by RealStartTime asc";
            sql = string.Format(sql, _dtStart, _dtEnd, lineid);
            List<Production> lstProduction = DALLib<Production>.DataAccess.GetSome(sql);
            //这里面的产量纪录的CT要取值正确，如果没有的话就去默认的值

            Dictionary<int, decimal> dic_real_ct = new Dictionary<int, decimal>();

            for (int i = 0; i < lstProduction.Count; i++)
            {
                Production production = lstProduction[i];


                if (dic_real_ct.ContainsKey(production.ProductId))
                {
                    production._RealCT = dic_real_ct[production.ProductId];

                }
                else
                {
                    string sqllinect = @"select * from tbDic_Product_Line_CT
                        where Lineid={0} and ProductId={1}";
                    sqllinect = string.Format(sqllinect, lineid, production.ProductId);

                    Product_Line_CT linect = DALLib<Product_Line_CT>.DataAccess.GetOneBySQL(sqllinect);

                    bool iszero = false;
                    if (linect != null)
                    {
                        production._RealCT = DMES.Utility.CommonMethod.SafeGetDecimalFromObject(linect.CycleTime, 0);
                        if (production._RealCT <= 0.1m)
                        {
                            iszero = true;
                        }
                    }
                    else
                    {
                        iszero = true;
                    }



                    if (iszero)
                    {
                        decimal realct = 0;
                        Line line = DALLib<Line>.DataAccess.GetOne("Id", lineid);
                        switch (line.LineName.Trim())
                        {
                            case "Testing1": realct = 11.2m; break;
                            case "Testing2": realct = 11.5m; break;
                            case "Testing3": realct = 11m; break;
                            case "Testing4": realct = 10.5m; break;
                            case "Testing5": realct = 11.5m; break;
                            case "LALFA": realct = 14.5m; break;
                            case "NKS1FA": realct = 11m; break;
                            case "NKS2FA": realct = 11m; break;
                            case "NKS3FA": realct = 11m; break;
                            case "NKS4FA": realct = 10m; break;
                            case "Runin1": realct = 4.1m; break;
                            case "Runin2": realct = 6.2m; break;
                            case "Preprocess1": realct = 13.5m; break;
                            case "Preprocess2": realct = 11m; break;
                            case "Preprocess3": realct = 10.8m; break;
                            case "Preprocess4": realct = 10.8m; break;
                            case "ABS9M": realct = 55m; break;
                            case "EPSc": realct = 20m; break;
                            default: realct = 11m; break;
                        }

                        production._RealCT = realct;

                    }
                }
            }


            return lstProduction;
        }

        private double CalcCOTime(List<Production> lsProduction)
        {
            double time = 0;
            int productid = 0;
            for (int i = 0; i < lsProduction.Count; i++)
            {
                if (i == 0)
                {
                    productid = lsProduction[i].ProductId;
                    continue;
                }

                if (productid != lsProduction[i].ProductId)
                {
                    DateTime dtLastEnd = lsProduction[i - 1].RealEndTime;
                    DateTime dtCurrentStart = lsProduction[i].RealStartTime;
                    double _cotime = dtCurrentStart.Subtract(dtLastEnd).TotalMinutes;
                    if (_cotime > 60) //换型时间不可能需要一个小时
                    {
                        continue;
                    }
                    time += _cotime;
                    productid = lsProduction[i].ProductId;
                }

            }

            return time;
        }
    }
}
