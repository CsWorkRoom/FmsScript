using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Easyman.Service.Domain
{
    /// <summary>
    /// 用于口径，下载数据、外导数据
    /// </summary>
    public class TableFiled
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string DataType { get; set; }
        public string CSharpType { get; set; }
        public int Length { get; set; }
        public int isKey { get; set; }
        public int isPartition { get; set; }
        public int isIndex { get; set; }
    }
}
