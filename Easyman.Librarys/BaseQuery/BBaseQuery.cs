using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Librarys.Log;

namespace Easyman.Librarys.BaseQuery
{
    /// <summary>
    /// 表单查询基类（针对单表的增/删/改/查的操作）
    /// 文件功能描述：模块类，数据库常用操作的抽象类，包含了对于单表的常见增、删、改、查及缓存操作
    /// 依赖说明：BConfig、BLog、BDBHelper、BCache
    /// 异常处理：捕获异常，当出现异常时，会通过SWLog输出错误信息到日志文件。
    /// </summary>
    public abstract class BBaseQuery
    {
        /// <summary>
        /// 数据库中的表名
        /// </summary>
        public string TableName = string.Empty;
        /// <summary>
        /// 数据库表名的后缀（适用于动态分表的情形。如果没有后缀留空；如果有后缀则在每一个具体操作之前先为后缀赋值）
        /// </summary>
        public string TableNameSuffix = string.Empty;
        /// <summary>
        /// 查询项目名称
        /// </summary>
        protected string ItemName = string.Empty;

        private bool _isAddIntoCache = false;
        /// <summary>
        /// 是否写入缓存（如果是动态表——表名带后缀，则始终不会写缓存）
        /// </summary>
        protected bool IsAddIntoCache
        {
            get { return _isAddIntoCache && string.IsNullOrWhiteSpace(TableNameSuffix); }
            set { _isAddIntoCache = value; }
        }

        /// <summary>
        /// 缓存键名
        /// </summary>
        protected string CacheKey
        {
            get
            {
                return "CacheKeyQuery" + TableName;
            }
        }

        /// <summary>
        /// 缓存有效时间（分钟）
        /// </summary>
        protected int CacheTimeOut = 30;

        /// <summary>
        /// 主键字段
        /// </summary>
        protected string KeyField = "KID";
        /// <summary>
        /// 主键是否为自增字段（如果为自增长，则在添加时不赋值）
        /// </summary>
        protected bool KeyFieldIsAutoIncrement = true;
        /// <summary>
        /// 分组字段
        /// </summary>
        //protected string GroupByFields = string.Empty;
        /// <summary>
        /// 排序字段
        /// </summary>
        public string OrderbyFields = "KID ASC";



        #region 添加

        /// <summary>
        /// 添加记录其它Add方法其实都是在最终在这里实现
        /// </summary>
        /// <param name="fieldList">字段列表</param>
        /// <param name="valueList">值列表</param>
        /// <returns>插入记录条数</returns>
        public int AddReturnRowsCount(List<string> fieldList, List<object> valueList)
        {
            return Add(fieldList, valueList, false);
        }

