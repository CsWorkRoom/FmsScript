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
    /// 节点连线配置
    /// </summary>
    public class EM_CONNECT_LINE : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_CONNECT_LINE Instance = new EM_CONNECT_LINE();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_CONNECT_LINE()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_CONNECT_LINE";
            this.ItemName = "节点连线配置";
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
            public string CONTENT { get; set; }
            public string FROM_DIV_ID { get; set; }
            public string FROM_POINT_ANCHORS { get; set; }
            public string TO_DIV_ID { get; set; }
            public string TO_POINT_ANCHORS { get; set; }
        }

        /// <summary>
        /// 查询脚本流的所有连线
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