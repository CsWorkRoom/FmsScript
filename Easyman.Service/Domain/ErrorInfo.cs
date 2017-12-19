using System;
using System.Collections.Generic;
using System.Text;

namespace Easyman.Service.Domain
{
    /// <summary>
    /// 错误信息
    /// </summary>
    public class ErrorInfo
    {
        /// <summary>
        /// 错误信息
        /// </summary>
        public ErrorInfo()
        {
        }
        /// <summary>
        /// 是否出错
        /// </summary>
        public bool IsError { get; set; }
        /// <summary>
        /// 错误信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 备用参数
        /// </summary>
        public string Params { get; set; }
        /// <summary>
        /// 错误实例
        /// </summary>
        public Exception Excep { get; set; }
    }
}
