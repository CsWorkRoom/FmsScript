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
    public sealed class DealHandJob : IJob
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(DealHandJob));
        //处理手工加入的脚本和节点
        //被处理过的手工记录均被标记为Cancle(不能成功与否)
        public void Execute(IJobExecutionContext context)
        {
            ErrorInfo err=new ErrorInfo();
            string msg = "";
            var allHandList= ScriptManager.GetAllHandList();
            if (allHandList != null && allHandList.Count > 0)
            {
                for (int i = 0; i < allHandList.Count; i++)
                {
                    var hand = allHandList[i];
                    
                    #region 脚本流
                    if (hand.HAND_TYPE == (short)PubEnum.HandType.Script)
                    {
                        var runScriptCase = ScriptManager.GetEffectScriptCase(hand.OBJECT_ID);
                        //添加脚本流实例
                        if (runScriptCase == null)
                        {
                            var scriptCase = ScriptManager.StartScriptCase(hand.OBJECT_ID, PubEnum.StatusModel.Hand, ref err);
                            if (err.IsError)
                            {
                                logger.InfoFormat("脚本流【" + scriptCase.NAME + "】实例编号【" + scriptCase.ID + "】添加【失败】");
                                return;
                            }
                            //启动EM_HAND_RECORD中记录
                            msg = "已成功启动脚本流实例。";
                            ScriptManager.ModifyHandRecord(hand.ID, scriptCase.ID, PubEnum.IsCancel.Cancel, ref err);
                            if (err.IsError)
                            {
                                logger.InfoFormat("记录【" + hand.ID + "】启动修改【失败】：" + err.Message);
                                return;
                            }

                        }
                        else
                        {
                            //作废EM_HAND_RECORD中记录
                            ScriptManager.ModifyHandRecord(hand.ID, PubEnum.IsCancel.Cancel, "脚本【" + runScriptCase.NAME + "】已存在运行中实例【" + runScriptCase.ID + "】", ref err);
                            if (err.IsError)
                            {
                                logger.InfoFormat("记录【" + hand.ID + "】作废修改【失败】：" + err.Message);
                            }
                        }
                    }
                    #endregion

                    #region 脚本流节点实例
                    else if (hand.HAND_TYPE == (short)PubEnum.HandType.ScriptNode)
                    { 
                        //不支持单独新建节点实例，只支持对失败节点实例再启动
                        var nodeCase= ScriptManager.GetScriptNodeCase(hand.OBJECT_CASE_ID, ref err);
                        if (err.IsError)
                        {
                            logger.InfoFormat("未找到脚本节点实例【{0}】：{1}",hand.OBJECT_CASE_ID, err.Message);
                            return;
                            //logger.InfoFormat("脚本节点实例【{0}】编号【{1}】启动【失败】：{2}", err.Message); 
                        }
                        if (nodeCase.RUN_STATUS == (short)PubEnum.RunStatus.Wait || nodeCase.RUN_STATUS == (short)PubEnum.RunStatus.Excute)
                        {
                            msg="无需重复启动，脚本节点实例编号【"+hand.OBJECT_CASE_ID+"】已被启动";
                            logger.InfoFormat(msg);
                            ScriptManager.ModifyHandRecord(hand.ID, PubEnum.IsCancel.Cancel, msg, ref err);
                            if (err.IsError)
                            {
                                logger.InfoFormat("记录【" + hand.ID + "】修改为作废【失败】：" + err.Message);
                                return;
                            }
                        }

                        if (nodeCase.RETURN_CODE == (short)PubEnum.ReturnCode.Fail)
                        {
                            //修改节点实例状态
                            ScriptManager.ModifyScriptNodeCase(nodeCase.ID, PubEnum.RunStatus.Wait, ref err);
                            if (err.IsError)
                            {
                                msg = string.Format(@"脚本节点实例编号【{0}】启动【失败】", hand.OBJECT_CASE_ID);
                                logger.InfoFormat(msg);
                                //写日志
                                ScriptManager.LogForNodeCase(msg, "", nodeCase.ID, ref err);
                                ScriptManager.LogForScriptCase(msg, "", nodeCase.SCRIPT_CASE_ID, ref err);
                            }
                            else
                            {
                                msg = string.Format(@"脚本节点实例编号【{0}】启动【成功】", hand.OBJECT_CASE_ID);
                                logger.InfoFormat(msg);
                                //写日志
                                ScriptManager.LogForNodeCase(msg, "", nodeCase.ID, ref err);
                                ScriptManager.LogForScriptCase(msg, "", nodeCase.SCRIPT_CASE_ID, ref err);
                                //修改EM_HAND_RECORD
                                msg = "已成功启动节点实例。";
                                ScriptManager.ModifyHandRecord(hand.ID, hand.OBJECT_CASE_ID, PubEnum.IsCancel.Cancel, ref err, msg);
                                if (err.IsError)
                                {
                                    logger.InfoFormat("记录【" + hand.ID + "】修改为启动【失败】", hand.OBJECT_CASE_ID);
                                    return;
                                }
                            }
                        }
                        else
                        {
                            logger.InfoFormat("脚本节点实例编号【{0}】启动【失败】", hand.OBJECT_CASE_ID);
                            logger.InfoFormat("原由：被启动脚本节点实例编号【{0}】应为失败状态", hand.OBJECT_CASE_ID);
                            return;
                        }
                    }
                    #endregion

                }
            }
        }
    }
}
