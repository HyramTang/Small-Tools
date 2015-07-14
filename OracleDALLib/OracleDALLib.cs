using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Linq;
using System.Text;

namespace OracleDataAccess
{
    public class OracleDALLib
    {
        string Connectstring;
        OracleConnection conn;
        public OracleDALLib(string ConnectStr)
        {
            Connectstring = System.Configuration.ConfigurationManager.ConnectionStrings[ConnectStr].ConnectionString;
            conn = new OracleConnection(Connectstring);
        }

        //增删改
        public int ExcuteIDU(string cmdstr)
        {
            conn.Open();
            OracleCommand cmd = new OracleCommand(cmdstr, conn);
            int EffectRows = cmd.ExecuteNonQuery();
            conn.Close();
            return EffectRows;
        }

        //查询
        public DataTable Query(string cmdstr)
        {
            conn.Open();
            OracleCommand cmd = new OracleCommand(cmdstr, conn);
            OracleDataAdapter adpt = new OracleDataAdapter(cmd);

            DataSet ds = new DataSet();
            adpt.Fill(ds);

            DataTable tab=null;
            if(ds!=null)
                tab=ds.Tables[0];

            return tab;
        }
    }
}
