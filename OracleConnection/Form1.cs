using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OracleClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OracleDataAccess;

namespace OracleConnectionTool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string connstr = System.Configuration.ConfigurationManager.ConnectionStrings["StrConn"].ConnectionString;
            OracleConnection conn = new OracleConnection(connstr);



            conn.Open();

            OracleCommand cmd = new OracleCommand("SELECT * FROM dept", conn);
            OracleDataAdapter adpt = new OracleDataAdapter(cmd);

            DataSet dt = new DataSet();


            adpt.Fill(dt, "table");
            conn.Close();

            DataTable tab = dt.Tables["table"];
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OracleDALLib dal = new OracleDALLib("StrConn");
            DataTable tab = dal.Query("SELECT * FROM dept");

            //DataTable tab = dal.Query("SELECT COPRODUCTION.*,to_char(time,'yyyy-mm-dd hh24:mi:ss') AS TIMES FROM COPRODUCTION");

            
            dataGridView1.DataSource = tab;
            //dataGridView1.Columns["TIMES"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
        }
    }
}
