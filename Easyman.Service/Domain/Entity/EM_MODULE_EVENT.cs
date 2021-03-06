//------------------------------------------------------------------------------
// <auto-generated>
//    此代码是根据模板生成的。
//
//    手动更改此文件可能会导致应用程序中发生异常行为。
//    如果重新生成代码，则将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace Easyman.Service
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class EM_MODULE_EVENT
    {
        public EM_MODULE_EVENT()
        {
            this.EM_ROLE_MODULE_EVENT = new HashSet<EM_ROLE_MODULE_EVENT>();
        }
    [Key]
        public long ID { get; set; }
        public long ANALYSIS_ID { get; set; }
        public string CODE { get; set; }
        public string EVENT_TYPE { get; set; }
        public string EVENT_NAME { get; set; }
        public string SOURCE_TABLE { get; set; }
        public long SOURCE_ID { get; set; }
    
        [ForeignKey("ANALYSIS_ID")]
        public virtual EM_ANALYSIS EM_ANALYSIS { get; set; }
        public virtual ICollection<EM_ROLE_MODULE_EVENT> EM_ROLE_MODULE_EVENT { get; set; }
    }
}
