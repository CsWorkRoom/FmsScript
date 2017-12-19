using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Librarys.BaseQuery;
using Easyman.Librarys.DBHelper;

namespace Easyman.ScriptService.BLL
{
    /// <summary>
    /// 任务节点实例的执行日志
    /// </summary>
    public class EM_SCRIPT_NODE_CASE_LOG : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_SCRIPT_NODE_CASE_LOG Instance = new EM_SCRIPT_NODE_CASE_LOG();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_SCRIPT_NODE_CASE_LOG()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_SCRIPT_NODE_CASE_LOG";
            this.ItemName = "脚本流";
            this.KeyField = "ID";
            this.KeyFieldIsAutoIncrement = Main.KeyFieldIsAutoIncrement;
            this.OrderbyFields = "ID";
        }

        /// <summary>
        /// 添加一条日志
        /// </summary>
        /// <param name="scriptNodeCaseID">节点实例ID</param>
        /// /// <param name="logLevel">日志等级</param>
        /// <param name="logMessage">日志信息</param>
        /// <param name="sql">SQL脚本</param>
        /// <returns></returns>
        public int Add(long scriptNodeCaseID, int logLevel, string logMessage, string sql)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            if (Main.KeyFieldIsUseSequence)
            {
                dic.Add("ID", GetNextValueFromSeq());
            }
            dic.Add("SCRIPT_NODE_CASE_ID", scriptNodeCaseID);
            dic.Add("LOG_TIME", DateTime.Now);
            dic.Add("LOG_MSG", logMessage);
            dic.Add("LOG_LEVEL", logLevel);
            dic.Add("SQL_MSG", sql);

            return Add(dic);
        }
    }
}