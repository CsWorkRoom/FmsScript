using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.Service.Domain
{
    public class ScriptNode
    {
        public long ID { get; set; }
        public Nullable<long> SCRIPT_NODE_TYPE_ID { get; set; }
        public string NAME { get; set; }
        public string CODE { get; set; }
        public Nullable<long> DB_SERVER_ID { get; set; }
        public Nullable<short> SCRIPT_MODEL { get; set; }
        public string CONTENT { get; set; }
        public string REMARK { get; set; }
        public string E_TABLE_NAME { get; set; }
        public string C_TABLE_NAME { get; set; }
        public Nullable<short> TABLE_TYPE { get; set; }
        public Nullable<short> TABLE_MODEL { get; set; }
        public Nullable<System.DateTime> CREATE_TIME { get; set; }
        public Nullable<long> USER_ID { get; set; }
    }
}
