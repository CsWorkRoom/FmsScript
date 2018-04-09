using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Easyman.Librarys.Log;
using System.ComponentModel;
using System.IO;
using Easyman.Librarys.Config;
using Easyman.Librarys.DBHelper;

namespace Easyman.ScriptService
{
    /// <summary>
    /// 主程序，入口
    /// </summary>
    public class Main
    {
        /// <summary>
        /// 运行标志
        /// </summary>
        public static bool IsRun = false;

        /// <summary>
        /// 重新加载策略的时间间隔（秒钟）
        /// </summary>
        public const int RELOAD_RULE_SECONDS = 10;
        /// <summary>
        /// 允许最大同时执行的节点数量（默认为10）
        /// </summary>
        public static int MaxExecuteNodeCount = 10;
        /// <summary>
        /// 允许并行的上传数（默认为10）
        /// </summary>
        public static int MaxUploadCount = 10;
        /// <summary>
        /// 每次从库中查询出的待处理文件数量（默认50）
        /// </summary>
        public static int EachSearchUploadCount = 50;
        /// <summary>
        /// 单次从队列获取上传数（默认为5）
        /// </summary>
        public static int EachUploadCount = 5;
        /// <summary>
        /// 当前正在上传的数量
        /// </summary>
        public static int CurUploadCount = 0;

        /// <summary>
        /// 当前正在监控的数量
        /// </summary>
        public static int CurMonitCount = 0;

        /// <summary>
        /// 监控的限定的数量（默认为2）
        /// </summary>
        public static int MaxMonitCount = 0;
        /// <summary>
        /// 主键是否是自增长（可能已经使用了数据库自带的自增或者通过触发器实现自增，默认为true）
        /// </summary>
        public static bool KeyFieldIsAutoIncrement = true;
        /// <summary>
        /// 主键字段是否使用序列，如果不使用序列，则使用数据库的触发器或者自增长实现主键值的填充
        /// </summary>
        public static bool KeyFieldIsUseSequence = false;
        /// <summary>
        /// 后台线程，不断扫描新的脚本流
        /// </summary>
        private static BackgroundWorker _bw;
        /// <summary>
        /// 已经启动的脚本流线程
        /// </summary>
        private static Dictionary<int, Task.Flow> _dicTaskers = new Dictionary<int, Task.Flow>();
        /// <summary>
        /// 上次加载脚本流的时间
        /// </summary>
        private static DateTime _lastLoadTaskFlowTime = DateTime.MinValue;
        /// <summary>
        /// 当前正在运行的脚本及其实例 键：脚本节点实例ID，值：启动时间
        /// </summary>
        private static Dictionary<long, DateTime> _dicRunningNodeCaseID = new Dictionary<long, DateTime>();
        /// <summary>
        /// 错误的时间表达式
        /// </summary>
        private static Dictionary<long, bool> _dicErrorTimeExpression = new Dictionary<long, bool>();

