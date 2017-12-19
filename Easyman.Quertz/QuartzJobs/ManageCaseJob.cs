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
    public sealed class ManageCaseJob : IJob
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(ManageCaseJob));
        //启动脚本流实例
        public void Execute(IJobExecutionContext context)
        {
            logger.DebugFormat("将执行脚本流【" + context.JobDetail.Key.Name + "】的实例");
            var scriptId = Convert.ToInt32(context.JobDetail.Key.Name.Replace("ScriptJob_", ""));
            //var scriptId = 61;
            ErrorInfo err = new ErrorInfo();
            var script = ScriptManager.GetScripByID(scriptId, ref err);
            if (err.IsError)
            {
                logger.InfoFormat("未找到编号为【" + scriptId + "】的脚本流");
                return;
            }

            //根据脚本ID获取当前运行中脚本实例
            var runScript = ScriptManager.GetEffectScriptCase(scriptId);
            //没有运行中的脚本实例，则新建实例
            if (runScript == null)
            {
                logger.InfoFormat("将为脚本流【" + script.NAME + "】创建实例");
                var scriptCase = ScriptManager.StartScriptCase(scriptId, PubEnum.StatusModel.Anto, ref err);
                if (err.IsError)
                {
                    logger.InfoFormat("脚本流【" + script.NAME + "】实例创建【失败】：" + err.Message);
                }
                else
                {
                    logger.InfoFormat("脚本流【" + script.NAME + "】实例编号【" + scriptCase.ID + "】创建【成功】");
                }
            }
            else
            {
                logger.InfoFormat("脚本流【" + script.NAME + "】已经有运行中的实例，不用创建新实例");
            }
        }
    }
}
