using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.ScriptService.BLL.Computer
{
    public class MonitConst
    {
        /// <summary>
        /// 还原特殊字符串
        /// </summary>
        public const string RestoreStr = "abcdefg$$lcz&&cs";//命名规则后面可以调整

        /// <summary>
        /// 原文件删除前被重命名的后缀
        /// </summary>
        public const string MiddleStr = "abcdefghig$$lcz&&cs12345";//命名规则后面可以调整

        //下载特殊字符串
        public const string DownStr = "higklmn$$lcz&&cs6789";//命名规则后面可以调整

        //数据库备份文件
        public const string DataBaseStr = "DataBase$$lcz$$cs";//命名规则后面可以调整

    }
}
