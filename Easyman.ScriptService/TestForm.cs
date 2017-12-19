using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Easyman.ScriptService
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
            buttonStop.Enabled = false;
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            buttonStart.Enabled = false;
            Main.Start();
            buttonStop.Enabled = true;
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            buttonStop.Enabled = false;
            Main.Stop();
            buttonStart.Enabled = true;
        }

        private void buttonTest_Click(object sender, EventArgs e)
        {
            string tableName = "AAA";
            DataTable dt = BLL.EM_SCRIPT_NODE_CASE.Instance.GetTable();
            using (Easyman.Librarys.DBHelper.BDBHelper dbHelper = new Librarys.DBHelper.BDBHelper())
            {
                if (dbHelper.TableIsExists(tableName))
                {
                    dbHelper.Drop(tableName);
                }
                dbHelper.CreateTableFromDataTable(tableName, dt);
            }
        }
    }
}
