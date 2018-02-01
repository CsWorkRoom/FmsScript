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
    /// 手工触发记录表
    /// </summary>
    public class EM_HAND_RECORD : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_HAND_RECORD Instance = new EM_HAND_RECORD();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_HAND_RECORD()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_HAND_RECORD";
            this.ItemName = "手工触发记录表";
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
            public short HAND_TYPE { get; set; }
            public long USER_ID { get; set; }
            public long OBJECT_ID { get; set; }
            public System.DateTime ADD_TIME { get; set; }
            public short IS_CANCEL { get; set; }
            public string CANCEL_REASON { get; set; }
            public Nullable<System.DateTime> START_TIME { get; set; }
            public Nullable<long> OBJECT_CASE_ID { get; set; }
        }
        /// <summary>
        /// 获取需要执行的任务列表
        /// </summary>
        /// <returns></returns>
        public IList<Entity> GetNeedRunList()
        {
            //return GetList<Entity>("(IS_CANCEL=? OR IS_CANCEL IS NULL) AND (OBJECT_CASE_ID<1 OR OBJECT_CASE_ID IS NULL)", Enums.IsCancel.NoCancel);
            return GetList<Entity>("IS_CANCEL IS NULL");//IS_CANCEL为空表示未处理
        }

        /// <summary>
        /// 记录任务的实例ID
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <param name="objectCaseID">实例ID</param>
        /// <returns></returns>
        public int SetCaseID(long id, long objectCaseID)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("IS_CANCEL", Enums.IsCancel.NoCancel.GetHashCode());
            dic.Add("START_TIME", DateTime.Now);
            dic.Add("OBJECT_CASE_ID", objectCaseID);

            return UpdateByKey(dic, id);
        }

        /// <summary>
        /// 将任务设置为“取消执行”状态并记录实例ID
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <param name="objectCaseID">实例ID</param>
        /// <returns></returns>
        public int SetCancel(long id, long objectCaseID, string cancelReason = null)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("IS_CANCEL", Enums.IsCancel.Cancel.GetHashCode());
            dic.Add("START_TIME", DateTime.Now);
            dic.Add("OBJECT_CASE_ID", objectCaseID);
            dic.Add("CANCEL_REASON", cancelReason);

            return UpdateByKey(dic, id);
        }
    }
}
