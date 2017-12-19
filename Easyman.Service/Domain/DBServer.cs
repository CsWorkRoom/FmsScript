using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.Service.Domain
{
    public class DBServer
    {
        public long ID { get; set; }
        /// <summary>
        /// 别名
        /// </summary>
        public string BYNAME { get; set; }
        public Nullable<long> DB_TAG_ID { get; set; }
        public string DB_TYPE { get; set; }
        public string IP { get; set; }
        public Nullable<int> PORT { get; set; }
        public string DATA_CASE { get; set; }
        public string USER { get; set; }
        public string PASSWORD { get; set; }
        /// <summary>
        /// Connection对象
        /// </summary>
        //public object DBConnection { get; set; }
        /// <summary>
        /// 链接字符串
        /// </summary>
        public string ConnectionStr { get; set; }
    }
}
