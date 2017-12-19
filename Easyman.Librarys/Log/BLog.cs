using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Easyman.Librarys.Config;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;

namespace Easyman.Librarys.Log
{
    /// <summary>
    /// 文件日志记录器（线程安全的）
    /// </summary>
    public class BLog
    {
        /// <summary>
        /// OFF > FATAL > ERROR > WARN > INFO > DEBUG > ALL (低)
        /// </summary>
        public enum LogLevel
        {
            /// <summary>
            /// 关闭
            /// </summary>
            OFF = 0,
            /// <summary>
            /// 致命的错误
            /// </summary>
            FATAL = 1,
            /// <summary>
            /// 错误
            /// </summary>
            ERROR = 2,
            /// <summary>
            /// 警告
            /// </summary>
            WARN = 3,
            /// <summary>
            /// 信息
            /// </summary>
            INFO = 4,
            /// <summary>
            /// 调试
            /// </summary>
            DEBUG = 5
        }

        /// <summary>
        /// 日志对象
        /// </summary>
        public struct LogItem
        {
            /// <summary>
            /// 日志发生时间
            /// </summary>
            public DateTime Time;
            /// <summary>
            /// 日志等级
            /// </summary>
            public LogLevel Level;
            /// <summary>
            /// 日志信息
            /// </summary>
            public string Message;

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="time">日志发生时间</param>
            /// <param name="level">日志等级</param>
            /// <param name="message">日志内容</param>
            public LogItem(DateTime time, LogLevel level, string message)
            {
                Time = time;
                Level = level;
                Message = message;
            }
        }

        private static string _logFilePath = BConfig.GetConfigToString("LogFilePath");
        private static int _logFileMaxSize = BConfig.GetConfigToInt("LogFileMaxSize");
        private static int _logWriteLevel = BConfig.GetConfigToInt("LogWriteLevel");
        private static StreamWriter _streamWriter;
        private static long _curLogFileLength = 0;
        private static ConcurrentQueue<LogItem> _logQueue;
        private static BackgroundWorker _bw;
        private static object lockObj = new object();

        /// <summary>
        /// 日志文件路径，默认为当前应用程序下log子目录,包含路径结束符号
        /// </summary>
        public static string LogFilePath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_logFilePath.ToString()))
                {
                    return Config.BConfig.BaseDirectory + "log" + Path.DirectorySeparatorChar;
                }
                else
                {
                    return _logFilePath;
                }
            }
        }

        /// <summary>
        /// 日志文件大小，默认为1MB
        /// </summary>
        public static int LogFileMaxSize
        {
            get
            {
                if (_logFileMaxSize < 0)
                {
                    return 1024 * 1024;
                }
                else
                {
                    return 1024 * 1024 * _logFileMaxSize;
                }
            }
        }

        /// <summary>
        /// 写日志等级设置，默认只写“警告”以上等级的日志
        /// </summary>
        public static LogLevel LogWriteLevel
        {
            get
            {
                if (_logWriteLevel < 0 || _logWriteLevel > 7)
                {
                    return LogLevel.WARN;
                }
                else
                {
                    return (LogLevel)_logWriteLevel;
                }
            }
        }

        /// <summary>
        /// 写日志文件
        /// </summary>
        /// <param name="logLevel">日志等级</param>
        /// <param name="message">日志内容</param>
        public static void Write(LogLevel logLevel, string message)
        {
            if (_logQueue == null)
            {
                lock (lockObj)
                {
                    //实例化队列
                    _logQueue = new ConcurrentQueue<LogItem>();
                    //开启后台线程，不断从队列提取日志往磁盘写
                    _bw = new BackgroundWorker();
                    _bw.WorkerSupportsCancellation = true;
                    _bw.DoWork += DoWork;
                    _bw.RunWorkerAsync();
                }
            }

            if (logLevel > LogWriteLevel)
            {
                return;
            }

            //添加到队列
            _logQueue.Enqueue(new LogItem(DateTime.Now, logLevel, message));
        }

        /// <summary>
        /// 一直写日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void DoWork(object sender, DoWorkEventArgs e)
        {
            LogItem item;
            while (true)
            {
                if (_logQueue.Count < 1)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                if (_logQueue.TryDequeue(out item))
                {
                    WriteIntoFile(ref item);
                }
            }
        }

        /// <summary>
        /// 将日志对象写到文件
        /// </summary>
        /// <param name="item">日志对象</param>
        private static void WriteIntoFile(ref LogItem item)
        {
            if (_streamWriter == null)
            {
                if (!Directory.Exists(LogFilePath))
                {
                    try
                    {
                        Directory.CreateDirectory(LogFilePath);
                    }
                    catch
                    {
                        return;
                    }
                }
                try
                {
                    _streamWriter = new StreamWriter(LogFilePath + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt", true, Encoding.UTF8);
                    _curLogFileLength = 0;
                }
                catch
                {
                    return;
                }
            }

            try
            {
                _streamWriter.WriteLine(item.Time.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + item.Level.ToString() + "\t" + item.Message);
                _curLogFileLength += _streamWriter.BaseStream.Length;
                _streamWriter.Flush();

                if (_streamWriter.BaseStream.Length >= LogFileMaxSize)
                {
                    _streamWriter.Close();
                    _streamWriter.Dispose();
                    _streamWriter = null;
                    _curLogFileLength = 0;
                }
            }
            catch
            {

            }
        }

    }
}