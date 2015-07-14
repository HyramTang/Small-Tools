namespace SqlCommandTool
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.btnAction1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btnAction2 = new System.Windows.Forms.Button();
            this.btnAction3 = new System.Windows.Forms.Button();
            this.btnAction4 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnAction1
            // 
            this.btnAction1.Location = new System.Drawing.Point(85, 128);
            this.btnAction1.Name = "btnAction1";
            this.btnAction1.Size = new System.Drawing.Size(75, 23);
            this.btnAction1.TabIndex = 0;
            this.btnAction1.Text = "Action1";
            this.btnAction1.UseVisualStyleBackColor = true;
            this.btnAction1.Visible = false;
            this.btnAction1.Click += new System.EventHandler(this.btnAction1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("微软雅黑", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.ForeColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(49, 36);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(139, 22);
            this.label1.TabIndex = 1;
            this.label1.Text = "Used only once!";
            // 
            // btnAction2
            // 
            this.btnAction2.Location = new System.Drawing.Point(85, 157);
            this.btnAction2.Name = "btnAction2";
            this.btnAction2.Size = new System.Drawing.Size(75, 23);
            this.btnAction2.TabIndex = 2;
            this.btnAction2.Text = "Action2";
            this.btnAction2.UseVisualStyleBackColor = true;
            this.btnAction2.Visible = false;
            this.btnAction2.Click += new System.EventHandler(this.btnAction2_Click);
            // 
            // btnAction3
            // 
            this.btnAction3.Location = new System.Drawing.Point(85, 186);
            this.btnAction3.Name = "btnAction3";
            this.btnAction3.Size = new System.Drawing.Size(75, 23);
            this.btnAction3.TabIndex = 3;
            this.btnAction3.Text = "Action3";
            this.btnAction3.UseVisualStyleBackColor = true;
            this.btnAction3.Visible = false;
            this.btnAction3.Click += new System.EventHandler(this.btnAction3_Click);
            // 
            // btnAction4
            // 
            this.btnAction4.Location = new System.Drawing.Point(85, 215);
            this.btnAction4.Name = "btnAction4";
            this.btnAction4.Size = new System.Drawing.Size(75, 23);
            this.btnAction4.TabIndex = 4;
            this.btnAction4.Text = "Action4";
            this.btnAction4.UseVisualStyleBackColor = true;
            this.btnAction4.Visible = false;
            this.btnAction4.Click += new System.EventHandler(this.btnAction4_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(71, 99);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(106, 23);
            this.button1.TabIndex = 5;
            this.button1.Text = "删除错误的产量数据";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Visible = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(85, 71);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 6;
            this.button2.Text = "Action";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(243, 242);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnAction4);
            this.Controls.Add(this.btnAction3);
            this.Controls.Add(this.btnAction2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnAction1);
            this.Name = "Form1";
            this.Text = "SqlCommandTool";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnAction1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnAction2;
        private System.Windows.Forms.Button btnAction3;
        private System.Windows.Forms.Button btnAction4;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}

