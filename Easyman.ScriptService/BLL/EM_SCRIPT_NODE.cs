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
    /// 脚本节点
    /// </summary>
    public class EM_SCRIPT_NODE : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_SCRIPT_NODE Instance = new EM_SCRIPT_NODE();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_SCRIPT_NODE()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_SCRIPT_NODE";
            this.ItemName = "脚本节点";
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
            public string NAME { get; set; }
            public long SCRIPT_NODE_TYPE_ID { get; set; }
            public string CODE { get; set; }
            public long DB_SERVER_ID { get; set; }
            public short SCRIPT_MODEL { get; set; }
            public string CONTENT { get; set; }
            public string REMARK { get; set; }
            public DateTime CREATE_TIME { get; set; }
            public long CREATE_UID { get; set; }
            public Nullable<long> DELETE_UID { get; set; }
            public Nullable<System.DateTime> DELETE_TIME { get; set; }
            public short IS_DELETE { get; set; }
            public Nullable<System.DateTime> UPDATE_TIME { get; set; }
            public Nullable<long> UPDATE_UID { get; set; }
            public string E_TABLE_NAME { get; set; }
            public string C_TABLE_NAME { get; set; }
            public Nullable<short> TABLE_TYPE { get; set; }
            public Nullable<short> TABLE_MODEL { get; set; }
        }
    }
}
