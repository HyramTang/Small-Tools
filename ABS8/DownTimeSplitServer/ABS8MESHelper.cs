using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Linq;
using System.Text;
using AJ.Andon.Entity;
using AJ.Andon.Entity.Dictionary;
using AJ.Andon.Entity.Report;

namespace DownTimeSplitService
{
    public class ABS8MESHelper
    {
        public static DataTable GetMesTable(DateTime dtStart, DateTime dtEnd, List<LocationInfo> lstLocationInfo)
        {
                string SqlGetProductiveData = string.Empty;
                string SqlGetParameterName = "SELECT Top 1 * FROM tb_Dic_SpecialParameter WHERE Id=" + lstLocationInfo[0].ParameterId + "";
                SpecialParameter sp = DALLib<SpecialParameter>.DataAccess.GetOneBySQL(SqlGetParameterName);

            
                List<ProductiveDataNew> UnDisposedData = new List<ProductiveDataNew>();
                List<ProductiveDataNew> DisposedData = new List<ProductiveDataNew>();

            OracleConnection conn = new OracleConnection(System.Configuration.ConfigurationManager.ConnectionStrings["MESConn"].ConnectionString);
//            string sql = @"select   to_char(time, 'yyyy-mm-dd hh24:mi:ss') as createtime, line, process_number, type_number,RESULT_STATE,
//                uniquepart_id
//                from szhquality.cry_abs9_palletize_ecu_output tb  where  time  between   to_timestamp('{0}','yyyy-mm-dd hh24:mi:ss') 
//                and   to_timestamp('{1}','yyyy-mm-dd hh24:mi:ss')
//                and line in({2})  and RESULT_STATE=1   and uniquepart_id  not   like '%G'  ";
//            sql = string.Format(sql, dtStart.ToString("yyyy-MM-dd HH:mm:ss"), dtEnd.ToString("yyyy-MM-dd HH:mm:ss"), stationlocations);

            string sql = @"select THMID,LINIENNR,LINIENNAME,STATIONNR,STATIONNAME,NAME,WERT,to_char(time, 'yyyy-mm-dd hh24:mi:ss') as TIME
                        from fmsh.auswdaten where time>=to_date('{0}','yyyy-mm-dd hh24:mi:ss') and time<to_date('{1}','yyyy-mm-dd hh24:mi:ss') AND LINIENNR IN ({2}) ORDER BY TIME";
            sql = string.Format(sql, dtStart, dtEnd, lstLocationInfo[0].LineCode);

            try
            {
                conn.Open();
                OracleCommand cmd = new OracleCommand(sql, conn);
                OracleDataAdapter dapt = new OracleDataAdapter();
                dapt.SelectCommand = cmd;
                DataSet ds = new DataSet();
                dapt.Fill(ds, "product");
                DataTable dt = ds.Tables["product"];
                return dt;
            }
            catch (Exception ex)
            {
                DMES.Utility.Logger.Log4netHelper.Error("获取信息", ex);
                return null;
            }
            finally
            {
                conn.Close();
            }
        }
        public static void AnalyzeDatatable(DataTable dtProduct, int lineid, string lineName, DateTime p_dtStart, DateTime p_dtEnd, int spiltcount, string spiltname)
        {

            //在这里会把数据写入到更新的system表中。

            //计算一个时间段内的数据。



            List<FlowProduction> lstFlowProduction = new List<FlowProduction>();
            List<FlowDowntime> lstDowntime = new List<FlowDowntime>();
            if (dtProduct == null)
            {
                dtProduct = new DataTable();
            }


            FlowProduction m_production = new FlowProduction();


            DateTime dtStart = p_dtStart;
            DateTime dtEnd = p_dtEnd;


            string lastproductname = "";
            int output = spiltcount;
            for (int i = 0; i < dtProduct.Rows.Count; i++)
            {
                string temp = lastproductname = DMES.Utility.CommonMethod.SafeGetStringFromObj(dtProduct.Rows[i]["type_number"]);
                DateTime dt__end = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(dtProduct.Rows[i]["createtime"]);

                if (i == 0)
                {
                    //找出最后一条纪录的时间
                    string sqllastoutput = "select top 1 * from tbFlowProduction where LineId={0} and RealStartTime>'{1}'  order by RealStartTime asc ";
                    sqllastoutput = string.Format(sqllastoutput, lineid, dtStart);
                    FlowProduction __flowproduction = DALLib<FlowProduction>.DataAccess.GetOneBySQL(sqllastoutput);
                    if (__flowproduction != null)
                    {

                        if (!String.IsNullOrEmpty(spiltname) && spiltname != temp)
                        {


                            if (__flowproduction.RealEndTime < dt__end)
                            {
                                //写入一条换型纪录

                                WriteOneU6Record(spiltname, temp, __flowproduction.RealEndTime, dt__end, lineid, lineName);
                                dtStart = dt__end;


                            }
                        }

                    }
                }




                if (lastproductname == "")
                {
                    lastproductname = temp;
                }

                if (lastproductname == temp)
                {
                    output++;
                }
                else
                {
                    DateTime dt__last = DMES.Utility.CommonMethod.SafeGetDateTimeFromObj(dtProduct.Rows[i - 1]["createtime"]);
                    WriteOneProductTime(lastproductname, dtStart, dt__last, output, lineid, lineName);
                    WriteOneU6Record(lastproductname, temp, dt__last, dt__end, lineid, lineName);
                    dtStart = dt__end;
                    output = 1;
                }
                lastproductname = temp;
            }

            if (dtProduct.Rows.Count > 0)
            {
                //写入最后一条纪录

                WriteOneProductTime(lastproductname, dtStart, dtEnd, output, lineid, lineName);
            }


        }



