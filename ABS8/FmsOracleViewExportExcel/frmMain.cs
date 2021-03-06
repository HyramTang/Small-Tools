﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OracleClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SpreadsheetGear;

namespace FmsOracleViewExportExcel
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }
        DataTable dt = new DataTable();
        string SqlContent = string.Empty;
        OracleDataAdapter dapt = new OracleDataAdapter();
        DataSet ds = new DataSet();

        public delegate void daili();
        daili dailiEvent = null;
        private void btnSelect_Click(object sender, EventArgs e)
        {
            //Thread GetDataThread = new Thread(GetDataMethod);
            //GetDataThread.Start();


            try
            {
                string connstr = System.Configuration.ConfigurationManager.ConnectionStrings["StrConn"].ConnectionString;
                OracleConnection conn = new OracleConnection(connstr);

                if (!string.IsNullOrEmpty(txtSqlCommand.Text))
                    SqlContent = txtSqlCommand.Text;
                else
                {
                    MessageBox.Show("SqlCommand Can't Empty！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                conn.Open();
                OracleCommand cmd = new OracleCommand(SqlContent, conn);
                dapt = new OracleDataAdapter(cmd);
                ds = new DataSet();

                dataGridView1.Cursor = Cursors.WaitCursor;

                //Thread GetDataThread = new Thread(GetDataMethod);
                //GetDataThread.Start();

                dapt.Fill(ds, "Table1");

                conn.Close();
                dt = ds.Tables["Table1"];

                dataGridView1.DataSource = dt;
                dataGridView1.Cursor = Cursors.Default;
                if (SqlContent.Contains("*"))
                    dataGridView1.Columns["TIME"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExportExcel_Click(object sender, EventArgs e)
        {
            if (dataGridView1 != null && dataGridView1.Rows.Count > 0)
            {
                //Excel操作
                SpreadsheetGear.IWorkbook workbook = SpreadsheetGear.Factory.GetWorkbook();
                SpreadsheetGear.IWorksheet worksheet = workbook.Worksheets["Sheet1"];
                worksheet.Name = "Spice Order";

                // Get the top left cell for the DataTable.
                SpreadsheetGear.IRange range = worksheet.Cells["A1"];

                // Copy the DataTable to the worksheet range.
                range.CopyFromDataTable(dt, SpreadsheetGear.Data.SetDataFlags.None);

                // Auto size all worksheet columns which contain data
                worksheet.UsedRange.Columns.AutoFit();


                saveFileDialog1.Filter = "Excel文件（*.xls）|*.xls";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    worksheet.SaveAs(saveFileDialog1.FileName, FileFormat.Excel8);
                else
                    return;
            }
            else
                MessageBox.Show("不能导出为空的Excel！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            string strTime = "select fmsh.auswdaten.* from fmsh.auswdaten WHERE (time>=to_date('{0}','yyyy-mm-dd hh24:mi:ss') and time<to_date('{1}','yyyy-mm-dd hh24:mi:ss')) and LINIENNR=67 and STATIONNR=40";
            txtSqlCommand.Text = string.Format(strTime, dateTimePicker1.Value.ToString(), dateTimePicker2.Value.ToString());
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            //txtSqlCommand.Text = "select fmsh.auswdaten.* from fmsh.auswdaten WHERE (time>=to_date('2015-05-13 19:40:00','yyyy-mm-dd hh24:mi:ss') and time<to_date('2015-05-13 19:55:00','yyyy-mm-dd hh24:mi:ss')) and LINIENNR=66 and STATIONNR=40";
        }
    }
}
