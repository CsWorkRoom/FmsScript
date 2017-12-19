using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.Librarys.DBHelper
{
    /// <summary>
    /// 数据库操作基类
    /// 依赖说明：无依赖，不要直接实例化，通过BDBHelper来调用具体的实例。
    /// 异常处理：捕获但不处理异常。
    /// </summary>
    public abstract class DBOperator
    {
        #region 连接信息
        protected string _connString = string.Empty;
        protected string _ip = string.Empty;
        protected int _port = -1;
        protected string _userName = string.Empty;
        protected string _password = string.Empty;
        protected string _database = string.Empty;
        protected string _service = string.Empty;
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnString { get { return _connString; } }
        public string IP
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_ip))
                {
                    GetConnectInfoFromConnString();
                }
                return _ip;
            }
        }
        public int Port
        {
            get
            {
                if (_port < 0)
                {
                    GetConnectInfoFromConnString();
                }
                return _port;
            }
        }

        public string UserName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_userName))
                {
                    GetConnectInfoFromConnString();
                }
                return _userName;
            }
        }
        public string Password
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_password))
                {
                    GetConnectInfoFromConnString();
                }
                return _password;
            }
        }
        public string DataBase { get { return _database; } }
        public string ServiceName { get { return _service; } }

        #endregion
        /// <summary>
        /// 命令执行时长（秒，默认为0，永不过期）
        /// </summary>
        public int CommandTimeout = 0;

        /// <summary>
        /// SQL命令
        /// </summary>
        protected DbCommand comm;

        #region 生成连接字符串
        /// <summary>
        /// 获取数据库连接字符串
        /// </summary>
        /// <param name="dbType">数据库类型（MYSQL/ORACLE/SQLSERVER/POSTGRESQL/DB2/SYBASE/INFORMIX）</param>
        /// <param name="ip">数据库主机IP</param>
        /// <param name="port">主机端口</param>
        /// <param name="userName">登录账号</param>
        /// <param name="password">登录密码</param>
        /// <param name="database">数据库</param>
        /// <param name="serviceName">服务名/实例名（可选，Oracle必填，为服务名）</param>
        /// <returns></returns>
        protected string GetDBConnString(string dbType)
        {
            switch (dbType.ToUpper())
            {
                case "MYSQL":
                    return string.Format("Host={0};Port={1};Username={2};Password={3};Database={4};Allow Zero Datetime=True;", _ip, _port, _userName, _password, _database);
                case "ORACLE":
                    return string.Format("User ID={2};Password={3};Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1}))(CONNECT_DATA=(SERVICE_NAME={4})));Persist Security Info=True;", _ip, _port, _userName, _password, _service);
                case "SQLSERVER":
                    return string.Format("Data Source={0},{1};Network Library=DBMSSOCN;Initial Catalog={4};User ID={2};Password={3}", _ip, _port, _userName, _password, _database);
                case "POSTGRESQL":
                    return string.Format("Host={0};Port={1};Username={2};Password={3};Database={4};", _ip, _port, _userName, _password, _database);
                case "DB2":
                    return string.Format("Server={0}:{1};UID={2};PWD={3};DataBase={4};", _ip, _port, _userName, _password, _database);
                case "SYBASE":
                    return string.Format("Data Source={0};Port={1};UID={2};PWD={3};database={4};charset=cp850;", _ip, _port, _userName, _password, _database);
                case "INFORMIX":
                    return string.Format("Host={0};Port={1}; User id={2}; Password={3}; Database={4};Service={5};", _ip, _port, _userName, _password, _database);
                default:
                    throw new Exception("未知的数据库类型：" + dbType);
            }
        }

        #endregion
        #region 提取连接信息
        /// <summary>
        /// 从连接字符串里面提取连接信息
        /// </summary>
        public abstract void GetConnectInfoFromConnString();
        #endregion

        #region 打开/关闭数据库连接
        /// <summary>
        /// 打开数据库连接
        /// </summary>
        public abstract void Open();

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        public abstract void Close();
        #endregion

        #region 获取连接状态
        /// <summary>
        /// 获取连接状态
        /// </summary>
        /// <returns></returns>
        public abstract ConnectionState GetConnectionState();

        #endregion

        #region 事务操作
        /// <summary>
        /// 开始事务
        /// </summary>
        public abstract void BeginTrans();

        /// <summary>
        /// 执行事务
        /// </summary>
        public abstract void CommitTrans();

        /// <summary>
        /// 事务回滚
        /// </summary>
        public abstract void RollbackTrans();
        #endregion

        /// <summary>
        /// 创建一个命令
        /// </summary>
        /// <param name="command">SQL语句或命令</param>
        /// <returns></returns>
        protected abstract DbCommand CreateCommand(string command);

        /// <summary>
        /// 构造包含参数的SQL语句或命令
        /// </summary>
        /// <param name="command">SQL语句或命令</param>
        /// <param name="value">参数值列表</param>
        protected abstract DbCommand CreateCommand(string command, params object[] value);

        /// <summary>
        /// 从序列中提取下一个值
        /// </summary>
        /// <param name="seqName">序列名</param>
        /// <returns></returns>
        public abstract int GetNextValueFromSeq(string seqName);

        /// <summary>
        /// 生成参数列表
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public List<object> GetParamsList(params object[] value)
        {
            List<object> paramsList = new List<object>();
            foreach (object obj in value)
            {
                if (obj is IList<object>)
                {
                    IList<object> listTemp = obj as IList<object>;
                    for (int i = 0; i < listTemp.Count; i++)
                    {
                        paramsList.Add(listTemp[i]);
                    }
                }
                else if (obj is System.Enum)
                {
                    paramsList.Add(obj.GetHashCode());
                }
                else
                {
                    paramsList.Add(obj);
                }
            }

            return paramsList;
        }

        #region 无参数调用
        /// <summary>
        /// 执行SQL语句，返回受影响记录条数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        public int ExecuteNonQuery(string sql)
        {
            DbCommand comm = CreateCommand(sql);

            return comm.ExecuteNonQuery();
        }

        /// <summary>
        /// A2返回一个DataSet
        /// </summary>
        /// <param name="sql">SQL语句或命令</param>
        /// <returns>DataSet</returns>
        public abstract DataSet ExecuteDataSet(string sql);

        /// <summary>
        /// A3执行一条SQL语句，返回一个object
        /// </summary>
        /// <param name="sql">SQL语句或命令</param>
        /// <returns>object</returns>
        public object ExecuteScalar(string sql)
        {
            DbCommand comm = CreateCommand(sql);

            return comm.ExecuteScalar();
        }

        /// <summary>
        /// 执行一条SQL语句，返回一个DbDataReader
        /// </summary>
        /// <param name="sql">SQL语句或命令</param>
        /// <returns>DbDataReader</returns>
        public DbDataReader ExecuteReader(string sql)
        {
            DbCommand comm = CreateCommand(sql);

            return comm.ExecuteReader();
        }

        /// <summary>
        /// 执行SQL语句，返回DataTable
        /// </summary>
        /// <param name="sql">SQL语句（为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <returns>DataTable</returns>
        public DataTable ExecuteDataTable(string sql)
        {
            DataSet ds = ExecuteDataSet(sql);
            if (ds != null && ds.Tables.Count > 0)
            {
                return ds.Tables[0];
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region 带参数查询
        /// <summary>
        /// B1执行一个语句不返回任何值
        /// </summary>
        /// <param name="sql">SQL语句或命令</param>
        /// <param name="value">参数值列表</param>
        /// <returns>受影响行数</returns>
        public int ExecuteNonQueryParams(string sql, params object[] value)
        {
            DbCommand comm = CreateCommand(sql, value);

            return comm.ExecuteNonQuery();
        }

        /// <summary>
        /// B2返回一个DataSet
        /// </summary>
        /// <param name="sql">SQL语句或命令</param>
        /// <param name="value">参数值列表</param>
        /// <returns>DataSet</returns>
        public abstract DataSet ExecuteDataSetParams(string sql, params object[] value);

        /// <summary>
        /// B3执行一条SQL语句，返回一个object
        /// </summary>
        /// <param name="sql">SQL语句或命令</param>
        /// <param name="value">参数值列表</param>
        /// <returns>object</returns>
        public object ExecuteScalarParams(string sql, params object[] value)
        {
            DbCommand comm = CreateCommand(sql, value);
            comm.CommandTimeout = 0;

            return comm.ExecuteScalar();
        }

        /// <summary>
        /// B4执行一条SQL语句，返回一个DbDataReader
        /// </summary>
        /// <param name="sql">SQL语句或命令</param>
        /// <param name="value">参数值列表</param>
        /// <returns>DbDataReader</returns>
        public DbDataReader ExecuteReaderParams(string sql, params object[] value)
        {
            DbCommand comm = CreateCommand(sql, value);

            return comm.ExecuteReader();
        }

        /// <summary>
        /// 执行SQL语句，返回DataTable
        /// </summary>
        /// <param name="sql">SQL语句或命令（为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <param name="value">参数值列表</param>
        /// <returns>DataTable</returns>
        public DataTable ExecuteDataTableParams(string sql, params object[] value)
        {
            DataSet ds = ExecuteDataSetParams(sql, value);
            if (ds != null && ds.Tables.Count > 0)
            {
                return ds.Tables[0];
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region 分页查询

        /// <summary>
        /// 分页查询，返回DataSet
        /// </summary>
        /// <param name="sql">除去分页之外的SQL语句</param>
        /// <param name="pageSize">页面大小（单页记录条数）</param>
        /// <param name="pageIndex">当前页码（页号从1开始）</param>
        /// <returns></returns>
        public abstract DataSet ExecuteDataSetPage(string sql, int pageSize, int pageIndex);

        /// <summary>
        /// 分页查询，返回DataSet
        /// </summary>
        /// <param name="sql">除去分页之外的SQL语句</param>
        /// <param name="pageSize">页面大小（单页记录条数）</param>
        /// <param name="pageIndex">当前页码（页号从1开始）</param>
        /// <param name="value">参数列表</param>
        /// <returns></returns>
        public abstract DataSet ExecuteDataSetPageParams(string sql, int pageSize, int pageIndex, params object[] value);

        /// <summary>
        /// 分页查询，返回DataTable
        /// </summary>
        /// <param name="sql">除去分页之外的SQL语句</param>
        /// <param name="pageSize">页面大小（单页记录条数）</param>
        /// <param name="pageIndex">当前页码（页号从1开始）</param>
        /// <returns></returns>
        public DataTable ExecuteDataTablePage(string sql, int pageSize, int pageIndex)
        {
            DataSet ds = ExecuteDataSetPage(sql, pageSize, pageIndex);
            if (ds != null && ds.Tables.Count > 0)
            {
                return ds.Tables[0];
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// 分页查询，返回DataTable
        /// </summary>
        /// <param name="sql">除去分页之外的SQL语句</param>
        /// <param name="pageSize">页面大小（单页记录条数）</param>
        /// <param name="pageIndex">当前页码（页号从1开始）</param>
        /// <param name="value">参数列表</param>
        /// <returns></returns>
        public DataTable ExecuteDataTablePageParams(string sql, int pageSize, int pageIndex, params object[] value)
        {
            DataSet ds = ExecuteDataSetPageParams(sql, pageSize, pageIndex, value);
            if (ds != null && ds.Tables.Count > 0)
            {
                return ds.Tables[0];
            }
            else
            {
                return null;
            }
        }

        #endregion


        #region 查询表的记录数

        /// <summary>
        /// 查询表的记录数
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public int GetRowsCount(string tableName)
        {
            string sql = "SELECT COUNT(*) FROM " + tableName;
            return Convert.ToInt32(ExecuteScalar(sql));
        }

        #endregion

        #region 从内存导入数据到数据库

        /// <summary>
        /// 从DataTable导入数据到数据库表（适用于小批量数据导入）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="dt">数据表（字段名通过ColumnName来指定）</param>
        /// <returns></returns>
        public virtual int LoadDataInDataTable(string tableName, DataTable dt)
        {
            if (dt == null || dt.Rows.Count < 1)
            {
                return 0;
            }

            List<string> fileds = new List<string>();
            foreach (DataColumn col in dt.Columns)
            {
                fileds.Add(col.ColumnName);
            }
            int i = 0;
            int colcount = dt.Columns.Count;
            List<object> par = new List<object>();

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("INSERT INTO " + tableName + "({0}) VALUES ", string.Join(",", fileds));
            foreach (DataRow dr in dt.Rows)
            {
                if (i == 0)
                {
                    sb.Append("(");
                }
                else
                {
                    sb.Append(",(");
                }

                for (int c = 0; c < colcount; c++)
                {
                    if (c > 0)
                    {
                        sb.Append(",");
                    }

                    if (dr[c] == null || dr[c] == DBNull.Value)
                    {
                        sb.Append("null");
                    }
                    else
                    {
                        sb.Append("?");
                        par.Add(dr[c]);
                    }
                }
                sb.AppendLine(")");

                i++;
            }

            //执行SQL语句
            return ExecuteNonQueryParams(sb.ToString(), par);
        }

        /// <summary>
        /// 通过先写文件再导入的方式来将DataTable导入到表，适用于大数据（至少10万条）才有优势
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="dt">数据表（字段名通过ColumnName来指定）</param>
        /// <returns></returns>
        public abstract int LoadDataInDataTableWithFile(string tableName, DataTable dt);

        /// <summary>
        /// 将DataTable里面的内容写入文件，字段间以制表符分隔
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <param name="columnNames">要写的字段列表</param>
        /// <param name="fileName">文件名，全路径，建议以.txt为后缀</param>
        /// <param name="encoding">文件编码格式</param>
        /// <param name="fieldsTerminated">字段分隔符，默认为\t制表符</param>
        /// <returns></returns>
        public void WriteDataTableIntoFile(DataTable dt, List<string> columnNames, string fileName, Encoding encoding, string fieldsTerminated = "\t")
        {
            if (dt == null)
            {
                return;
            }

            //写数据文件
            using (StreamWriter streamWriter = new StreamWriter(fileName, false, encoding))
            {
                int col = 0;
                foreach (DataRow dr in dt.Rows)
                {
                    col = 0;
                    foreach (string column in columnNames)
                    {
                        if (col > 0)
                        {
                            streamWriter.Write(fieldsTerminated);
                        }
                        if (dt.Columns[column].DataType == typeof(DateTime))
                        {
                            if (dr[column] == DBNull.Value)
                            {
                                //streamWriter.Write("0000-00-00 00:00:00");
                            }
                            else
                            {
                                streamWriter.Write(((DateTime)dr[column]).ToString("yyyy-MM-dd HH:mm:ss"));
                            }
                        }
                        else
                        {

                            streamWriter.Write(Convert.ToString(dr[column]).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' '));
                        }
                        col++;
                    }
                    streamWriter.WriteLine();
                    streamWriter.Flush();
                }
                streamWriter.Close();
                streamWriter.Dispose();
            }
        }

        /// <summary>
        /// 删除同名的所有文件（仅后缀名不同）
        /// </summary>
        /// <param name="baseFileName">基准文件</param>
        /// <returns></returns>
        public static int DeleteAllFilesLikeBase(string baseFileName)
        {
            int i = 0;

            if (File.Exists(baseFileName) == false)
            {
                return 0;
            }

            FileInfo fi = new FileInfo(baseFileName);
            FileInfo[] fis = fi.Directory.GetFiles(Path.GetFileNameWithoutExtension(baseFileName) + ".*");
            foreach (FileInfo f in fis)
            {
                try
                {
                    if (f.Name.EndsWith(".bad"))
                    {
                        continue;
                    }
                    f.Delete();
                    i++;
                }
                catch
                {

                }
            }

            return i;
        }

        #endregion

        #region 导入txt到数据库

        /// <summary>
        /// 从本地文件导入数据到数据库表中
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fileName">本地文件名</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns></returns>
        public abstract int LoadDataInLocalFile(string tableName, string fileName, bool isReplace = false);

        /// <summary>
        /// 从本地文件导入数据到数据库表中
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fields">字段列表</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns></returns>
        public abstract int LoadDataInLocalFile(string tableName, string fileName, List<string> fields, bool isReplace = false);

        /// <summary>
        /// 从本地文件导入数据到数据库表中
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fieldsTerminated">字段分隔符</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns></returns>
        public abstract int LoadDataInLocalFile(string tableName, string fileName, string fieldsTerminated, bool isReplace = false);

        /// <summary>
        /// 从本地文件导入数据到数据库表中
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fields">字段列表</param>
        /// <param name="fieldsTerminated">字段分隔符</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns></returns>
        public abstract int LoadDataInLocalFile(string tableName, string fileName, List<string> fields, string fieldsTerminated, bool isReplace = false);

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
        public abstract int LoadDataInLocalFile(string tableName, string fileName, List<string> fields, string fieldsTerminated, string linesTerminated, bool isReplace = false);

        #endregion

        #region 复制表结构

        /// <summary>
        /// 根据一张表的结构创建另外一张表
        /// </summary>
        /// <param name="createTableName">要创建的表名</param>
        /// <param name="likeTableName">另外一张表的表名</param>
        /// <returns>创建成功返回true，失败返回false</returns>
        public abstract bool CreateTable(string createTableName, string likeTableName);

        /// <summary>
        /// 根据DataTable创建表
        /// </summary>
        /// <param name="createTableName">要创建的表名</param>
        /// <param name="datatable">数据</param>
        /// <returns></returns>
        public bool CreateTableFromDataTable(string createTableName, DataTable datatable)
        {
            string sql = "";
            try
            {
                sql = MakeCreateTableSql(createTableName, datatable);
                ExecuteNonQuery(sql);
            }
            catch (Exception ex)
            {
                new Exception("根据DataTable创建表失败，转换的SQL为：\r\n" + sql + "\r\n错误信息：\r\n" + ex.ToString());
            }

            return true;
        }

        /// <summary>
        /// 生成要创建表的SQL脚本
        /// </summary>
        /// <param name="createTableName">要创建的表名</param>
        /// <param name="datatable">数据</param>
        /// <returns></returns>
        public abstract string MakeCreateTableSql(string createTableName, DataTable datatable);

        /// <summary>
        /// 转换字段数据类型（用于生成创建表的脚本）
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <param name="col">字段</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>字段类型</returns>
        public abstract string ConvertColumnType(DataTable dt, DataColumn col, int maxLength);

        /// <summary>
        /// 获取数据表各个字段的最大长度
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <returns>各个字段的值最大长度 键：字段名 值：最大长度</returns>
        public static Dictionary<string, int> GetColumnsMaxLength(DataTable dt)
        {
            if (dt == null)
            {
                return null;
            }
            Dictionary<string, int> dic = new Dictionary<string, int>();
            foreach (DataColumn col in dt.Columns)
            {
                dic.Add(col.ColumnName, 1);
            }

            List<string> columns = dic.Keys.ToList<string>();
            foreach (DataRow dr in dt.Rows)
            {
                foreach (string col in columns)
                {
                    if (dr[col] != null && dr[col] != DBNull.Value)
                    {
                        string v = Convert.ToString(dr[col]);

                        if (v.Length > dic[col])
                        {
                            dic[col] = v.Length;
                        }
                    }
                }
            }

            //Easyman.Librarys.Log.BLog.Write(Log.BLog.LogLevel.DEBUG, string.Format("字段【{0}】的最大长度为【{1}】", colName, len));
            return dic;
        }

        #endregion

        #region 获取表的一些信息
        /// <summary>
        /// 判断表名是否存在
        /// </summary>
        /// <param name="tableName">对于Oracle和DB2，要同时传入所有者，格式：OWNER.TABLENAME</param>
        /// <param name="isIgnoreCase">是否忽略大小写，默认为忽略</param>
        /// <returns></returns>
        public abstract bool TableIsExists(string tableName, bool isIgnoreCase = true);

        /// <summary>
        /// 查询表名列表
        /// </summary>
        /// <returns></returns>
        public abstract List<string> GetTablesList();

        /// <summary>
        /// 根据表名，获取字段列表
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>表字段列表</returns>
        public List<string> GetFieldsList(string tableName)
        {
            List<string> list = new List<string>();

            string sql = "SELECT * FROM " + tableName + " WHERE 1=0";
            DataTable dt = ExecuteDataTable(sql);
            if (dt != null)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    list.Add(col.ColumnName);
                }
            }

            return list;
        }

        #endregion

        #region 显示参数列表(多数情况下用于调试输出)
        /// <summary>
        /// 显示参数列表
        /// </summary>
        /// <param name="paramsList"></param>
        /// <returns></returns>
        public string ShowParamsList(List<object> paramsList)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            foreach (object obj in paramsList)
            {
                sb.Append(i + ":[");
                if (obj == null || obj == DBNull.Value)
                {
                    sb.Append("null");
                }
                else
                {
                    sb.Append(Convert.ToString(obj));
                }
                sb.AppendLine("]");
                i++;
            }

            return sb.ToString();
        }
        #endregion
    }
}