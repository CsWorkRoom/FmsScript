using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace Easyman.Quartz
{
    static class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "log4net.config"));

            HostFactory.Run(x =>
            {
                x.UseLog4Net();

                x.Service<ServiceRunner>();

                #region 设置脚本服务的名称及相关信息
                string description = ConfigurationSettings.AppSettings["Description"];
                string displayName = ConfigurationSettings.AppSettings["DisplayName"];
                string serviceName = ConfigurationSettings.AppSettings["ServiceName"];

                x.SetDescription(description);
                x.SetDisplayName(displayName);
                x.SetServiceName(serviceName);
                #endregion

                x.EnablePauseAndContinue();
            });

            #region 调试
            //Easyman.Quartz.QuartzJobs.ManageCaseJob job = new QuartzJobs.ManageCaseJob();
            //job.Execute(null);

            //Easyman.Quartz.QuartzJobs.ScriptQuertzJob job2 = new QuartzJobs.ScriptQuertzJob();
            //job2.Execute(null);
            #endregion
        }
    }
}
