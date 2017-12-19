using Easyman.Librarys.Cron;
using Easyman.Librarys.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.TestForm
{
    public class CronTest : BCron
    {
        public CronTest(int id, string expression) : base(id, expression)
        {
            BLog.Write(BLog.LogLevel.INFO, string.Format("任务【{0}】已经初始化。", id));
        }

        protected override bool Execute(DateTime runTime)
        {
            if (FormMain.IsRun == false)
            {
                BLog.Write(BLog.LogLevel.INFO, string.Format("主程序已经停止，任务【{0}】将不会继续执行任务。", ID));
                return false;
            }

            BLog.Write(BLog.LogLevel.DEBUG, string.Format("任务【{0}】执行中……。", ID));
            return true;
        }
    }
}
