
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.Service
{
    
    public partial class EM_MODULE
    {
        [Key]
        public long ID { get; set; }
        public Nullable<long> PARENT_ID { get; set; }
        public string PATH_ID { get; set; }
        public Nullable<int> LEVEL { get; set; }
        public Nullable<int> SHOW_ORDER { get; set; }
        public Nullable<int> TYPE { get; set; }
        public string NAME { get; set; }
        public string APPLICATION_TYPE { get; set; }
        public string URL { get; set; }
        public string IDENTIFIER { get; set; }
        public string CODE { get; set; }
        public short IS_DEBUG { get; set; }
        public short IS_HIDE { get; set; }
        public string DESCRIPTION { get; set; }
        public string IMAGE_URL { get; set; }
        public string REMARK { get; set; }
        public string ICON { get; set; }
        public int TENANT_ID { get; set; }
        public System.DateTime CREATE_TIME { get; set; }
        public Nullable<long> CREATE_UID { get; set; }
        public Nullable<long> DELETE_UID { get; set; }
        public Nullable<System.DateTime> DELETE_TIME { get; set; }
        public short IS_DELETE { get; set; }
        public Nullable<System.DateTime> UPDATE_TIME { get; set; }
        public Nullable<long> UPDATE_UID { get; set; }
    
        public virtual ICollection<EM_MODULE> EM_MODULE1 { get; set; }
        public virtual EM_MODULE EM_MODULE2 { get; set; }
        public virtual ICollection<EM_ROLE_MODULE_EVENT> EM_ROLE_MODULE_EVENT { get; set; }
        public virtual ICollection<EM_ROLE_MODULE> EM_ROLE_MODULE { get; set; }
    }
}
