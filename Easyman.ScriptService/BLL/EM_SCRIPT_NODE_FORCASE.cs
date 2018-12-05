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
    /// 脚本节点支持两种形式脚本：常规命令、建表脚本。
    /// 表英文名、表中文别名、表类型、建表模式字段为‘建表脚本节点’特有。
    /// </summary>
    public class EM_SCRIPT_NODE_FORCASE : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_SCRIPT_NODE_FORCASE Instance = new EM_SCRIPT_NODE_FORCASE();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_SCRIPT_NODE_FORCASE()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_SCRIPT_NODE_FORCASE";
            this.ItemName = "";
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
            public long SCRIPT_NODE_ID { get; set; }
            public long SCRIPT_NODE_TYPE_ID { get; set; }
            public string NAME { get; set; }
            public string CODE { get; set; }
            public long DB_SERVER_ID { get; set; }
            public short SCRIPT_MODEL { get; set; }
            public string CONTENT { get; set; }
            public string REMARK { get; set; }
            public string E_TABLE_NAME { get; set; }
            public string C_TABLE_NAME { get; set; }
            public Nullable<short> TABLE_TYPE { get; set; }
            public Nullable<short> TABLE_MODEL { get; set; }
            public DateTime CREATE_TIME { get; set; }
            public long CREATE_UID { get; set; }
        }

        /// <summary>
        /// 添加节点实例列表
        /// </summary>
        /// <param name="scriptCaseID">脚本实例ID</param>
        /// <param name="nodeIDList">脚本相关节点ID列表</param>
        /// <returns>添加成功的节点实例ID列表</returns>
        public List<long> AddCaseReturnList(long scriptCaseID, List<long> nodeIDList)
        {
            List<long> list = new List<long>();

            if (scriptCaseID < 1 || nodeIDList.Count < 1)
            {
                return list;
            }

            int i = 0;
            int caseid = 0;
            using (BDBHelper dbHelper = new BDBHelper())
            {
                //开始事务
                dbHelper.BeginTrans();
                foreach (long nodeID in nodeIDList)
                {
                    try
                    {
                        EM_SCRIPT_NODE.Entity ne = EM_SCRIPT_NODE.Instance.GetEntityByKey<EM_SCRIPT_NODE.Entity>(nodeID);
                        Entity entity = new Entity();
                        if (Main.KeyFieldIsUseSequence)
                        {
                            entity.ID = GetNextValueFromSeq();
                        }
                        entity.SCRIPT_CASE_ID = scriptCaseID;
                        entity.SCRIPT_NODE_ID = ne.ID;
                        entity.SCRIPT_NODE_TYPE_ID = ne.SCRIPT_NODE_TYPE_ID;
                        entity.NAME = ne.NAME;
                        entity.CODE = ne.CODE;
                        entity.DB_SERVER_ID = ne.DB_SERVER_ID;
                        entity.SCRIPT_MODEL = ne.SCRIPT_MODEL;
                        if (entity.SCRIPT_MODEL == Enums.ScriptModel.CreateTb.GetHashCode())
                        {
                            entity.CONTENT = ne.CONTENT.ToUpper();
                            entity.E_TABLE_NAME = ne.E_TABLE_NAME.ToUpper();
                        }
                        else
                        {
                            entity.CONTENT = ne.CONTENT;
                            entity.E_TABLE_NAME = ne.E_TABLE_NAME;
                        }
                        entity.REMARK = ne.REMARK;
                        entity.C_TABLE_NAME = ne.C_TABLE_NAME;
                        entity.TABLE_TYPE = ne.TABLE_TYPE;
                        entity.TABLE_MODEL = ne.TABLE_MODEL;
                        entity.CREATE_TIME = ne.CREATE_TIME;
                        entity.CREATE_UID = ne.CREATE_UID;

                        caseid = Add(entity, true);
                        if (caseid > 0)
                        {
                            i++;
                            list.Add(caseid);
                        }
                    }
                    catch (Exception ex)
                    {
                        i = 0;
                        dbHelper.RollbackTrans();
                        list.Clear();

                        break;
                    }
                }

                if (i != nodeIDList.Count)
                {
                    i = 0;
                    dbHelper.RollbackTrans();
                    list.Clear();
                }

                //提交事务
                dbHelper.CommitTrans();
                dbHelper.Close();
            }

            return list;
        }
    }
}