﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Librarys.BaseQuery;
using Easyman.Librarys.DBHelper;

namespace Easyman.ScriptService.BLL
{
    /// <summary>
    /// 脚本类型
    /// </summary>
    public class EM_SCRIPT_TYPE : BBaseQuery
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static EM_SCRIPT_TYPE Instance = new EM_SCRIPT_TYPE();

        /// <summary>
        /// 构造函数
        /// </summary>
        public EM_SCRIPT_TYPE()
        {
            this.IsAddIntoCache = true;
            this.TableName = "EM_SCRIPT_TYPE";
            this.ItemName = "脚本流类型";
            this.KeyField = "ID";
            this.KeyFieldIsAutoIncrement = Main.KeyFieldIsAutoIncrement;
            this.OrderbyFields = "ID";
        }
    }
}