using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IBM.Data.DB2;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.IO;
using Easyman.Librarys.Config;
using System.Diagnostics;

namespace Easyman.Librarys.DBHelper.Providers
{
    /// <summary>
    /// DB2数据库实例
    /// 文件功能描述：模块类，DB2数据库操作类，在这里实现了该数据库的相关特性
    /// 依赖说明：IBM.Data.DB2.dll，不要直接实例化，通过BDBHelper来调用。
    /// 异常处理：捕获但不处理异常。
    /// </summary>
    public class IBMDB2 : DBOperator
    {
        private DB2Connection _conn;
        private DB2Transaction _trans;

        /// <summary>
        /// 当前是否在存储过程中
        /// </summary>
        protected bool _isInTransaction = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="strConnection"></param>
        public IBMDB2(string strConnection)
        {
            _connString = strConnection;
            _conn = new DB2Connection(strConnection);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ip">主机IP</param>
        /// <param name="ip">数据库主机IP</param>
        /// <param name="port">主机端口</param>
        /// <param name="userName">登录账号</param>
        /// <param name="password">登录密码</param>
        /// <param name="database">数据库名</param>
        public IBMDB2(string ip, int port, string userName, string password, string database)
        {
            _ip = ip;
            _port = port;
            _userName = userName;
            _password = password;
            _database = database;
            _connString = GetDBConnString("DB2");
            _conn = new DB2Connection(_connString);
        }

        #region 提取连接信息
        /// <summary>
        /// 从连接字符串里面提取连接信息
        /// 格式：Server={0}:{1};UID={2};PWD={3};DataBase={4};", ip, port, userName, password, database);
        /// </summary>
        public override void GetConnectInfoFromConnString()
        {
            Regex regIP = new Regex(@"SERVER=(?<value>[^:;]+)", RegexOptions.IgnoreCase);
            Regex regPort = new Regex(@":=(?<value>[^;]+)", RegexOptions.IgnoreCase);
            Regex regUser = new Regex(@"UID=(?<value>[^;]+)", RegexOptions.IgnoreCase);
            Regex regPswd = new Regex(@"PWD=(?<value>[^;]+)", RegexOptions.IgnoreCase);
            Regex regDtbs = new Regex(@"DATABASE=(?<value>[^)]+)", RegexOptions.IgnoreCase);
            Match match = regIP.Match(_connString);
            if (match.Success)
            {
                _ip = match.Result("${value}");
            }

            match = regPort.Match(_connString);
            if (match.Success)
            {
                _port = Convert.ToInt32(match.Result("${value}"));
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

            match = regDtbs.Match(_connString);
            if (match.Success)
            {
                _database = match.Result("${value}");
                _service = DataBase;
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
                    _conn = new DB2Connection(_connString);
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
            if (_conn.State != ConnectionState.Closed)
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
            DB2Command comm = new DB2Command();
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
            DB2Command comm = new DB2Command();
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
                    temp[i] = temp[i] + "@p" + (i + 1).ToString();
                    //判断是否为null
                    if (paramsList[i] == null)
                    {
                        comm.Parameters.Add("p" + (i + 1).ToString(), DBNull.Value);
                    }
                    else
                    {
                        comm.Parameters.Add("@p" + (i + 1).ToString(), paramsList[i]);//.ToString()
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
            string sql = "SELECT " + seqName + ".NEXTVAL FROM SYSIBM.DUAL";
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
            DB2Command comm = (DB2Command)CreateCommand(sql);

            DB2DataAdapter adapter = new DB2DataAdapter();
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
            DB2Command comm = (DB2Command)CreateCommand(sql, value);

            DataSet ds = new DataSet();
            DB2DataAdapter adapter = new DB2DataAdapter();

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
            string s = string.Format("SELECT * FROM (SELECT tpi.*, ROW_NUMBER() OVER() AS BROW_NUM FROM ({0}) AS tpi) WHERE BROW_NUM <={1} AND BROW_NUM >={2}", sql, endRow, startRow);

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
            string s = string.Format("SELECT * FROM (SELECT tpi.*, ROW_NUMBER() OVER() AS ROW_NUM FROM ({0}) tpi WHERE ROW_NUM <={1}) WHERE ROW_NUM >={2}", sql, endRow, startRow);

            return ExecuteDataSetParams(s, value);
        }
        #endregion

        #region 从Datatable导入数据到数据库
        /// <summary>
        /// 从DataTable导入数据到数据库表（适用于小批量数据导入）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="dt">数据表（字段名通过ColumnName来指定）</param>
        /// <returns></returns>
        public override int LoadDataInDataTable(string tableName, DataTable dt)
        {
            Log.BLog.Write(Log.BLog.LogLevel.DEBUG, "00");
            if (dt == null || dt.Rows.Count < 1)
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
            DB2Command command = _conn.CreateCommand();
            command.CommandTimeout = base.CommandTimeout;
            command.CommandType = CommandType.Text;
            command.ArrayBindCount = rowsCount;
            command.CommandText = string.Format("INSERT INTO {0} ({1}) VALUES({2})", tableName, string.Join(", ", columnNames), "@" + string.Join(", @", columnNames));
            int colIndex = 0;
            Dictionary<string, int> dicMaxLength = GetColumnsMaxLength(dt);
            foreach (DataColumn col in dt.Columns)
            {
                int maxLength = 0;
                dicMaxLength.TryGetValue(col.ColumnName, out maxLength);
                DB2Type type = GetDbType(dt, col, maxLength);
                DB2Parameter colParameter = new DB2Parameter(col.ColumnName, type);
                colParameter.Direction = ParameterDirection.Input;
                colParameter.Value = allData[colIndex];
                command.Parameters.Add(colParameter);
                colIndex++;
            }
            return command.ExecuteNonQuery();
        }

        #region 通过先写文件再导入的方式来将DataTable导入到表，适用于大数据（至少10万条）才有优势

        /// <summary>
        /// DB2的远程导入，需要先编目远程数据库的节点，映射到本机，为避免重复定义，特将定义写入文件
        /// </summary>
        private static string _db2CatalogPath = "C:/DB2_CATALOG";
        /// <summary>
        /// 数据库节点编目文件
        /// </summary>
        private static string _nodeCatalogFileName = _db2CatalogPath + "/node.txt";
        /// <summary>
        /// 数据库编目文件
        /// </summary>
        private static string _databaseCatalogFileName = _db2CatalogPath + "/database.txt";

        /// <summary>
        /// 根据数据库远程主机和IP，获取映射的节点名
        /// </summary>
        /// <param name="ip">主机IP</param>
        /// <param name="port">端口</param>
        /// <returns>节点编目</returns>
        public static string GetNodeName(string ip, int port, ref bool isNew)
        {
            string key = ip + "_" + port;

            if (Directory.Exists(_db2CatalogPath) == false)
            {
                Directory.CreateDirectory(_db2CatalogPath);
            }

            string[] listNode = new string[] { };
            if (File.Exists(_nodeCatalogFileName) == true)
            {
                listNode = File.ReadAllLines(_nodeCatalogFileName);
            }

            for (int i = 0; i < listNode.Length; i++)
            {
                if (listNode[i] == key)
                {
                    isNew = false;
                    return "DBND" + i;
                }
            }

            //新节点，添加到文件末尾
            isNew = true;
            File.AppendAllText(_nodeCatalogFileName, key + "\r\n");
            return "DBND" + listNode.Length;
        }

        /// <summary>
        /// 根据数据库节点编目和数据库名，获取映射的数据库编目名
        /// </summary>
        /// <param name="nodeName">节点编目名</param>
        /// <param name="database">数据库名</param>
        /// <param name="isNew">是否为新添加节点编目</param>
        /// <returns></returns>
        public static string GetDataBaseName(string nodeName, string database, ref bool isNew)
        {
            string key = nodeName + "_" + database;

            if (Directory.Exists(_db2CatalogPath) == false)
            {
                Directory.CreateDirectory(_db2CatalogPath);
            }

            string[] listDatabase = new string[] { };
            if (File.Exists(_databaseCatalogFileName) == true)
            {
                listDatabase = File.ReadAllLines(_databaseCatalogFileName);
            }

            for (int i = 0; i < listDatabase.Length; i++)
            {
                if (listDatabase[i] == key)
                {
                    isNew = false;
                    return "DBNM" + i;
                }
            }

            //新节点，添加到文件末尾
            isNew = true;
            File.AppendAllText(_databaseCatalogFileName, key + "\r\n");
            return "DBNM" + listDatabase.Length;
        }

        /// <summary>
        /// 通过先写文件再导入的方式来将DataTable导入到表，适用于大数据（至少10万条）才有优势，依赖于DB2客户端db2cmd.exe
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
            string batFile = basePath + fileName + ".bat";
            string logFile = txtFile + ".log";

            List<string> columnNames = new List<string>();
            //行列转换
            foreach (DataColumn column in dt.Columns)
            {
                columnNames.Add(column.ColumnName);
            }

            //写数据文件
            var utf8WithNotBom = new UTF8Encoding(false);
            WriteDataTableIntoFile(dt, columnNames, txtFile, utf8WithNotBom);

            //写批处理文件
            StringBuilder batContent = new StringBuilder();
            bool isNewNode = false;
            bool isNewDataBase = false;
            string nodeName = GetNodeName(IP, Port, ref isNewNode);
            string dataBaseName = GetDataBaseName(nodeName, DataBase, ref isNewDataBase);
            if (isNewNode)
            {
                batContent.AppendLine(string.Format("db2 catalog tcpip node {0} remote {1} server {2}", nodeName, IP, Port));
            }
            if (isNewDataBase)
            {
                batContent.AppendLine(string.Format("db2 catalog database {0} as {1} at node {2} authentication server", DataBase, dataBaseName, nodeName));
            }

            batContent.AppendLine(string.Format("db2 connect to {0} user {1} using {2}", dataBaseName, UserName, Password));
            //batContent.AppendLine(string.Format("db2 load client from '{0}' of del modified by codepage = 1208 COLDEL0x09 insert into {1}", txtFile, tableName));
            //batContent.AppendLine(string.Format("db2 load client from '{0}' of del modified by codepage = 1208 COLDEL0x09 insert into {1} > {2}", txtFile, tableName, logFile));
            batContent.AppendLine(string.Format("db2 load client from '{0}' of del modified by codepage = 1208 COLDEL0x09 MESSAGES '{1}' insert into {2}", txtFile, logFile, tableName));
            batContent.AppendLine("exit");

            File.WriteAllText(batFile, batContent.ToString());

            //导入，并删除产生的文件
            return LoadDataInLocalFile(tableName, txtFile, true);
        }

        #endregion

        #endregion
        #region 导入txt到数据库

        /// <summary>
        /// 从本地文件导入数据到数据库表中，依赖于DB2客户端db2cmd.exe
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

            using (Process p = new Process())
            {
                p.EnableRaisingEvents = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = false;
                //p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = @"db2cmd.exe";
                p.StartInfo.Arguments = batFile;
                p.Start();
                
                p.WaitForExit();

                //从日志文件中读取导入记录条数
                string logFile = fileName + ".log";
                //等待日志文件产生，最多等30秒
                DateTime waitetime = DateTime.Now.AddSeconds(30);
                while (true)
                {
                    if (DateTime.Now > waitetime || File.Exists(logFile))
                    {
                        //多等2秒，确保文件写完
                        System.Threading.Thread.Sleep(2000);
                        break;
                    }
                    System.Threading.Thread.Sleep(10);
                }
                string output = p.StandardOutput.ReadToEnd();

                if (File.Exists(logFile) == true)
                {
                    n = 0;
                    //装入行数         = 166888
                    Regex reg = new Regex(@"装入行数.+?(?<value>\d+)");
                    using (FileStream fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        StreamReader sr = new StreamReader(fs, Encoding.Default);
                        string content = sr.ReadToEnd();
                        Match match = reg.Match(content);
                        if (match.Success)
                        {
                            n = Convert.ToInt32(match.Result("${value}"));
                        }
                        sr.Close();
                    }
                }

                //p.Refresh();
                //p.Close();
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
            throw (new Exception("暂未实现。"));
        }

        /// <summary>
        /// 生成要创建表的SQL脚本
        /// </summary>
        /// <param name="createTableName">要创建的表名</param>
        /// <param name="datatable">数据</param>
        /// <returns></returns>
        public override string MakeCreateTableSql(string createTableName, DataTable datatable)
        {
            int colIndex = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CREATE TABLE " + createTableName + " (");
            Dictionary<string, int> dicMaxLength = GetColumnsMaxLength(datatable);
            foreach (DataColumn col in datatable.Columns)
            {
                if (colIndex > 0)
                {
                    sb.AppendLine(",");
                }

                int maxLength = 0;
                dicMaxLength.TryGetValue(col.ColumnName, out maxLength);
                sb.Append("\"" + col.ColumnName + "\" " + ConvertColumnType(datatable, col, maxLength));

                colIndex++;
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
                    return "timestamp";
                case "System.String":
                    if (maxLength > 600)
                    {
                        return "CLOB";
                    }
                    else if (maxLength > 400)
                    {
                        return "VARCHAR(800)";
                    }
                    else if (maxLength > 200)
                    {
                        return "VARCHAR(600)";
                    }
                    else if (maxLength > 100)
                    {
                        return "VARCHAR(400)";
                    }
                    else if (maxLength > 10)
                    {
                        return "VARCHAR(200)";
                    }
                    return "VARCHAR(100)";
                case "System.Decimal":
                    return "DECIMAL(19,2)";
                case "System.Int16":
                    return "SMALLINT";
                case "System.Int32":
                    return "INT";
                case "System.Int64":
                    return "BIGINT";
            }

            return "VARCHAR(512)";
        }

        /// <summary>
        /// 转换为数据库类型
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <param name="col">数据列</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>数据库数据类型</returns>
        public static DB2Type GetDbType(DataTable dt, DataColumn col, int maxLength)
        {
            switch (col.DataType.ToString())
            {
                case "System.DateTime":
                    return DB2Type.Timestamp;
                case "System.String":
                    if (maxLength > 600)
                    {
                        return DB2Type.Clob;
                    }
                    return DB2Type.VarChar;
                case "System.Decimal":
                    return DB2Type.Double;
                case "System.Int16":
                    return DB2Type.SmallInt;
                case "System.Int32":
                    return DB2Type.Integer;
                case "System.Int64":
                    return DB2Type.BigInt;
            }
            return DB2Type.VarChar;
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
                sql = string.Format("SELECT TABNAME FROM SYSCAT.TABLES WHERE TABNAME = '{0}' WITH UR", isIgnoreCase ? ss[0].ToUpper() : ss[0]);
            }
            else if (ss.Length == 2)
            {
                sql = string.Format("SELECT TABNAME FROM SYSCAT.TABLES WHERE TABSCHEMA = '{0}' AND TABNAME = '{1}' WITH UR", ss[0], isIgnoreCase ? ss[1].ToUpper() : ss[1]);
            }
            else
            {
                throw new Exception("输入值：" + ownerAndTableName + "不正确，请传入所有者和表名，格式：OWNER.TABLENAME");
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
            List<string> list = new List<string>();

            string sql = "SELECT OWNER, TABNAME FROM SYSCAT.TABLES WHERE TYPE='T'";
            //string sql = "SELECT OWNER, TABNAME FROM SYSCAT.TABLES WHERE OWNERTYPE='U' AND TYPE='T'";
            DataTable dt = ExecuteDataTable(sql);
            if (dt != null)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(Convert.ToString(dr[0]) + "." + Convert.ToString(dr[1]));
                }
            }

            return list;
        }

        #endregion
    }
}
