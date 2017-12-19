using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Easyman.Librarys;
using Easyman.Librarys.Cron;
using Easyman.Librarys.DBHelper;
using Oracle.ManagedDataAccess.Client;
using Easyman.Librarys.Config;
using System.IO;
using System.Diagnostics;
//using System.Configuration;
using System.Collections;
using System.Text.RegularExpressions;

namespace Easyman.TestForm
{
    public partial class FormMain : Form
    {
        public static bool IsRun = false;
        private Form form;
        private List<BCron> listCron = new List<BCron>();

        public FormMain()
        {
            InitializeComponent();
        }

        private List<BCron> cronList = new List<BCron>();

        /// <summary>
        /// 开始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonStart_Click(object sender, EventArgs e)
        {
            IsRun = true;
            listCron = new List<BCron>();
            int i = 0;
            foreach (string exp in textBox1.Text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                CronTest cron = new CronTest(i, exp);
                listCron.Add(cron);
                cron.Start();
                i++;
            }

            MessageBox.Show("共有" + i + "个计划任务已经启动。");
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            IsRun = false;

            foreach(var cron in listCron)
            {
                cron.Stop();
            }

            MessageBox.Show("共有" + listCron.Count + "个计划任务已经停止。");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Easyman.Service.Server.ExtScriptHelper helper = new Service.Server.ExtScriptHelper();
            helper.dbServer = new Service.Domain.DBServer();
            helper.dbServer.ID = 1;
            helper.dbServer.BYNAME = "139远程库";
            helper.dbServer.DB_TYPE = "ORACLE";
            helper.dbServer.ConnectionStr = string.Format("Data Source={0}:{1}/{2};User Id={3};Password={4};Connection Timeout =3600", "139.196.212.68", 1521, "ORCL", "C##ABPBASE", "C##ABPBASE");

            string sql = "CREATE TABLE TB_222 (ID NUMBER(10))";
            int i = helper.execute(sql);
            MessageBox.Show("aaa :" + i);
        }

        /// <summary>
        /// 导入数据测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            string sql = "select * from em_script_node";
            DataTable dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("NAME", typeof(string));
            dt.Columns.Add("CREATETIME", typeof(DateTime));
            for (int i = 0; i < 10; i++)
            {
                DataRow dr = dt.NewRow();
                dr[0] = i;
                dr[1] = "name is: " + i;
                dr[2] = DateTime.Now.AddMinutes(i);

                dt.Rows.Add(dr);
            }
            string tableName = "zz_0628";
            int n = 0;
            TimeSpan ts = new TimeSpan();
            using (BDBHelper dbHelper = new BDBHelper())
            {
                //从另外一张表查询出结果再导入
                //dt = dbHelper.ExecuteDataTable(sql);
                try
                {
                    dbHelper.Drop(tableName);
                }
                catch
                {

                }
                if (dbHelper.TableIsExists(tableName))
                {
                    dbHelper.Drop(tableName, false);
                }
                dbHelper.CreateTableFromDataTable(tableName, dt);
                DateTime begin = DateTime.Now;
                n = dbHelper.LoadDataInDataTable(tableName, dt);
                ts = DateTime.Now - begin;
            }
            MessageBox.Show(string.Format("共有【{0}】条记录导入表【{1}】，用时【{2}】毫秒。", n, tableName, ts.TotalMilliseconds));
        }

        /// <summary>
        /// 文件导DB2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            string tableName = "ZZZ_TEST_3";
            string basePath = BConfig.BaseDirectory;
            string fileName = "loadtest";

            string ip = "10.95.240.9";
            int port = 443;
            string dbName = "DMDY";
            string user = "ZY_FAS";
            string password = "easyman123@@@";
            string createSql = string.Format("CREATE TABLE {0} (ID INT, NAME NVARCHAR2(50))", tableName);
            string clearSql = string.Format("DELETE FROM {0}", tableName);

            string txtFile = basePath + fileName + ".txt";
            string batFile = basePath + fileName + ".bat";
            StringBuilder sbMessage = new StringBuilder();
            //先删除当前目录相应文件
            try
            {
                DirectoryInfo di = new DirectoryInfo(basePath);
                FileInfo[] fis = di.GetFiles(fileName + ".*");
                foreach (FileInfo fi in fis)
                {
                    fi.Delete();
                }
                sbMessage.AppendLine(string.Format("成功删除了【{0}】个旧文件", fis.Length));
            }
            catch (Exception ex)
            {
                sbMessage.AppendLine("删除旧文件失败");
                sbMessage.AppendLine(ex.ToString());
            }

