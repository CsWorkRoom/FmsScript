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
    /// 节点连线配置实例（用于页面展示流事例的快照）
    /// </summary>
    public class EM_CONNECT_LINE_FORCASE : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_CONNECT_LINE_FORCASE Instance = new EM_CONNECT_LINE_FORCASE();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_CONNECT_LINE_FORCASE()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_CONNECT_LINE_FORCASE";
            this.ItemName = "节点连线实例";
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
            public long SCRIPT_CASE_ID { get; set; }
            public string FROM_DIV_ID { get; set; }
            public string FROM_POINT_ANCHORS { get; set; }
            public string TO_DIV_ID { get; set; }
            public string TO_POINT_ANCHORS { get; set; }
            public string CONTENT { get; set; }
        }

        /// <summary>
        /// 添加一个脚本实例的所有连线
        /// </summary>
        /// <param name="scriptCaseID">脚本实例</param>
        /// <param name="connectEntityList">节点连线对象列表</param>
        /// <returns></returns>
        public int Add(long scriptCaseID, IList<EM_CONNECT_LINE.Entity> connectEntityList)
        {
            if (scriptCaseID <= 0 || connectEntityList == null || connectEntityList.Count <= 0)
            {
                return 0;
            }
            int i = 0;
            foreach (var ce in connectEntityList)
            {
                Entity entity = new Entity();
                if (Main.KeyFieldIsUseSequence)
                {
                    entity.ID = GetNextValueFromSeq();
                }
                entity.SCRIPT_ID = ce.SCRIPT_ID;
                entity.SCRIPT_CASE_ID = scriptCaseID;
                entity.FROM_DIV_ID = ce.FROM_DIV_ID;
                entity.FROM_POINT_ANCHORS = ce.FROM_POINT_ANCHORS;
                entity.TO_DIV_ID = ce.TO_DIV_ID;
                entity.TO_POINT_ANCHORS = ce.TO_POINT_ANCHORS;
                entity.CONTENT = ce.CONTENT;

                i += Add<Entity>(entity, false);
            }

            return i;
        }
    }
}
