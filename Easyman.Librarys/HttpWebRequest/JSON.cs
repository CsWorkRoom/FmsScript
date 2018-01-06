using System;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Easyman.Common
{
    /// <summary>
    /// json操作
    /// </summary>
    public static class JSON
    {
        /// <summary>
        /// 转换成对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        public static T EncodeToEntity<T>(string jsonStr)
        {
            if (jsonStr == null) return default(T);
            JavaScriptSerializer jss = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
            var ent = jss.Deserialize<T>(jsonStr);
            return ent;
        }
        /// <summary>
        /// 对象转换成字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static string DecodeToStr<T>(T entity)
        {
            if (entity == null) return null;
            JavaScriptSerializer jss = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
            try
            {
                if (entity == null) return null;
                if ((entity.GetType() == typeof(String) || entity.GetType() == typeof(string)))
                {
                    return entity.ToString();
                }
                string DateTimeFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss";
                IsoDateTimeConverter dt = new IsoDateTimeConverter();
                dt.DateTimeFormat = DateTimeFormat;
                return JsonConvert.SerializeObject(entity, dt);
            }
            catch
            {
                var ent = jss.Serialize(entity);
                return ent;
            }
        }


        public static string DecodeToStr(int allNum, object o)
        {
            string json = DecodeToStr(o);
            if (json == null || json == "") json = "[]";
            return "{\"total\":" + allNum + ",\"rows\":" + json + "}";
        }

        /// <summary>
        /// 将datatable转换为json  
        /// </summary>
        /// <param name="dtb">Dt</param>
        /// <returns>JSON字符串</returns>
        public static string Dtb2Json(DataTable dtb)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            System.Collections.ArrayList dic = new System.Collections.ArrayList();
            foreach (DataRow dr in dtb.Rows)
            {
                System.Collections.Generic.Dictionary<string, object> drow = new System.Collections.Generic.Dictionary<string, object>();
                foreach (DataColumn dc in dtb.Columns)
                {
                    drow.Add(dc.ColumnName, dr[dc.ColumnName]);
                }
                dic.Add(drow);

            }
            //序列化  
            return jss.Serialize(dic);
        }

        /// <summary>  
        /// Json 字符串 转换为 DataTable数据集合  
        /// </summary>  
        /// <param name="json"></param>  
        /// <returns></returns>  
        public static DataTable ToDataTable(string json)
        {
            DataTable dataTable = new DataTable();  //实例化  
            DataTable result;
            try
            {
                JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
                javaScriptSerializer.MaxJsonLength = Int32.MaxValue; //取得最大数值  
                ArrayList arrayList = javaScriptSerializer.Deserialize<ArrayList>(json);
                if (arrayList.Count > 0)
                {
                    foreach (Dictionary<string, object> dictionary in arrayList)
                    {
                        if (dictionary.Keys.Count<string>() == 0)
                        {
                            result = dataTable;
                            return result;
                        }
                        if (dataTable.Columns.Count == 0)
                        {
                            foreach (string current in dictionary.Keys)
                            {
                                dataTable.Columns.Add(current);//此处忽略了列的类型（当行值为空时，无法准确获取到类型）
                                //dataTable.Columns.Add(current, dictionary[current].GetType());
                            }
                        }
                        DataRow dataRow = dataTable.NewRow();
                        foreach (string current in dictionary.Keys)
                        {
                            dataRow[current] = dictionary[current];
                        }

                        dataTable.Rows.Add(dataRow); //循环添加行到DataTable中  
                    }
                }
            }
            catch(Exception ex)
            {

            }
            result = dataTable;
            return result;
        }
    }
}