using Easyman.Librarys.DBHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Easyman.TestForm
{
    public partial class FormQueryPage : Form
    {
        public FormQueryPage()
        {
            InitializeComponent();
        }

        private void FormQueryPage_Load(object sender, EventArgs e)
        {
            textBoxConn.Width = this.Width - 126;
            textBoxSql.Width = this.Width - 44;
            dataGridView1.Width = this.Width - 44;
            dataGridView1.Height = this.Height - 275;
            comboBoxDbType.SelectedIndex = 0;
            labelInfo.Text = "";
        }

        private void buttonPage_Click(object sender, EventArgs e)
        {
            try
            {
                labelInfo.Text = "";
                TimeSpan ts = new TimeSpan();
                string dbType = comboBoxDbType.Text;
                string sql = textBoxSql.Text;
                int pageSize = (int)numericUpDownPageSize.Value;
                int pageIndex = (int)numericUpDownPageIndex.Value;
                int start = (pageIndex - 1) * pageSize + 1;
                DataTable dt = new DataTable();

                using (BDBHelper dbHelper = new BDBHelper(dbType, textBoxConn.Text))
                {
                    DateTime begin = DateTime.Now;
                    dt = dbHelper.ExecuteDataTablePage(sql, pageSize, pageIndex);
                    ts = DateTime.Now - begin;
                }

                dataGridView1.DataSource = dt;
                labelTime.Text = ts.TotalMilliseconds + "毫秒";
                labelInfo.Text = "返回时间：" + DateTime.Now.ToString();
            }
            catch (Exception ex)
            {
                labelInfo.Text = ex.Message;
            }
        }

        private void buttonFlow_Click(object sender, EventArgs e)
        {
            try
            {
                labelInfo.Text = "";
                TimeSpan ts = new TimeSpan();
                string dbType = comboBoxDbType.Text;
                string sql = textBoxSql.Text;
                int pageSize = (int)numericUpDownPageSize.Value;
                int pageIndex = (int)numericUpDownPageIndex.Value;
                int start = (pageIndex - 1) * pageSize + 1;
                int rowsCount = 0;
                DataTable dt = new DataTable();
                using (BDBHelper dbHelper = new BDBHelper(dbType, textBoxConn.Text))
                {
                    DateTime begin = DateTime.Now;
                    using (IDataReader reader = dbHelper.ExecuteReader(sql))
                    {
                        for (int c = 0; c < reader.FieldCount; c++)
                        {
                            dt.Columns.Add(reader.GetName(c), reader.GetFieldType(c));
                        }
                        
                        int i = 0;
                        while (reader.Read())
                        {
                            i++;
                            if (i < start)
                            {
                                continue;
                            }

                            DataRow dr = dt.NewRow();
                            for (int c = 0; c < reader.FieldCount; c++)
                            {
                                dr[c] = reader.GetValue(c);
                            }

                            dt.Rows.Add(dr);

                            rowsCount++;

                            if (rowsCount >= pageSize)
                            {
                                break;
                            }
                        }
                        ts = DateTime.Now - begin;
                    }
                }

                dataGridView1.DataSource = dt;
                labelTime.Text = ts.TotalMilliseconds + "毫秒";
                labelInfo.Text = "返回时间：" + DateTime.Now.ToString();
            }
            catch (Exception ex)
            {
                labelInfo.Text = ex.Message;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                labelInfo.Text = "";
                TimeSpan ts = new TimeSpan();
                string dbType = comboBoxDbType.Text;
                string sql = textBoxSql.Text;
                int pageSize = (int)numericUpDownPageSize.Value;
                int pageIndex = (int)numericUpDownPageIndex.Value;
                int start = (pageIndex - 1) * pageSize + 1;
                DataTable dt = new DataTable();

                using (BDBHelper dbHelper = new BDBHelper(dbType, textBoxConn.Text))
                {
                    DateTime begin = DateTime.Now;
                    dt = dbHelper.ExecuteDataTablePageWithReader(sql, pageSize, pageIndex);
                    ts = DateTime.Now - begin;
                }

                dataGridView1.DataSource = dt;
                labelTime.Text = ts.TotalMilliseconds + "毫秒";
                labelInfo.Text = "返回时间：" + DateTime.Now.ToString();
            }
            catch (Exception ex)
            {
                labelInfo.Text = ex.Message;
            }
        }

        private void textBoxConn_DoubleClick(object sender, EventArgs e)
        {
            textBoxConn.SelectAll();
        }

        private void textBoxSql_DoubleClick(object sender, EventArgs e)
        {
            textBoxSql.SelectAll();
        }
    }
}
