using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Librarys.BaseQuery;
using Easyman.Librarys.DBHelper;
using System.Data;
using Easyman.Librarys.Log;

namespace Easyman.ScriptService.BLL
{
    /// <summary>
    /// 脚本流节点配置的实例
    /// </summary>
    public class EM_SCRIPT_REF_NODE_FORCASE : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_SCRIPT_REF_NODE_FORCASE Instance = new EM_SCRIPT_REF_NODE_FORCASE();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_SCRIPT_REF_NODE_FORCASE()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_SCRIPT_REF_NODE_FORCASE";
            this.ItemName = "脚本流节点配置的实例";
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
            public long PARENT_NODE_ID { get; set; }
            public long CURR_NODE_ID { get; set; }
            public string REMARK { get; set; }
        }

        /// <summary>
        /// 获取脚本流实例的所有节点
        /// </summary>
        /// <param name="scriptCaseID">脚本流实例的ID</param>
        /// <returns>键：当前节点ID，值：父节点ID列表</returns>
        public Dictionary<long, List<long>> GetNodeAndParents(long scriptCaseID)
        {
            Dictionary<long, List<long>> dic = new Dictionary<long, List<long>>();
            DataTable dt = GetTableFields("PARENT_NODE_ID, CURR_NODE_ID", "SCRIPT_CASE_ID=?", scriptCaseID);
            if (dt != null && dt.Rows.Count > 0)
            {
                long curNode = 0;
                long parentNode = 0;
                foreach (DataRow dr in dt.Rows)
                {
                    curNode = Convert.ToInt64(dr["CURR_NODE_ID"]);
                    parentNode = Convert.ToInt64(dr["PARENT_NODE_ID"]);
                    if (dic.ContainsKey(curNode) == false)
                    {
                        dic.Add(curNode, new List<long>());
                    }
                    //父节点
                    if (parentNode > 0)
                    {
                        dic[curNode].Add(parentNode);
                    }
                }
            }
            return dic;
        }

        /// <summary>
        /// 添加脚本流节点配置的实例（后期执行脚本流时，按此配置顺序执行相应节点）
        /// </summary>
        /// <param name="scriptID">脚本流ID</param>
        /// <param name="scriptCaseID">脚本流实例ID</param>
        /// <returns></returns>
        public List<long> AddReturnNodeIDList(long scriptID, long scriptCaseID)
        {
            IList<EM_SCRIPT_REF_NODE.Entity> refList = EM_SCRIPT_REF_NODE.Instance.GetNodeListByScriptID(scriptID);
            //用于去重
            Dictionary<long, byte> dic = new Dictionary<long, byte>();
            if (refList != null && refList.Count > 0)
            {
                using (BDBHelper dbHelper = new BDBHelper())
                {
                    //开始事务
                    dbHelper.BeginTrans();
                    foreach (EM_SCRIPT_REF_NODE.Entity refEntity in refList)
                    {
                        try
                        {
                            Entity entity = new Entity();
                            if (Main.KeyFieldIsUseSequence)
                            {
                                entity.ID = GetNextValueFromSeq();
                            }
                            entity.SCRIPT_ID = scriptID;
                            entity.SCRIPT_CASE_ID = scriptCaseID;
                            entity.PARENT_NODE_ID = refEntity.PARENT_NODE_ID;
                            entity.CURR_NODE_ID = refEntity.CURR_NODE_ID;
                            entity.REMARK = refEntity.REMARK;

                            int i = Add(entity);
                            if (i < 0)
                            {
                                dbHelper.RollbackTrans();
                                dic.Clear();
                                break;
                            }
                            if (dic.ContainsKey(entity.CURR_NODE_ID) == false)
                            {
                                dic.Add(entity.CURR_NODE_ID, 1);
                            }
                        }
                        catch (Exception ex)
                        {
                            BLog.Write(BLog.LogLevel.ERROR, "添加脚本流节点配置的实例出错\t" + ex.ToString());
                            //出错回滚
                            dbHelper.RollbackTrans();
                            return new List<long>();
                        }
                    }
                    //提交事务
                    dbHelper.CommitTrans();
                }
            }

            return dic.Keys.ToList<long>();
        }
    }
}