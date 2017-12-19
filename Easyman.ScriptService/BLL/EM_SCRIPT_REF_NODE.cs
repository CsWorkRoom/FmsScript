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
    /// 节点前后依赖关系，形成脚本流
    /// </summary>
    public class EM_SCRIPT_REF_NODE : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_SCRIPT_REF_NODE Instance = new EM_SCRIPT_REF_NODE();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_SCRIPT_REF_NODE()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_SCRIPT_REF_NODE";
            this.ItemName = "脚本节点顺序";
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
            public long SCRIPT_ID { get; set; }
            public long PARENT_NODE_ID { get; set; }
            public long CURR_NODE_ID { get; set; }
            public string REMARK { get; set; }
        }

        /// <summary>
        /// 获取一个脚本流的所有节点
        /// </summary>
        /// <param name="scriptID"></param>
        /// <returns></returns>
        public IList<Entity> GetNodeListByScriptID(long scriptID)
        {
            return GetList<Entity>("SCRIPT_ID=?", scriptID);
        }
    }
}
