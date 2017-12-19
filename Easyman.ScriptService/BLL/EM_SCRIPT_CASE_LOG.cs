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
    /// 脚本流实例的执行日志
    /// </summary>
    public class EM_SCRIPT_CASE_LOG : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_SCRIPT_CASE_LOG Instance = new EM_SCRIPT_CASE_LOG();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_SCRIPT_CASE_LOG()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_SCRIPT_CASE_LOG";
            this.ItemName = "脚本流实例的执行日志";
            this.KeyField = "ID";
            this.KeyFieldIsAutoIncrement = Main.KeyFieldIsAutoIncrement;
            this.OrderbyFields = "ID";
        }

        /// <summary>
        /// 实体
        /// </summary>
        public class Entity
        {
            public long ID { get; set; }
            public long SCRIPT_CASE_ID { get; set; }
            public DateTime LOG_TIME { get; set; }
            public short LOG_LEVEL { get; set; }
            public string LOG_MSG { get; set; }
            public string SQL_MSG { get; set; }
        }

        /// <summary>
        /// 添加一条日志
        /// </summary>
        /// <param name="scriptCaseID">脚本流实例ID</param>
        /// <param name="level">日志等级</param>
        /// <param name="message">日志内容</param>
        /// <param name="sql">SQL脚本</param>
        /// <returns></returns>
        public int Add(long scriptCaseID, int level, string message, string sql = "")
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            if (Main.KeyFieldIsUseSequence)
            {
                dic.Add("ID", GetNextValueFromSeq());
            }
            dic.Add("SCRIPT_CASE_ID", scriptCaseID);
            dic.Add("LOG_TIME", DateTime.Now);
            dic.Add("LOG_LEVEL", level);
            dic.Add("LOG_MSG", message);
            dic.Add("SQL_MSG", sql);

            return Add(dic);
        }
    }
}