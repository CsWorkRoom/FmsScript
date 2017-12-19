using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Librarys.BaseQuery;
using Easyman.Librarys.DBHelper;
using System.Data;

namespace Easyman.ScriptService.BLL
{
    /// <summary>
    /// 节点的实例
    /// </summary>
    public class EM_SCRIPT_NODE_CASE : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_SCRIPT_NODE_CASE Instance = new EM_SCRIPT_NODE_CASE();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_SCRIPT_NODE_CASE()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_SCRIPT_NODE_CASE";
            this.ItemName = "节点实例";
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
            public long SCRIPT_NODE_ID { get; set; }
            public Nullable<long> DB_SERVER_ID { get; set; }
            public Nullable<short> SCRIPT_MODEL { get; set; }
            public string CONTENT { get; set; }
            public string REMARK { get; set; }
            public string E_TABLE_NAME { get; set; }
            public string C_TABLE_NAME { get; set; }
            public Nullable<short> TABLE_TYPE { get; set; }
            public Nullable<short> TABLE_MODEL { get; set; }
            public Nullable<System.DateTime> CREATE_TIME { get; set; }
            public Nullable<long> USER_ID { get; set; }
            public Nullable<long> TABLE_SUFFIX { get; set; }
            public Nullable<System.DateTime> START_TIME { get; set; }
            public Nullable<short> RUN_STATUS { get; set; }
            public Nullable<short> RETURN_CODE { get; set; }
            public int RETRY_TIME { get; set; }
            public Nullable<System.DateTime> END_TIME { get; set; }
        }

        /// <summary>
        /// 根据脚本流实例ID获取所有节点实例
        /// </summary>
        /// <param name="scriptCaseID">脚本流实例ID</param>
        /// <returns>节点实例列表</returns>
        public IList<Entity> GetListByScriptCaseID(long scriptCaseID)
        {
            return GetList<Entity>("SCRIPT_CASE_ID=? ", scriptCaseID);
        }

        /// <summary>
        /// 根据脚本流实例ID获取所有节点实例，并转为字典
        /// </summary>
        /// <param name="scriptCaseID">脚本流实例ID</param>
        /// <returns>节点实例列表，键：节点实例ID，值：节点实例</returns>
        public Dictionary<long, Entity> GetDictionaryByScriptCaseID(long scriptCaseID)
        {
            Dictionary<long, Entity> dic = new Dictionary<long, Entity>();

            IList<Entity> list = GetList<Entity>("SCRIPT_CASE_ID=? ", scriptCaseID);
            if (list != null && list.Count > 0)
            {
                foreach(Entity entity in list)
                {
                    if (dic.ContainsKey(entity.ID) == false)
                    {
                        dic.Add(entity.ID, entity);
                    }
                }
            }
            return dic;
        }

        /// <summary>
        /// 获取节点运行状态
        /// </summary>
        /// <param name="scriptID">脚本流ID</param>
        /// <param name="scriptCaseID">脚本实例ID</param>
        /// <param name="scriptNodeID">脚本节点ID</param>
        /// <returns></returns>
        public int GetRunStatus(long scriptID, long scriptCaseID, long scriptNodeID)
        {
            DataRow dr = GetRowFields("RUN_STATUS", "SCRIPT_ID=? AND SCRIPT_CASE_ID=? AND SCRIPT_NODE_ID=?", scriptID, scriptCaseID, scriptNodeID);
            if (dr != null)
            {
                return Convert.ToInt32(dr["RUN_STATUS"]);
            }

            return -1;
        }

        /// <summary>
        /// 获取节点运行状态
        /// </summary>
        /// <param name="scriptNodeCaseID">脚本节点实例ID</param>
        /// <returns></returns>
        public int GetRunStatus(long scriptNodeCaseID)
        {
            DataRow dr = GetRowByKey(scriptNodeCaseID);
            if (dr != null)
            {
                return Convert.ToInt32(dr["RUN_STATUS"]);
            }

            return -1;
        }

        /// <summary>
        /// 更新节点实例运行状态
        /// </summary>
        /// <param name="runStatus">运行状态</param>
        /// <returns></returns>
        public int SetRunStatus(long scriptNodeCaseID, Enums.RunStatus runStatus)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("RUN_STATUS", runStatus.GetHashCode());
            return Update(dic, "ID=?", scriptNodeCaseID);
        }

        /// <summary>
        /// 更新节点实例运行状态为“停止”
        /// </summary>
        /// <param name="scriptNodeCaseID">脚本节点实例ID</param>
        /// <param name="returnCode">执行结果标志</param>
        /// <returns></returns>
        public int SetStop(long scriptNodeCaseID, int returnCode)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("RUN_STATUS", Enums.RunStatus.Stop.GetHashCode());
            dic.Add("RETURN_CODE", returnCode);
            dic.Add("END_TIME", DateTime.Now);
            return Update(dic, "ID=?", scriptNodeCaseID);
        }

        /// <summary>
        /// 更新节点实例运行状态
        /// </summary>
        /// <param name="scriptNodeCaseID">脚本节点实例ID</param>
        /// <param name="runStatus">运行状态</param>
        /// <returns></returns>
        public int UpdateRunStatus(long scriptNodeCaseID, Enums.RunStatus runStatus)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("RUN_STATUS", runStatus.GetHashCode());
            return Update(dic, "ID=?", scriptNodeCaseID);
        }

        /// <summary>
        /// 记录重试次数
        /// </summary>
        /// <param name="scriptNodeCaseID">脚本节点实例ID</param>
        /// <returns>更新后的重试次数</returns>
        public int RecordTryTimes(long scriptNodeCaseID)
        {
            string sqlUpdate = "UPDATE " + TableName + " SET RETRY_TIME=RETRY_TIME+1 WHERE ID =?";
            string sqlGet = "SELECT RETRY_TIME FROM " + TableName + " WHERE ID =?";
            int i = 0;
            using (BDBHelper dbHelper = new BDBHelper())
            {
                i = dbHelper.ExecuteNonQueryParams(sqlUpdate, scriptNodeCaseID);
                if (i > 0)
                {
                    i = dbHelper.ExecuteScalarIntParams(sqlGet, scriptNodeCaseID);
                }
            }

            return i;
        }

        /// <summary>
        /// 获取脚本实例的所有运行中的节点实例ID
        /// </summary>
        /// <param name="scriptCaseID">脚本实例ID</param>
        /// <returns>节点实例ID列表</returns>
        public List<long> GetAllRunByScriptCaseID(long scriptCaseID)
        {
            List<long> list = new List<long>();
            DataTable dt = GetTableFields("ID", "SCRIPT_CASE_ID=? AND RUN_STATUS <> ?", scriptCaseID, Enums.RunStatus.Stop.GetHashCode());
            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    list.Add(Convert.ToInt64(dr["ID"]));
                }
            }

            return list;
        }

        /// <summary>
        /// 获取脚本实例的所有运行中的节点实例数量
        /// </summary>
        /// <param name="scriptCaseID">脚本实例ID</param>
        /// <returns></returns>
        public int GetRunCountByScriptCaseID(long scriptCaseID)
        {
            return GetCount("SCRIPT_CASE_ID=? AND RUN_STATUS <> ?", scriptCaseID, Enums.RunStatus.Stop.GetHashCode());
        }

        /// <summary>
        /// 获取脚本实例的结果（只要有一个节点失败则为失败）
        /// </summary>
        /// <param name="scriptCaseID">脚本实例ID</param>
        /// <returns></returns>
        public Enums.ReturnCode GetReturnCodeByScriptCaseID(long scriptCaseID)
        {
            int i = GetCount("SCRIPT_CASE_ID=? AND RETURN_CODE=?", scriptCaseID, Enums.ReturnCode.Fail.GetHashCode());
            if (i > 0)
            {
                return Enums.ReturnCode.Fail;
            }

            return Enums.ReturnCode.Success;
        }

        /// <summary>
        /// 获取一个执行失败的实例
        /// </summary>
        /// <param name="scriptNodeID">节点ID</param>
        /// <returns></returns>
        public Entity GetFailCase(long scriptNodeID)
        {
            return GetEntity<Entity>("SCRIPT_NODE_ID=? AND RETURN_CODE=?", scriptNodeID, Enums.ReturnCode.Fail.GetHashCode());
        }


        /// <summary>
        /// 添加节点实例列表
        /// </summary>
        /// <param name="scriptID">脚本流ID</param>
        /// <param name="scriptCaseID">脚本实例ID</param>
        /// <param name="nodeID">脚本相关节点ID列表</param>
        /// <returns>添加成功的节点实例ID</returns>
        public long AddReturnCaseID(long scriptID, long scriptCaseID, long nodeID)
        {
            EM_SCRIPT_NODE.Entity nodeEntity = EM_SCRIPT_NODE.Instance.GetEntityByKey<EM_SCRIPT_NODE.Entity>(nodeID);
            Entity entity = new Entity();
            if (Main.KeyFieldIsUseSequence)
            {
                entity.ID = GetNextValueFromSeq();
            }
            entity.SCRIPT_ID = scriptID;
            entity.SCRIPT_CASE_ID = scriptCaseID;
            entity.SCRIPT_NODE_ID = nodeID;
            entity.DB_SERVER_ID = nodeEntity.DB_SERVER_ID;
            entity.SCRIPT_MODEL = nodeEntity.SCRIPT_MODEL;
            if (nodeEntity.SCRIPT_MODEL == Enums.ScriptModel.CreateTb.GetHashCode())
            {
                entity.CONTENT = nodeEntity.CONTENT.ToUpper();
                entity.E_TABLE_NAME = nodeEntity.E_TABLE_NAME.ToUpper();
            }
            else
            {
                //替换自定义函数
                entity.CONTENT = Script.Transfer.ReplaceFunctions('$', nodeEntity.CONTENT);
                entity.E_TABLE_NAME = nodeEntity.E_TABLE_NAME;
            }
            entity.C_TABLE_NAME = nodeEntity.C_TABLE_NAME;
            entity.TABLE_TYPE = nodeEntity.TABLE_TYPE;
            entity.TABLE_MODEL = nodeEntity.TABLE_MODEL;
            entity.CREATE_TIME = nodeEntity.CREATE_TIME;
            entity.USER_ID = nodeEntity.CREATE_UID;
            entity.TABLE_SUFFIX = 0;
            entity.START_TIME = DateTime.Now;
            entity.RUN_STATUS = (short)Enums.RunStatus.Wait;
            entity.RETRY_TIME = 0;  //初始为0，每失败一次加1
            entity.REMARK = nodeEntity.REMARK;

            int i = Add(entity, true);
            if (i > 0)
            {
                return i;
            }

            return 0;
        }
    }
}