        /// <summary>
        /// 启动
        /// </summary>
        public static void Start()
        {
            IsRun = true;

            try
            {
                BLog.Write(BLog.LogLevel.INFO, "程序即将启动。");

                MaxExecuteNodeCount = BConfig.GetConfigToInt("MaxExecuteNodeCount");
                if (MaxExecuteNodeCount < 1)
                {
                    MaxExecuteNodeCount = 10;
                }

                MaxUploadCount = BConfig.GetConfigToInt("MaxUploadCount");

                EachUploadCount = BConfig.GetConfigToInt("EachUploadCount");

                EachSearchUploadCount = BConfig.GetConfigToInt("EachSearchUploadCount");

                MaxMonitCount = BConfig.GetConfigToInt("MaxMonitCount");

                //if (MaxExecuteNodeCount < 1)
                //{
                //    MaxExecuteNodeCount = 10;
                //}

                if (bool.TryParse(BConfig.GetConfigToString("KeyFieldIsAutoIncrement"), out KeyFieldIsAutoIncrement) == false)
                {
                    KeyFieldIsAutoIncrement = true;
                }

                if (bool.TryParse(BConfig.GetConfigToString("KeyFieldIsUseSequence"), out KeyFieldIsUseSequence) == false)
                {
                    BLog.Write(BLog.LogLevel.FATAL, "KeyFieldIsUseSequence配置不正确，请在.config中配置为true或false。");
                    IsRun = false;
                    return;
                }

                if (KeyFieldIsAutoIncrement && KeyFieldIsUseSequence)
                {
                    BLog.Write(BLog.LogLevel.FATAL, "KeyFieldIsAutoIncrement和KeyFieldIsUseSequence不可以同时配置为true，即：数据库已经可以自己实现自增长了，就不再需要另外配置序列，请在.config中修改配置。");
                    IsRun = false;
                    return;
                }

                if (KeyFieldIsAutoIncrement == false && KeyFieldIsUseSequence == false)
                {
                    BLog.Write(BLog.LogLevel.FATAL, "KeyFieldIsAutoIncrement和KeyFieldIsUseSequence不可以同时配置为false，即：数据库不能实现自增长，对于oracle和DB2来说，就需要使用序列，请在.config中修改配置。");
                    IsRun = false;
                    return;
                }

                //节点任务记录器
                _dicRunningNodeCaseID = new Dictionary<long, DateTime>();

                lock (_dicRunningNodeCaseID)
                {
                    _dicRunningNodeCaseID = new Dictionary<long, DateTime>();
                }
                _bw = new BackgroundWorker();
                _bw.WorkerSupportsCancellation = true;
                _bw.DoWork += bw_DoWork;
                _bw.RunWorkerAsync();

                #region 并行：停止遗留的(等待+执行中)任务组
                var supCaseList = BLL.EM_SCRIPT_CASE.Instance.GetRunningSuperveneCaseList();
                if (supCaseList != null && supCaseList.Count > 0)
                {
                    foreach (var sc in supCaseList)
                    {
                        BLL.EM_SCRIPT_CASE.Instance.SetStop(sc.ID, Enums.ReturnCode.Success);
                    }
                }
                #endregion

                #region 非并行：停止等待中的任务组
                var noSupCaseList = BLL.EM_SCRIPT_CASE.Instance.GetRunningNoSuperveneCaseList();
                if (noSupCaseList != null && noSupCaseList.Count > 0)
                {
                    foreach (var sc in noSupCaseList)
                    {
                        BLL.EM_SCRIPT_CASE.Instance.SetStop(sc.ID, Enums.ReturnCode.Success);
                    }
                }
                #endregion

                #region 5回复为0
                using (BDBHelper dbop = new BDBHelper())
                {
                    dbop.ExecuteNonQuery(string.Format(@"update FM_MONIT_FILE set COPY_STATUS=0 where COPY_STATUS= 5"));
                }
                #endregion

                #region 重启时删除临时表FM_MONIT_FILE_TEMP_PRO FM_MONIT_FILE_TEMP
                using (BDBHelper dbop = new BDBHelper())
                {
                    dbop.ExecuteNonQuery(string.Format(@"truncate table  FM_MONIT_FILE_TEMP"));
                    dbop.ExecuteNonQuery(string.Format(@"truncate table  FM_MONIT_FILE_TEMP_PRO"));
                }
                #endregion

                //启动手动任务线程
                Task.Hand.Start();

                //启动节点扫描线程
                Task.Scanner.Start();

                BLog.Write(BLog.LogLevel.INFO, "程序已经启动。");
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.FATAL, "程序启动失败。" + ex.ToString());
            }
        }

        /// <summary>
        /// 停止
        /// </summary>
        public static void Stop()
        {
            IsRun = false;

            try
            {
                BLog.Write(BLog.LogLevel.INFO, "程序即将停止。");
                foreach (var kvp in _dicTaskers)
                {
                    kvp.Value.Stop();
                }
                _dicTaskers.Clear();
                _bw.CancelAsync();
                _bw.Dispose();

                //停止手动任务线程
                Task.Hand.Stop();

                //停止节点扫描
                Task.Scanner.Stop();

                BLog.Write(BLog.LogLevel.INFO, "程序已经停止。");
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "程序停止失败。" + ex.ToString());
            }

