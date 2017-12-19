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
    /// 数据库服务器
    /// </summary>
    public class EM_DB_SERVER : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_DB_SERVER Instance = new EM_DB_SERVER();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_DB_SERVER()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_DB_SERVER";
            this.ItemName = "数据库服务器";
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
            public string BYNAME { get; set; }
            public Nullable<long> DB_TAG_ID { get; set; }
            public string DB_TYPE { get; set; }
            public string IP { get; set; }
            public int PORT { get; set; }
            public string DATA_CASE { get; set; }
            public string USER { get; set; }
            public string PASSWORD { get; set; }
            public string REMARK { get; set; }
            public System.DateTime CREATE_TIME { get; set; }
            public Nullable<long> CREATE_UID { get; set; }
            public Nullable<long> DELETE_UID { get; set; }
            public Nullable<System.DateTime> DELETE_TIME { get; set; }
            public short IS_DELETE { get; set; }
            public Nullable<System.DateTime> UPDATE_TIME { get; set; }
            public Nullable<long> UPDATE_UID { get; set; }
        }

        /// <summary>
        /// 根据数据库名称获取数据库实体
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        /// <returns>数据库实体</returns>
        public Entity GetDbByName(string dbName)
        {
            return GetEntity<Entity>("BYNAME=?", dbName);
        }
    }
}