            //写数据文件
            StringBuilder txtContent = new StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                txtContent.AppendLine(i + "\tname is:" + i);
            }
            //txtContent.AppendLine(11 + "aaa\tname is:" + 11);
            File.WriteAllText(txtFile, txtContent.ToString());


            //写批处理文件
            StringBuilder batContent = new StringBuilder();

            batContent.AppendLine(string.Format("db2 catalog tcpip node N240 remote {0} server {1}", ip, port));
            batContent.AppendLine(string.Format("db2 catalog database {0} as DMDY240 at node N240 authentication server", dbName));
            batContent.AppendLine(string.Format("db2 connect to DMDY240 user {0} using {1}", user, password));
            batContent.AppendLine(string.Format("db2 load client from '{0}' of del modified by codepage = 1208 COLDEL0x09 insert into {1}", txtFile, tableName));
            File.WriteAllText(batFile, batContent.ToString());

            //创建表
            using (BDBHelper dbHelper = new BDBHelper("DB2", ip, port, user, password, dbName, dbName))
            {
                //try
                //{
                //    dbHelper.ExecuteNonQuery(createSql);
                //    sbMessage.AppendLine("创建表成功");
                //}
                //catch (Exception ex)
                //{
                //    sbMessage.AppendLine("创建表出错");
                //    sbMessage.AppendLine(ex.ToString());
                //}

                try
                {
                    dbHelper.ExecuteNonQuery(clearSql);
                    sbMessage.AppendLine("清空表成功");
                }
                catch (Exception ex)
                {
                    sbMessage.AppendLine("清空表出错");
                    sbMessage.AppendLine(ex.ToString());
                }
            }

            //调用批处理，导入数据
            string output = string.Empty;
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = @"C:\Program Files\IBM\SQLLIB_01\BIN\db2cwadmin.bat";
                info.Arguments = batFile;
                Process p = System.Diagnostics.Process.Start(info);
                sbMessage.AppendLine("导入表成功");
                sbMessage.AppendLine(output);
            }
            catch (Exception ex)
            {
                sbMessage.AppendLine("导入表失败");
                sbMessage.AppendLine(ex.ToString());
            }

            MessageBox.Show(sbMessage.ToString());

            return;

            try
            {
                batFile = @"C:\Program Files\IBM\SQLLIB_01\BIN\db2cwadmin.bat";
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.Arguments = batContent.ToString();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = batContent.ToString();
                p.StartInfo.WorkingDirectory = @"C:\Program Files\IBM\SQLLIB_01\BIN";
                p.Start();
                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                p.Close();

                sbMessage.AppendLine("导入表成功");
                sbMessage.AppendLine(output);
            }
            catch (Exception ex)
            {
                sbMessage.AppendLine("导入表失败");
                sbMessage.AppendLine(ex.ToString());
            }

            MessageBox.Show(sbMessage.ToString());
        }


        /// <summary>
        /// 文件导入ORACLE测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            string tableName = "Z_LOAD_TEST";
            string basePath = BConfig.BaseDirectory;
            string fileName = "loadtest";

            string ip = "10.95.240.9";
            int port = 1521;
            string dbName = "ORCL";
            string user = "C##ABPBASE";
            string password = "C##ABPBASE";
            string createSql = string.Format("CREATE TABLE {0} (ID NUMBER(10), NAME NVARCHAR2(50))", tableName);

            string txtFile = basePath + fileName + ".txt";
            string ctlFile = basePath + fileName + ".ctl";
            string batFile = basePath + fileName + ".bat";

            StringBuilder sbMessage = new StringBuilder();
            //先删除当前目录相应文件
            try
            {
                DirectoryInfo di = new DirectoryInfo(basePath);
                FileInfo[] fis = di.GetFiles(fileName + ".*");
                foreach (FileInfo fi in fis)
                {
                    fi.Delete();
                }
                sbMessage.AppendLine(string.Format("成功删除了【{0}】个旧文件", fis.Length));
            }
            catch (Exception ex)
            {
                sbMessage.AppendLine("删除旧文件失败");
                sbMessage.AppendLine(ex.ToString());
            }

            //写数据文件
            StringBuilder txtContent = new StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                txtContent.AppendLine(i + "\tname is:" + i);
            }
            //txtContent.AppendLine(11 + "aaa\tname is:" + 11);
            File.WriteAllText(txtFile, txtContent.ToString());

            //写格式文件
            string ctlContent = string.Format(@"LOAD DATA 
INFILE '{0}'
APPEND
INTO TABLE {1}
FIELDS TERMINATED BY '\t'
TRAILING NULLCOLS
(
   ID,
   NAME
)", txtFile, tableName);
            File.WriteAllText(ctlFile, ctlContent);

            //写批处理文件    mh/mh@22.11.97.96:1521/ora10 control=fund_inf.ctl
            string batContent = string.Format("sqlldr {0}/{1}@{2}:{3}/{4} control={5}", user, password, ip, port, dbName, ctlFile);
            File.WriteAllText(batFile, batContent);

            //创建表
            using (BDBHelper dbHelper = new BDBHelper("ORACLE", ip, port, user, password, dbName, dbName))
            {
                try
                {
                    dbHelper.ExecuteNonQuery(createSql);
                    sbMessage.AppendLine("创建表成功");
                }
                catch (Exception ex)
                {
                    sbMessage.AppendLine("创建表出错");
                    sbMessage.AppendLine(ex.ToString());
                }
            }

            //调用批处理，导入数据
            string output = string.Empty;
            try
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = batFile;
                p.StartInfo.WorkingDirectory = basePath;
                p.Start();
                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                p.Close();

                sbMessage.AppendLine("导入表成功");
                sbMessage.AppendLine(output);
            }
            catch (Exception ex)
            {
                sbMessage.AppendLine("导入表失败");
                sbMessage.AppendLine(ex.ToString());
            }

            MessageBox.Show(sbMessage.ToString());
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("name", typeof(string));
            dt.Columns.Add("remark", typeof(string));
            dt.Columns.Add("createtime", typeof(DateTime));
            for (int i = 0; i < 110; i++)
            {
                DataRow dr = dt.NewRow();
                dr[0] = i;
                dr[1] = "";
                dr[2] = "";
                for (int j = 0; j <= i; j++)
                {
                    dr[1] += j.ToString();
                    dr[2] += "abc";
                }
                dt.Rows.Add(dr);
            }

            Dictionary<string, int> dic = Easyman.Librarys.DBHelper.DBOperator.GetColumnsMaxLength(dt);
            string fn = "name";

            MessageBox.Show("最大长度" + dic[fn]);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            DateTime today = DateTime.Today.AddDays(2);
            MessageBox.Show(today.DayOfWeek.GetHashCode().ToString());

            return;



            //System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            //System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            //string PasswdText = "abc";
            //string Salt = "def";
            //byte[] bs = Encoding.UTF8.GetBytes("111111");
            //byte[] HashResult = md5.ComputeHash(bs);
            //StringBuilder sb = new StringBuilder();
            //foreach (byte b in HashResult)
            //{
            //    sb.Append(b.ToString("x2"));
            //}
            //string md5String = sb.ToString();
            //MessageBox.Show(md5String);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string conn = string.Empty;
            string file = string.Empty;
            string tableName = "";
            int i = 0;

            //方法一，写文件再导
            using (BDBHelper dbHelper = new BDBHelper("DB2", conn))
            {
                i = dbHelper.LoadDataInLocalFile(tableName, file);
            }

            //方法二，转DataTable，再导入
            DataTable dt = new DataTable();
            using (BDBHelper dbHelper = new BDBHelper("DB2", conn))
            {
                //使用（适合数据量小，纯内存方式）
                i = dbHelper.LoadDataInDataTable(tableName, dt);
                //或者（适合数据量很大，超过10万的级别，内部会先写文件再导入）
                i = dbHelper.LoadDataInDataTableWithFile(tableName, dt);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            DateTime dt = DateTime.Now;
            string s = dt.ToString("yyyyMM01");
            MessageBox.Show(s);

            Dictionary<int, int> dic = new Dictionary<int, int>();
            dic.Add(1, 1);
            try
            {
                int i = dic[2];
            }
            catch (Exception ex)
            {
                s = ex.ToString();
            }
            MessageBox.Show(s);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            string content = @"string tableName = 'ZZDTL_RWY_TEST_${day(-2)}';
log('将把查询结果直接写入表：' + tableName);
string sql= 'SELECT OP_TIME FROM GZ852.BRK_D_PSN_BASE_@{day(-2)}';

int i = down_db_to_db(sql, tableName, '本地结果集市',1,500000);
log('操作完成，共有' + i + '条记录写入表：' + tableName);";

            //Regex regFunction = new Regex(@"[@\$]{(?<fun>\w+\(.*?\))}");
            //Match match = regFunction.Match(content);
            //int i = 0;
            //content = "string.Format(\"" + content + "\"";
            //while (match.Success)
            //{
            //    content = regFunction.Replace(content, "{" + i + "}", 1) + "," + match.Result("${fun}");
            //    match = regFunction.Match(content);
            //    i++;
            //}

            content = Easyman.ScriptService.Script.Transfer.ReplaceFunctions('@', content);
            MessageBox.Show(content);

            //下面编译content再执行
        }

        public static string day(int d = 0)
        {
            return DateTime.Now.AddDays(d).ToString("yyyyMMdd");
        }

        /// <summary>
        /// 向EM_SCRIPT_NODE_CASE_LOG添加一条日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button10_Click(object sender, EventArgs e)
        {
            Encoding encoding = Encoding.UTF8;
            long scriptNodeCaseID = 1072;
            int logLevel = 3;
            string logMessage = File.ReadAllText("F:/msg.txt", encoding);
            string sql = File.ReadAllText("F:/sql.txt", encoding);

            int i = 0;
            try
            {
                i = Easyman.ScriptService.BLL.EM_SCRIPT_NODE_CASE_LOG.Instance.Add(scriptNodeCaseID, logLevel, logMessage, sql);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                File.WriteAllText("F:/err.txt", ex.ToString(), encoding);
            }
            if (i > 0)
            {
                MessageBox.Show("添加成功！");
            }
            else
            {
                MessageBox.Show("添加失败！");
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            ClassAsync c = new ClassAsync();
            var s = c.MethodAsync(DateTime.Now);
            MessageBox.Show(s.ToString());
        }

        private void button12_Click(object sender, EventArgs e)
        {
            string s = string.Empty;
            ClassAsync c = new ClassAsync();
            int i = c.Test(1);
            s += i.ToString() + ",";
            i = c.Test(1, true);
            s += i.ToString() + ",";
            i = c.Test(1, false);
            s += i.ToString() + ",";
            MessageBox.Show(s.ToString());
        }

        private void button13_Click(object sender, EventArgs e)
        {
            int rowsCount = 0;
            int pageSize = 50;
            int pageIndex = 0;
            string sql = "select * from EM_MODULE_EVENT";
            string dbType = "oracle";
            string ip = "139.196.212.68";
            int port = 1521;
            string user = "C##EM2";
            string password = "C##EM2";
            string serviceName = "ORCL";

            using (BDBHelper dbHelper = new BDBHelper(dbType, ip, port, user, password, serviceName, serviceName))
            {
                using (IDataReader reader = dbHelper.ExecuteReader(sql))
                {
                    int i = 0;
                    
                    DataTable dt = new DataTable();
                    for (int c = 0; c < reader.FieldCount; c++)
                    {
                        dt.Columns.Add(reader.GetName(c), reader.GetFieldType(c));
                    }

                    bool isCanRead = reader.Read();

                    while (true && isCanRead)
                    {
                        DataRow dr = dt.NewRow();
                        for (int c = 0; c < reader.FieldCount; c++)
                        {
                            dr[c] = reader.GetValue(c);
                        }

                        dt.Rows.Add(dr);
                        i++;
                        rowsCount++;

                        isCanRead = reader.Read();
                        if (i >= pageSize || isCanRead == false)
                        {
                            pageIndex++;
                            string fileName = "G:/dt_" + pageIndex + ".txt";
                            WriteDataTableIntoFile(dt, fileName);
                            dt.Rows.Clear();
                            i = 0;
                        }

                        if (isCanRead == false)
                        {
                            break;
                        }
                    }
                }
            }

            MessageBox.Show(string.Format("共有{0}页，{1}条记录，已经写入文件。", pageIndex, rowsCount));
        }

        private void WriteDataTableIntoFile(DataTable dt, string fileName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (DataColumn column in dt.Columns)
            {
                sb.Append(column.ColumnName + "(" + column.DataType.ToString() + ")\t");
            }
            sb.AppendLine();
            int columnsCount = dt.Columns.Count;
            foreach (DataRow dr in dt.Rows)
            {
                for (int c = 0; c < columnsCount; c++)
                {
                    sb.Append(Convert.ToString(dr[c]) + "\t");
                }
                sb.AppendLine();
            }
            File.WriteAllText(fileName, sb.ToString());
        }

        private void button14_Click(object sender, EventArgs e)
        {
            form = new FormQueryPage();
            form.Show(this);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            bool isNewNode = false;
            bool isNewDatabase = false;
            string ip = "192.168.1.2";
            int port = 1521;
            string dbName = "AAAA";

            string nodeName = Easyman.Librarys.DBHelper.Providers.IBMDB2.GetNodeName(ip, port, ref isNewNode);
            string databaseName = Easyman.Librarys.DBHelper.Providers.IBMDB2.GetDataBaseName(nodeName, dbName, ref isNewDatabase);

            MessageBox.Show(string.Format("节点{0} 【{1}】,数据库{2} 【{3}】", nodeName, isNewNode, databaseName, isNewDatabase));
        }
    }
}