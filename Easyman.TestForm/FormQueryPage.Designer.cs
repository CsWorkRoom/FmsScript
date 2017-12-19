namespace Easyman.TestForm
{
    partial class FormQueryPage
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.comboBoxDbType = new System.Windows.Forms.ComboBox();
            this.textBoxConn = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxSql = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.numericUpDownPageSize = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownPageIndex = new System.Windows.Forms.NumericUpDown();
            this.labelTime = new System.Windows.Forms.Label();
            this.buttonPage = new System.Windows.Forms.Button();
            this.buttonFlow = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.labelInfo = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPageSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPageIndex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // comboBoxDbType
            // 
            this.comboBoxDbType.FormattingEnabled = true;
            this.comboBoxDbType.Items.AddRange(new object[] {
            "Oracle",
            "DB2"});
            this.comboBoxDbType.Location = new System.Drawing.Point(14, 37);
            this.comboBoxDbType.Name = "comboBoxDbType";
            this.comboBoxDbType.Size = new System.Drawing.Size(78, 20);
            this.comboBoxDbType.TabIndex = 0;
            this.comboBoxDbType.Text = "Oracle";
            // 
            // textBoxConn
            // 
            this.textBoxConn.Location = new System.Drawing.Point(98, 12);
            this.textBoxConn.Multiline = true;
            this.textBoxConn.Name = "textBoxConn";
            this.textBoxConn.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxConn.Size = new System.Drawing.Size(898, 45);
            this.textBoxConn.TabIndex = 1;
            this.textBoxConn.Text = "User ID=C##EM2;Password=C##EM2;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(H" +
    "OST=139.196.212.68)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL)));Persist Secur" +
    "ity Info=True;";
            this.textBoxConn.DoubleClick += new System.EventHandler(this.textBoxConn_DoubleClick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "数据库配置";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 60);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(23, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "SQL";
            // 
            // textBoxSql
            // 
            this.textBoxSql.Location = new System.Drawing.Point(12, 75);
            this.textBoxSql.Multiline = true;
            this.textBoxSql.Name = "textBoxSql";
            this.textBoxSql.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxSql.Size = new System.Drawing.Size(982, 65);
            this.textBoxSql.TabIndex = 4;
            this.textBoxSql.Text = "SELECT * FROM EM_MODULE_EVENT";
            this.textBoxSql.DoubleClick += new System.EventHandler(this.textBoxSql_DoubleClick);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 154);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(341, 12);
            this.label3.TabIndex = 5;
            this.label3.Text = "每页查询              条，查询第             页，用时： ";
            // 
            // numericUpDownPageSize
            // 
            this.numericUpDownPageSize.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDownPageSize.Location = new System.Drawing.Point(66, 150);
            this.numericUpDownPageSize.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDownPageSize.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDownPageSize.Name = "numericUpDownPageSize";
            this.numericUpDownPageSize.Size = new System.Drawing.Size(75, 21);
            this.numericUpDownPageSize.TabIndex = 6;
            this.numericUpDownPageSize.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // numericUpDownPageIndex
            // 
            this.numericUpDownPageIndex.Location = new System.Drawing.Point(208, 150);
            this.numericUpDownPageIndex.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDownPageIndex.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownPageIndex.Name = "numericUpDownPageIndex";
            this.numericUpDownPageIndex.Size = new System.Drawing.Size(72, 21);
            this.numericUpDownPageIndex.TabIndex = 7;
            this.numericUpDownPageIndex.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // labelTime
            // 
            this.labelTime.AutoSize = true;
            this.labelTime.Location = new System.Drawing.Point(340, 154);
            this.labelTime.Name = "labelTime";
            this.labelTime.Size = new System.Drawing.Size(35, 12);
            this.labelTime.TabIndex = 8;
            this.labelTime.Text = "0毫秒";
            // 
            // buttonPage
            // 
            this.buttonPage.Location = new System.Drawing.Point(484, 147);
            this.buttonPage.Name = "buttonPage";
            this.buttonPage.Size = new System.Drawing.Size(75, 23);
            this.buttonPage.TabIndex = 9;
            this.buttonPage.Text = "普通分页";
            this.buttonPage.UseVisualStyleBackColor = true;
            this.buttonPage.Click += new System.EventHandler(this.buttonPage_Click);
            // 
            // buttonFlow
            // 
            this.buttonFlow.Location = new System.Drawing.Point(565, 147);
            this.buttonFlow.Name = "buttonFlow";
            this.buttonFlow.Size = new System.Drawing.Size(75, 23);
            this.buttonFlow.TabIndex = 10;
            this.buttonFlow.Text = "流式分页";
            this.buttonFlow.UseVisualStyleBackColor = true;
            this.buttonFlow.Click += new System.EventHandler(this.buttonFlow_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(14, 177);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.Size = new System.Drawing.Size(980, 493);
            this.dataGridView1.TabIndex = 11;
            // 
            // labelInfo
            // 
            this.labelInfo.AutoSize = true;
            this.labelInfo.Location = new System.Drawing.Point(12, 674);
            this.labelInfo.Name = "labelInfo";
            this.labelInfo.Size = new System.Drawing.Size(53, 12);
            this.labelInfo.TabIndex = 12;
            this.labelInfo.Text = "运行信息";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(646, 147);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 13;
            this.button1.Text = "流式分页2";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // FormQueryPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 730);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.labelInfo);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.buttonFlow);
            this.Controls.Add(this.buttonPage);
            this.Controls.Add(this.labelTime);
            this.Controls.Add(this.numericUpDownPageIndex);
            this.Controls.Add(this.numericUpDownPageSize);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxSql);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxConn);
            this.Controls.Add(this.comboBoxDbType);
            this.Name = "FormQueryPage";
            this.Text = "分页查询测试";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.FormQueryPage_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPageSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPageIndex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxDbType;
        private System.Windows.Forms.TextBox textBoxConn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxSql;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numericUpDownPageSize;
        private System.Windows.Forms.NumericUpDown numericUpDownPageIndex;
        private System.Windows.Forms.Label labelTime;
        private System.Windows.Forms.Button buttonPage;
        private System.Windows.Forms.Button buttonFlow;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label labelInfo;
        private System.Windows.Forms.Button button1;
    }
}