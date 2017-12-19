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
    /// 数据库标识
    /// </summary>
    public class EM_DB_TAG : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_DB_TAG Instance = new EM_DB_TAG();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_DB_TAG()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_DB_TAG";
            this.ItemName = "数据库标识";
            this.KeyField = "ID";
            this.KeyFieldIsAutoIncrement = Main.KeyFieldIsAutoIncrement;
            this.OrderbyFields = "ID";
        }
    }
}
