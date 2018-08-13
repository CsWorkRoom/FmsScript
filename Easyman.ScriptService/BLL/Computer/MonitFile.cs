using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.ScriptService.BLL.Computer
{
    [Serializable]
    public class MonitFile
    {

        public string Id { get; set; }
        /// <summary>
        /// 父级ID
        /// </summary>
        public string ParentId { get; set; }
        /// <summary>
        /// 所属终端ID
        /// </summary>
        public long? ComputerId { get; set; }

        /// <summary>
        /// 所属共享文件夹ID
        /// </summary>
        public long? FolderId { get; set; }



        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 是否是文件夹
        /// </summary>
        public int IsDir { get; set; }
        /// <summary>
        /// 文件格式名称
        /// </summary>
        public string FormatName { get; set; }


        /// <summary>
        /// 客户端路径
        /// </summary>
        public string ClientPath { get; set; }
        /// <summary>
        /// 服务器路径
        /// </summary>
        public string ServerPath { get; set; }
        /// <summary>
        /// MD5
        /// </summary>
        public string MD5 { get; set; }


        public double? Sizes { get; set; }

        public int IsHide { get; set; }

        public string Ticks { get; set; }
    }
}
