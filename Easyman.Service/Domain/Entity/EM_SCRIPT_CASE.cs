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

    public partial class EM_SCRIPT_CASE
    {
        public EM_SCRIPT_CASE()
        {
            this.EM_NODE_POSITION_FORCASE = new HashSet<EM_NODE_POSITION_FORCASE>();
            this.EM_SCRIPT_NODE_CASE = new HashSet<EM_SCRIPT_NODE_CASE>();
        }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ID { get; set; }
        public string NAME { get; set; }
        public Nullable<long> SCRIPT_ID { get; set; }
        public Nullable<int> RETRY_TIME { get; set; }
        public Nullable<System.DateTime> START_TIME { get; set; }
        public Nullable<short> START_MODEL { get; set; }
        public Nullable<long> USER_ID { get; set; }
        public Nullable<short> RUN_STATUS { get; set; }
        public Nullable<short> IS_HAVE_FAIL { get; set; }
        public Nullable<short> RETURN_CODE { get; set; }
        public Nullable<System.DateTime> END_TIME { get; set; }
    
        public virtual ICollection<EM_NODE_POSITION_FORCASE> EM_NODE_POSITION_FORCASE { get; set; }
        public virtual ICollection<EM_SCRIPT_NODE_CASE> EM_SCRIPT_NODE_CASE { get; set; }
    }
}