            Thread.Sleep(1001);
        }

        /// <summary>
        /// 后台线程，定时加载新的任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            while (Main.IsRun)
            {
                DateTime now = DateTime.Now;
                if ((now - _lastLoadTaskFlowTime).TotalSeconds >= RELOAD_RULE_SECONDS)
                {
                    ReLoadTasks();
                    _lastLoadTaskFlowTime = now;
                }

                Thread.Sleep(5000);
            }
        }

        /// <summary>
        /// 获取当前运行中的节点实例数量
        /// </summary>
        public static int RunningNodeCount
        {
            get
            {
                lock (_dicRunningNodeCaseID)
                {
                    return _dicRunningNodeCaseID.Count;
                }
            }
        }

        /// <summary>
        /// 获取节点实例开始执行时间
        /// </summary>
        /// <param name="nodeCaseID">节点实例ID</param>
        /// <returns></returns>
        public static DateTime GetNodeTaskStartTime(long nodeCaseID)
        {
            lock (_dicRunningNodeCaseID)
            {
                if (_dicRunningNodeCaseID.ContainsKey(nodeCaseID) == true)
                {
                    return _dicRunningNodeCaseID[nodeCaseID];
                }
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// 添加一个节点实例任务（用于内存中限定一个节点实例同时只允许一个实例运行）
        /// </summary>
        /// <param name="nodeCaseID">节点实例ID</param>
        /// <returns>如果已经存在这个节点实例ID，则返回为false</returns>
        public static bool AddNodeTask(long nodeCaseID)
        {
            lock (_dicRunningNodeCaseID)
            {
                if (_dicRunningNodeCaseID.ContainsKey(nodeCaseID) == true)
                {
                    return false;
                }

                _dicRunningNodeCaseID.Add(nodeCaseID, DateTime.Now);
            }

            return true;
        }

        /// <summary>
        /// 移除一个节点实例ID（用于内存中限定一个节点实例同时只允许一个实例运行）
        /// </summary>
        /// <param name="nodeCaseID">节点实例ID</param>
        /// <returns>如果不存在这个脚本流ID，则返回为false</returns>
        public static bool RemoveNodeTask(long nodeCaseID)
        {
            lock (_dicRunningNodeCaseID)
            {
                if (_dicRunningNodeCaseID.ContainsKey(nodeCaseID) == true)
                {
                    _dicRunningNodeCaseID.Remove(nodeCaseID);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 重新加载任务列表
        /// </summary>
        private static void ReLoadTasks()
        {
            //BLog.Write(BLog.LogLevel.DEBUG, "即将重新加载脚本流。");
            Dictionary<int, string> dicScripts = BLL.EM_SCRIPT.Instance.GetAllEnables();
            //BLog.Write(BLog.LogLevel.DEBUG, "当前共有【" + dicScripts.Count + "】个脚本流。");
            int addCount = 0;
            int delCount = 0;
            int errCount = 0;
            int updCount = 0;
            //停止已经删除及停用的脚本流
            List<int> delTemp = new List<int>();
            foreach (var kvp in _dicTaskers)
            {
                if (dicScripts.ContainsKey(kvp.Key) == false)
                {
                    kvp.Value.Stop();
                    delTemp.Add(kvp.Key);
                    delCount++;
                    BLog.Write(BLog.LogLevel.DEBUG, "脚本流ID【" + kvp.Key + "】已经停止。");
                }
            }
            //添加新的脚本流，并启动它
            foreach (var kvp in dicScripts)
            {
                if (_dicTaskers.ContainsKey(kvp.Key) == true)
                {
                    //更新时间表达式
                    if (_dicTaskers[kvp.Key].TimeExpression != kvp.Value)
                    {
                        _dicTaskers[kvp.Key].SetTimeExpression(kvp.Value);
                        BLog.Write(BLog.LogLevel.INFO, string.Format("脚本流【{0}】的时间表达式已经更新为【{1}】，1分钟后生效。", kvp.Key, kvp.Value));
                        updCount++;
                    }
                }
                else
                {
                    try
                    {
                        Task.Flow tf = new ScriptService.Task.Flow(kvp.Key, kvp.Value);
                        _dicTaskers.Add(kvp.Key, tf);
                        tf.Start();
                        addCount++;
                    }
                    catch (Exception ex)
                    {
                        //只输出一次错误日志，避免日志太多
                        if (_dicErrorTimeExpression.ContainsKey(kvp.Key) == false)
                        {
                            BLog.Write(BLog.LogLevel.WARN, "脚本流【" + kvp.Key + "】初始化失败：" + ex.Message);
                            _dicErrorTimeExpression.Add(kvp.Key, false);
                        }
                        errCount++;
                    }
                }
            }
            //移除已经删除及停用的脚本流
            if (delTemp.Count > 0)
            {
                foreach (int id in delTemp)
                {
                    _dicTaskers.Remove(id);
                }
            }

            if (delCount > 0 || addCount > 0 || updCount > 0)
            {
                BLog.Write(BLog.LogLevel.INFO, string.Format("已经成功重新加载脚本流，删除了{0}个，成功添加了{1}个，有{2}个添加失败，更新了{3}个任务的时间表达式，当前共有{4}个计划任务，将定时执行。", delCount, addCount, errCount, updCount, _dicTaskers.Count));
            }
        }

    }
}