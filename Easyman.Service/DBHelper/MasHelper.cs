using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data;

namespace DbHelper
{
    /// <summary>
    /// Mas操作类
    /// </summary>
    public class MasHelper
    {
        /// <summary>
        /// Mas平台短信功能序号（固定）
        /// </summary>
        public const string Sismsid = "64a24682-e267-4564-8e58-6450d74e631e";

        /// <summary>
        /// 业务扩展码
        /// </summary>
        public static string Extcode = ProInterface.AppSet.MasPwd;

        /// <summary>
        /// 插件ID
        /// </summary>
        public static string Applicationid = ProInterface.AppSet.MasUid;

        /// <summary>
        /// 发送短信
        /// </summary>
        /// <param name="phone">手机号码(多个用分号隔开)</param>
        /// <param name="content">短信类容</param>
        /// <returns>返回入库条数</returns>
        public static int Send(string phone, string content)
        {
            int count = 0;

            StringBuilder sbSql = new StringBuilder();
            sbSql.Append("Insert into sms_outbox ");
            sbSql.Append("(sismsid, extcode, destaddr, messagecontent, reqdeliveryreport,msgfmt,sendmethod,requesttime,applicationid)");
            sbSql.Append("VALUES ('{0}', '{1}', '{2}', '{3}', 1, 15, 0, now(), '{4}')");

            string sql = string.Format(sbSql.ToString(), Sismsid, Extcode, phone, content, Applicationid);

            MySqlConnection conn_Mas = null;

            try
            {
                conn_Mas = GetMASConn();
                conn_Mas.Open();
                count = MySqlHelper.ExecuteNonQuery(conn_Mas, sql);
            }
            catch (Exception err)
            {
                //ErrLog.Record(err, sql);
            }
            finally
            {
                if (null != conn_Mas)
                    conn_Mas.Close(); //关闭连接
            }

            return count;
        }

        public static MySqlConnection GetMASConn()
        {
            return new MySqlConnection(ProInterface.AppSet.MasDb);
        }

        /// <summary>
        /// 得到回复短信
        /// </summary>
        /// <param name="phone">回复手机号码</param>
        /// <returns></returns>
        public static DataTable GetReply(string phone)
        {
            StringBuilder sbSql = new StringBuilder();
            sbSql.Append("select * from sms_inbox ");
            sbSql.Append("where extcode='{0}' and sourceaddr='{1}' and applicationid='{2}'");

            string sql = string.Format(sbSql.ToString(), Extcode, phone, Applicationid);

            DataTable dt = new DataTable();

            MySqlConnection conn_mas = GetMASConn();
            try
            {
                MySqlDataAdapter adapter = new MySqlDataAdapter(sql, conn_mas);
                adapter.Fill(dt);
            }
            catch (Exception er)
            {
                //ErrLog.Record(er);
            }
            finally
            {
                conn_mas.Close(); //关闭连接
            }

            return dt;
        }

        public static int Send1(string phone, string content)
        {
            int count = 0;

            StringBuilder sbSql = new StringBuilder();
            sbSql.Append("Insert into sms_outbox (a,b) VALUES('{0}', '{1}')");

            string sql = string.Format(sbSql.ToString(), phone, content);

            MySqlConnection conn_Mas = null;

            try
            {
                conn_Mas = GetMASConn();
                conn_Mas.Open();
                count = MySqlHelper.ExecuteNonQuery(conn_Mas, sql);
            }
            catch (Exception err)
            {
                //ErrLog.Record(err, sql);
            }
            finally
            {
                if (null != conn_Mas)
                    conn_Mas.Close(); //关闭连接
            }

            return count;
        }
    }

}