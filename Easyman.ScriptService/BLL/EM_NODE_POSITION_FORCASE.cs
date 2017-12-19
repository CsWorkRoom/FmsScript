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
    /// 节点位置实例
    /// </summary>
    public class EM_NODE_POSITION_FORCASE : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_NODE_POSITION_FORCASE Instance = new EM_NODE_POSITION_FORCASE();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_NODE_POSITION_FORCASE()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_NODE_POSITION_FORCASE";
            this.ItemName = "节点位置实例";
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
            public Nullable<long> SCRIPT_CASE_ID { get; set; }
            public Nullable<long> SCRIPT_NODE_ID { get; set; }
            public Nullable<int> X { get; set; }
            public Nullable<int> Y { get; set; }
            public string DIV_ID { get; set; }
        }

        /// <summary>
        /// 添加一个脚本实例的所有节点
        /// </summary>
        /// <param name="scriptCaseID">脚本实例</param>
        /// <param name="positionEntityList">节点位置对象列表</param>
        /// <returns></returns>
        public int Add(long scriptCaseID, IList<EM_NODE_POSITION.Entity> positionEntityList)
        {
            if (scriptCaseID <= 0 || positionEntityList == null || positionEntityList.Count <= 0)
            {
                return 0;
            }
            int i = 0;
            foreach (var pe in positionEntityList)
            {
                Entity entity = new Entity();
                if (Main.KeyFieldIsUseSequence)
                {
                    entity.ID = GetNextValueFromSeq();
                }
                entity.SCRIPT_ID = pe.SCRIPT_ID;
                entity.SCRIPT_CASE_ID = scriptCaseID;
                entity.SCRIPT_NODE_ID = pe.SCRIPT_NODE_ID;
                entity.X = pe.X;
                entity.Y = pe.Y;
                entity.DIV_ID = pe.DIV_ID;

                i += Add<Entity>(entity, false);
            }

            return i;
        }
    }
}
