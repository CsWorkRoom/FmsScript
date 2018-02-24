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
    /// 脚本流
    /// </summary>
    public class EM_SCRIPT : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_SCRIPT Instance = new EM_SCRIPT();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_SCRIPT()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_SCRIPT";
            this.ItemName = "脚本流";
            this.KeyField = "ID";
            this.KeyFieldIsAutoIncrement = Main.KeyFieldIsAutoIncrement;
            this.OrderbyFields = "ID";
        }

        /// <summary>
        /// 实体对象
        /// </summary>
        public class Entity
        {
            public long ID { get; set; }
            public string NAME { get; set; }
            public long SCRIPT_TYPE_ID { get; set; }
            public string CRON { get; set; }
            public Int16? STATUS { get; set; }
            public int RETRY_TIME { get; set; }
            public string REMARK { get; set; }
            public DateTime CREATE_TIME { get; set; }
            public Nullable<long> CREATE_UID { get; set; }
            public Nullable<long> DELETE_UID { get; set; }
            public Nullable<DateTime> DELETE_TIME { get; set; }
            public short IS_DELETE { get; set; }
            public Nullable<DateTime> UPDATE_TIME { get; set; }
            public Nullable<long> UPDATE_UID { get; set; }
            public virtual EM_SCRIPT_TYPE EM_SCRIPT_TYPE { get; set; }
            /// <summary>
            /// 是否支持并发》20180224cs添加,为支持任务组能并发执行
            /// </summary>
            public Nullable<short> IS_SUPERVENE { get; set; }
        }

        /// <summary>
        /// 获取所有启用的脚本
        /// </summary>
        /// <returns>键：脚本ID，值：时间表达式</returns>
        public Dictionary<int, string> GetAllEnables()
        {
            return GetDictionary("ID", "CRON", "STATUS=? AND IS_DELETE=?", Enums.ScriptStatus.Open.GetHashCode(), Enums.IsDelete.No.GetHashCode());
        }

        /// <summary>
        /// 获取脚本的时间表达式
        /// </summary>
        /// <param name="scriptID">脚本ID</param>
        /// <returns>时间表达式</returns>
        public string GetTimeExpression(long scriptID)
        {
            return GetStringValueByKey(scriptID, "CRON");
        }
    }
}