        private static void WriteOneProductTime(string productname, DateTime dtStart, DateTime dtEnd, int output, int lineId, string lineName)
        {
            FlowProduction m_production = new FlowProduction();
            m_production.ProductName = productname;
            m_production.RealStartTime = dtStart;
            m_production.RealEndTime = dtEnd;
            m_production.ProductOutput = output;
            m_production.Person = "output";
            //接下来一些算CT之类的加上来。
            m_production.LineId = lineId;

            //查找CT  
            string sqlct = @" select [tbDic_Product_Line_CT].*  from [dbo].[tbDic_Product]
                     inner join 
                    [dbo].[tbDic_Product_Line_CT] on [tbDic_Product].Id=[tbDic_Product_Line_CT].ProductId
                    where LineId={0} and [tbDic_Product].PCBA='{1}'";

            sqlct = string.Format(sqlct, lineId, m_production.ProductName);

            Product_Line_CT linct = DALLib<Product_Line_CT>.DataAccess.GetOneBySQL(sqlct);
            if (linct == null)
            {
                linct = new Product_Line_CT();
            }

            decimal d_linect = DMES.Utility.CommonMethod.SafeGetDecimalFromObject(linct.CycleTime, 0);
            if (d_linect <= 0)
            {
                d_linect = 11;
            }

            m_production.PlanedCT = d_linect;
            decimal span_s = DMES.Utility.CommonMethod.SafeGetDecimalFromObject(m_production.RealEndTime.Subtract(m_production.RealStartTime).TotalMinutes, 0);

            m_production.RealProductTime = span_s;


            DALLib<FlowProduction>.DataAccess.SaveOne(m_production);

        }

        private static void WriteOneU6Record(string productname, string tempproduct, DateTime dtEnd, DateTime dtTemp, int lineId, string lineName)
        {
            FlowDowntime m_downtime = new FlowDowntime();
            m_downtime.DowntimeCode = "U6";
            m_downtime.DowntimeType = "U";
            m_downtime.StartTime = dtEnd;
            m_downtime.EndTime = dtTemp;
            m_downtime.UnPlandDowntimeSpan = DMES.Utility.CommonMethod.SafeGetDecimalFromObject(
                m_downtime.EndTime.Subtract(m_downtime.StartTime).TotalMinutes, 0);

            m_downtime.LineName = lineName;
            m_downtime.LineId = lineId;
            DALLib<FlowDowntime>.DataAccess.SaveOne(m_downtime);


            FlowProduction flowproduction = new FlowProduction();
            flowproduction.RealEndTime = m_downtime.EndTime;
            flowproduction.RealStartTime = m_downtime.StartTime;
            flowproduction.UnplanDowntimeCode = "U6";
            flowproduction.UnplanedDowntimeSpan = m_downtime.UnPlandDowntimeSpan;
            flowproduction.Person = "changeover";
            DALLib<FlowProduction>.DataAccess.SaveOne(flowproduction);


        }
    }
}
