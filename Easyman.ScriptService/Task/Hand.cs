using Easyman.Librarys.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Easyman.ScriptService.Task
{
    /// <summary>
    /// 手动任务，定时扫描手动任务表，当有任务需要执行时：
    ///     1）流任务：调用Flow类中的方法，创建流实例
    ///     2）手动任务任务：调用Node类中的方法，执行手动任务
    /// </summary>
    public static class Hand
    {
        /// <summary>
        /// 重新加载手工任务的时间间隔（秒）
        /// </summary>
        public const int RELOAD_JOB_SECONDS = 10;

        /// <summary>
        /// 后台线程，不断扫描需要执行的手动任务实例
        /// </summary>
        private static BackgroundWorker _bw;

        /// <summary>
        /// 开始启动
        /// </summary>
        /// <returns></returns>
        public static void Start()
        {
            try
            {
                BLog.Write(BLog.LogLevel.INFO, "手动任务扫描线程即将启动。");
                _bw = new BackgroundWorker();
                _bw.WorkerSupportsCancellation = true;
                _bw.DoWork += DoWork;
                _bw.RunWorkerAsync();
                BLog.Write(BLog.LogLevel.INFO, "手动任务扫描线程已经启动。");
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "手动任务扫描线程启动失败。" + ex.ToString());
            }
        }

        /// <summary>
        /// 停止
        /// </summary>
        public static void Stop()
        {
            try
            {
                BLog.Write(BLog.LogLevel.INFO, "手动任务扫描线程即将停止。");
                _bw.CancelAsync();
                _bw.Dispose();
                BLog.Write(BLog.LogLevel.INFO, "手动任务扫描线程已经停止。");
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "手动任务扫描线程停止失败。" + ex.ToString());
            }
        }

        /// <summary>
        /// 定时扫描手动任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DoWork(object sender, DoWorkEventArgs e)
        {
            while (Main.IsRun)
            {
                try
                {
                    IList<BLL.EM_HAND_RECORD.Entity> jobList = BLL.EM_HAND_RECORD.Instance.GetNeedRunList();

                    if (jobList != null && jobList.Count > 0)
                    {
                        BLog.Write(BLog.LogLevel.DEBUG, "扫描到" + jobList.Count + "个手动任务需要执行");

                        foreach (var jobEntity in jobList)
                        {
                            ErrorInfo err = new ErrorInfo();
                            if (jobEntity.HAND_TYPE == Enums.HandType.Script.GetHashCode())
                            {
                                RunScriptJob(jobEntity.ID, jobEntity.OBJECT_ID, ref err);
                            }
                            else if (jobEntity.HAND_TYPE == Enums.HandType.ScriptNode.GetHashCode())
                            {
                                RunNodeJob(jobEntity.ID, jobEntity.OBJECT_ID, ref err);
                            }

                            //执行结果
                            if (err.IsError)
                            {
                                WriteLog(0, BLog.LogLevel.WARN, "执行手动任务失败。" + err.Message);
                            }
                            else
                            {
                                WriteLog(0, BLog.LogLevel.INFO, "执行手动任务成功：" + err.Message);
                            }
                        }
                    }
                    else
                    {
                        //WriteLog(0, BLog.LogLevel.DEBUG, "当前没有需要执行的手动任务。");
                    }
                }
                catch (Exception ex)
                {
                    WriteLog(0, BLog.LogLevel.WARN, "扫描手动任务列表失败。" + ex.ToString());
                }

                Thread.Sleep(RELOAD_JOB_SECONDS * 1000);
            }
        }

        /// <summary>
        /// 运行脚本任务
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <param name="scriptID">脚本流ID</param>
        /// <param name="err">错误信息</param>
        /// <returns></returns>
        private static bool RunScriptJob(long id, long scriptID, ref ErrorInfo err)
        {
            long scriptCaseID = 0;
            try
            {
                //先尝试取之前未完成的节点
                BLL.EM_SCRIPT_CASE.Entity scriptCaseEntity = BLL.EM_SCRIPT_CASE.Instance.GetRunningCase(scriptID);
                if (scriptCaseEntity != null)
                {
                    string msg = string.Format("脚本流【{0}】找到了之前未运行完成的实例【{1}】，本次手动任务将不会执行。", scriptID, scriptCaseEntity.ID);
                    WriteLog(scriptCaseEntity.ID, BLog.LogLevel.DEBUG, msg);
                    err.Message = msg;

                    //不用创建实例，不用执行
                    BLL.EM_HAND_RECORD.Instance.SetCancel(id, scriptCaseEntity.ID);
                    return false;
                }
                else
                {
                    if (Flow.CreateScriptCase(scriptID, ref scriptCaseID, ref err) == true)
                    {
                        //记录执行的任务ID
                        BLL.EM_HAND_RECORD.Instance.SetCaseID(id, scriptCaseID);
                        WriteLog(scriptCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】成功创建了新的实例【{1}】，等待执行。", scriptID, scriptCaseID));
                    }
                    else
                    {
                        //不能创建实例，取消执行
                        BLL.EM_HAND_RECORD.Instance.SetCancel(id, 0);
                        //标记为失败状态
                        if (scriptCaseID > 0)
                        {
                            BLL.EM_SCRIPT_CASE.Instance.SetFail(scriptCaseID);
                        }
                        WriteLog(scriptCaseID, BLog.LogLevel.WARN, err.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(scriptCaseID, BLog.LogLevel.ERROR, "创建脚本流实例发生了未知错误。" + ex.ToString());
                //不能创建实例，取消执行
                BLL.EM_HAND_RECORD.Instance.SetCancel(id, 0);
                //标记为失败状态
                if (scriptCaseID > 0)
                {
                    BLL.EM_SCRIPT_CASE.Instance.SetFail(scriptCaseID);
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// 运行节点实例（不支持单独新建节点实例，只支持对失败节点实例再启动）
        /// </summary>
        /// <param name="scriptNodeID">节点ID</param>
        /// <param name="err">错误信息</param>
        private static bool RunNodeJob(long id, long scriptNodeID, ref ErrorInfo err, long? nodeCaseId = null)
        {
            int i = 0;
            try
            {
                BLL.EM_SCRIPT_NODE_CASE.Entity nodeCaseEntity = null;
                if (nodeCaseId == null)
                {
                    nodeCaseEntity = BLL.EM_SCRIPT_NODE_CASE.Instance.GetFailCase(scriptNodeID);
                }
                else
                {
                    nodeCaseEntity = BLL.EM_SCRIPT_NODE_CASE.Instance.GetNodeCase(nodeCaseId.Value);
                }
                //BLL.EM_SCRIPT_NODE_CASE.Entity nodeCaseEntity = BLL.EM_SCRIPT_NODE_CASE.Instance.GetNodeCase(scriptNodeID);
                if (nodeCaseEntity == null)
                {
                    err.IsError = true;
                    err.Message = string.Format("执行节点【{0}】的手动任务，当前没有该节点的失败节点(或未查找到实例对象)可以执行，因此本次任务已经取消。", scriptNodeID);
                    //设置为“取消执行”状态，无具体实例对应
                    i = BLL.EM_HAND_RECORD.Instance.SetCancel(id, 0, err.Message);

                    return false;
                }

                //记录执行的任务ID(将处理后的手工改为已处理状态)
                i = BLL.EM_HAND_RECORD.Instance.SetCaseID(id, nodeCaseEntity.ID);

                Node node = new Node(nodeCaseEntity.ID, 1);
                node.Start();
                BLog.Write(BLog.LogLevel.INFO, string.Format("节点【{0}】的手动任务的实例【{1}】已经被执行。", scriptNodeID, nodeCaseEntity.ID));
            }
            catch (Exception ex)
            {
                err.IsError = true;
                err.Message = string.Format("执行节点【{0}】的手动任务【{1}】失败。{2}", scriptNodeID, id, ex.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="scriptCaseID">脚本流实例ID</param>
        /// <param name="level">日志等级</param>
        /// <param name="message">日志内容</param>
        /// <param name="sql">SQL语句</param>
        private static void WriteLog(long scriptCaseID, BLog.LogLevel level, string message, string sql = "")
        {
            //写日志文件
            BLog.Write(level, message);
            try
            {
                //写数据库表
                if (scriptCaseID > 0)
                {
                    BLL.EM_SCRIPT_CASE_LOG.Instance.Add(scriptCaseID, level.GetHashCode(), message, sql);
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "写手动任务日志到脚本流日志表出错：" + ex.ToString());
            }
        }
    }
}
