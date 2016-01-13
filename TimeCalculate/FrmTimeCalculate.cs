using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TimeCalculate
{
    public partial class FrmTimeCalculate : Form
    {
        public FrmTimeCalculate()
        {
            InitializeComponent();
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            DateTime dt1 = dateTimePicker1.Value;
            DateTime dt2 = dateTimePicker2.Value;

            int totalSeconds = 0;
            if (dt1 > dt2)
            {
                totalSeconds = Convert.ToInt32(dt1.Subtract(dt2).TotalSeconds);
            }
            else if (dt1 < dt2)
            {
                totalSeconds = Convert.ToInt32(dt2.Subtract(dt1).TotalSeconds);
            }

            int hours = 0;
            int mins = 0;
            int seconds = 0;
            if (totalSeconds > 60)
            {
                mins = totalSeconds / 60;
                seconds = totalSeconds % 60;
            }
            else if (totalSeconds > 3600)
            {
                hours = totalSeconds / 3600;
                int remainseconds = totalSeconds % 3600;

                if (remainseconds > 60)
                {
                    mins = remainseconds / 60;
                    seconds = remainseconds % 60;
                }
                else
                    seconds = remainseconds;
            }
            else
                seconds = totalSeconds;

            textBox1.Text = hours.ToString();
            textBox2.Text = mins.ToString();
            textBox3.Text = seconds.ToString();
        }
    }
}
