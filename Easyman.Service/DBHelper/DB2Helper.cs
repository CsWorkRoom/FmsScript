//===============================================================================
// DB2Helper based on Microsoft Data Access Application Block (DAAB) for .NET
// http://msdn.microsoft.com/library/en-us/dnbda/html/daab-rm.asp
//
// DB2Helper.cs
//
// This file contains the implementations of the DB2Helper and DB2HelperParameterCache
// classes.
//
// The DAAB for MS .NET Provider for DB2 has been tested in the context of this Nile implementation,
// but has not undergone the generic functional testing that the SQL version has gone through.
// You can use it in other .NET applications using DB2 databases.  For complete docs explaining how to use
// and how it's built go to the originl appblock link. 
// For this sample, the code resides in the Nile namespaces not the Microsoft.ApplicationBlocks namespace
//==============================================================================
using System;
using System.Data;
using System.Xml;
using IBM.Data.DB2;
using System.Collections;
using System.Configuration;

namespace DbHelper
{
    /// <summary>
    /// DB2的数据库操作类
    /// </summary>
    public partial class DB2Helper
    {
        private string _connString;
        private DB2Connection _conn;
        private DB2Transaction _trans;
        protected bool _isInTransaction = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="strConnection"></param>
        public DB2Helper(string strConnection)
        {
            _connString = strConnection;
            _conn = new DB2Connection(strConnection);
            _conn.Open();
        }

        #region 关闭数据库连接，释放资源

        /// <summary>
        /// 析构函数，关闭连接，释放资源
        /// </summary>
        ~DB2Helper()
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
        /// 参数缓存
        /// </summary>
        private static Hashtable parmCache = Hashtable.Synchronized(new Hashtable());

        /// <summary>
        /// 设置命令参数
        /// </summary>
        /// <param name="command">SQL语句</param>
        /// <param name="commandParameters">参数</param>
        private static void AttachParameters(DB2Command command, DB2Parameter[] commandParameters)
        {
            if (commandParameters != null)
            {
                foreach (DB2Parameter parm in commandParameters)
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
        /// <param name="cmdText">Acutall SQL Command</param>
        /// <param name="commandParameters">Parameters to bind to the command</param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string connectionString, string cmdText, params DB2Parameter[] commandParameters)
        {
            using (DB2Connection connection = new DB2Connection(connectionString))
            {
                connection.Open();
                DB2Command cmd = connection.CreateCommand();
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
        public static object ExecuteScalar(string connectionString, string cmdText, params DB2Parameter[] commandParameters)
        {
            using (DB2Connection connection = new DB2Connection(connectionString))
            {
                connection.Open();
                DB2Command cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = cmdText;

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
        public static DataSet ExecuteDataset(string connectionString, string cmdText, params DB2Parameter[] commandParameters)
        {
            using (DB2Connection connection = new DB2Connection(connectionString))
            {
                connection.Open();
                DB2Command cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = cmdText;

                DB2DataAdapter oda = new DB2DataAdapter(cmd);
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
        public static DataTable ExecuteDataTable(string connectionString, string cmdText, params DB2Parameter[] commandParameters)
        {
            DataSet ds = ExecuteDataset(connectionString, cmdText, commandParameters);
            if (ds != null && ds.Tables.Count > 0)
            {
                return ds.Tables[0];
            }

            return null;
        }


        /// <summary>
        /// 执行SQL语句返回DB2DataReader，由于在返回前已经关闭连接，实际上，本方法没什么用，建议使用ExecuteDataTable方法
        /// </summary>
        /// <param name="connString">Connection string</param>
        /// <param name="commandText">the stored procedure name or PL/SQL command</param>
        /// <param name="commandParameters">an array of OracleParamters used to execute the command</param>
        /// <returns></returns>
        public DB2DataReader ExecuteReader(string connectionString, string cmdText, params DB2Parameter[] commandParameters)
        {
            DB2Command cmd = _conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = cmdText;
            AttachParameters(cmd, commandParameters);

            return cmd.ExecuteReader();
        }
    }
}