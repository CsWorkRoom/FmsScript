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
    /// 脚本流实例
    /// </summary>
    public class EM_SCRIPT_CASE : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_SCRIPT_CASE Instance = new EM_SCRIPT_CASE();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_SCRIPT_CASE()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_SCRIPT_CASE";
            this.ItemName = "脚本流实例";
            this.KeyField = "ID";
            this.KeyFieldIsAutoIncrement = Main.KeyFieldIsAutoIncrement;
            this.OrderbyFields = "ID";
        }

        /// <summary>
        /// 实体对象
        /// </summary>
        public class Entity
        {
            public long ID { get; set; }
            public string NAME { get; set; }
            public long SCRIPT_ID { get; set; }
            public int RETRY_TIME { get; set; }
            public Nullable<DateTime> START_TIME { get; set; }
            public Nullable<short> START_MODEL { get; set; }
            public Nullable<long> USER_ID { get; set; }
            public Nullable<short> RUN_STATUS { get; set; }
            public Nullable<short> IS_HAVE_FAIL { get; set; }
            public Nullable<short> RETURN_CODE { get; set; }
            public Nullable<System.DateTime> END_TIME { get; set; }
            /// <summary>
            /// 是否支持并发
            /// </summary>
            public Nullable<short> IS_SUPERVENE { get; set; }
        }

        /// <summary>
        /// 更新脚本实例运行状态为“停止”
        /// </summary>
        /// <param name="scriptCaseID">脚本实例ID</param>
        /// <param name="returnCode">执行结果</param>
        /// <returns></returns>
        public int SetStop(long scriptCaseID, Enums.ReturnCode returnCode)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("RUN_STATUS", Enums.RunStatus.Stop.GetHashCode());
            dic.Add("RETURN_CODE", returnCode.GetHashCode());
            dic.Add("END_TIME", DateTime.Now);
            return Update(dic, "ID=?", scriptCaseID);
        }

        /// <summary>
        /// 更新实例运行状态
        /// </summary>
        /// <param name="scriptCaseID">脚本实例ID</param>
        /// <param name="runStatus">运行状态</param>
        /// <returns></returns>
        public int UpdateRunStatus(long scriptCaseID, Enums.RunStatus runStatus)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("RUN_STATUS", runStatus.GetHashCode());
            return Update(dic, "ID=?", scriptCaseID);
        }

        /// <summary>
        /// 标记失败状态
        /// </summary>
        /// <param name="scriptCaseID">脚本实例ID</param>
        /// <returns></returns>
        public int SetFail(long scriptCaseID)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("RUN_STATUS", Enums.RunStatus.Stop.GetHashCode());
            dic.Add("IS_HAVE_FAIL", Enums.IsHaveFail.HaveFail.GetHashCode());
            dic.Add("RETURN_CODE", Enums.ReturnCode.Fail.GetHashCode());

            return Update(dic, "ID=?", scriptCaseID);
        }

        /// <summary>
        /// 获取脚本正在运行中的实例(排除并发的任务组)
        /// </summary>
        /// <param name="scriptID">脚本ID</param>
        /// <returns></returns>
        public Entity GetRunningCase(long scriptID)
        {
            return GetEntity<Entity>("SCRIPT_ID=? AND RUN_STATUS<>? AND IS_SUPERVENE<>?", scriptID, Enums.RunStatus.Stop.GetHashCode(), Enums.IsSupervene.Yes.GetHashCode());
        }

        /// <summary>
        /// 获取所有待执行及执行中脚本流实例的ID列表
        /// </summary>
        /// <returns></returns>
        public IList<Entity> GetRunningCaseList()
        {
            return GetList<Entity>("RUN_STATUS=?", Enums.RunStatus.Excute.GetHashCode());
        }

        /// <summary>
        /// 添加一个实例，返回实例ID
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="startModel">启动类型</param>
        /// <returns>实例ID</returns>
        public long AddReturnCaseID(long scriptID, Enums.StatusModel startModel = Enums.StatusModel.Anto)
        {
            EM_SCRIPT.Entity scriptEntity = EM_SCRIPT.Instance.GetEntityByKey<EM_SCRIPT.Entity>(scriptID);

            if (scriptEntity == null)
            {
                return 0;
            }

            Entity entity = new Entity();
            if (Main.KeyFieldIsUseSequence)
            {
                entity.ID = GetNextValueFromSeq();
            }
            entity.NAME = scriptEntity.NAME;
            entity.SCRIPT_ID = scriptID;
            entity.RETRY_TIME = scriptEntity.RETRY_TIME;
            entity.START_TIME = DateTime.Now;
            entity.START_MODEL = (short)startModel.GetHashCode();
            //初始添加为“等待”状态
            entity.RUN_STATUS = (short)Enums.RunStatus.Wait.GetHashCode();
            entity.IS_SUPERVENE = scriptEntity.IS_SUPERVENE;//是否并发
            int i = Add(entity, true);
            if (i > 0)
            {
                return i;
            }

            return 0;
        }
    }
}
