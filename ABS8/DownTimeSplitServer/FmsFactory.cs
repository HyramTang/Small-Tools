using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using AJ.Andon.Entity;
using AJ.Andon.Entity.Dictionary;
using AJ.Andon.Entity.Report;
using DMES.Utility;

namespace DownTimeSplitService
{
    public class FmsFactory
    {

        private ABS8MESHelper m_ABS8MESHelper;
        private static FmsFactory Instance;
        private List<LocationInfo> lstLocationInfo;
        private FmsFactory()
        {
            m_ABS8MESHelper = new ABS8MESHelper();
            lstLocationInfo = new List<LocationInfo>();
        }
        public static FmsFactory GetInstance()
        {
            if (Instance == null)
            {
                Instance = new FmsFactory();
            }
            return Instance;
        }


        public void DoProduction(DateTime dtStart, DateTime dtEnd, int lineId, String LineName)
        {
            DataTable TabLocationInfo = new DataTable();
            GetPackageStation(lineId, out TabLocationInfo);
            if (TabLocationInfo == null || TabLocationInfo.Rows.Count <= 0)
                return;
            else
                lstLocationInfo=TabToObj(TabLocationInfo);

            //获取这条线最后一次的产量查询的结束时间
            string sqllast_productime = @"SELECT top 1  *  
                          FROM [DBAJAndon].[dbo].[tbFlowProduction]
                          where Person='output' and LineId={0} order by RealStartTime desc ";


            sqllast_productime = string.Format(sqllast_productime, lineId);

            FlowProduction flowproduction = DALLib<FlowProduction>.DataAccess.GetOneBySQL(sqllast_productime);

            DateTime dtLastFlowproductionTime = dtStart;
            if (flowproduction != null)
            {
                dtLastFlowproductionTime = flowproduction.RealEndTime;
            }

            //首先计算下它的中间产出
            int spilitcount = 0;
            DataTable dtTempcount = null;
            string spiltproductname = "";
            if (dtLastFlowproductionTime < dtStart)
            {
                dtTempcount = ABS8MESHelper.GetMesTable(dtLastFlowproductionTime, dtStart, lstLocationInfo);
                if (dtTempcount != null && dtTempcount.Rows.Count > 0)
                {
                    spilitcount = dtTempcount.Rows.Count;
                    spiltproductname = dtTempcount.Rows[0]["type_number"].ToString();
                }

            }

            DataTable dtResult = ABS8MESHelper.GetMesTable(dtStart, dtEnd, lstLocationInfo);
            ABS8MESHelper.AnalyzeDatatable(dtResult, lineId, LineName, dtStart, dtEnd, spilitcount, spiltproductname);
        }

        private List<LocationInfo> TabToObj(DataTable TabLocationInfo)
        {
            foreach (DataRow row in TabLocationInfo.Rows)
            {
                lstLocationInfo.Add(new LocationInfo
                {
                    Id = CommonMethod.SafeGetIntFromObj(row["Id"], 0),
                    Ver = CommonMethod.SafeGetIntFromObj(row["Ver"], 0),
                    LineCode = row["LineCode"].ToString(),
                    StationId = CommonMethod.SafeGetIntFromObj(row["StationId"], 0),
                    StatNo = row["StatNo"].ToString(),
                    ParameterId = CommonMethod.SafeGetIntFromObj(row["ParameterId"], 0),
                    ParameterContent = row["ParameterContent"].ToString(),
                    ParameterName = row["ParameterName"].ToString(),
                    IsOutput = CommonMethod.SafeGetBooleanFromObj(row["IsOutput"])
                });
            }
            return lstLocationInfo;
        }

        public void GetPackageStation(int lineId, out DataTable TabLocationInfo)
        {
            string sql = @"SELECT sp.ParameterName,sl.* FROM tbDic_Station station
                         INNER JOIN tbDic_WorkStation ws ON ws.Id=station.WorkStationId
                         INNER JOIN tbDic_Line line ON line.Id=station.LineId
                         INNER JOIN tbDic_SpecialLocation sl ON sl.StationId=station.Id
                         INNER JOIN tb_Dic_SpecialParameter sp ON sp.Id=sl.ParameterId
                         WHERE ws.IsLineOutput='true' AND sl.IsOutput='true' AND line.Id={0}";

            sql = string.Format(sql, lineId);
            //获取采集整线产量的Location(最后一站)
            TabLocationInfo = DALLib<LocationInfo>.DataAccess.ExecuteDataTable(sql);
        }
    }
}
