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
    /// 具体任务扫描，两项工作：
    ///     1）定时扫描未完成的脚本流实例，将当前可执行节点写到执行任务实例表
    ///     2）如果脚本流实例所有节点都执行完成，则更新该脚本流实例的结果和状态
    /// </summary>
    public static class Scanner
    {
        /// <summary>
        /// 后台线程，不断扫描需要执行的节点实例
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
                BLog.Write(BLog.LogLevel.INFO, "节点扫描线程即将启动。");
                _bw = new BackgroundWorker();
                _bw.WorkerSupportsCancellation = true;
                _bw.DoWork += DoWork;
                _bw.RunWorkerAsync();
                BLog.Write(BLog.LogLevel.INFO, "节点扫描线程已经启动。");
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "节点扫描线程启动失败。" + ex.ToString());
            }
        }

        /// <summary>
        /// 停止
        /// </summary>
        public static void Stop()
        {
            try
            {
                BLog.Write(BLog.LogLevel.INFO, "节点扫描线程即将停止。");
                _bw.CancelAsync();
                _bw.Dispose();
                BLog.Write(BLog.LogLevel.INFO, "节点扫描线程已经停止。");
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "节点扫描线程停止失败。" + ex.ToString());
            }
        }

        /// <summary>
        /// 不断扫描脚本实例表，对于需要运行的实例，为其添加节点实例并运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DoWork(object sender, DoWorkEventArgs e)
        {
            while (Main.IsRun)
            {
                long scriptCaseID = 0;
                try
                {
                    IList<BLL.EM_SCRIPT_CASE.Entity> runningCaseList = BLL.EM_SCRIPT_CASE.Instance.GetRunningCaseList();
                    if (runningCaseList != null && runningCaseList.Count > 0)
                    {
                        foreach (var scriptCase in runningCaseList)
                        {
                            scriptCaseID = scriptCase.ID;
                            ErrorInfo err = new ErrorInfo();
                            bool isSuccess = AddAndRunNode(scriptCase.SCRIPT_ID, scriptCaseID, scriptCase.RETRY_TIME, ref err);
                            if (isSuccess == false)
                            {
                                WriteLog(scriptCaseID, BLog.LogLevel.WARN, string.Format("添加脚本实例【{0}】的节点并启动节点实例失败，错误信息为：{1}", scriptCaseID, err.Message));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLog(scriptCaseID, BLog.LogLevel.ERROR, "扫描并添加待执行节点出现未知错误，错误信息为：" + ex.ToString());
                }

                //Thread.Sleep(3000);
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// 添加并运行节点实例
        /// </summary>
        /// <param name="scriptID">脚本ID</param>
        /// <param name="scriptCaseID">脚本实例ID</param>
        /// <param name="maxTryTimes">出错最大尝试次数</param>
        /// <param name="err">错误信息</param>
        /// <returns></returns>
        private static bool AddAndRunNode(long scriptID, long scriptCaseID, int maxTryTimes, ref ErrorInfo err)
        {
            //已添加的节点实例
            Dictionary<long, BLL.EM_SCRIPT_NODE_CASE.Entity> nodeCaseList = BLL.EM_SCRIPT_NODE_CASE.Instance.GetDictionaryByScriptCaseID(scriptCaseID);
            //已添加的节点ID
            Dictionary<long, bool> dicExecutedNodeID = new Dictionary<long, bool>();
            //脚本流运行结果
            Enums.ReturnCode returnCode = Enums.ReturnCode.Success;
            //脚本流实例是否已经完成
            bool isComplete = true;
            //运行中的节点数
            int runningCount = 0;
            //启动之前未完成的节点
            foreach (var kvp in nodeCaseList)
            {
                if (kvp.Value.RUN_STATUS != (short)Enums.RunStatus.Stop)
                {
                    isComplete = false;
                    Node node = new Node(kvp.Value.ID, maxTryTimes);
                    bool isStarted = node.Start(true);
                    if (isStarted == true)
                    {
                        WriteLog(scriptCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】之前未完成的实例【{3}】已经重新启动。", scriptID, scriptCaseID, kvp.Value.SCRIPT_NODE_ID, kvp.Value.ID));
                        runningCount++;
                    }
                }
                else if (kvp.Value.RUN_STATUS == (short)Enums.RunStatus.Stop)
                {
                    //已经结束的节点，记录状态，只要有一个失败则为失败
                    if (kvp.Value.RETURN_CODE == (short)Enums.ReturnCode.Fail)
                    {
                        returnCode = Enums.ReturnCode.Fail;
                    }
                    else if (kvp.Value.RETURN_CODE == (short)Enums.ReturnCode.Warn)
                    {
                        returnCode = Enums.ReturnCode.Warn;
                    }
                }

                //记录已经成功完成的节点
                if (dicExecutedNodeID.ContainsKey(kvp.Value.SCRIPT_NODE_ID) == false)
                {
                    dicExecutedNodeID.Add(kvp.Value.SCRIPT_NODE_ID, (kvp.Value.RUN_STATUS == (short)Enums.RunStatus.Stop) && (kvp.Value.RETURN_CODE == (short)Enums.ReturnCode.Success));
                }
            }

            if (runningCount > 0)
            {
                WriteLog(scriptCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】的实例【{1}】启动了【{2}】个之前未完成的节点实例。", scriptID, scriptCaseID, runningCount));
            }

            //所有节点及依赖关系
            Dictionary<long, List<long>> dicAllNodes = BLL.EM_SCRIPT_REF_NODE_FORCASE.Instance.GetNodeAndParents(scriptCaseID);

            int addCount = 0;
            //没有失败节点的情况下，添加新节点
            if (returnCode != Enums.ReturnCode.Fail)
            {
                foreach (var kvp in dicAllNodes)
                {
                    if (dicExecutedNodeID.ContainsKey(kvp.Key) == false)
                    {
                        //无父节点或所有父节点均已经执行完成
                        bool isNeedAdd = kvp.Value.Count < 1;
                        if (kvp.Value.Count > 0)
                        {
                            isNeedAdd = true;
                            foreach (long pNodeID in kvp.Value)
                            {
                                if (dicExecutedNodeID.ContainsKey(pNodeID) == false)
                                {
                                    isNeedAdd = false;
                                    break;
                                }
                                if (dicExecutedNodeID[pNodeID] == false)
                                {
                                    isNeedAdd = false;
                                    break;
                                }
                            }
                        }

                        //需要添加节点
                        if (isNeedAdd == true)
                        {
                            isComplete = false;
                            long nodeCaseID = BLL.EM_SCRIPT_NODE_CASE.Instance.AddReturnCaseID(scriptID, scriptCaseID, kvp.Key);
                            if (nodeCaseID > 0)
                            {
                                WriteLog(scriptCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】成功添加节点实例【{3}】。", scriptID, scriptCaseID, kvp.Key, nodeCaseID));
                                //启动节点
                                Node node = new Node(nodeCaseID, maxTryTimes);

                                bool isStarted = node.Start();
                                if (isStarted == true)
                                {
                                    addCount++;
                                }
                            }
                            else
                            {
                                err.IsError = true;
                                err.Message = string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】添加节点实例【{3}】失败。", scriptID, scriptCaseID, kvp.Key, nodeCaseID);
                                return false;
                            }
                        }
                    }
                }
                if (addCount > 0)
                {
                    WriteLog(scriptCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】的实例【{1}】添加了【{2}】个新的节点实例准备执行。", scriptID, scriptCaseID, addCount));
                }
            }

            //完成或者有失败节点时停止脚本流
            if (isComplete || returnCode == Enums.ReturnCode.Fail)
            {
                var sc = BLL.EM_SCRIPT_CASE.Instance.GetCase(scriptCaseID);
                BLL.EM_SCRIPT_CASE.Instance.SetStop(scriptCaseID, returnCode);
                if (sc != null && sc.IS_SUPERVENE.Value==1)
                {
                    lock (dicAllNodes)
                    {
                        if (Main.CurUploadCount > 0)
                            Main.CurUploadCount--;
                        WriteLog(0, BLog.LogLevel.DEBUG, string.Format("完成一个并行任务，删除后当前MaxUploadCount{0},CurUploadCount{1}。", Main.MaxUploadCount.ToString(), Main.CurUploadCount));
                    }
                }
                else
                {
                    lock (dicAllNodes)
                    {
                        if (Main.CurMonitCount > 0)
                            Main.CurMonitCount--;
                        WriteLog(0, BLog.LogLevel.DEBUG, string.Format("完成一个监控任务，删除后当前CurMonitCount{0}", Main.CurMonitCount.ToString()));
                    }
                }
               
                WriteLog(scriptCaseID, BLog.LogLevel.DEBUG, string.Format("脚本流【{0}】的实例【{1}】所有节点已经执行完成，执行结果【{2}】。", scriptID, scriptCaseID, returnCode.ToString()));
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
                BLog.Write(BLog.LogLevel.ERROR, "写日志到脚本流日志表出错：" + ex.ToString());
            }
        }
    }
}
