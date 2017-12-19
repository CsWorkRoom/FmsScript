using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Easyman.Librarys.Cron
{
    /// <summary>
    /// 计划任务基类，继承自时间表达式，可以简便地获取自己下一个执行的时间点
    /// </summary>
    public abstract class BCron : BTimeExpression
    {
        private BackgroundWorker _bw;
        private int _id;
        private bool _isRun = true;
        /// <summary>
        /// 计划任务ID
        /// </summary>
        public int ID { get { return _id; } }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <param name="expression">时间表达式</param>
        public BCron(int id, string expression)
        {
            _id = id;
            bool isSuccess = false;
            try
            {
                isSuccess = SetTimeExpression(expression);
            }
            catch
            {
                isSuccess = false;
            }

            if (isSuccess == false)
            {
                throw new Exception("时间表达式【" + expression + "】错误！");
            }
        }

        /// <summary>
        /// 开始执行
        /// </summary>
        public void Start()
        {
            _isRun = true;
            _bw = new BackgroundWorker();
            _bw.WorkerSupportsCancellation = true;
            _bw.DoWork += DoWork;
            _bw.RunWorkerAsync();
        }

        /// <summary>
        /// 停止执行
        /// </summary>
        public void Stop()
        {
            try
            {
                _isRun = false;
                _bw.CancelAsync();
                _bw.Dispose();
            }
            catch (Exception ex)
            {
                throw new Exception("停止计划任务出错。" + ex.ToString());
            }
        }

        /// <summary>
        /// 运行任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoWork(object sender, DoWorkEventArgs e)
        {
            DateTime runTime = DateTime.MaxValue;
            TimeSpan ts;

            while (_isRun == true)
            {
                runTime = GetNextRunTime();
                ts = runTime - DateTime.Now;
                //更多每分钟刷新一下，因为期间可能有任务更新了时间表达式
                if (ts.TotalMinutes > 1)
                {
                    Thread.Sleep(1 * 60 * 1000);
                    continue;
                }
                else if (ts.TotalSeconds > 0)
                {
                    Thread.Sleep((int)ts.TotalMilliseconds);
                }
                //执行计划任务
                Execute(runTime);
                //执行完休息一秒
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// 执行任务，由派生类来重写，到达时间点之后，执行具体任务（本方法由基类自动调用，不需要在外部来调用）
        /// </summary>
        /// <param name="runTime">时间时间点</param>
        /// <returns></returns>
        protected abstract bool Execute(DateTime runTime);
    }
}
