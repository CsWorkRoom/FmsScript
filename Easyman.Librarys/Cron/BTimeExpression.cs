using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.Librarys.Cron
{
    /// <summary>
    /// 计划任务之时间表达式
    /// </summary>
    public class BTimeExpression
    {
        /// <summary>
        /// 时间表达式
        /// </summary>
        private string _timeExpression = string.Empty;
        /// <summary>
        /// 上次运行时间
        /// </summary>
        private DateTime _lastRuntime = DateTime.Now;
        /// <summary>
        /// 是否运行中
        /// </summary>
        private bool _isRun = false;
        /// <summary>
        /// 是否运行中
        /// </summary>
        public bool IsRun { get { return _isRun; } }
        /// <summary>
        /// 时间表达式
        /// </summary>
        public string TimeExpression { get { return _timeExpression; } }

        //时间表达式分解
        private List<int> _years = new List<int>();
        private List<int> _months = new List<int>();
        private List<int> _week = new List<int>();      //输入为1-7 或者 SUN-SAT，C#中要+1操作
        private List<int> _days = new List<int>();
        private List<int> _hours = new List<int>();
        private List<int> _minutes = new List<int>();
        private List<int> _seconds = new List<int>();
        private bool _isUseWeekDay = false;
        private DateTime _now = DateTime.Now;
        private Dictionary<string, int> _dicWeekDay = new Dictionary<string, int>();
        /// <summary>
        /// 避免线程冲突，加锁
        /// </summary>
        private object _lockObj = new object();

        /// <summary>
        /// 设置时间表达式
        /// </summary>
        /// <param name="timeExpression">时间表达式</param>
        /// <returns>设置成功返回true</returns>
        public bool SetTimeExpression(string timeExpression)
        {
            lock (_lockObj)
            {
                _timeExpression = timeExpression;
                string[] expressions = _timeExpression.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (expressions.Length < 6)
                {
                    return false;
                }

                _dicWeekDay = new Dictionary<string, int>();
                _dicWeekDay.Add("SUN", 1);
                _dicWeekDay.Add("MON", 2);
                _dicWeekDay.Add("TUE", 3);
                _dicWeekDay.Add("WED", 4);
                _dicWeekDay.Add("THU", 5);
                _dicWeekDay.Add("FRI", 6);
                _dicWeekDay.Add("SAT", 7);

                _now = DateTime.Now;
                //星期，先判断
                _week = GetRange(5, expressions[5].ToUpper());
                _isUseWeekDay = _week.Count > 0;

                //秒
                _seconds = GetRange(0, expressions[0]);
                //分
                _minutes = GetRange(1, expressions[1]);
                //时
                _hours = GetRange(2, expressions[2]);
                //天
                if (_isUseWeekDay == true)
                {
                    //每一天，加上星期验证
                    _days = GetRange(3, "*");
                }
                else
                {
                    _days = GetRange(3, expressions[3]);
                }
                //月
                _months = GetRange(4, expressions[4]);

                //年
                if (expressions.Length == 7)
                {
                    _years = GetRange(6, expressions[6]);
                }
                else
                {
                    _years = GetRange(6, "*");
                }

                return true;
            }
        }

        /// <summary>
        /// 获取下一个时间点
        /// </summary>
        /// <returns></returns>
        public DateTime GetNextRunTime()
        {
            lock (_lockObj)
            {
                List<DateTime> list = new List<DateTime>();

                DateTime now = DateTime.Now;
                DateTime time = DateTime.MaxValue;
                int maxDay = 31;
                //配置了星期

                //年
                foreach (int year in _years)
                {
                    //往年，跳过
                    if (year < now.Year)
                    {
                        continue;
                    }
                    //月
                    foreach (int month in _months)
                    {
                        //当年往月，跳过
                        if (year <= now.Year && month < now.Month)
                        {
                            continue;
                        }
                        if (month == 12)
                        {
                            maxDay = 31;
                        }
                        else
                        {
                            maxDay = (new DateTime(year, month + 1, 1)).AddDays(-1).Day;
                        }
                        //日
                        foreach (int day in _days)
                        {
                            //当年当月往日，跳过
                            if (year <= now.Year && month <= now.Month && day < now.Day)
                            {
                                continue;
                            }

                            if (day > maxDay)
                            {
                                break;
                            }
                            //如果需要根据星期筛选
                            if (_isUseWeekDay == true)
                            {
                                if (_week.Contains((new DateTime(year, month, day)).DayOfWeek.GetHashCode() + 1) == false)
                                {
                                    continue;
                                }
                            }
                            //小时
                            foreach (int hour in _hours)
                            {
                                //当年当月当日前面小时，跳过
                                if (year <= now.Year && month <= now.Month && day <= now.Day && hour < now.Hour)
                                {
                                    continue;
                                }

                                //分钟
                                foreach (int minute in _minutes)
                                {
                                    //当年当月当日当前小时前面分钟，跳过
                                    if (year <= now.Year && month <= now.Month && day <= now.Day && hour <= now.Hour && minute < now.Minute)
                                    {
                                        continue;
                                    }

                                    //秒
                                    foreach (int second in _seconds)
                                    {
                                        time = new DateTime(year, month, day, hour, minute, second);
                                        if (time >= now)
                                        {
                                            return time;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return DateTime.MaxValue;
        }

        /// <summary>
        /// 获取表达式的取值范围
        /// </summary>
        /// <param name="type"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        private List<int> GetRange(byte type, string expression)
        {
            List<int> list = new List<int>();
            int begin = 0;
            int end = 59;
            switch (type)
            {
                case 2:         //小时
                    begin = 0;
                    end = 23;
                    break;
                case 3:         //日
                    begin = 1;
                    end = 31;
                    break;
                case 4:         //月
                    begin = 1;
                    end = 12;
                    break;
                case 5:         //星期
                    begin = 1;
                    end = 7;
                    break;
                case 6:         //年（默认取一年）
                    begin = _now.Year;
                    end = begin + 1;
                    break;
            }

            if (expression == "?")                  //未知
            {
            }
            else if (expression == "*")              //每一个
            {
                for (int n = begin; n <= end; n++)
                {
                    list.Add(n);
                }
            }
            else if (expression.Contains('/'))       //固定间隔
            {
                string[] ss = expression.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (ss.Length != 2)
                {
                    throw new Exception("时间表达式错误，位于第：" + type + " 节处，字符串为：" + expression + "。");
                }
                for (int n = Convert.ToInt32(ss[0]); n <= end; n += Convert.ToInt32(ss[1]))
                {
                    list.Add(n);
                }
            }
            else if (expression.Contains(',') || expression.Contains('-'))      //多个值
            {
                string[] sss = expression.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string ss in sss)
                {
                    if (ss.Contains('-'))           //还有范围
                    {
                        string[] s = ss.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                        if (s.Length != 2)
                        {
                            throw new Exception("时间表达式错误，位于第：" + type + " 节处，字符串为：" + expression + "。");
                        }
                        //星期，字母转换为数字,SUN-SAT转换为为1-7
                        if (type == 5 && _dicWeekDay.ContainsKey(s[0]))
                        {
                            for (int n = _dicWeekDay[s[0]]; n <= _dicWeekDay[s[1]]; n++)
                            {
                                list.Add(n);
                            }
                        }
                        else
                        {
                            for (int n = Convert.ToInt32(s[0]); n <= Convert.ToInt32(s[1]); n++)
                            {
                                list.Add(n);
                            }
                        }
                    }
                    else
                    {
                        //星期，字母转换为数字,SUN-SAT转换为为1-7
                        if (type == 5 && _dicWeekDay.ContainsKey(ss))
                        {
                            list.Add(_dicWeekDay[ss]);
                        }
                        else
                        {
                            list.Add(Convert.ToInt32(ss));
                        }
                    }
                }
            }
            else
            {
                //星期，字母转换为数字,SUN-SAT转换为为1-7
                if (type == 5 && _dicWeekDay.ContainsKey(expression))
                {
                    list.Add(_dicWeekDay[expression]);
                }
                else
                {
                    list.Add(Convert.ToInt32(expression));      //单个值
                }
            }

            return list;
        }
    }
}
