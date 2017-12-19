using log4net;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Service.Server;
using Easyman.Service.Domain;

namespace Easyman.Quartz.QuartzJobs
{
    public sealed class ScriptQuertzJob : IJob
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(ScriptQuertzJob));
        //执行脚本节点实例
        //（一次只运行一个实例）cron间隔时间可设置为1秒
        public void Execute(IJobExecutionContext context)
        {
            ErrorInfo err = new ErrorInfo();
            string msg = "";
            //获取【等待】的脚本节点实例集合
            var nodeCaseList = ScriptManager.GetWaitNodeCaseList();
            if (nodeCaseList != null && nodeCaseList.Count > 0)
            {
                var nodeCase = nodeCaseList[0];//只执行一条实例
                var scriptCase= ScriptManager.GetScriptCase(nodeCase.SCRIPT_CASE_ID,ref err);
                if (err.IsError)
                {
                    ScriptManager.LogForScriptCase("未找到编号【"+ nodeCase.SCRIPT_CASE_ID .ToString()+ "】脚本流实例："+err.Message, "", nodeCase.SCRIPT_CASE_ID, ref err);
                    return;
                }

                msg = "开始执行脚本节点实例编号【" + nodeCase.ID.ToString() + "】";
                //写日志
                ScriptManager.LogForNodeCase(msg, "", nodeCase.ID, ref err);
                ScriptManager.LogForScriptCase(msg, "", nodeCase.SCRIPT_CASE_ID, ref err);
                //修改脚本节点实例状态为【执行中】
                ScriptManager.ModifyScriptNodeCase(nodeCase.ID, PubEnum.RunStatus.Excute,ref err);
                //修改脚本实例状态为【执行中】
                ScriptManager.ModifyScriptCase(nodeCase.SCRIPT_CASE_ID, PubEnum.RunStatus.Excute, ref err);

                //执行脚本节点实例
                try
                {
                    ExtScript.ExcuteScriptNodeCase(nodeCase, ref err);
                }
                catch (Exception ex)
                {
                    err.IsError = true;
                    err.Message = "执行脚本时现出了未知异常：" + ex.ToString();
                }

                if (err.IsError)
                {
                    msg = "执行脚本节点实例【" + nodeCase.ID.ToString() + "】【失败】：" + err.Message;
                    logger.InfoFormat(msg);
                    err.IsError = false;

                    //修改实例状态为【失败】
                    var nCase= ScriptManager.ModifyScriptNodeCase(nodeCase.ID, PubEnum.RunStatus.Stop, PubEnum.ReturnCode.Fail, ref err);
                    //获取脚本流实例
                    var sc = ScriptManager.GetScriptCase(nCase.SCRIPT_CASE_ID, ref err);
                    //写日志
                    ScriptManager.LogForNodeCase(msg, "", nodeCase.ID, ref err);
                    ScriptManager.LogForScriptCase(msg, "", nodeCase.SCRIPT_CASE_ID, ref err);
                    //再次修改实例状态为【等待】(当前重试数<脚本流设定的重试数)
                    if (nCase.RETRY_TIME <= sc.RETRY_TIME)
                    {
                        msg = "失败数【" + nCase.RETRY_TIME.ToString() + "】≤重试数【" + sc.RETRY_TIME + "】,再次启动脚本节点实例【" + nodeCase.ID.ToString() + "】";
                        ScriptManager.LogForNodeCase(msg, "", nodeCase.ID, ref err);
                        ScriptManager.LogForScriptCase(msg, "", nodeCase.SCRIPT_CASE_ID, ref err);
                        //再次修改实例状态
                        ScriptManager.ModifyScriptNodeCase(nodeCase.ID, PubEnum.RunStatus.Wait, ref err);
                    }
                    else//达到重试次数时，需要在脚本流实例中标注有失败
                    {
                        ScriptManager.ModifyScriptCase(nCase.SCRIPT_CASE_ID, PubEnum.RunStatus.Excute, ref err,PubEnum.IsHaveFail.HaveFail);
                        string log = string.Format(@"很遗憾，脚本流【{0}】实例【{1}】执行失败！引起失败的节点实例【{2}】。",
                            scriptCase.NAME, scriptCase.ID.ToString(), nodeCase.ID.ToString());
                        ScriptManager.LogForNodeCase(log, "", nodeCase.ID, ref err);
                        ScriptManager.LogForScriptCase(log, "", nodeCase.SCRIPT_CASE_ID, ref err);
                    }
                }
                else
                {
                    msg = "执行脚本节点实例【" + nodeCase.ID.ToString() + "】【成功】";
                    logger.InfoFormat(msg);
                    //写日志
                    ScriptManager.LogForNodeCase(msg, "", nodeCase.ID, ref err);
                    ScriptManager.LogForScriptCase(msg, "", nodeCase.SCRIPT_CASE_ID, ref err);
                    err.IsError = false;
                    //修改当前实例状态
                    ScriptManager.ModifyScriptNodeCase(nodeCase.ID, PubEnum.RunStatus.Stop, PubEnum.ReturnCode.Success, ref err);
                    //添加下一组节点实例
                    ScriptManager.AddNextScritNodeCase(nodeCase.SCRIPT_NODE_ID, nodeCase.SCRIPT_CASE_ID, nodeCase.ID, ref err);
                }
            }
        }
    }
}
