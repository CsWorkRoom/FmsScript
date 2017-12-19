using log4net;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Easyman.Service.Server;
using Easyman.Service.Domain;

namespace Easyman.Quartz.QuartzJobs
{
    public sealed class AntoJob : IJob
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(AntoJob));
        //为每个脚本流添加任务和触发器
        public void Execute(IJobExecutionContext context)
        {
            //遍历所有的任务流，为其添加job和trigger，已删除的记录不再添加

            //扫描script列表，添加未添加的job和trrigger
            var allScriptList= ScriptManager.GetAllScriptList();
            var openSList= allScriptList.Where(p => p.STATUS == (short)PubEnum.ScriptStatus.Open 
            && p.IS_DELETE == (short)PubEnum.IsDelete.No).ToList();

            #region 添加脚本任务
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            IScheduler scheduler = schedulerFactory.GetScheduler();

            string jobGroupName = "ScriptJobGroup";
            string triGroupName = "ScriptTriGroup";
            string jobNamePex = "ScriptJob_";
            string triNamePex = "ScriptTri_";

            //所有需要运行的脚本
            var allScript = openSList; ;
            var triKeyArr = scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals("ScriptTriGroup"));
            //删除触发器，删除这个触发器没有在运行的脚本里
            foreach (var t in triKeyArr)
            {
                
                var trigger = scheduler.GetTrigger(t);
                IJobDetail job = scheduler.GetJobDetail(trigger.JobKey);
                var tmp = allScript.SingleOrDefault(x => t.Name == triNamePex + x.ID.ToString());
                if (tmp == null)
                {
                    //结束脚本流的实例
                    //StopTask(Convert.ToInt32(t.Name.Replace(triNamePex, "")));
                    scheduler.DeleteJob(trigger.JobKey);
                    logger.InfoFormat("移除脚本流job和trrigger编号【{0}】",t.Name);
                }
            }

            foreach (var t in allScript)
            {
                try
                {
                    //新任务
                    if (triKeyArr.SingleOrDefault(x => x.Name == triNamePex + t.ID.ToString()) == null)
                    {

                        if (VilidateCron(t.CRON))
                        {
                            IJobDetail job = JobBuilder.Create<ManageCaseJob>()
                                            .WithIdentity(new JobKey(jobNamePex + t.ID.ToString(), jobGroupName))
                                            .StoreDurably()
                                            .Build();

                            ICronTrigger trigger = (ICronTrigger)TriggerBuilder.Create()
                                                .WithIdentity(new TriggerKey(triNamePex + t.ID.ToString(), triGroupName))
                                                .ForJob(job)
                                                .StartNow().WithCronSchedule(t.CRON)
                                                .Build();
                            logger.InfoFormat("添加脚本流【{0},{1}】job{2}和trrigger编号{3}", t.ID, t.NAME, job.Key.Name, trigger.Key.Name);
                            scheduler.ScheduleJob(job, trigger);
                        }
                        else
                        {
                            logger.InfoFormat("添加脚本流【{0},{1}】失败：时间表达式【{2}】不正确！",t.ID, t.NAME, t.CRON);
                        }
                    }
                    else
                    {

                        ICronTrigger trigger = (ICronTrigger)scheduler.GetTrigger(new TriggerKey(triNamePex + t.ID.ToString(), triGroupName));
                        IJobDetail job = scheduler.GetJobDetail(trigger.JobKey);
                        if (trigger.CronExpressionString != t.CRON)
                        {
                            if (VilidateCron(t.CRON))
                            {
                                logger.InfoFormat("修改脚本流【{0},{1}】job和trrigger编号【{2}】的时间表达式【{3}】为【{4}】",t.ID, t.NAME, trigger.Key.Name, trigger.CronExpressionString, t.CRON);
                                trigger.CronExpressionString = t.CRON;
                                scheduler.DeleteJob(trigger.JobKey);
                                scheduler.ScheduleJob(job, trigger);
                            }
                            else
                            {
                                logger.InfoFormat("修改脚本流【{0},{1}】失败：时间表达式【{2}】不正确！",t.ID, t.NAME, t.CRON);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.InfoFormat("添加或修改脚本流【{0},{1}】job和trrigger【失败】：{2}",t.ID, t.NAME, e.Message);
                }

            }
            #endregion
        }

        /// <summary>
        /// 验证cron时间表达式正确性
        /// </summary>
        /// <param name="cron"></param>
        /// <returns></returns>
        public bool VilidateCron(string cron)
        {
            try
            {
                CronExpression exp = new CronExpression(cron);
                DateTimeOffset time = new DateTimeOffset();

                int i = 0;
                // 循环得到接下来n此的触发时间点，供验证  
                while (i < 10)
                {
                    var t = exp.GetNextValidTimeAfter(time);
                    if (t == null)
                    {
                        return false;
                    }
                    ++i;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
