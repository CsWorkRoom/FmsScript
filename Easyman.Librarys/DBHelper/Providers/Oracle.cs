using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using Easyman.Librarys.Config;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Easyman.Librarys.DBHelper.Providers
{
    /// <summary>
    /// Oracle数据库实例
    /// 文件功能描述：模块类，Oracle数据库操作类，在这里实现了该数据库的相关特性
    /// 依赖说明：依赖库Oracle.ManagedDataAccess.dll，不要直接实例化，通过BDBHelper来调用。
    /// 异常处理：捕获但不处理异常。
    public class Oracle : DBOperator
    {
        private OracleConnection _conn;
        private OracleTransaction _trans;

        /// <summary>
        /// 当前是否在存储过程中
        /// </summary>
        protected bool _isInTransaction = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="strConnection">连接字符串</param>
        public Oracle(string strConnection)
        {
            _connString = strConnection;
            _conn = new OracleConnection(strConnection);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ip">主机IP</param>
        /// <param name="ip">数据库主机IP</param>
        /// <param name="port">主机端口</param>
        /// <param name="userName">登录账号</param>
        /// <param name="password">登录密码</param>
        /// <param name="serviceName">服务名/实例名（可选，Oracle必填，为服务名）</param>
        public Oracle(string ip, int port, string userName, string password, string serviceName)
        {
            _ip = ip;
            _port = port;
            _userName = userName;
            _password = password;
            _service = serviceName;
            _connString = GetDBConnString("Oracle");
            _conn = new OracleConnection(_connString);
        }

        #region 提取连接信息
        /// <summary>
        /// 从连接字符串里面提取连接信息
        /// 格式：User ID={2};Password={3};Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1}))(CONNECT_DATA=(SERVICE_NAME={4})));Persist Security Info=True;", ip, port, userName, password, serviceName);
        /// </summary>
        public override void GetConnectInfoFromConnString()
        {
            Regex regIP = new Regex(@"HOST=(?<value>[^)]+)", RegexOptions.IgnoreCase);
            Regex regPort = new Regex(@"PORT=(?<value>[^)]+)", RegexOptions.IgnoreCase);
            Regex regUser = new Regex(@"ID=(?<value>[^;]+)", RegexOptions.IgnoreCase);
            Regex regPswd = new Regex(@"PASSWORD=(?<value>[^;]+)", RegexOptions.IgnoreCase);
            Regex regSrvc = new Regex(@"SERVICE_NAME=(?<value>[^)]+)", RegexOptions.IgnoreCase);
            Match match = regIP.Match(_connString);
            if (match.Success)
            {
                _ip = match.Result("${value}");
            }
            else
            {
                _ip = "localhost";
            }

            match = regPort.Match(_connString);
            if (match.Success)
            {
                _port = Convert.ToInt32(match.Result("${value}"));
            }
            else
            {
                _port = 1521;
            }

            match = regUser.Match(_connString);
            if (match.Success)
            {
                _userName = match.Result("${value}");
            }

            match = regPswd.Match(_connString);
            if (match.Success)
            {
                _password = match.Result("${value}");
            }

            match = regSrvc.Match(_connString);
            if (match.Success)
            {
                _service = match.Result("${value}");
                _database = ServiceName;
            }
        }
        #endregion

        #region 打开/关闭数据库连接
        /// <summary>
        /// 打开数据库连接
        /// </summary>
        public override void Open()
        {
            if (_conn != null && _conn.State != ConnectionState.Open)
            {
                try
                {
                    _conn = new OracleConnection(_connString);
                    this._conn.Open();
                }
                catch (Exception ee)
                {
                    throw new Exception("打开数据库连接失败。\r\n" + ee.ToString());
                }
            }
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        public override void Close()
        {
            if (_conn != null && _conn.State != ConnectionState.Closed)
            {
                try
                {
                    this._conn.Close();
                }
                catch (Exception ee)
                {
                    throw new Exception("关闭数据库连接失败。\r\n" + ee.ToString());
                }
            }
        }
        #endregion

        #region 获取连接状态

        /// <summary>
        /// 获取连接当前状态
        /// </summary>
        /// <returns></returns>
        public override ConnectionState GetConnectionState()
        {
            return _conn.State;
        }

        #endregion

        #region 事务操作
        /// <summary>
        /// 开始事务
        /// </summary>
        public override void BeginTrans()
        {
            if (_isInTransaction)
            {
                throw new Exception("当前事务尚未提交!");
            }

            _trans = _conn.BeginTransaction();
            _isInTransaction = true;
        }

        /// <summary>
        /// 执行事务
        /// </summary>
        public override void CommitTrans()
        {
            _trans.Commit();
            _isInTransaction = false;
        }

        /// <summary>
        /// 事务回滚
        /// </summary>
        public override void RollbackTrans()
        {
            _trans.Rollback();
            _isInTransaction = false;
        }

        #endregion

        /// <summary>
        /// 创建一个命令
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        protected override DbCommand CreateCommand(string command)
        {
            OracleCommand comm = new OracleCommand();
            comm.Connection = _conn;
            comm.CommandText = command;
            comm.CommandTimeout = base.CommandTimeout;

            return comm;
        }

        /// <summary>
        /// 构造包含参数的SQL语句或命令
        /// </summary>
        /// <param name="command">SQL语句或命令</param>
        /// <param name="value">参数值列表</param>
        protected override DbCommand CreateCommand(string command, params object[] value)
        {
            OracleCommand comm = new OracleCommand();
            comm.Connection = _conn;
            comm.CommandText = command;
            comm.CommandTimeout = base.CommandTimeout;

            if (value.Length > 0)
            {
                List<object> paramsList = GetParamsList(value);

                //SQL语句/命令中参数 用“?”符号占位
                string[] temp = command.Split('?');
                if (temp.Length - 1 != paramsList.Count)
                {
                    throw new Exception("参数数量不正确！");
                }

                for (int i = 0; i < paramsList.Count; i++)
                {
                    temp[i] = temp[i] + ":p" + (i + 1).ToString();
                    //判断是否为null
                    if (paramsList[i] == null || paramsList[i] == DBNull.Value)
                    {
                        comm.Parameters.Add("p" + (i + 1).ToString(), DBNull.Value);
                    }
                    else
                    {
                        comm.Parameters.Add(":p" + (i + 1).ToString(), paramsList[i]);//.ToString()
                    }
                }

                comm.CommandText = string.Join("", temp);
            }

            return comm;
        }
        #region 取序列值
        /// <summary>
        /// 从序列中提取下一个值
        /// </summary>
        /// <param name="seqName">序列名</param>
        /// <returns></returns>
        public override int GetNextValueFromSeq(string seqName)
        {
            string sql = "SELECT " + seqName + ".NEXTVAL FROM DUAL";
            return Convert.ToInt32(ExecuteScalar(sql));
        }

        #endregion

        #region 无参数调用
        /// <summary>
        /// A2返回一个DataSet
        /// </summary>
        /// <param name="sql">SQL语句或命令</param>
        /// <returns>DataSet</returns>
        public override DataSet ExecuteDataSet(string sql)
        {
            OracleCommand comm = (OracleCommand)CreateCommand(sql);

            OracleDataAdapter adapter = new OracleDataAdapter();
            DataSet ds = new DataSet();
            adapter.SelectCommand = comm;

            adapter.Fill(ds);
            return ds;
        }

        #endregion

        #region 带参数查询
        /// <summary>
        /// B2返回一个DataSet
        /// </summary>
        /// <param name="sql">SQL语句或命令</param>
        /// <param name="value">参数值列表</param>
        /// <returns>DataSet</returns>
        public override DataSet ExecuteDataSetParams(string sql, params object[] value)
        {
            OracleCommand comm = (OracleCommand)CreateCommand(sql, value);

            DataSet ds = new DataSet();
            OracleDataAdapter adapter = new OracleDataAdapter();

            adapter.SelectCommand = comm;
            adapter.Fill(ds);
            return ds;
        }

        #endregion

        #region 分页查询

        /// <summary>
        /// 分页查询，返回DataTable
        /// </summary>
        /// <param name="sql">除去分页之外的SQL语句</param>
        /// <param name="pageSize">页面大小（单页记录条数）</param>
        /// <param name="pageIndex">当前页码（页号从1开始）</param>
        /// <returns></returns>
        public override DataSet ExecuteDataSetPage(string sql, int pageSize, int pageIndex)
        {
            int startRow = pageSize * (pageIndex - 1) + 1;
            int endRow = startRow + pageSize - 1;
            string s = string.Format("SELECT * FROM (SELECT tpi.*, ROWNUM BROW_NUM FROM ({0}) tpi WHERE ROWNUM <={1}) WHERE BROW_NUM >={2}", sql, endRow, startRow);

            return ExecuteDataSet(s);
        }

        /// <summary>
        /// 分页查询，返回DataTable
        /// </summary>
        /// <param name="sql">除去分页之外的SQL语句</param>
        /// <param name="pageSize">页面大小（单页记录条数）</param>
        /// <param name="pageIndex">当前页码（页号从1开始）</param>
        /// <param name="value">参数列表</param>
        /// <returns></returns>
        public override DataSet ExecuteDataSetPageParams(string sql, int pageSize, int pageIndex, params object[] value)
        {
            int startRow = pageSize * (pageIndex - 1) + 1;
            int endRow = startRow + pageSize - 1;
            string s = string.Format("SELECT * FROM (SELECT tpi.*, ROWNUM ROW_NUM FROM ({0}) tpi WHERE ROW_NUM <={1}) WHERE ROW_NUM >={2}", sql, endRow, startRow);

            return ExecuteDataSetParams(s, value);
        }

        #endregion

        #region 从内存导入数据到数据库
        /// <summary>
        /// 从DataTable导入数据到数据库表（适用于小批量数据导入）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="dt">数据表（字段名通过ColumnName来指定）</param>
        /// <returns></returns>
        public override int LoadDataInDataTable(string tableName, DataTable dt)
        {
            if (dt == null || dt.Rows.Count < 1 || string.IsNullOrWhiteSpace(tableName))
            {
                return 0;
            }
            int rowsCount = dt.Rows.Count;
            int colsCount = dt.Columns.Count;
            List<string> columnNames = new List<string>();

            //行列转换
            object[][] allData = new object[colsCount][];
            for (int col = 0; col < colsCount; col++)
            {
                allData[col] = new object[rowsCount];
                columnNames.Add(dt.Columns[col].ColumnName);
            }
            for (int row = 0; row < rowsCount; row++)
            {
                for (int col = 0; col < colsCount; col++)
                {
                    allData[col][row] = dt.Rows[row][col];
                }
            }

            OracleCommand command = _conn.CreateCommand();
            command.CommandTimeout = base.CommandTimeout;
            command.CommandType = CommandType.Text;
            command.ArrayBindCount = rowsCount;
            command.CommandText = string.Format("INSERT INTO {0} ({1}) VALUES({2})", tableName, string.Join(", ", columnNames), ":" + string.Join(", :", columnNames));

            int colIndex = 0;
            Dictionary<string, int> dicMaxLength = GetColumnsMaxLength(dt);
            foreach (DataColumn col in dt.Columns)
            {
                int maxLength = 0;
                dicMaxLength.TryGetValue(col.ColumnName, out maxLength);
                OracleDbType type = GetDbType(dt, col, maxLength);
                OracleParameter colParameter = new OracleParameter(col.ColumnName, type);
                colParameter.Direction = ParameterDirection.Input;
                colParameter.Value = allData[colIndex];
                command.Parameters.Add(colParameter);
                colIndex++;
            }

            return command.ExecuteNonQuery();
        }

        #region 从DataTable导入数据到表，传统方式，性能较差
        /// <summary>
        /// 从DataTable导入数据到数据库表（适用于小批量数据导入）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="dt">数据表（字段名通过ColumnName来指定）</param>
        /// <param name="isOld">旧方法，较慢</param>
        /// <returns></returns>
        public int LoadDataInDataTable(string tableName, DataTable dt, bool isOld)
        {
            if (dt == null || dt.Rows.Count < 1 || string.IsNullOrWhiteSpace(tableName))
            {
                return 0;
            }

            List<string> fileds = new List<string>();
            foreach (DataColumn col in dt.Columns)
            {
                if (col.ColumnName.ToUpper() == "ROWNUM")
                {
                    continue;
                }
                fileds.Add(col.ColumnName);
            }
            int i = 0;
            int colcount = dt.Columns.Count;
            List<object> par = new List<object>();

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("INSERT /*+ APPEND */ INTO " + tableName + " nologging ({0})\r\n select * from (", string.Join(",", fileds));
            foreach (DataRow dr in dt.Rows)
            {
                if (i == 0)
                {
                    sb.Append("\r\n select ");
                }
                else
                {
                    sb.Append(" union all \r\n select ");
                }

                for (int c = 0; c < colcount; c++)
                {
                    if (dt.Columns[c].ColumnName == "ROWNUM")
                    {
                        continue;
                    }
                    if (c > 0)
                    {
                        sb.Append(",");
                    }

                    //空值，不通过参数传递
                    if (dr[c] == null || dr[c] == DBNull.Value)
                    {
                        sb.Append("null");
                    }
                    else
                    {
                        sb.Append("?");
                        par.Add(dr[c]);
                    }

                    //第一行，加上列的别名
                    if (i == 0)
                    {
                        sb.Append(" " + dt.Columns[c].ColumnName);
                    }
                }
                sb.Append(" from dual ");

                i++;
            }

            sb.AppendLine("\r\n)");

            string s = sb.ToString();
            //执行SQL语句
            try
            {
                return ExecuteNonQueryParams(sb.ToString(), par);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString() + "SQL语句为：\r\n" + sb.ToString() + "\r\n参数列表为：\r\n" + ShowParamsList(par));
            }
        }
        #endregion
        #endregion

        #region 通过先写文件再导入的方式来将DataTable导入到表，适用于大数据（至少10万条）才有优势

        /// <summary>
        /// 通过先写文件再导入的方式来将DataTable导入到表，适用于大数据（至少10万条）才有优势
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="dt">数据表（字段名通过ColumnName来指定）</param>
        /// <returns></returns>
        public override int LoadDataInDataTableWithFile(string tableName, DataTable dt)
        {
            if (dt == null || dt.Rows.Count < 1 || string.IsNullOrWhiteSpace(tableName))
            {
                return 0;
            }

            string basePath = BConfig.BaseDirectory + "loadtemp\\";
            if (Directory.Exists(basePath) == false)
            {
                Directory.CreateDirectory(basePath);
            }
            string fileName = tableName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");

            string txtFile = basePath + fileName + ".txt";
            string ctlFile = basePath + fileName + ".ctl";
            string batFile = basePath + fileName + ".bat";

            List<string> columnNames = new List<string>();
            List<string> colctrl = new List<string>();
            //行列转换
            foreach (DataColumn column in dt.Columns)
            {
                columnNames.Add(column.ColumnName);
                if (column.DataType == typeof(DateTime))
                {
                    colctrl.Add(column.ColumnName + " date \"yyyy-mm-dd hh24:mi:ss\"");
                }
                else
                {
                    colctrl.Add(column.ColumnName);
                }

            }

            //写数据文件
            //WriteDataTableIntoFile(dt, columnNames, txtFile, Encoding.GetEncoding("GB2312"));
            var utf8WithNotBom = new UTF8Encoding(false);
            WriteDataTableIntoFile(dt, columnNames, txtFile, utf8WithNotBom);

            //写格式文件
            string ctlContent = string.Format(@"LOAD DATA 
CHARACTERSET 'UTF8'
INFILE '{0}'
APPEND
INTO TABLE {1}
FIELDS TERMINATED BY '\t'
TRAILING NULLCOLS
(
   {2}
)", txtFile, tableName, string.Join(",\r\n", colctrl));
            File.WriteAllText(ctlFile, ctlContent);

            //从连接字符串里面提取连接信息
            GetConnectInfoFromConnString();
            //写批处理文件    mh/mh@22.11.97.96:1521/ora10 control=fund_inf.ctl
            string batContent = string.Format("sqlldr {0}/{1}@{2}:{3}/{4} control={5}\r\nexit", UserName, Password, IP, Port, DataBase, ctlFile);
            File.WriteAllText(batFile, batContent);

            //从文件导入，并删除产生的文件
            return LoadDataInLocalFile(tableName, txtFile, true);
        }

        #endregion

        #region 导入txt到数据库

        /// <summary>
        /// 从本地文件导入数据到数据库表中
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fileName">本地文件名</param>
        /// <param name="isDeleteFiles">是否删除相应的所有文件（默认为不删除）</param>
        /// <returns></returns>
        public override int LoadDataInLocalFile(string tableName, string fileName, bool isDeleteFiles = false)
        {
            int n = -1;
            if (File.Exists(fileName) == false)
            {
                throw (new Exception(string.Format("数据文件【{0}】不存在，无法导入。", fileName)));
            }
            FileInfo fi = new FileInfo(fileName);
            string batFile = fi.DirectoryName + "\\" + Path.GetFileNameWithoutExtension(fileName) + ".bat";
            if (File.Exists(batFile) == false)
            {
                throw (new Exception(string.Format("批处理文件【{0}】不存在，无法导入。", batFile)));
            }

            //导入前的记录数
            int preRowsCount = GetRowsCount(tableName);

            string output = string.Empty;
            using (Process p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = false;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = batFile;
                //p.StartInfo.WorkingDirectory = basePath;
                p.Start();
                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                p.Close();
            }

            //Easyman.Librarys.Log.BLog.Write(Log.BLog.LogLevel.DEBUG, string.Format("导入输出：\r\n{0}", output));

            //达到提交点 - 逻辑记录计数 166860
            //达到提交点 - 逻辑记录计数 166888
            Regex reg = new Regex(@"逻辑记录计数.+?(?<value>\d+)\s*$");
            Match match = reg.Match(output);
            if (match.Success)
            {
                n = Convert.ToInt32(match.Result("${value}"));
            }

            //导入后的记录数
            if (n < 1)
            {
                //通过查表来计算
                int afterRowsCount = GetRowsCount(tableName);
                n = afterRowsCount - preRowsCount;
            }

            //删除相应文件
            if (isDeleteFiles == true)
            {
                DeleteAllFilesLikeBase(fileName);
            }

            return n;
        }

        /// <summary>
        /// 从本地文件导入数据到数据库表中
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fields">字段列表</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns></returns>
        public override int LoadDataInLocalFile(string tableName, string fileName, List<string> fields, bool isReplace = false)
        {
            throw (new Exception("暂未实现。"));
        }

        /// <summary>
        /// 从本地文件导入数据到数据库表中
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fieldsTerminated">字段分隔符</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns></returns>
        public override int LoadDataInLocalFile(string tableName, string fileName, string fieldsTerminated, bool isReplace = false)
        {
            throw (new Exception("暂未实现。"));
        }

        /// <summary>
        /// 从本地文件导入数据到数据库表中
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fields">字段列表</param>
        /// <param name="fieldsTerminated">字段分隔符</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns></returns>
        public override int LoadDataInLocalFile(string tableName, string fileName, List<string> fields, string fieldsTerminated, bool isReplace = false)
        {
            throw (new Exception("暂未实现。"));
        }

        /// <summary>
        /// 从本地文件导入数据到数据库表中
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fields">字段列表</param>
        /// <param name="fieldsTerminated">字段分隔符</param>
        /// <param name="linesTerminated">记录分隔符</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns></returns>
        public override int LoadDataInLocalFile(string tableName, string fileName, List<string> fields, string fieldsTerminated, string linesTerminated, bool isReplace = false)
        {
            throw (new Exception("暂未实现。"));
        }

        #endregion

        #region 复制表结构

        /// <summary>
        /// 根据一张表的结构创建另外一张表
        /// </summary>
        /// <param name="createTableName">要创建的表名</param>
        /// <param name="likeTableName">另外一张表的表名</param>
        /// <returns>创建成功返回true，失败返回false</returns>
        public override bool CreateTable(string createTableName, string likeTableName)
        {
            string sql = string.Format("CREATE TABLE {0} AS SELECT * FROM {1} WHERE 1=0", createTableName, likeTableName);
            return ExecuteNonQuery(sql) >= 0;
        }

        /// <summary>
        /// 生成要创建表的SQL脚本
        /// </summary>
        /// <param name="createTableName">要创建的表名</param>
        /// <param name="datatable">数据</param>
        /// <returns></returns>
        public override string MakeCreateTableSql(string createTableName, DataTable datatable)
        {
            int cols = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CREATE TABLE " + createTableName + " (");
            Dictionary<string, int> dicMaxLength = GetColumnsMaxLength(datatable);
            foreach (DataColumn col in datatable.Columns)
            {
                if (cols > 0)
                {
                    sb.AppendLine(",");
                }
                int maxLength = 0;
                dicMaxLength.TryGetValue(col.ColumnName, out maxLength);
                sb.Append("\"" + col.ColumnName + "\" " + ConvertColumnType(datatable, col, maxLength));
                cols++;
            }

            sb.AppendLine("  )");

            //Easyman.Librarys.Log.BLog.Write(Log.BLog.LogLevel.DEBUG, "创建表的脚本为：\r\n" + sb.ToString());

            return sb.ToString();
        }

        /// <summary>
        /// 转换字段数据类型（用于生成创建表的脚本）
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <param name="col">字段</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>字段类型</returns>
        public override string ConvertColumnType(DataTable dt, DataColumn col, int maxLength)
        {
            switch (col.DataType.ToString())
            {
                case "System.DateTime":
                    return "DATE";
                case "System.String":
                    if (maxLength > 600)
                    {
                        return "NCLOB";
                    }
                    else if (maxLength > 400)
                    {
                        return "NVARCHAR2(800)";
                    }
                    else if (maxLength > 200)
                    {
                        return "NVARCHAR2(600)";
                    }
                    else if (maxLength > 100)
                    {
                        return "NVARCHAR2(400)";
                    }
                    else if (maxLength > 10)
                    {
                        return "NVARCHAR2(200)";
                    }
                    return "NVARCHAR2(100)";
                case "System.Decimal":
                    return "NUMBER(19,2)";
                case "System.Int16":
                    return "NUMBER(3,0)";
                case "System.Int32":
                    return "NUMBER(10,0)";
                case "System.Int64":
                    return "NUMBER(20,0)";
            }

            return "NVARCHAR2(512)";
        }

        /// <summary>
        /// 转换为数据库类型
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <param name="col">数据列</param>
        /// <param name="maxLength" >最大长度</param>
        /// <returns>数据库数据类型</returns>
        public static OracleDbType GetDbType(DataTable dt, DataColumn col, int maxLength)
        {
            switch (col.DataType.ToString())
            {
                case "System.DateTime":
                    return OracleDbType.Date;
                case "System.String":
                    if (maxLength > 600)
                    {
                        return OracleDbType.NClob;
                    }
                    return OracleDbType.NVarchar2;
                case "System.Decimal":
                    return OracleDbType.Double;
                case "System.Int16":
                    return OracleDbType.Int16;
                case "System.Int32":
                    return OracleDbType.Int32;
                case "System.Int64":
                    return OracleDbType.Long;
            }
            return OracleDbType.NVarchar2;
        }

        #endregion

        #region 获取表的一些信息

        /// <summary>
        /// 判断表名是否存在
        /// </summary>
        /// <param name="ownerAndTableName">所有者及表名，格式：OWNER.TABLENAME</param>
        /// <param name="isIgnoreCase">是否忽略大小写，默认为忽略</param>
        /// <returns>存在返回true,反之返回false</returns>
        public override bool TableIsExists(string ownerAndTableName, bool isIgnoreCase = true)
        {
            string[] ss = ownerAndTableName.Split(new char[] { '.' });
            string sql = string.Empty;

            if (ss.Length == 1)
            {
                sql = string.Format("SELECT TABLE_NAME FROM ALL_TABLES WHERE TABLE_NAME = '{0}'", isIgnoreCase ? ss[0].ToUpper() : ss[0]);
            }
            else if (ss.Length == 2)
            {
                sql = string.Format("SELECT TABLE_NAME FROM ALL_TABLES WHERE OWNER='{0}' AND TABLE_NAME = '{1}'", ss[0], isIgnoreCase ? ss[1].ToUpper() : ss[1]);
            }
            else
            {
                throw new Exception("输入的值：" + ownerAndTableName + "不正确，请传入所有者和表名，格式：OWNER.TABLENAME");
            }

            object obj = ExecuteScalar(sql);
            return obj != null;
        }

        /// <summary>
        /// 查询表名列表
        /// </summary>
        /// <returns></returns>
        public override List<string> GetTablesList()
        {
            Dictionary<string, byte> dic = new Dictionary<string, byte>();

            string sql = "SELECT OWNER,TABLE_NAME,TABLESPACE_NAME FROM ALL_TABLES";
            DataTable dt = ExecuteDataTable(sql);
            if (dt != null)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    string owner = dr[0].ToString().ToUpper();
                    string table = dr[1].ToString().ToUpper();
                    string space = dr[2].ToString().ToUpper();
                    if (owner.Contains("SYS") == true || table.Contains('$') == true || space.Contains("SYS") == true)
                    {
                        continue;
                    }
                    if (dic.ContainsKey(owner + "." + table) == false)
                    {
                        dic.Add(owner + "." + table, 1);
                    }
                }
            }

            return dic.Keys.ToList();
        }

        #endregion
    }
}