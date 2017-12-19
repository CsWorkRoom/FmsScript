using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oracle.ManagedDataAccess.Client;
using System.Collections;
using System.Data;
using System.IO;

namespace DbHelper
{
    /// <summary>
    /// Oracle数据库操作
    /// </summary>
    public partial class OracleHelper:IDisposable
    {
        private string _connString;
        private OracleConnection _conn;
        private OracleTransaction _trans;
        protected bool _isInTransaction = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="strConnection"></param>
        public OracleHelper(string strConnection)
        {
            _connString = strConnection;
            _conn = new OracleConnection(strConnection);
            _conn.Open();
        }

        #region 关闭数据库连接，释放资源

        /// <summary>
        /// 析构函数，关闭连接，释放资源
        /// </summary>
        ~OracleHelper()
        {
            Close();
        }

        /// <summary>
        /// 显示关闭连接
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        private void Close()
        {
            if (_conn != null && _conn.State != ConnectionState.Closed)
            {
                try
                {
                    _conn.Close();
                    _conn.Dispose();
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
        public ConnectionState GetConnectionState()
        {
            return _conn.State;
        }

        #endregion

        #region 事务操作
        /// <summary>
        /// 开始事务
        /// </summary>
        public void BeginTrans()
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
        public void CommitTrans()
        {
            _trans.Commit();
            _isInTransaction = false;
        }

        /// <summary>
        /// 事务回滚
        /// </summary>
        public void RollbackTrans()
        {
            _trans.Rollback();
            _isInTransaction = false;
        }

        #endregion

        /// <summary>
        /// 设置命令参数
        /// </summary>
        /// <param name="command">SQL语句</param>
        /// <param name="commandParameters">参数</param>
        private static void AttachParameters(OracleCommand command, OracleParameter[] commandParameters)
        {
            if (commandParameters != null)
            {
                foreach (OracleParameter parm in commandParameters)
                {
                    if ((parm.Direction == ParameterDirection.InputOutput) && (parm.Value == null))
                    {
                        parm.Value = DBNull.Value;
                    }
                    command.Parameters.Add(parm);
                }
            }
        }

        /// <summary>
        /// 执行一条SQL语句，返回整数
        /// </summary>
        /// <param name="connString">Connection string to database</param>
        /// <param name="cmdType">Command type either stored procedure or SQL</param>
        /// <param name="cmdText">Acutall SQL Command</param>
        /// <param name="commandParameters">Parameters to bind to the command</param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string connectionString, string cmdText, params OracleParameter[] commandParameters)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                connection.Open();
                OracleCommand cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = cmdText;
                AttachParameters(cmd, commandParameters);

                return cmd.ExecuteNonQuery();
            }
        }


        /// <summary>
        /// 执行一条SQL语句，返回一个结果对象
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteScalar(connString, CommandType.StoredProcedure, "PublishOrders", new OracleParameter(":prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a SqlConnection</param>
        /// <param name="commandText">the stored procedure name or PL/SQL command</param>
        /// <param name="commandParameters">an array of OracleParamters used to execute the command</param>
        /// <returns>An object that should be converted to the expected type using Convert.To{Type}</returns>
        public static object ExecuteScalar(string connectionString, string cmdText, params OracleParameter[] commandParameters)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                connection.Open();
                OracleCommand cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = cmdText;
                AttachParameters(cmd, commandParameters);

                return cmd.ExecuteScalar();
            }
        }
        

        /// <summary>
        /// 查询结果集返回DataSet
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="cmdText"></param>
        /// <param name="commandParameters"></param>
        /// <returns></returns>
        public static DataSet ExecuteDataset(string connectionString, string cmdText, params OracleParameter[] commandParameters)
        {
            using (OracleConnection connection = new OracleConnection(connectionString))
            {
                connection.Open();
                OracleCommand cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = cmdText;
                AttachParameters(cmd, commandParameters);

                OracleDataAdapter oda = new OracleDataAdapter(cmd);
                DataSet ds = new DataSet();
                oda.Fill(ds, "temp");

                return ds;
            }
        }

        /// <summary>
        /// 查询返回DataTable
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="cmdText"></param>
        /// <param name="commandParameters"></param>
        /// <returns></returns>
        public static DataTable ExecuteDataTable(string connectionString, string cmdText, params OracleParameter[] commandParameters)
        {
            DataSet ds = ExecuteDataset(connectionString, cmdText, commandParameters);
            if (ds != null && ds.Tables.Count > 0)
            {
                return ds.Tables[0];
            }

            return null;
        }


        /// <summary>
        /// 执行SQL语句返回DB2DataReader，为了防止异常而无法关闭连接，请通过using方式实例化本类再调用本方法
        /// </summary>
        /// <param name="connString">Connection string</param>
        //// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or PL/SQL command</param>
        /// <param name="commandParameters">an array of OracleParamters used to execute the command</param>
        /// <returns></returns>
        public OracleDataReader ExecuteReader(string connectionString, string cmdText, params OracleParameter[] commandParameters)
        {
            OracleCommand cmd = _conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = cmdText;
            AttachParameters(cmd, commandParameters);

            return cmd.ExecuteReader();
        }

    }
}
