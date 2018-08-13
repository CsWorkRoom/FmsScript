using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.ScriptService.BLL.Computer
{
    [Serializable]
    public class FileProperty
    {
        public string Id { get; set; }

        public string MD5 { get; set; }

        public string PName { get; set; }
        public string PValue { get; set; }
        public string Ticks { get; set; }
    }
}
