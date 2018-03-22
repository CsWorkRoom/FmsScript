using Easyman.Librarys.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.ScriptService.Task
{
    /// <summary>
    /// 节点，负责执行脚本任务中当前可以执行的节点，每一个节点开启一个线程
    /// 为了确保数据库连接数可控，通过配置文件限定线程数。
    /// </summary>
    public class Node
    {
        private bool _isAuto = true;
        private long _scriptNodeCaseID;
        private int _maxTryTimes = 0;
        BLL.EM_SCRIPT_NODE_CASE.Entity _nodeCaseEntity;
        private BackgroundWorker _bw;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="nodeCaseID">节点实例ID</param>
        /// <param name="maxTryTimes">出错最大尝试次数</param>
        /// <param name="isAuto">是否自动添加</param>
        public Node(long nodeCaseID, int maxTryTimes, bool isAuto = true)
        {
            _scriptNodeCaseID = nodeCaseID;
            _maxTryTimes = maxTryTimes;
            _isAuto = isAuto;
        }

        /// <summary>
        /// 开始执行任务
        /// </summary>
        /// <param name="isLastNodeCase">是否为程序上次运行添加的节点实例，这种节点可能正处于运行中程序被停止了</param>
        /// <returns></returns>
        public bool Start(bool isLastNodeCase = false)
        {
            try
            {
                //验证运行节点实例数量
                if (Main.RunningNodeCount >= Main.MaxExecuteNodeCount)
                {
                    WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("当前已经有【{0}】个节点实例运行，超过系统设定的最大数【{1}】，本节点实例将暂时不被执行。", Main.RunningNodeCount, Main.MaxExecuteNodeCount));
                    return false;
                }

                //读取当前节点
                _nodeCaseEntity = BLL.EM_SCRIPT_NODE_CASE.Instance.GetEntityByKey<BLL.EM_SCRIPT_NODE_CASE.Entity>(_scriptNodeCaseID);

                if (_nodeCaseEntity == null)
                {
                    WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("没有获取脚本流节点实例ID【{0}】的实体对象，将不被执行。", _scriptNodeCaseID));
                    return false;
                }

                //已经停止的节点，不再执行
                if (_nodeCaseEntity.RUN_STATUS == (short)Enums.RunStatus.Stop)
                {
                    WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】的运行状态为【停止】，本节点将不被执行。", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID));
                    return false;
                }

                //当前状态不等于等待执行
                if (_nodeCaseEntity.RUN_STATUS != (short)Enums.RunStatus.Wait)
                {
                    //上次未完成的节点实例，可以继续执行
                    if (isLastNodeCase == false)
                    {
                        WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】的运行状态不为【等待执行】，本节点将不被执行。", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID));
                        return false;
                    }
                }

                //添加到内存
                if (Main.AddNodeTask(_scriptNodeCaseID) == false)
                {
                    //WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】已经于【{4}】开始运行，本次将不被执行。", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID, Main.GetNodeTaskStartTime(_scriptNodeCaseID).ToString("yyyy-MM-dd HH:mm:ss.fff")));
                    return false;
                }

                //更新当前节点状态
                int i = BLL.EM_SCRIPT_NODE_CASE.Instance.UpdateRunStatus(_scriptNodeCaseID, Enums.RunStatus.Excute);
                if (i < 0)
                {
                    WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("更新脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】的运行状态为【执行中】失败，本节点将不被执行。", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID));
                    return false;
                }

                WriteLog(_scriptNodeCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】的运行状态已经更新为【执行中】，下面将执行节点脚本内容。", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID));

                _bw = new BackgroundWorker();
                _bw.WorkerSupportsCancellation = true;
                _bw.DoWork += DoWork;
                _bw.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("执行节点实例【{0}】出现了未知异常，错误信息为：\r\n{1}", _scriptNodeCaseID, ex.ToString()));
                return false;
            }

            return true;
        }

        /// <summary>
        /// 生成代码、编译及运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoWork(object sender, DoWorkEventArgs e)
        {
            while (Main.IsRun)
            {
                try
                {
                    ErrorInfo err = new ErrorInfo();
                    string code = Script.Transfer.Trans(_nodeCaseEntity, ref err);
                    if (err.IsError == true)
                    {
                        WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】生成脚本代码失败，错误信息为：\r\n{4}", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID, err.Message));
                        //从内存记录中移除
                        Main.RemoveNodeTask(_nodeCaseEntity.ID);
                        return;
                    }
                    
                    //保存源代码到数据库
                    int i = BLL.EM_SCRIPT_NODE_CASE.Instance.UpdateCompileContent(_nodeCaseEntity.ID, code);

                    bool isSuccess = Script.Execute.Run(code, _nodeCaseEntity, ref err);
                    if (isSuccess)
                    {
                        //结束运行状态
                        if (err.IsWarn)
                        {
                            BLL.EM_SCRIPT_NODE_CASE.Instance.SetStop(_nodeCaseEntity.ID, Enums.ReturnCode.Warn.GetHashCode());
                        }
                        else
                        {
                            BLL.EM_SCRIPT_NODE_CASE.Instance.SetStop(_nodeCaseEntity.ID, Enums.ReturnCode.Success.GetHashCode());
                        }                       
                        WriteLog(_scriptNodeCaseID, BLog.LogLevel.INFO, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】已经成功执行。预警状态:{4}", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID, err.IsWarn.ToString()));
                        //从内存记录中移除
                        Main.RemoveNodeTask(_nodeCaseEntity.ID);
                        return;
                    }

                    //记录重试次数
                    int reTryTimes = BLL.EM_SCRIPT_NODE_CASE.Instance.RecordTryTimes(_nodeCaseEntity.ID);

                    //超过最大尝试次数
                    if (reTryTimes >= _maxTryTimes)
                    {
                        WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】作了最后一次尝试，仍然执行失败，本脚本流将不再执行。", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID));

                        BLL.EM_SCRIPT_NODE_CASE.Instance.SetStop(_nodeCaseEntity.ID, Enums.ReturnCode.Fail.GetHashCode());
                        BLL.EM_SCRIPT_CASE.Instance.SetFail(_nodeCaseEntity.SCRIPT_CASE_ID);
                        Main.CurUploadCount--;

                        //从内存记录中移除
                        Main.RemoveNodeTask(_nodeCaseEntity.ID);
                        return;
                    }
                    else
                    {
                        WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】第【{4}】次尝试执行失败，将再次尝试。", _nodeCaseEntity.SCRIPT_ID, _nodeCaseEntity.SCRIPT_CASE_ID, _nodeCaseEntity.SCRIPT_NODE_ID, _nodeCaseEntity.ID, reTryTimes));
                    }
                }
                catch (Exception ex)
                {
                    WriteLog(_scriptNodeCaseID, BLog.LogLevel.WARN, string.Format("执行节点实例【{0}】出现了未知异常，错误信息为：\r\n{1}", _scriptNodeCaseID, ex.ToString()));
                    //从内存记录中移除
                    Main.RemoveNodeTask(_nodeCaseEntity.ID);
                    Main.CurUploadCount--;
                    return;
                }
            }
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="scriptNodeCaseID">脚本节点实例ID</param>
        /// <param name="level">日志等级</param>
        /// <param name="message">日志内容</param>
        /// <param name="sql">SQL语句</param>
        protected static void WriteLog(long scriptNodeCaseID, BLog.LogLevel level, string message, string sql = "")
        {
            //写日志文件
            BLog.Write(level, message);
            try
            {
                //写数据库表
                if (scriptNodeCaseID > 0)
                {
                    BLL.EM_SCRIPT_NODE_CASE_LOG.Instance.Add(scriptNodeCaseID, level.GetHashCode(), message, sql);
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "写日志到脚本节点日志表出错：" + ex.ToString());
            }
        }
    }
}