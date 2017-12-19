using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.Librarys.DBHelper
{
    /// <summary>
    /// 数据库操作类
    /// </summary>
    public class BDBHelper : IDisposable
    {
        private string _dbType;
        private string _connStr;
        private DBOperator _db;

        /// <summary>
        /// 配置文件中数据库类型定义的键名
        /// </summary>
        protected string ConfigKeyForDataBaseType = "DataBaseType";

        /// <summary>
        /// 配置文件中数据库连接字符串的键名
        /// </summary>
        protected string ConfigKeyForConnString = "ConnString";

        /// <summary>
        /// 数据库类型
        /// </summary>
        public string DbType { get { return _dbType; } }
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string ConnStr { get { return _connStr; } }

        /// <summary>
        /// 获取连接状态
        /// </summary>
        public ConnectionState ConnectionState { get { return _db.GetConnectionState(); } }

        /// <summary>
        /// 构造函数,数据库类型及连接字符串会读取默认配置项DataBaseType、ConnString
        /// </summary>
        public BDBHelper()
        {
            _dbType = Config.BConfig.GetConfigToString(ConfigKeyForDataBaseType);
            _connStr = Config.BConfig.GetConfigToString(ConfigKeyForConnString);
            _db = GetDBOperator(_dbType, _connStr);
            _db.Open();
        }


        /// <summary>
        /// 设置Timeout时间 命令执行时长（秒，默认为0，永不过期）
        /// </summary>
        public int CommandTimeout
        {
            set
            {
                _db.CommandTimeout = value;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbType">数据库类型（MYSQL/ORACLE/SQLSERVER/POSTGRESQL/DB2/SYBASE/INFORMIX）</param>
        /// <param name="connStr">连接字符串</param>
        public BDBHelper(string dbType, string connStr)
        {
            _dbType = dbType;
            _connStr = connStr;
            _db = GetDBOperator(dbType, connStr);
            _db.Open();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbType">数据库类型（MYSQL/ORACLE/SQLSERVER/POSTGRESQL/DB2/SYBASE/INFORMIX）</param>
        /// <param name="ip">数据库主机IP</param>
        /// <param name="port">主机端口</param>
        /// <param name="userName">登录账号</param>
        /// <param name="password">登录密码</param>
        /// <param name="database">数据库名</param>
        /// <param name="serviceName">服务名/实例名（可选，Oracle必填，为服务名）</param>
        public BDBHelper(string dbType, string ip, int port, string userName, string password, string database, string serviceName = "")
        {
            _dbType = dbType;
            _db = GetDBOperator(dbType, ip, port, userName, password, database, serviceName);
            _connStr = _db.ConnString;
            _db.Open();
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~BDBHelper()
        {
            _db.Close();
        }

        /// <summary>
        /// 显示关闭连接
        /// </summary>
        public void Dispose()
        {
            _db.Close();
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            try
            {
                _db.Close();
            }
            catch { }
        }

        #region 轻工厂制造数据库实例

        /// <summary>
        /// 创建数据库工厂实例
        /// </summary>
        /// <param name="dbType">数据库类型（MYSQL/ORACLE/SQLSERVER/POSTGRESQL/DB2/SYBASE/INFORMIX）</param>
        /// <param name="ip">数据库主机IP</param>
        /// <param name="port">主机端口</param>
        /// <param name="userName">登录账号</param>
        /// <param name="password">登录密码</param>
        /// <param name="database">数据库名</param>
        /// <param name="serviceName">服务名/实例名（可选，Oracle必填，为服务名）</param>
        /// <returns>数据库操作实例</returns>
        private static DBOperator GetDBOperator(string dbType, string connStr)
        {
            if (string.IsNullOrWhiteSpace(connStr) == true)
            {
                throw new Exception("连接字符串不可为空");
            }
            switch (dbType.ToUpper())
            {
                case "MYSQL":
                    throw new Exception("暂未实现的数据库类型：" + dbType);
                case "ORACLE":
                    return new Providers.Oracle(connStr);
                case "SQLSERVER":
                    throw new Exception("暂未实现的数据库类型：" + dbType);
                case "POSTGRESQL":
                    throw new Exception("暂未实现的数据库类型：" + dbType);
                case "DB2":
                    return new Providers.IBMDB2(connStr);
                case "SYBASE":
                    throw new Exception("暂未实现的数据库类型：" + dbType);
                case "INFORMIX":
                    throw new Exception("暂未实现的数据库类型：" + dbType);
                default:
                    throw new Exception("未知的数据库类型：" + dbType);
            }
        }

        /// <summary>
        /// 创建数据库工厂实例
        /// </summary>
        /// <param name="dbType">数据库类型（MYSQL/ORACLE/SQLSERVER/POSTGRESQL/DB2/SYBASE/INFORMIX）</param>
        /// <param name="connStr">连接字符串</param>
        /// <returns>数据库操作实例</returns>
        private static DBOperator GetDBOperator(string dbType, string ip, int port, string userName, string password, string database, string serviceName = "")
        {
            switch (dbType.ToUpper())
            {
                case "MYSQL":
                    throw new Exception("暂未实现的数据库类型：" + dbType);
                case "ORACLE":
                    return new Providers.Oracle(ip, port, userName, password, serviceName);
                case "SQLSERVER":
                    throw new Exception("暂未实现的数据库类型：" + dbType);
                case "POSTGRESQL":
                    throw new Exception("暂未实现的数据库类型：" + dbType);
                case "DB2":
                    return new Providers.IBMDB2(ip, port, userName, password, database);
                case "SYBASE":
                    throw new Exception("暂未实现的数据库类型：" + dbType);
                case "INFORMIX":
                    throw new Exception("暂未实现的数据库类型：" + dbType);
                default:
                    throw new Exception("未知的数据库类型：" + dbType);
            }
        }

        #endregion

        #region 事务操作
        /// <summary>
        /// 开始事务操作
        /// </summary>
        public void BeginTrans()
        {
            _db.BeginTrans();
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void CommitTrans()
        {
            _db.CommitTrans();
        }

        /// <summary>
        /// 事务回滚
        /// </summary>
        public void RollbackTrans()
        {
            _db.RollbackTrans();
        }

        #endregion

        #region 取序列值
        /// <summary>
        /// 从序列中提取下一个值
        /// </summary>
        /// <param name="seqName">序列名</param>
        /// <returns></returns>
        public int GetNextValueFromSeq(string seqName)
        {
            return _db.GetNextValueFromSeq(seqName);
        }

        #endregion

        #region ExecuteNonQuery

        /// <summary>
        /// 执行SQL语句，返回受影响记录条数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        public int ExecuteNonQuery(string sql)
        {
            try
            {
                return _db.ExecuteNonQuery(sql);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\nSQL语句为：{0}。\r\n{1}", sql, ex.ToString()));
            }
        }

        /// <summary>
        /// 执行SQL语句，返回受影响记录条数
        /// </summary>
        /// <param name="sql">SQL语句或命令（参数用问号?占位。为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <param name="value">参数值列表</param>
        /// <returns>受影响记录条数</returns>
        public int ExecuteNonQueryParams(string sql, params object[] value)
        {
            try
            {
                return _db.ExecuteNonQueryParams(sql, value);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\n数据库：{0}。\r\nSQL语句：{1}。\r\n参数列表\r\n{2}。\r\n{3}", DbType, sql, _db.ShowParamsList(_db.GetParamsList(value)), ex.ToString()));
            }
        }

        #endregion

        #region ExecuteScalar

        /// <summary>
        /// 执行一条SQL语句，返回第一行第一列object
        /// </summary>
        /// <param name="sql">SQL语句（为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <returns>object</returns>
        public object ExecuteScalar(string sql)
        {
            try
            {
                return _db.ExecuteScalar(sql);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\nSQL语句为：{0}。\r\n{1}", sql, ex.ToString()));
            }
        }

        /// <summary>
        /// 执行一条SQL语句，返回第一行第一列int
        /// </summary>
        /// <param name="sql">SQL语句（为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <returns>int</returns>
        public int ExecuteScalarInt(string sql)
        {
            object obj = ExecuteScalar(sql);
            if (obj is DBNull)
            {
                return 0;
            }
            try
            {
                return Convert.ToInt32(obj);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 执行一条SQL语句，返回第一行第一列string
        /// </summary>
        /// <param name="sql">SQL语句（为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <returns>string</returns>
        public string ExecuteScalarString(string sql)
        {
            object obj = ExecuteScalar(sql);
            if (obj is DBNull)
            {
                return string.Empty;
            }
            try
            {
                return Convert.ToString(obj);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 执行一条SQL语句，返回一个object
        /// </summary>
        /// <param name="sql">SQL语句或命令（为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <param name="value">参数值列表</param>
        /// <returns>object</returns>
        public object ExecuteScalarParams(string sql, params object[] value)
        {
            try
            {
                return _db.ExecuteScalarParams(sql, value);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\n数据库：{0}。\r\nSQL语句：{1}。\r\n参数列表\r\n{2}。\r\n{3}", DbType, sql, _db.ShowParamsList(_db.GetParamsList(value)), ex.ToString()));
            }
        }

        /// <summary>
        /// 执行一条SQL语句，返回一个int
        /// </summary>
        /// <param name="sql">SQL语句或命令（为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <param name="value">参数值列表</param>
        /// <returns>int</returns>
        public int ExecuteScalarIntParams(string sql, params object[] value)
        {
            object obj = ExecuteScalarParams(sql, value);
            if (obj is DBNull)
            {
                return 0;
            }
            try
            {
                return Convert.ToInt32(obj);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 执行一条SQL语句，返回一个string
        /// </summary>
        /// <param name="sql">SQL语句或命令（为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <param name="value">参数值列表</param>
        /// <returns>string</returns>
        public string ExecuteScalarStringParams(string sql, params object[] value)
        {
            object obj = ExecuteScalarParams(sql, value);
            if (obj is DBNull)
            {
                return string.Empty;
            }
            try
            {
                return Convert.ToString(obj);
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion

        #region ExecuteReader

        /// <summary>
        /// 执行一条SQL语句，返回一个IDataReader
        /// </summary>
        /// <param name="sql">SQL语句或命令</param>
        /// <returns>IDataReader</returns>
        public IDataReader ExecuteReader(string sql)
        {
            try
            {
                return _db.ExecuteReader(sql);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\n数据库：{0}。\r\nSQL语句：{1}。\r\n{2}", DbType, sql, ex.ToString()));
            }
        }

        /// <summary>
        /// B4执行一条SQL语句，返回一个IDataReader
        /// </summary>
        /// <param name="sql">SQL语句或命令</param>
        /// <param name="value">参数值列表</param>
        /// <returns>IDataReader</returns>
        public IDataReader ExecuteReaderParams(string sql, params object[] value)
        {
            try
            {
                return _db.ExecuteReader(sql);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\n数据库：{0}。\r\nSQL语句：{1}。\r\n参数列表\r\n{2}。\r\n{3}", DbType, sql, _db.GetParamsList(value), ex.ToString()));
            }
        }

        #endregion

        #region ExecuteDataRow

        /// <summary>
        /// 执行SQL语句，返回DataRow
        /// </summary>
        /// <param name="sql">SQL语句（为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <returns>DataRow</returns>
        public DataRow ExecuteDataRow(string sql)
        {
            try
            {
                DataTable dt = ExecuteDataTable(sql);
                if (dt != null && dt.Rows.Count > 0)
                {
                    return dt.Rows[0];
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\n数据库：{0}。\r\nSQL语句：{1}。\r\n{2}", DbType, sql, ex.ToString()));
            }
        }

        /// <summary>
        /// 执行SQL语句，返回DataRow
        /// </summary>
        /// <param name="sql">SQL语句或命令（为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <param name="value">参数值列表</param>
        /// <returns>DataRow</returns>
        public DataRow ExecuteDataRowParams(string sql, params object[] value)
        {
            try
            {
                DataTable dt = ExecuteDataTableParams(sql, value);
                if (dt != null && dt.Rows.Count != 0)
                {
                    return dt.Rows[0];
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\n数据库：{0}。\r\nSQL语句：{1}。\r\n参数列表\r\n{2}。\r\n{3}", DbType, sql, _db.GetParamsList(value), ex.ToString()));
            }
        }

        #endregion

        #region ExecuteDataTable

        /// <summary>
        /// 执行SQL语句，返回DataTable
        /// </summary>
        /// <param name="sql">SQL语句（为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <returns>DataTable</returns>
        public DataTable ExecuteDataTable(string sql)
        {
            try
            {
                return _db.ExecuteDataTable(sql);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\nSQL语句为：{0}。\r\n{1}", sql, ex.ToString()));
            }
        }

        /// <summary>
        /// 执行SQL语句，返回DataTable
        /// </summary>
        /// <param name="sql">SQL语句或命令（为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <param name="value">参数值列表</param>
        /// <returns>DataTable</returns>
        public DataTable ExecuteDataTableParams(string sql, params object[] value)
        {
            try
            {
                return _db.ExecuteDataTableParams(sql, value);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\n数据库：{0}。\r\nSQL语句：{1}。\r\n参数列表\r\n{2}。\r\n{3}", DbType, sql, _db.ShowParamsList(_db.GetParamsList(value)), ex.ToString()));
            }
        }

        #endregion

        #region ExecuteDataSet

        /// <summary>
        /// 执行SQL语句，返回DataSet
        /// </summary>
        /// <param name="sql">SQL语句（为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <returns>DataTable</returns>
        public DataSet ExecuteDataSet(string sql)
        {
            try
            {
                return _db.ExecuteDataSet(sql);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\nSQL语句为：{0}。\r\n{1}", sql, ex.ToString()));
            }
        }

        /// <summary>
        /// 执行SQL语句，返回DataSet
        /// </summary>
        /// <param name="sql">SQL语句或命令（为了Oracle上能够使用，表的别名前不要加AS）</param>
        /// <param name="value">参数值列表</param>
        /// <returns>DataTable</returns>
        public DataSet ExecuteDataSetParams(string sql, params object[] value)
        {
            try
            {
                return _db.ExecuteDataSetParams(sql, value);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\n数据库：{0}。\r\nSQL语句：{1}。\r\n参数列表\r\n{2}。\r\n{3}", DbType, sql, _db.ShowParamsList(_db.GetParamsList(value)), ex.ToString()));
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
        public DataSet ExecuteDataSetPage(string sql, int pageSize, int pageIndex)
        {
            try
            {
                return _db.ExecuteDataSetPage(sql, pageSize, pageIndex);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\n数据库：{0}。\r\nSQL语句：{1}。\r\n{2}", DbType, sql, ex.ToString()));
            }
        }

        /// <summary>
        /// 分页查询，返回DataSet
        /// </summary>
        /// <param name="sql">除去分页之外的SQL语句</param>
        /// <param name="pageSize">页面大小（单页记录条数）</param>
        /// <param name="pageIndex">当前页码（页号从1开始）</param>
        /// <param name="value">参数列表</param>
        /// <returns></returns>
        public DataSet ExecuteDataSetPageParams(string sql, int pageSize, int pageIndex, params object[] value)
        {
            try
            {
                return _db.ExecuteDataSetPageParams(sql, pageSize, pageIndex, value);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\n数据库：{0}。\r\nSQL语句：{1}。\r\n参数列表\r\n{2}。\r\n{3}", DbType, sql, _db.ShowParamsList(_db.GetParamsList(value)), ex.ToString()));
            }
        }

        /// <summary>
        /// 分页查询，返回DataTable
        /// </summary>
        /// <param name="sql">除去分页之外的SQL语句</param>
        /// <param name="pageSize">页面大小（单页记录条数）</param>
        /// <param name="pageIndex">当前页码（页号从1开始）</param>
        /// <returns></returns>
        public DataTable ExecuteDataTablePage(string sql, int pageSize, int pageIndex)
        {
            try
            {
                return _db.ExecuteDataTablePage(sql, pageSize, pageIndex);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\n数据库：{0}。\r\nSQL语句：{1}。\r\n{2}", DbType, sql, ex.ToString()));
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
            try
            {
                return _db.ExecuteDataTablePageParams(sql, pageSize, pageIndex, value);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\n数据库：{0}。\r\nSQL语句：{1}。\r\n参数列表\r\n{2}。\r\n{3}", DbType, sql, _db.ShowParamsList(_db.GetParamsList(value)), ex.ToString()));
            }
        }
        #endregion

        #region 流式查询分页
        /// <summary>
        /// 使用Reader分页查询，返回DataTable（适用于比较靠前的页）
        /// </summary>
        /// <param name="sql">除去分页之外的SQL语句</param>
        /// <param name="pageSize">页面大小（单页记录条数）</param>
        /// <param name="pageIndex">当前页码（页号从1开始）</param>
        /// <returns></returns>
        public DataTable ExecuteDataTablePageWithReader(string sql, int pageSize, int pageIndex)
        {
            return ExecuteDataTablePageWithReaderParams(sql, pageSize, pageIndex, null);
        }

        /// <summary>
        /// 使用Reader分页查询，返回DataTable（适用于比较靠前的页）
        /// </summary>
        /// <param name="sql">除去分页之外的SQL语句</param>
        /// <param name="pageSize">页面大小（单页记录条数）</param>
        /// <param name="pageIndex">当前页码（页号从1开始）</param>
        /// <param name="value">参数列表</param>
        /// <returns></returns>
        public DataTable ExecuteDataTablePageWithReaderParams(string sql, int pageSize, int pageIndex, params object[] value)
        {
            try
            {
                DataTable dt = new DataTable();
                int start = (pageIndex - 1) * pageSize + 1;
                int rowsCount = 0;

                using (IDataReader reader = ExecuteReader(sql))
                {
                    for (int c = 0; c < reader.FieldCount; c++)
                    {
                        dt.Columns.Add(reader.GetName(c), reader.GetFieldType(c));
                    }

                    int i = 0;
                    while (reader.IsClosed == false && reader.Read())
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
                }

                return dt;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("查询失败！\r\n数据库：{0}。\r\nSQL语句：{1}。\r\n参数列表\r\n{2}。\r\n{3}", DbType, sql, _db.ShowParamsList(_db.GetParamsList(value)), ex.ToString()));
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
            return _db.GetRowsCount(tableName);
        }

        #endregion

        #region 将数据导出到文件

        /// <summary>
        /// 将DataTable里面的内容写入文件，字段间以制表符分隔
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <param name="fileName">文件名，全路径，建议以.txt为后缀</param>
        /// <param name="fieldsTerminated">字段分隔符，默认为\t制表符</param>
        /// <returns></returns>
        public void WriteDataTableIntoFile(DataTable dt, string fileName, string fieldsTerminated = "\t")
        {
            if (dt == null)
            {
                return;
            }
            List<string> columnNames = new List<string>();
            foreach (DataColumn col in dt.Columns)
            {
                columnNames.Add(col.ColumnName);
            }

            WriteDataTableIntoFile(dt, columnNames, fileName, Encoding.UTF8, fieldsTerminated);
        }

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
            _db.WriteDataTableIntoFile(dt, columnNames, fileName, encoding, fieldsTerminated);
        }

        #endregion

        #region 导入txt到数据库
        /// <summary>
        /// 从本地文件导入数据到数据库表中，默认：文件为UTF-8编码，导入所有字段，字段分隔符为\t，记录分隔符为\n
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fileName">本地文件名</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns>导入记录条数</returns>
        public int LoadDataInLocalFile(string tableName, string fileName, bool isReplace = false)
        {
            if (!System.IO.File.Exists(fileName))
            {
                throw new Exception("文件" + fileName + "不存在，无法导入表" + tableName);
            }
            return _db.LoadDataInLocalFile(tableName, fileName, isReplace);
        }

        /// <summary>
        /// 从本地文件导入数据到数据库表中，默认：文件为UTF-8编码，字段分隔符为\t，记录分隔符为\n
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fields">字段列表</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns>导入记录条数</returns>
        public int LoadDataInLocalFile(string tableName, string fileName, List<string> fields, bool isReplace = false)
        {
            if (!System.IO.File.Exists(fileName))
            {
                throw new Exception("文件" + fileName + "不存在，无法导入表" + tableName);
            }
            return _db.LoadDataInLocalFile(tableName, fileName, fields, isReplace);
        }

        /// <summary>
        /// 从本地文件导入数据到数据库表中，默认：文件为UTF-8编码，导入所有字段，记录分隔符为\n
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fieldsTerminated">字段分隔符</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns>导入记录条数</returns>
        public int LoadDataInLocalFile(string tableName, string fileName, string fieldsTerminated, bool isReplace = false)
        {
            if (!System.IO.File.Exists(fileName))
            {
                throw new Exception("文件" + fileName + "不存在，无法导入表" + tableName);
            }
            return _db.LoadDataInLocalFile(tableName, fileName, fieldsTerminated, isReplace);
        }

        /// <summary>
        /// 从本地文件导入数据到数据库表中，默认：文件为UTF-8编码，记录分隔符为\n
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fields">字段列表</param>
        /// <param name="fieldsTerminated">字段分隔符</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns>导入记录条数</returns>
        public int LoadDataInLocalFile(string tableName, string fileName, List<string> fields, string fieldsTerminated, bool isReplace = false)
        {
            if (!System.IO.File.Exists(fileName))
            {
                throw new Exception("文件" + fileName + "不存在，无法导入表" + tableName);
            }
            return _db.LoadDataInLocalFile(tableName, fileName, fields, fieldsTerminated, isReplace);
        }

        /// <summary>
        /// 从本地文件导入数据到数据库表中，默认：文件为UTF-8编码
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fields">字段列表</param>
        /// <param name="fieldsTerminated">字段分隔符</param>
        /// <param name="linesTerminated">记录分隔符</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns>导入记录条数</returns>
        public int LoadDataInLocalFile(string tableName, string fileName, List<string> fields, string fieldsTerminated, string linesTerminated, bool isReplace = false)
        {
            if (!System.IO.File.Exists(fileName))
            {
                throw new Exception("文件" + fileName + "不存在，无法导入表" + tableName);
            }
            return _db.LoadDataInLocalFile(tableName, fileName, fields, fieldsTerminated, linesTerminated, isReplace);
        }

        #endregion

        #region 从内存导入数据到数据库

        /// <summary>
        /// 从DataTable导入数据到数据库表（适用于小批量数据导入）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="dt">数据表（字段名通过ColumnName来指定）</param>
        /// <returns></returns>
        public int LoadDataInDataTable(string tableName, DataTable dt)
        {
            return _db.LoadDataInDataTable(tableName, dt);
        }

        /// <summary>
        /// 通过先写文件再导入的方式来将DataTable导入到表，适用于大数据（至少10万条）才有优势
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="dt">数据表（字段名通过ColumnName来指定）</param>
        /// <returns></returns>
        public int LoadDataInDataTableWithFile(string tableName, DataTable dt)
        {
            return _db.LoadDataInDataTableWithFile(tableName, dt);
        }

        /// <summary>
        /// 定义一个委托
        /// </summary>
        /// <param name="result"></param>
        public delegate void SomeKindOfDelegate(int loadRowsCount);
        /// <summary>
        /// 定义一个事件
        /// </summary>
        public event SomeKindOfDelegate aDelegate;

        /// <summary>
        /// 从List导入数据到数据库表（适用于小批量数据导入）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="list">数据列表（每条记录为一个字典，字典的键为字段名，值为字段值</param>
        /// <returns>导入数据的条数</returns>
        public int LoadDataInList(string tableName, List<Dictionary<string, object>> list)
        {
            if (list == null || list.Count < 1 || list[0] == null)
            {
                return 0;
            }
            int i = 0;
            int colcount = list[0].Count;

            List<object> par = new List<object>();

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("INSERT INTO " + tableName + "({0}) VALUES ", string.Join(",", list[0].Keys));


            foreach (Dictionary<string, object> dic in list)
            {
                if (i == 0)
                {
                    sb.Append("(");
                }
                else
                {
                    sb.Append(",(");
                }

                int c = 0;
                foreach (var kvp in dic)
                {
                    par.Add(kvp.Value);

                    if (c == 0)
                    {
                        sb.Append("?");
                    }
                    else
                    {
                        sb.Append(",?");
                    }
                    c++;
                }
                sb.AppendLine(")");

                i++;
            }


            //执行SQL语句
            return ExecuteNonQueryParams(sb.ToString(), par);
        }

        #endregion


        #region 复制表结构

        /// <summary>
        /// 根据一张表的结构创建另外一张表
        /// </summary>
        /// <param name="createTableName">要创建的表名</param>
        /// <param name="likeTableName">另外的表名</param>
        /// <returns>创建成功返回true，失败返回false</returns>
        public bool CreateTable(string createTableName, string likeTableName)
        {
            return _db.CreateTable(createTableName, likeTableName);
        }

        /// <summary>
        /// 根据DataTable创建表
        /// </summary>
        /// <param name="createTableName">要创建的表名</param>
        /// <param name="datatable">数据</param>
        /// <returns></returns>
        public bool CreateTableFromDataTable(string createTableName, DataTable datatable)
        {
            return _db.CreateTableFromDataTable(createTableName, datatable);
        }

        /// <summary>
        /// 生成要创建表的SQL脚本
        /// </summary>
        /// <param name="createTableName">要创建的表名</param>
        /// <param name="datatable">数据</param>
        /// <returns></returns>
        public string MakeCreateTableSql(string createTableName, DataTable datatable)
        {
            return _db.MakeCreateTableSql(createTableName, datatable);
        }

        #endregion

        #region 各种删除表

        /// <summary>
        /// 删除表所有记录(逐行删除，会写事务日志，较慢）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public int Delete(string tableName)
        {
            try
            {
                return _db.ExecuteNonQuery("DELETE FROM " + tableName);
            }
            catch (Exception ex)
            {
                throw new Exception("删除表" + tableName + "所有记录出错！" + ex.Message);
            }
        }

        /// <summary>
        /// 清除表所有记录（不写事务日志，更快）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public bool Truncate(string tableName)
        {
            try
            {
                _db.ExecuteNonQuery("TRUNCATE TABLE " + tableName);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("删除表" + tableName + "所有记录出错！" + ex.Message);
            }
        }

        /// <summary>
        /// 删除表，慎用！！！！
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="isIgnoreCase">是否忽略大小写，默认为忽略</param>
        /// <returns></returns>
        public bool Drop(string tableName, bool isIgnoreCase = true)
        {
            try
            {
                if (isIgnoreCase)
                {
                    _db.ExecuteNonQuery("DROP TABLE " + tableName);
                }
                else
                {
                    string[] ss = tableName.Split(new char[] { '.' });
                    if (ss.Length == 1)
                    {
                        _db.ExecuteNonQuery("DROP TABLE \"" + ss[0] + "\"");
                    }
                    else
                    {
                        _db.ExecuteNonQuery("DROP TABLE \"" + ss[0] + "\".\"" + ss[1] + "\"");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("删除表" + tableName + "出错！" + ex.Message);
            }
        }

        #endregion

        #region 获取表信息

        /// <summary>
        /// 判断表名是否存在
        /// </summary>
        /// <param name="tableName">对于Oracle和DB2，要同时传入所有者，格式：OWNER.TABLENAME</param>
        /// <returns></returns>
        public bool TableIsExists(string tableName)
        {
            try
            {
                return _db.TableIsExists(tableName);
            }
            catch (Exception ex)
            {
                throw new Exception("验证表名" + tableName + "是否存在出错!" + ex.Message);
            }
        }

        /// <summary>
        /// 查询表名列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetTablesList()
        {
            try
            {
                return _db.GetTablesList();
            }
            catch (Exception ex)
            {
                throw new Exception("获取表名列表出错!" + ex.Message);
            }
        }

        /// <summary>
        /// 根据表名，获取字段列表
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>表字段列表</returns>
        public List<string> GetFieldsList(string tableName)
        {
            try
            {
                return _db.GetFieldsList(tableName);
            }
            catch (Exception ex)
            {
                throw new Exception("获取表" + tableName + "的字段列表出错!" + ex.Message);
            }
        }

        /// <summary>
        /// 判断表某字段值是否重复
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fieldName">字段名</param>
        /// <param name="value">字段值</param>
        /// <param name="keyField">主键字段名</param>
        /// <param name="keyValue">主键值</param>
        /// <returns>bool</returns>
        public bool IsDuplicate(string tableName, string fieldName, string value, string keyField, string keyValue)
        {
            string sql = string.Format("SELECT COUNT(*) FROM {0} WHERE {1} != ? AND {2} = ?", tableName, keyField, fieldName);
            byte ret = Convert.ToByte(ExecuteScalarParams(sql, keyValue, value));
            return ret > 0 ? true : false;
        }

        /// <summary>
        /// 判断表某字段是否重复
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fieldName">字段名</param>
        /// <param name="oldValue">如果是修改，则为旧值；如果是添加，则为string.Empty</param>
        /// <param name="newValue">新值</param>
        /// <returns>bool</returns>
        public bool IsDuplicate(string tableName, string fieldName, string oldValue, string newValue)
        {
            if (oldValue == newValue)
            {
                return false;
            }
            string sql = string.Format("SELECT COUNT(*) FROM {0} WHERE {1} = ?", tableName, fieldName);
            byte ret = Convert.ToByte(ExecuteScalarParams(sql, newValue));
            return ret > 0 ? true : false;
        }

        /// <summary>
        /// 判断表某字段是否重复
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="fieldName">字段名</param>
        /// <param name="oldValue">如果是修改，则为旧值；如果是添加，则为string.Empty</param>
        /// <param name="newValue">新值</param>
        /// <param name="condition">附加判断条件（不带WHERE，不带AND）</param>
        /// <returns>bool</returns>
        public bool IsDuplicate(string tableName, string fieldName, string oldValue, string newValue, string[] conditions)
        {
            if (oldValue == newValue)
            {
                return false;
            }
            string conondition = string.Empty;
            foreach (string c in conditions)
            {
                conondition += " AND " + c;
            }

            string sql = string.Format("SELECT COUNT(*) FROM {0} WHERE {1} = ? {2}", tableName, fieldName, conondition);
            byte ret = Convert.ToByte(ExecuteScalarParams(sql, newValue));
            return ret > 0 ? true : false;
        }

        /// <summary>
        /// 判断表某字段是否重复
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="condition">判断条件（不带关键词WHERE）</param>
        /// <returns>bool</returns>
        public bool IsDuplicate(string tableName, string condition)
        {
            string sql = string.Format("SELECT COUNT(*) FROM {0} WHERE {1}", tableName, condition);
            byte ret = Convert.ToByte(ExecuteScalar(sql));
            return ret > 0 ? true : false;
        }

        #endregion

        #region 获取当前连接串中的数据库名称


        /// <summary>
        /// 获取数据库连接串中涉及的数据库名,通过database关键字获取
        /// </summary>
        /// <returns></returns>
        public string GetDBName()
        {
            try
            {
                var ary = ConnStr.ToLower().Trim().Split(';');

                foreach (var item in ary)
                {
                    if (item.Contains("database"))
                    {
                        if (item.IndexOf('=') > -1)
                        {
                            return item.Split('=')[1];
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}