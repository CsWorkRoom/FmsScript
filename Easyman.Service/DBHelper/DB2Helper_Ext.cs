
using System;
using System.Data;
using System.Xml;
using IBM.Data.DB2;
using System.Collections;
using System.Configuration;
using System.Collections.Generic;
using System.Text;

namespace DbHelper
{
    public partial class DB2Helper
    {
        /// <summary>
        /// 导入
        /// </summary>
        /// <param name="recc"></param>
        /// <param name="connectStr"></param>
        /// <param name="sql"></param>
        /// <param name="allDB2Para"></param>
        /// <returns></returns>
        public static bool Import(int recc, string connectStr, string sql, IList<DB2Parameter> allDB2Para)
        {
            //设置一个数据库的连接串
            using (DB2Connection conn = new DB2Connection(connectStr))
            {
                conn.Open();
                DB2Transaction transaction = conn.BeginTransaction();
                try
                {
                    DB2Command command = new DB2Command();
                    command.Connection = conn;
                    command.CommandType = CommandType.Text;
                    command.Transaction = transaction;
                    //到此为止，还都是我们熟悉的代码，下面就要开始喽
                    //这个参数需要指定每次批插入的记录数
                    command.ArrayBindCount = recc;
                    //在这个命令行中,用到了参数,参数我们很熟悉,但是这个参数在传值的时候
                    //用到的是数组,而不是单个的值,这就是它独特的地方
                    command.CommandText = sql;
                    //下面定义几个数组,分别表示三个字段,数组的长度由参数直接给出
                    foreach (var t in allDB2Para)
                    {
                        command.Parameters.Add(t);
                    }
                    //这个调用将把参数数组传进SQL,同时写入数据库
                    command.ExecuteNonQuery();
                    transaction.Commit();//提交事务
                }
                catch (Exception e)
                {
                    transaction.Rollback();//事务回滚
                    throw e;
                }
            }
            return true;
        }

        /// <summary>
        /// 导出
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="commandText"></param>
        /// <returns></returns>
        public static string ExportTxt(string connectionString, string commandText)
        {
            DataTable dt = ExecuteDataTable(connectionString, commandText);
            if (dt == null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            IList<string> allColumns = new List<string>();
            foreach (DataColumn dc in dt.Columns)
            {
                allColumns.Add(dc.ColumnName);
            }
            sb.AppendLine(string.Join(",", allColumns));

            if (dt.Rows.Count>0)
            {
                foreach(DataRow dr in dt.Rows)
                {
                    IList<string> row = new List<string>();
                    foreach (string c in allColumns)
                    {
                        if (dr[c] == null || dr[c] == DBNull.Value)
                        {
                            row.Add("");
                        }
                        else
                        {
                            row.Add(dr[c].ToString());
                        }
                    }

                    sb.AppendLine(string.Join(",", row));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取数据类型，用于类型转换
        /// </summary>
        /// <param name="dbTypeFullName"></param>
        /// <returns></returns>
        public static DB2Type GetDbType(string dbTypeFullName)
        {
            if (dbTypeFullName.IndexOf("System.Nullable`1[[") == 0)
            {
                dbTypeFullName = dbTypeFullName.Replace("System.Nullable`1[[", "");
                dbTypeFullName = dbTypeFullName.Substring(0, dbTypeFullName.IndexOf(","));
            }
            string dbTypeName = dbTypeFullName.Substring(dbTypeFullName.IndexOf(".") + 1);

            switch (dbTypeName)
            {
                case "Int16":
                    return DB2Type.SmallInt;
                case "Int32":
                    return DB2Type.Integer;
                case "Int64":
                    return DB2Type.Integer;
                case "DateTime":
                    return DB2Type.Timestamp;
                default:
                    return DB2Type.VarChar;
            }
        }
    }
}

