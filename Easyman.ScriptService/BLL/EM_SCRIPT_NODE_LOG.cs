using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Librarys.BaseQuery;
using Easyman.Librarys.DBHelper;

namespace Easyman.ScriptService.BLL
{
    /// <summary>
    /// 后台程序好像不需要用这张表
    /// </summary>
    public class EM_SCRIPT_NODE_LOG : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_SCRIPT_NODE_LOG Instance = new EM_SCRIPT_NODE_LOG();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_SCRIPT_NODE_LOG()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_SCRIPT_NODE_LOG";
            this.ItemName = "";
            this.KeyField = "ID";
            this.KeyFieldIsAutoIncrement = Main.KeyFieldIsAutoIncrement;
            this.OrderbyFields = "ID";
        }
    }
}