        /// <summary>
        /// 添加记录其它Add方法其实都是在最终在这里实现
        /// </summary>
        /// <param name="fieldList">字段列表</param>
        /// <param name="valueList">值列表</param>
        /// <param name="isReturnCurID">是否返回自增长主键</param>
        /// <returns>插入记录条数或当前自增长主键的值</returns>
        public int Add(List<string> fieldList, List<object> valueList, bool isReturnCurID)
        {
            if (fieldList.Count < 1 || fieldList.Count != valueList.Count)
            {
                return 0;
            }

            //忽略自增长主键
            if (KeyFieldIsAutoIncrement)
            {
                try
                {
                    int keyIndex = fieldList.IndexOf(KeyField);
                    if (keyIndex >= 0)
                    {
                        fieldList.RemoveAt(keyIndex);
                        valueList.RemoveAt(keyIndex);
                    }
                }
                catch (Exception ex)
                {
                    BLog.Write(BLog.LogLevel.ERROR, "Add:添加记录到表" + ItemName + TableName + TableNameSuffix + "移除自增主键出错\t" + ex.ToString());
                    return 0;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO " + TableName + TableNameSuffix + " (");
            sb.Append(string.Join(",", fieldList.ToArray()) + " ) VALUES (?");
            for (byte i = 1; i < fieldList.Count; i++)
            {
                sb.Append(", ?");
            }
            sb.Append(")");

            int row = 0;
            int maxID = 0;
            //插入并返回自增长ID
            if (isReturnCurID)
            {
                try
                {
                    string sqlMaxID = string.Format("SELECT MAX({0}) FROM {1}", KeyField, TableName);
                    using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                    {
                        dbHelper.BeginTrans();
                        row = dbHelper.ExecuteNonQueryParams(sb.ToString(), valueList);
                        if (row > 0)
                        {
                            maxID = dbHelper.ExecuteScalarInt(sqlMaxID);
                            if (maxID > 0)
                            {
                                dbHelper.CommitTrans();
                                Cache.BCache.Remove(CacheKey);
                            }
                            else
                            {
                                dbHelper.RollbackTrans();
                            }
                        }
                        //2018/12/8添加
                        dbHelper.Close();//主动关闭连接
                    }
                }
                catch (Exception ex)
                {
                    BLog.Write(BLog.LogLevel.ERROR, "Add93:添加记录到表" + ItemName + TableName + TableNameSuffix + "并返回最大ID出错\t" + ex.ToString());
                }

                return maxID;
            }
            else
            {
                try
                {
                    using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                    {
                        row = dbHelper.ExecuteNonQueryParams(sb.ToString(), valueList);
                        if (row > 0 && IsAddIntoCache)
                        {
                            Cache.BCache.Remove(CacheKey);
                        }
                        //2018/12/8添加
                        dbHelper.Close();//主动关闭连接
                    }
                }
                catch (Exception ex)
                {
                    BLog.Write(BLog.LogLevel.ERROR, "Add93:添加记录到表" + ItemName + TableName + TableNameSuffix + "出错\t" + ex.ToString());
                }

                return row;
            }
        }


        /// <summary>
        /// 添加记录
        /// </summary>
        /// <param name="fieldValue">字段值列表</param>
        /// <param name="isReturnCurID">是否返回自增长主键</param>
        /// <returns>插入记录条数或当前自增长主键的值</returns>
        public int Add(Dictionary<string, object> fieldValue, bool isReturnCurID = false)
        {
            if (fieldValue.Count == 0)
            {
                return 0;
            }
            else
            {
                List<string> fieldList = new List<string>();
                List<object> valueList = new List<object>();
                foreach (var item in fieldValue)
                {
                    fieldList.Add(item.Key);
                    valueList.Add(item.Value);
                }
                return Add(fieldList, valueList, isReturnCurID);
            }
        }

        /// <summary>
        /// 添加记录
        /// </summary>
        /// <typeparam name="T">待添加的模型类型  此类型变量必须和数据库字段一一对应</typeparam>
        /// <param name="item">模型实例</param>
        /// <param name="isReturnCurID">是否返回自增长主键</param>
        /// <returns>插入记录条数或当前自增长主键的值</returns>
        public int Add<T>(T item, bool isReturnCurID = false)
        {
            return Add(ToDictionary(item), isReturnCurID);
        }

        /// <summary>
        /// 添加记录
        /// </summary>
        /// <typeparam name="T">待添加的模型类型  此类型变量必须和数据库字段一一对应</typeparam>
        /// <param name="item">模型实例</param>
        /// <param name="excludeFields">要忽略的字段，多个字段以逗号分隔</param>
        /// <param name="isReturnCurID">是否返回自增长主键</param>
        /// <returns>插入记录条数或当前自增长主键的值</returns>
        public int Add<T>(T item, string excludeFields, bool isReturnCurID = false)
        {
            if (string.IsNullOrWhiteSpace(excludeFields))
            {
                return Add(ToDictionary(item), isReturnCurID);
            }
            else
            {
                return Add(ToDictionary(item, excludeFields.Split(',').ToList()), isReturnCurID);
            }
        }

        /// <summary>
        /// 添加记录
        /// </summary>
        /// <typeparam name="T">待添加的模型类型  此类型变量必须和数据库字段一一对应</typeparam>
        /// <param name="item">模型实例</param>
        /// <param name="excludeFields">要忽略的字段列表</param>
        /// <param name="isReturnCurID">是否返回自增长主键</param>
        /// <returns>插入记录条数或当前自增长主键的值</returns>
        public int Add<T>(T item, List<string> excludeFields, bool isReturnCurID = false)
        {
            return Add(ToDictionary(item, excludeFields), isReturnCurID);
        }

        #endregion

        #region 修改

        /// <summary>
        /// 根据主键更新记录
        /// </summary>
        /// <param name="fieldList">要修改的字段列表</param>
        /// <param name="valueList">要修改的值列表</param>
        /// <param name="keyValue">主键值</param>
        /// <returns></returns>
        public int UpdateByKey(List<string> fieldList, List<object> valueList, object keyValue)
        {
            return Update(fieldList, valueList, KeyField + "=?", keyValue);
        }

        /// <summary>
        /// 根据主键更新记录
        /// </summary>
        /// <param name="fieldValue">要修改的字段值列表</param>
        /// <param name="keyValue">主键值</param>
        /// <returns></returns>
        public int UpdateByKey(Dictionary<string, object> fieldValue, object keyValue)
        {
            if (fieldValue.Count == 0)
            {
                return 0;
            }
            else
            {
                List<string> fieldList = new List<string>();
                List<object> valueList = new List<object>();
                foreach (var item in fieldValue)
                {
                    //忽略主键
                    if (item.Key.ToLower() == this.KeyField.ToLower())
                    {
                        continue;
                    }

                    fieldList.Add(item.Key);
                    valueList.Add(item.Value);
                }
                return UpdateByKey(fieldList, valueList, keyValue);
            }
        }


        /// <summary>
        /// 根据主键更新记录 此方法会修改模型对对应的所有字段,调用时请注意
        /// </summary>
        /// <typeparam name="T">要修改的类型 此类型变量必须和数据库字段一一对应</typeparam>
        /// <param name="item">要修改的类型 实例</param>
        /// <param name="keyValue">主键值</param>
        /// <returns>返回成功条数</returns>
        public int UpdateByKey<T>(T item, object keyValue)
        {
            return UpdateByKey(ToDictionary(item), keyValue);
        }

        /// <summary>
        /// 修改记录
        /// </summary>
        /// <param name="fieldList">要修改的字段列表</param>
        /// <param name="valueList">要修改的值列表</param>
        /// <param name="where">where子句，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns></returns>
        public int Update(List<string> fieldList, List<object> valueList, string where, params object[] values)
        {
            if (fieldList.Count < 1 || fieldList.Count != valueList.Count)
            {
                return 0;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE " + TableName + TableNameSuffix + " SET ");
            sb.Append(string.Join("=?,", fieldList.ToArray()) + "=?");
            if (!string.IsNullOrWhiteSpace(where))
            {
                sb.Append(" WHERE " + where);
            }

            if (values != null)
            {
                valueList.AddRange(values);
            }

            int row = 0;
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    row = dbHelper.ExecuteNonQueryParams(sb.ToString(), valueList.ToArray());
                    if (row > 0 && IsAddIntoCache)
                    {
                        Cache.BCache.Remove(CacheKey);
                    }
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "Update233修改表" + ItemName + TableName + TableNameSuffix + "记录出错\t" + ex.ToString());
            }

            return row;
        }


        /// <summary>
        /// 修改记录
        /// </summary>
        /// <param name="fieldValue">要修改的字段值列表</param>
        /// <param name="where">where子句，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns></returns>
        public int Update(Dictionary<string, object> fieldValue, string where, params object[] values)
        {
            if (fieldValue.Count == 0)
            {
                return 0;
            }
            else
            {
                List<string> fieldList = new List<string>();
                List<object> valueList = new List<object>();
                foreach (var item in fieldValue)
                {
                    fieldList.Add(item.Key);
                    valueList.Add(item.Value);
                }
                return Update(fieldList, valueList, where, values);
            }
        }

        /// <summary>
        /// 修改记录
        /// </summary>
        /// <typeparam name="T">待修改模型</typeparam>
        /// <param name="item">待修改模型 实例</param>
        /// <param name="where">where子句，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns>返回成功条数</returns>
        public int Update<T>(T item, string where, params object[] values)
        {
            return Update(ToDictionary(item), where, values);
        }


        #endregion


        #region 删除记录

        /// <summary>
        /// 根据主键删除记录
        /// </summary>
        /// <param name="keyValue">主键值</param>
        /// <returns></returns>
        public int DeleteByKey(object keyValue)
        {
            return Delete(KeyField + "=?", keyValue);
        }

        /// <summary>
        /// 根据限定条件删除记录
        /// </summary>
        /// <param name="where">where子句，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns></returns>
        public int Delete(string where, params object[] values)
        {
            int row = 0;
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    row = dbHelper.ExecuteNonQueryParams("DELETE FROM " + TableName + TableNameSuffix + " WHERE " + where, values);
                    if (row > 0 && IsAddIntoCache)
                    {
                        Cache.BCache.Remove(CacheKey);
                    }
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "Delete335删除表" + ItemName + TableName + TableNameSuffix + "记录出错\t" + ex.ToString());
            }

            return row;
        }

