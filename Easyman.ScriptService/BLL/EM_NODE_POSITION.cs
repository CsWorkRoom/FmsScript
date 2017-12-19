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
    /// 节点位置
    /// </summary>
    public class EM_NODE_POSITION : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_NODE_POSITION Instance = new EM_NODE_POSITION();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_NODE_POSITION()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_NODE_POSITION";
            this.ItemName = "数据库标识";
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
            public Nullable<long> SCRIPT_ID { get; set; }
            public Nullable<long> SCRIPT_NODE_ID { get; set; }
            public Nullable<int> X { get; set; }
            public Nullable<int> Y { get; set; }
            public string DIV_ID { get; set; }
        }

        /// <summary>
        /// 查询脚本流的所有节点位置
        /// </summary>
        /// <param name="scriptID">脚本流ID</param>
        /// <returns>所有节点位置</returns>
        public IList<Entity> GetListByScriptID(long scriptID)
        {
            if (scriptID <= 0)
            {
                return null;
            }
            return GetList<Entity>("SCRIPT_ID=?", scriptID);
        }
    }
}
