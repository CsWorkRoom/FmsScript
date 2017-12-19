using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Librarys.BaseQuery;
using Easyman.Librarys.DBHelper;

namespace Easyman.ScriptService.BLL
{
    /// <summary>
    /// 脚本函数
    /// </summary>
    public class EM_SCRIPT_FUNCTION : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_SCRIPT_FUNCTION Instance = new EM_SCRIPT_FUNCTION();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_SCRIPT_FUNCTION()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_SCRIPT_FUNCTION";
            this.ItemName = "脚本函数";
            this.KeyField = "ID";
            this.KeyFieldIsAutoIncrement = Main.KeyFieldIsAutoIncrement;
            this.OrderbyFields = "ID";
        }

        /// <summary>
        /// 获取所有自定义函数，并输出为脚本字符串
        /// </summary>
        /// <returns></returns>
        public string GetAllFunctionsToString()
        {
            DataTable dt = GetTableFields("CONTENT", "STATUS=?", (short)Enums.ScriptStatus.Open);

            if (dt != null && dt.Rows.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (DataRow dr in dt.Rows)
                {
                    sb.AppendLine(dr["CONTENT"].ToString());
                }
                return sb.ToString();
            }

            return string.Empty;
        }
    }
}