        /// <summary>
        /// 删除表所有记录
        /// </summary>
        /// <returns></returns>
        public int DeleteAll()
        {
            int row = 0;
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    row = dbHelper.Delete(TableName + TableNameSuffix);
                    if (row > 0 && IsAddIntoCache)
                    {
                        Cache.BCache.Remove(CacheKey);
                    }
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "DeleteAll361删除表" + ItemName + TableName + TableNameSuffix + "所有记录出错\t" + ex.ToString());
            }

            return row;
        }

        /// <summary>
        /// 清空表
        /// </summary>
        /// <returns></returns>
        public bool Truncate()
        {
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    dbHelper.Truncate("TRUNCATE TABLE " + TableName + TableNameSuffix);
                    if (IsAddIntoCache)
                    {
                        Cache.BCache.Remove(CacheKey);
                    }
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "Truncate387清空表" + ItemName + TableName + TableNameSuffix + "出错\t" + ex.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// 删除表，慎用！！！！
        /// </summary>
        /// <returns></returns>
        public bool Drop()
        {
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    dbHelper.Drop(TableName + TableNameSuffix);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "Drop413删除表" + ItemName + TableName + TableNameSuffix + "出错\t" + ex.ToString());
                return false;
            }

            return true;
        }

        #endregion

        #region 取序列（针对于Oracle和DB2）
        /// <summary>
        /// 从序列中提取下一个值（默认序列名为：表名_SEQ）
        /// </summary>
        /// <returns></returns>
        public int GetNextValueFromSeq()
        {
            int result = 0;
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    result = dbHelper.GetNextValueFromSeq(TableName + "_SEQ");
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "GetMaxID439读取表" + ItemName + TableName + TableNameSuffix + "自增序列出错\t" + ex.ToString());
            }

            return result;
        }

        #endregion

        #region 读数据

        /// <summary>
        /// 查询最大ID（取主键当前最大值）
        /// </summary>
        /// <returns></returns>
        public int GetMaxKeyID()
        {
            string sql = "SELECT MAX(" + KeyField + ") FROM " + TableName + TableNameSuffix;
            int result = 0;

            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    result = Convert.ToInt32(dbHelper.ExecuteScalarInt(sql));
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "GetMaxID439读取表" + ItemName + TableName + TableNameSuffix + "最大ID出错\t" + ex.ToString());
            }

            return result;
        }

        /// <summary>
        /// 获取记录条数
        /// </summary>
        /// <returns></returns>
        public int GetCount()
        {
            return GetCount("1=1");
        }

        /// <summary>
        /// 获取记录条数
        /// </summary>
        /// <param name="where">where子句，不可为空，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns></returns>
        public int GetCount(string where, params object[] values)
        {
            string sql = "SELECT COUNT(*) FROM " + TableName + TableNameSuffix + " WHERE " + where;
            int result = 0;
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    result = Convert.ToInt32(dbHelper.ExecuteScalarIntParams(sql, values));
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "GetCount473读取表" + ItemName + TableName + TableNameSuffix + "记录条数出错\t" + ex.ToString());
            }

            return result;
        }

        /// <summary>
        /// 从缓存中获取数据
        /// </summary>
        /// <returns></returns>
        protected DataTable GetTableFromCache()
        {
            if (IsAddIntoCache)
            {
                object obj = Cache.BCache.Get(CacheKey);
                if (obj != null)
                {
                    return (DataTable)obj;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <returns></returns>
        public DataTable GetTable()
        {
            DataTable dt = null;
            if (IsAddIntoCache)
            {
                dt = GetTableFromCache();
            }
            if (dt != null)
            {
                return dt;
            }
            dt = GetTable("1=1");
            if (dt != null && IsAddIntoCache)
            {
                Cache.BCache.Add(CacheKey, dt, CacheTimeOut);
            }

            return dt;
        }

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns></returns>
        public IList<T> GetList<T>() where T : new()
        {
            return ToEntityList<T>(GetTable());
        }

        /// <summary>
        /// 查询两个字段，返回为字典，主要适用于K-V键值对的快速提取
        /// </summary>
        /// <param name="keyFieldName">键字段名，确保该字段值为整型</param>
        /// <param name="valueFieldName">值字段名，确保该字段值为字符串</param>
        /// <returns></returns>
        public Dictionary<int, string> GetDictionary(string keyFieldName, string valueFieldName)
        {
            return GetDictionary(keyFieldName, valueFieldName, "1=1");
        }

        /// <summary>
        /// 查询两个字段，返回为字典，主要适用于K-V键值对的快速提取
        /// </summary>
        /// <param name="keyFieldName">键字段名，确保该字段值为整型</param>
        /// <param name="valueFieldName">值字段名，确保该字段值为字符串</param>
        /// <param name="where">where子句，不可为空，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns></returns>
        public Dictionary<int, string> GetDictionary(string keyFieldName, string valueFieldName, string where, params object[] values)
        {
            Dictionary<int, string> dic = new Dictionary<int, string>();
            try
            {
                DataTable dt = GetTableFields(keyFieldName + "," + valueFieldName, where, values);
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        int id = Convert.ToInt32(dr[keyFieldName]);
                        if (dic.ContainsKey(id) == true)
                        {
                            continue;
                        }
                        dic.Add(id, Convert.ToString(dr[valueFieldName]));
                    }
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "GetDictionary读取表" + ItemName + TableName + TableNameSuffix + "数据出错\t" + ex.ToString());
            }
            return dic;
        }


        /// <summary>
        /// 获取列表 默认已主键OrderBy排序
        /// </summary>
        /// <param name="where">where子句，不可为空，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns></returns>
        public DataTable GetTable(string where, params object[] values)
        {
            DataTable dt = null;
            string sql = "SELECT * FROM " + TableName + TableNameSuffix + " WHERE " + where + (string.IsNullOrWhiteSpace(OrderbyFields) ? "" : " ORDER BY " + OrderbyFields);

            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    dt = dbHelper.ExecuteDataTableParams(sql, values);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "GetTable551读取表" + ItemName + TableName + TableNameSuffix + "数据出错\t" + ex.ToString());
            }

            return dt;
        }

        /// <summary>
        /// 获取指定字段的DataTable
        /// </summary>
        /// <param name="fields">字段列表 多个使用逗号连接</param>
        /// <returns></returns>
        public DataTable GetTableFields(string fields)
        {
            DataTable dt = null;
            string sql = "SELECT " + fields + " FROM " + TableName + TableNameSuffix + (string.IsNullOrWhiteSpace(OrderbyFields) ? "" : " ORDER BY " + OrderbyFields);

            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    dt = dbHelper.ExecuteDataTable(sql);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "GetTableFields576读取表" + ItemName + TableName + TableNameSuffix + "数据出错\t" + ex.ToString());
            }

            return dt;
        }

        /// <summary>
        /// 获取指定字段的DataTable
        /// </summary>
        /// <param name="fields">字段列表 多个使用逗号连接</param>
        /// <param name="where">where子句，不可为空，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns></returns>
        public DataTable GetTableFields(string fields, string where, params object[] values)
        {
            DataTable dt = null;
            string sql = "SELECT " + fields + " FROM " + TableName + TableNameSuffix + " WHERE " + where + (string.IsNullOrWhiteSpace(OrderbyFields) ? "" : " ORDER BY " + OrderbyFields);

            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    dt = dbHelper.ExecuteDataTableParams(sql, values);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "GetTableFields603读取表" + ItemName + TableName + TableNameSuffix + "数据出错\t" + ex.ToString());
            }

            return dt;
        }

        /// <summary>
        /// 分页查询获取指定字段的DataTable
        /// </summary>
        /// <param name="fields">字段列表 多个使用逗号连接</param>
        /// <param name="pageSize">分页大小</param>
        /// <param name="pageIndex">页码</param>
        /// <returns></returns>
        public DataTable GetTableFieldsPage(string fields, int pageSize, int pageIndex)
        {
            DataTable dt = null;
            string sql = "SELECT " + fields + " FROM " + TableName + TableNameSuffix + (string.IsNullOrWhiteSpace(OrderbyFields) ? "" : " ORDER BY " + OrderbyFields);

            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    dt = dbHelper.ExecuteDataTablePage(sql, pageSize, pageIndex);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "GetTableFieldsPage630分页读取表" + ItemName + TableName + TableNameSuffix + "数据出错\t" + ex.ToString());
            }

            return dt;
        }

        /// <summary>
        /// 分页查询获取指定字段的DataTable
        /// </summary>
        /// <param name="fields">字段列表 多个使用逗号连接</param>
        /// <param name="pageSize">分页大小</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="where">where子句，不可为空，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns></returns>
        public DataTable GetTableFieldsPage(string fields, int pageSize, int pageIndex, string where, params object[] values)
        {
            DataTable dt = null;
            string sql = "SELECT " + fields + " FROM " + TableName + TableNameSuffix + " WHERE " + where + (string.IsNullOrWhiteSpace(OrderbyFields) ? "" : " ORDER BY " + OrderbyFields);

            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    dt = dbHelper.ExecuteDataTablePageParams(sql, pageSize, pageIndex, values);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "GetTableFieldsPage659分页读取表" + ItemName + TableName + TableNameSuffix + "数据出错\t" + ex.ToString());
            }

            return dt;
        }

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="where">where子句，不可为空，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns></returns>
        public IList<T> GetList<T>(string where, params object[] values) where T : new()
        {
            return ToEntityList<T>(GetTable(where, values));
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageSize">分页大小</param>
        /// <param name="pageIndex">页码</param>
        /// <returns></returns>
        public DataTable GetTablePage(int pageSize, int pageIndex)
        {
            return GetTablePage(pageSize, pageIndex, "1=1");
        }

        /// <summary>
        /// 根据页面获取分页列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pageSize">分页大小</param>
        /// <param name="pageIndex">页码</param>
        /// <returns></returns>
        public IList<T> GetListPage<T>(int pageSize, int pageIndex) where T : new()
        {
            return ToEntityList<T>(GetTablePage(pageSize, pageIndex));
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageSize">分页大小</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="where">where子句，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns></returns>
        public DataTable GetTablePage(int pageSize, int pageIndex, string where, params object[] values)
        {
            DataTable dt = null;
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    dt = dbHelper.ExecuteDataTablePageParams("SELECT * FROM " + TableName + TableNameSuffix + " WHERE " + where + (string.IsNullOrEmpty(OrderbyFields) ? "" : (" ORDER BY " + OrderbyFields)), pageSize, pageIndex, values);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "GetTablePage722分页查询表" + ItemName + TableName + TableNameSuffix + "数据出错\t" + ex.ToString());
            }

            return dt;
        }

        /// <summary>
        /// 根据页面获取分页列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pageSize">分页大小</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="where">where子句，不可为空，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns></returns>
        public IList<T> GetListPage<T>(int pageSize, int pageIndex, string where, params object[] values) where T : new()
        {
            return ToEntityList<T>(GetTablePage(pageSize, pageIndex, where, values));
        }

        /// <summary>
        /// 根据限定条件查询一条记录
        /// </summary>
        /// <param name="where">where子句，不可为空，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns></returns>
        public DataRow GetRow(string where, params object[] values)
        {
            return GetRowFields("*", where, values);
        }

        /// <summary>
        /// 查询一条记录
        /// </summary>
        /// <param name="fields">字段列表 多个使用逗号连接</param>
        /// <param name="where">where子句，不可为空，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns></returns>
        public DataRow GetRowFields(string fields, string where, params object[] values)
        {
            DataRow dr = null;
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    dr = dbHelper.ExecuteDataRowParams("SELECT " + fields + " FROM " + TableName + TableNameSuffix + " WHERE " + where, values);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "GetRowFields759读取表" + ItemName + TableName + TableNameSuffix + "记录出错\t" + ex.ToString());
            }

            return dr;
        }

        /// <summary>
        /// 根据限定条件查询一条记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="where">where子句，不可为空，不带关键字，参数用问号占位</param>
        /// <param name="values">参数</param>
        /// <returns></returns>
        public T GetEntity<T>(string where, params object[] values) where T : new()
        {
            return ToEntity<T>(GetRow(where, values));
        }

        /// <summary>
        /// 根据主键查询一条记录
        /// </summary>
        /// <param name="keyValue">主键值</param>
        /// <returns></returns>
        public DataRow GetRowByKey(object keyValue)
        {
            DataRow dr = null;
            string sql = "SELECT * FROM " + TableName + TableNameSuffix + " WHERE " + KeyField + "=?";
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    dr = dbHelper.ExecuteDataRowParams(sql, keyValue);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "GetRowByKey793读取表" + ItemName + TableName + TableNameSuffix + "键为[" + keyValue + "]行出错\t" + ex.ToString());
            }

            return dr;
        }

        /// <summary>
        /// 根据主键查询一条记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="keyValue">主键值</param>
        /// <returns></returns>
        public T GetEntityByKey<T>(object keyValue) where T : new()
        {
            return ToEntity<T>(GetRowByKey(keyValue));
        }


        /// <summary>
        /// 根据键值获取记录某一字段值
        /// </summary>
        /// <param name="keyValue"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public object GetValueByKey(object keyValue, string fieldName)
        {
            DataRow dr = GetRowByKey(keyValue);
            if (dr != null)
            {
                return dr[fieldName];
            }

            return null;
        }

        /// <summary>
        /// 根据键值获取记录某一字段值
        /// </summary>
        /// <param name="keyValue">主键值</param>
        /// <param name="fieldName">要获取的字段名</param>
        /// <returns></returns>
        public string GetStringValueByKey(object keyValue, string fieldName)
        {
            DataRow dr = GetRowByKey(keyValue);
            if (dr != null)
            {
                return Convert.ToString(dr[fieldName]);
            }

            return string.Empty;
        }

        /// <summary>
        /// 根据键值获取记录某一字段值
        /// </summary>
        /// <param name="keyValue">主键值</param>
        /// <param name="fieldName">要获取的字段名</param>
        /// <returns></returns>
        public int GetIntValueByKey(object keyValue, string fieldName)
        {
            DataRow dr = GetRowByKey(keyValue);
            if (dr != null)
            {
                return Convert.ToInt32(dr[fieldName]);
            }

            return 0;
        }

        /// <summary>
        /// 根据键值获取记录某一字段值
        /// </summary>
        /// <param name="keyValue">主键值</param>
        /// <param name="fieldName">要获取的字段名</param>
        /// <returns></returns>
        public long GetLongValueByKey(object keyValue, string fieldName)
        {
            DataRow dr = GetRowByKey(keyValue);
            if (dr != null)
            {
                return Convert.ToInt64(dr[fieldName]);
            }

            return 0;
        }

        /// <summary>
        /// 根据键值获取记录某一字段值
        /// </summary>
        /// <param name="keyValue">主键值</param>
        /// <param name="fieldName">要获取的字段名</param>
        /// <returns></returns>
        public byte GetByteValueByKey(object keyValue, string fieldName)
        {
            DataRow dr = GetRowByKey(keyValue);
            if (dr != null)
            {
                return Convert.ToByte(dr[fieldName]);
            }

            return 0;
        }

        /// <summary>
        /// 根据键值获取记录某一字段值
        /// </summary>
        /// <param name="keyValue">主键值</param>
        /// <param name="fieldName">要获取的字段名</param>
        /// <returns></returns>
        public bool GetBoolValueByKey(object keyValue, string fieldName)
        {
            DataRow dr = GetRowByKey(keyValue);
            if (dr != null)
            {
                return Convert.ToInt32(dr[fieldName]) != 0;
            }

            return false;
        }

        #endregion

        #region 验证数据唯一性

        /// <summary>
        /// 检测字段值是否重复
        /// </summary>
        /// <param name="keyValue">主键值</param>
        /// <param name="fieldName">重复验证的字段名</param>
        /// <param name="value">重复验证的字段值</param>
        /// <returns></returns>
        public bool IsDuplicate(object keyValue, string fieldName, string value)
        {
            bool result = true;
            try
            {
                string sql = "SELECT COUNT(*) FROM " + TableName + TableNameSuffix + " WHERE " + KeyField + "<>? AND " + fieldName + "=?";

                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    result = (Convert.ToInt32(dbHelper.ExecuteScalarIntParams(sql, keyValue, value)) > 0);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "IsDuplicate936验证表" + ItemName + TableName + TableNameSuffix + "值重复性出错\t" + ex.ToString());
            }

            return result;
        }

        #endregion


        #region 类型转换
        /// <summary>
        /// 根据模型反射得到字典
        /// </summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="item">模型实例</param>
        /// <returns>返回字典,Key:string Value:object</returns>
        public Dictionary<string, object> ToDictionary<T>(T item)
        {
            return ToDictionary(item, null);
        }

        /// <summary>
        /// 根据模型反射得到字典
        /// </summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="item">模型实例</param>
        /// <param name="excludeFields">要忽略的字段列表</param>
        /// <returns>返回字典,Key:string Value:object</returns>
        public Dictionary<string, object> ToDictionary<T>(T item, List<string> excludeFields)
        {
            Dictionary<string, object> retdic = new Dictionary<string, object>();
            Dictionary<string, byte> dic = new Dictionary<string, byte>();
            if (excludeFields != null)
            {
                foreach (string f in excludeFields)
                {
                    if (!dic.ContainsKey(f.ToLower()))
                    {
                        dic.Add(f.ToLower(), 1);
                    }
                }
            }

            try
            {
                foreach (var p in item.GetType().GetProperties())
                {
                    if (!dic.ContainsKey(p.Name.ToLower()))
                    {
                        retdic.Add(p.Name, p.GetValue(item, null));
                    }
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "ToDictionary977将表" + ItemName + TableName + TableNameSuffix + "的Model转换为字典出错\t" + ex.ToString());
            }
            return retdic;
        }

        /// <summary>
        /// 根据模型 反射得到字段,从DataTable里面转换得到ListModel
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <returns></returns>
        public IList<T> ToEntityList<T>(DataTable dt) where T : new()
        {
            if (dt == null)
            {
                return null;
            }

            IList<T> list = new List<T>();

            foreach (DataRow dr in dt.Rows)
            {
                list.Add(ToEntity<T>(dr));
            }

            return list;
        }

        /// <summary>
        /// 根据模型 反射得到字段,从DataRow反射得到实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="dr">一行记录</param>
        /// <returns></returns>
        public T ToEntity<T>(DataRow dr) where T : new()
        {
            if (dr == null)
            {
                return default(T);
            }

            T t = new T();
            string colName = "";
            string typename = "";
            try
            {
                foreach (var pi in t.GetType().GetProperties())
                {
                    colName = pi.Name;
                    if (!pi.CanWrite)
                    {
                        continue;
                    }
                    if (!dr.Table.Columns.Contains(pi.Name))
                    {
                        continue;
                    }
                    object value = dr[pi.Name];
                    if (value == DBNull.Value)
                    {
                        continue;
                    }

                    typename = pi.PropertyType.ToString().ToLower().Replace("system.nullable`1", "").Replace("system", "").Replace("[", "").Replace("]", "");

                    switch (typename)
                    {
                        case "datetime":
                            try
                            {
                                value = Convert.ToDateTime(dr[pi.Name]);
                            }
                            catch
                            {
                                continue;
                            }
                            break;
                        case ".sbyte":
                            value = Convert.ToSByte(dr[pi.Name]);
                            break;
                        case ".byte":
                            value = Convert.ToByte(dr[pi.Name]);
                            break;
                        case ".single":
                            value = Convert.ToSingle(dr[pi.Name]);
                            break;
                        case ".int16":
                            value = Convert.ToInt16(dr[pi.Name]);
                            break;
                        case ".int32":
                            value = Convert.ToInt32(dr[pi.Name]);
                            break;
                        case ".int64":
                            value = Convert.ToInt64(dr[pi.Name]);
                            break;
                        case ".uint16":
                            value = Convert.ToUInt16(dr[pi.Name]);
                            break;
                        case ".uint32":
                            value = Convert.ToUInt32(dr[pi.Name]);
                            break;
                        case ".uint64":
                            value = Convert.ToUInt64(dr[pi.Name]);
                            break;
                    }

                    pi.SetValue(t, value, null);
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "将表" + ItemName + TableName + TableNameSuffix + " DataRow转换为：Entity数据出错,字段：" + colName + ",类型" + typename + "\t" + ex.ToString());
            }

            return t;
        }

        #endregion

        #region 导入txt到数据库

        /// <summary>
        /// 从本地文件导入数据到数据库表中，默认：文件为UTF-8编码，导入到派生类对应的表，导入所有字段，字段分隔符为\t，记录分隔符为\n
        /// </summary>
        /// <param name="fileName">本地文件名</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns>导入记录条数</returns>
        public int LoadDataInLocalFile(string fileName, bool isReplace = false)
        {
            int result = 0;
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    result = dbHelper.LoadDataInLocalFile(TableName + TableNameSuffix, fileName, isReplace);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "LoadDataInLocalFile1097从文件" + fileName + "导入表" + ItemName + TableName + TableNameSuffix + "记录出错\t" + ex.ToString());
            }

            return result;
        }

        /// <summary>
        /// 从本地文件导入数据到数据库表中，默认：文件为UTF-8编码，导入到派生类对应的表，字段分隔符为\t，记录分隔符为\n
        /// </summary>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fields">字段列表</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns>导入记录条数</returns>
        public int LoadDataInLocalFile(string fileName, List<string> fields, bool isReplace = false)
        {
            int result = 0;
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    result = dbHelper.LoadDataInLocalFile(TableName + TableNameSuffix, fileName, fields, isReplace);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "LoadDataInLocalFile1121从文件" + fileName + "导入表" + ItemName + TableName + TableNameSuffix + "记录出错\t" + ex.ToString());
            }

            return result;
        }

        /// <summary>
        /// 从本地文件导入数据到数据库表中，默认：文件为UTF-8编码，导入到派生类对应的表，导入所有字段，记录分隔符为\n
        /// </summary>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fieldsTerminated">字段分隔符</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns>导入记录条数</returns>
        public int LoadDataInLocalFile(string fileName, string fieldsTerminated, bool isReplace = false)
        {
            int result = 0;
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    result = dbHelper.LoadDataInLocalFile(TableName + TableNameSuffix, fileName, fieldsTerminated, isReplace);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "LoadDataInLocalFile1145从文件" + fileName + "导入表" + ItemName + TableName + TableNameSuffix + "记录出错\t" + ex.ToString());
            }

            return result;
        }

        /// <summary>
        /// 从本地文件导入数据到数据库表中，默认：文件为UTF-8编码，导入到派生类对应的表，记录分隔符为\n
        /// </summary>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fields">字段列表</param>
        /// <param name="fieldsTerminated">字段分隔符</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns>导入记录条数</returns>
        public int LoadDataInLocalFile(string fileName, List<string> fields, string fieldsTerminated, bool isReplace = false)
        {
            int result = 0;
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    result = dbHelper.LoadDataInLocalFile(TableName + TableNameSuffix, fileName, fields, fieldsTerminated, isReplace);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "LoadDataInLocalFile1170从文件" + fileName + "导入表" + ItemName + TableName + TableNameSuffix + "记录出错\t" + ex.ToString());
            }

            return result;
        }

        /// <summary>
        /// 从本地文件导入数据到数据库表中，默认：文件为UTF-8编码，导入到派生类对应的表
        /// </summary>
        /// <param name="fileName">本地文件名</param>
        /// <param name="fields">字段列表</param>
        /// <param name="fieldsTerminated">字段分隔符</param>
        /// <param name="linesTerminated">记录分隔符</param>
        /// <param name="isReplace">是否覆盖主键冲突记录（默认为不覆盖）</param>
        /// <returns>导入记录条数</returns>
        public int LoadDataInLocalFile(string fileName, List<string> fields, string fieldsTerminated, string linesTerminated, bool isReplace = false)
        {
            int result = 0;
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    result = dbHelper.LoadDataInLocalFile(TableName + TableNameSuffix, fileName, fields, fieldsTerminated, linesTerminated, isReplace);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "LoadDataInLocalFile1196从文件" + fileName + "导入表" + ItemName + TableName + TableNameSuffix + "记录出错\t" + ex.ToString());
            }

            return result;
        }

        #endregion

        #region 从内存导入数据到数据库

        /// <summary>
        /// 从DataTable导入数据到数据库表（适用于小批量数据导入），默认导入到派生类对应的表
        /// </summary>
        /// <param name="dt">数据表（字段名通过ColumnName来指定）</param>
        /// <returns></returns>
        public int LoadDataInDataTable(DataTable dt)
        {
            int result = 0;
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    result = dbHelper.LoadDataInDataTable(TableName + TableNameSuffix, dt);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "LoadDataInDataTable1222从DataTable导入表" + ItemName + TableName + TableNameSuffix + "记录出错\t" + ex.ToString());
            }

            return result;
        }

        /// <summary>
        /// 从List导入数据到数据库表（适用于小批量数据导入），默认导入到派生类对应的表
        /// </summary>
        /// <param name="list">数据列表（每条记录为一个字典，字典的键为字段名，值为字段值</param>
        /// <returns>导入数据的条数</returns>
        public int LoadDataInList(List<Dictionary<string, object>> list)
        {
            int result = 0;
            try
            {
                using (DBHelper.BDBHelper dbHelper = new DBHelper.BDBHelper())
                {
                    result = dbHelper.LoadDataInList(TableName + TableNameSuffix, list);
                    //2018/12/8添加
                    dbHelper.Close();//主动关闭连接
                }
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "LoadDataInList1244从List导入表" + ItemName + TableName + TableNameSuffix + "记录出错\t" + ex.ToString());
            }

            return result;
        }

        /// <summary>
        /// 从List导入数据到数据库表（适用于小批量数据导入），默认导入到派生类对应的表
        /// </summary>
        /// <param name="list">数据列表（每条记录为一个字典，字典的键为字段名，值为字段值</param>
        /// <returns>导入数据的条数</returns>
        public int LoadDataInList<T>(List<T> list)
        {
            return LoadDataInList(list, null);
        }

        /// <summary>
        /// 从List导入数据到数据库表（适用于小批量数据导入），默认导入到派生类对应的表
        /// </summary>
        /// <param name="list">数据列表（每条记录为一个字典，字典的键为字段名，值为字段值</param>
        /// <param name="excludeFields">要忽略的字段列表</param>
        /// <returns>导入数据的条数</returns>
        public int LoadDataInList<T>(List<T> list, List<string> excludeFields)
        {
            try
            {
                List<Dictionary<string, object>> l = new List<Dictionary<string, object>>();
                foreach (var item in list)
                {
                    l.Add(ToDictionary<T>(item, excludeFields));
                }

                //导入
                return LoadDataInList(l);
            }
            catch (Exception ex)
            {
                BLog.Write(BLog.LogLevel.ERROR, "LoadDataInList1277从List导入表" + ItemName + TableName + TableNameSuffix + "记录出错\t" + ex.ToString());
            }

            return 0;
        }

        #endregion

    }
}