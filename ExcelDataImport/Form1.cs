using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OracleDataAccess;

namespace ExcelDataImport
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        string FilePath = "";
        DataTable ImportData = new DataTable();
        private void btnGetExcel_Click(object sender, EventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.Title = "打开";
            OFD.Filter = "所文件|*.*";
            if (OFD.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = OFD.FileName;
                FilePath = OFD.FileName;
            }
            else
                return;


            //初始化一个DataTable
            DataTable dt = new DataTable();
            //获取Excel内容
            SpreadsheetGear.IWorkbook workbook = SpreadsheetGear.Factory.GetWorkbook(FilePath);
            SpreadsheetGear.IWorksheet worksheet = workbook.Worksheets[0];
            DataSet dataSet = workbook.GetDataSet(SpreadsheetGear.Data.GetDataFlags.FormattedText);

            //取第一张工作簿
            dt = dataSet.Tables[0];

            //去Tab中空的数据行
            //List<DataRow> lstDataRow = new List<DataRow>();
            //foreach (DataRow row in dt.Rows)
            //{
            //    if (string.IsNullOrEmpty(row[0].ToString().Replace(" ", "")))
            //        lstDataRow.Add(row);
            //}
            //foreach (DataRow row in lstDataRow)
            //{
            //    dt.Rows.Remove(row);
            //}

            ImportData = dt;
            ImportData.TableName = worksheet.Name;
            dataGridView1.DataSource = dt;
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            if (ImportData != null && ImportData.Rows.Count > 0)
            {
                string SqlInsert = "";
                int EffectRows = 0;
                OracleDALLib DALLib = new OracleDALLib("StrConn");
                foreach (DataRow row in ImportData.Rows)
                {
                    SqlInsert=@"INSERT INTO COPRODUCTION VALUES
                        ('" + row["THMID"] + "'," + row["LINIENNR"] + ",'" + row["LINIENNAME"] + "'," + row["STATIONNR"] + ",'" + row["STATIONNAME"] + "','" + row["NAME"] + "','" + row["WERT"] + "',to_date('" + row["TIME"] + "','yyyy-mm-dd hh24:mi:ss'))  ";

                    EffectRows+= DALLib.ExcuteIDU(SqlInsert);
                }
                MessageBox.Show("新增" + EffectRows + "行!");
            }
            else
                MessageBox.Show("数据不能为空！");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (ImportData != null && ImportData.Rows.Count > 0)
            {
                string SqlInsert = "";
                int EffectRows = 0;
                OracleDALLib DALLib = new OracleDALLib("StrConn");
                foreach (DataRow row in ImportData.Rows)
                {
                    SqlInsert = @"INSERT INTO PRODUCTION VALUES
                        ('" + row["THMID"] + "'," + row["LINIENNR"] + ",'" + row["LINIENNAME"] + "'," + row["STATIONNR"] + ",'" + row["STATIONNAME"] + "','" + row["NAME"] + "','" + row["WERT"] + "',to_date('" + row["TIME"] + "','yyyy-mm-dd hh24:mi:ss'))  ";

                    EffectRows += DALLib.ExcuteIDU(SqlInsert);
                }
                MessageBox.Show("新增" + EffectRows + "行!");
            }
            else
                MessageBox.Show("数据不能为空！");
        }
    }
}
