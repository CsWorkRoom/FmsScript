using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Text.RegularExpressions;
using System.Data.Entity.Validation;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.Web.Script.Serialization;


namespace Easyman.Service.Common
{
    public class Fun
    {
        /// <summary>
        /// 根据DbContext(DBEntities)传入类型获取自增ID
        /// 当前支持数据库：ORACLE\DB2
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static int GetSeqID<T>() where T : new()
        {
            using (DBEntities db = new DBEntities())
            {
                T tmp = new T();
                string sql = "SELECT   " + tmp.GetType().Name + "_SEQ.NEXTVAL   FROM   DUAL";
                string dbType = GetDataBaseType();
                switch (dbType)
                {
                    case "DB2":
                        sql = "SELECT   " + tmp.GetType().Name + "_SEQ.NEXTVAL   FROM   SYSIBM.DUAL";
                        break;
                    case "Oracle":
                        sql = "SELECT   " + tmp.GetType().Name + "_SEQ.NEXTVAL   FROM   DUAL";
                        break;
                    case "Sql":
                        return 0;
                }


                var conn = db.Database.Connection;
                var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
                int reInt = Convert.ToInt32(cmd.ExecuteScalar());
                conn.Close();
                return reInt;
            }
        }


        /// <summary>
        /// 获取当前数据库类型
        /// </summary>
        /// <returns></returns>
        public static string GetDataBaseType()
        {
            using (DBEntities db = new DBEntities())
            {
                string dbType = db.Database.Connection.GetType().Name;
                dbType = dbType.Substring(0, dbType.IndexOf("C"));
                return dbType;
            }
        }

        /// <summary>
        /// 在实体验证失败时
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public static string GetDbEntityErrMess(DbEntityValidationException error)
        {
            StringBuilder reStr = new StringBuilder();
            foreach (var t in error.EntityValidationErrors)
            {
                foreach (var t0 in t.ValidationErrors)
                {
                    reStr.Append(t0.ErrorMessage + ",");
                }
            }
            return reStr.ToString();
        }

        public static string GetExceptionMessage(Exception e)
        {
            IList<string> message = new List<string>();
            message.Add(e.Message);
            while (e.InnerException != null)
            {
                e = e.InnerException;
                message.Add(e.Message);
            }
            return message[message.Count - 1];
        }


        /// <summary>
        /// 将DataTable数据转换成实体类
        /// 本功能主要用于外导EXCEL
        /// </summary>
        /// <typeparam name="T">MVC的实体类</typeparam>
        /// <param name="dt">输入的DataTable</param>
        /// <returns>实体类的LIST</returns>
        public static IList<T> TableToClass<T>(DataTable dt) where T : new()
        {
            IList<T> outList = new List<T>();
            T tmpClass = new T();
            if (dt.Rows.Count == 0) return outList;
            PropertyInfo[] proInfoArr = tmpClass.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);//得到该类的所有公共属性
            Dictionary<string, string> dic = new Dictionary<string, string>();
            Dictionary<string, string> dic_all = new Dictionary<string, string>();
            foreach (var t in proInfoArr)
            {
                var attrsPro = t.GetCustomAttributes(typeof(DisplayAttribute), true);
                if (attrsPro.Length > 0)
                {
                    DisplayAttribute pro = (DisplayAttribute)attrsPro[0];
                    dic_all.Add(pro.Name, t.Name);
                }
            }
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (dic_all.Where(x => x.Key == dt.Columns[i].Caption).Count() != 0)
                {
                    dic.Add(dt.Columns[i].Caption, dic_all[dt.Columns[i].Caption]);
                }
                else if (dic_all.Where(x => x.Value == dt.Columns[i].Caption).Count() != 0)
                {
                    dic.Add(dt.Columns[i].Caption, dt.Columns[i].Caption);
                }
            }

            var rowTmp = dt.Rows[0];

            for (int a = 0; a < dt.Rows.Count; a++)
            {
                var row = dt.Rows[a];
                tmpClass = new T();
                proInfoArr = tmpClass.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);//得到该类的所有公共属性
                foreach (var t in dic)
                {
                    PropertyInfo outproInfo = tmpClass.GetType().GetProperty(t.Value);
                    if (outproInfo != null)
                    {
                        if (row[t.Key] == null || string.IsNullOrEmpty(row[t.Key].ToString()))
                        {
                            row[t.Key] = rowTmp[t.Key];
                        }
                        outproInfo.SetValue(tmpClass, Convert.ChangeType(row[t.Key], outproInfo.PropertyType, CultureInfo.CurrentCulture), null);
                    }
                }
                outList.Add(tmpClass);
                rowTmp = dt.Rows[a];
            }
            return outList;
        }

        /// <summary>
        /// 复制一个类里所有属性值到别一个类
        /// </summary>
        /// <typeparam name="inT">传入的类型</typeparam>
        /// <typeparam name="outT">输出类型</typeparam>
        /// <param name="inClass">传入的类</param>
        /// <param name="outClass">输入的类</param>
        /// <returns>复制结果的类</returns>
        public static outT ClassToCopy<inT, outT>(inT inClass, outT outClass, IList<string> allPar = null)
        {
            if (inClass == null) return outClass;
            PropertyInfo[] proInfoArr = inClass.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);//得到该类的所有公共属性
            for (int a = 0; a < proInfoArr.Length; a++)
            {
                if (allPar != null && !allPar.Contains(proInfoArr[a].Name)) continue;
                PropertyInfo outproInfo = outClass.GetType().GetProperty(proInfoArr[a].Name);
                if (outproInfo != null)
                {
                    var type = outproInfo.PropertyType;
                    object objValue = proInfoArr[a].GetValue(inClass, null);
                    if (null != objValue)
                    {
                        if (!outproInfo.PropertyType.IsGenericType)
                        {
                            objValue = Convert.ChangeType(objValue, outproInfo.PropertyType);
                        }
                        else
                        {
                            Type genericTypeDefinition = outproInfo.PropertyType.GetGenericTypeDefinition();
                            if (genericTypeDefinition == typeof(Nullable<>))
                            {
                                objValue = Convert.ChangeType(objValue, Nullable.GetUnderlyingType(outproInfo.PropertyType));
                            }
                        }
                    }
                    outproInfo.SetValue(outClass, objValue, null);
                }
            }
            return outClass;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="inT"></typeparam>
        /// <typeparam name="outT"></typeparam>
        /// <param name="inClass"></param>
        /// <returns></returns>
        public static outT ClassToCopy<inT, outT>(inT inClass) where outT : new()
        {
            if (inClass == null) return default(outT);
            outT outClass = new outT();
            return ClassToCopy(inClass, outClass);
        }
        /// <summary>
        /// 转换IList内的所有属性
        /// </summary>
        /// <typeparam name="inT"></typeparam>
        /// <typeparam name="outT"></typeparam>
        /// <param name="inClass"></param>
        /// <returns></returns>
        public static IList<outT> ClassListToCopy<inT, outT>(IList<inT> inClass) where outT : new()
        {
            if (inClass == null) return default(IList<outT>);
            IList<outT> outClass = new List<outT>();
            for (int a = 0; a < inClass.Count; a++)
            {
                outClass.Add(ClassToCopy<inT, outT>(inClass[a]));
            }
            return outClass;
        }


        /// <summary>
        /// 计算MD5
        /// </summary>
        /// <param name="fileContent"></param>
        /// <returns></returns>
        public static string FilesMakeMd5(byte[] fileContent)
        {
            if (fileContent == null) return null;
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(fileContent);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
                sb.Append(retVal[i].ToString("x2"));
            return sb.ToString();
        }



        public static DataTable JsonToDataTable(string strJson)
        {
            //取出表名  
            Regex rg = new Regex(@"(?<={)[^:]+(?=:\[)", RegexOptions.IgnoreCase);
            string strName = rg.Match(strJson).Value;
            DataTable tb = null;
            //去除表名  
            strJson = strJson.Substring(strJson.IndexOf("[") + 1);
            strJson = strJson.Substring(0, strJson.IndexOf("]"));

            //获取数据  
            rg = new Regex(@"(?<={)[^}]+(?=})");
            MatchCollection mc = rg.Matches(strJson);
            for (int i = 0; i < mc.Count; i++)
            {
                string strRow = mc[i].Value;
                string[] strRows = strRow.Split(',');

                //创建表  
                if (tb == null)
                {
                    tb = new DataTable();
                    tb.TableName = strName;
                    foreach (string str in strRows)
                    {
                        DataColumn dc = new DataColumn();
                        string[] strCell = str.Split(':');
                        dc.ColumnName = strCell[0].ToString().Replace("\"", "");
                        tb.Columns.Add(dc);
                    }
                    tb.AcceptChanges();
                }

                //增加内容  
                DataRow dr = tb.NewRow();
                for (int r = 0; r < strRows.Length; r++)
                {
                    dr[r] = strRows[r].Split(':')[1].Trim().Replace("，", ",").Replace("：", ":").Replace("\"", "");
                }
                tb.Rows.Add(dr);
                tb.AcceptChanges();
            }

            return tb;
        }
        public static string EvalExpression(string formula)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Result").Expression = formula;
            dt.Rows.Add(dt.NewRow());

            var result = dt.Rows[0]["Result"];
            return result.ToString();
        }
        /// <summary>
        /// 获取类的备注信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string GetClassDescription<T>()
        {

            object[] peroperties = typeof(T).GetCustomAttributes(typeof(DescriptionAttribute), true);
            if (peroperties.Length > 0)
            {
                return ((DescriptionAttribute)peroperties[0]).Description;
            }
            return "";
        }
        /// <summary>
        /// 获取类的属性说明
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string GetClassProperDescription<T>(string properName)
        {
            PropertyInfo[] peroperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in peroperties)
            {
                if (property.Name == properName)
                {
                    object[] objs = property.GetCustomAttributes(typeof(DescriptionAttribute), true);
                    if (objs.Length > 0)
                    {
                        return ((DescriptionAttribute)objs[0]).Description;
                    }
                }
            }
            return "";
        }

        public static string GetClassProperType<T>(string properName)
        {
            PropertyInfo[] peroperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in peroperties)
            {
                if (property.Name == properName)
                {
                    var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    return propertyType.Name;
                }
            }
            return "";
        }


        /// <summary>
        /// 产生一组不重复的随机数
        /// </summary>
        public static IList<int> RandomIntList(int MinValue, int MaxValue, int Length)
        {
            if (MaxValue - MinValue + 1 < Length)
            {
                return null;
            }
            Random R = new Random();
            Int32 SuiJi = 0;
            IList<int> suijisuzu = new List<int>();
            int min = MinValue - 1;
            int max = MaxValue + 1;
            for (int i = 0; i < Length; i++)
            {
                suijisuzu.Add(min);
            }
            for (int i = 0; i < Length; i++)
            {
                while (true)
                {
                    SuiJi = R.Next(min, max);
                    if (!suijisuzu.Contains(SuiJi))
                    {
                        suijisuzu[i] = SuiJi;
                        break;
                    }
                }
            }
            return suijisuzu;
        }

        #region 通过两个点的经纬度计算距离

        private const double EARTH_RADIUS = 6378.137; //地球半径
        private static double rad(double d)
        {
            return d * Math.PI / 180.0;
        }
        /// <summary>
        /// 通过两个点的经纬度计算距离(米)
        /// </summary>
        /// <param name="lat1"></param>
        /// <param name="lng1"></param>
        /// <param name="lat2"></param>
        /// <param name="lng2"></param>
        /// <returns></returns>
        public static double GetDistance(double lat1, double lng1, double lat2, double lng2)
        {
            double radLat1 = rad(lat1);
            double radLat2 = rad(lat2);
            double a = radLat1 - radLat2;
            double b = rad(lng1) - rad(lng2);
            double s = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) +
             Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2)));
            s = s * EARTH_RADIUS;
            s = Math.Round(s * 10000) / 10;
            return s;
        }
        /// <summary>
        /// 通过两个点的经纬度计算距离(米)
        /// </summary>
        /// <param name="lat1"></param>
        /// <param name="lng1"></param>
        /// <param name="lat2"></param>
        /// <param name="lng2"></param>
        /// <returns></returns>
        public static double GetDistance(string lat1, string lng1, string lat2, string lng2)
        {
            return Fun.GetDistance(Convert.ToDouble(lat1), Convert.ToDouble(lng1), Convert.ToDouble(lat2), Convert.ToDouble(lng2));
        }

        #endregion


        /// <summary>
        /// 替换@{day(0)}、@{month(0)}、@{years(0)},@{last_day()}
        /// </summary>
        /// <param name="inStr"></param>
        /// <param name="nowDt"></param>
        /// <returns></returns>
        public static string ReplaceDataTime(string inStr, DateTime nowDt)
        {
            
            inStr = inStr.Replace("@{day}", "@{day(0)}");
            inStr = inStr.Replace("@{month}", "@{month(0)}");
            inStr = inStr.Replace("@{years}", "@{year(0)}");
            inStr = inStr.Replace("@{years", "@{year");
            inStr = inStr.Replace("@{last_day}", "@{last_day(0)}");

            var sql = inStr;
            int nowPlace = 0;
            {
                int s = sql.IndexOf("@{day(");
                nowPlace = s;
                if (s > -1)
                {
                    int e = sql.IndexOf(")}", s);
                    while (e > s && s > -1)
                    {
                        s = s + 6;
                        int per = 0;
                        if (e > s)
                        {
                            per = Convert.ToInt32(sql.Substring(s, e - s));
                        }
                        sql = sql.Replace("@{day(" + per + ")}", nowDt.AddDays(per).ToString("yyyyMMdd"));
                        if (per == 0)
                        {
                            sql = sql.Replace("@{day()}", nowDt.AddDays(per).ToString("yyyyMMdd"));
                        }

                        s = sql.IndexOf("@{day(");
                        if (nowPlace == s)
                        {
                            return "";
                        }
                        nowPlace = s;
                        if (s > -1)
                        {
                            e = sql.IndexOf(")}", s);
                        }
                    }
                }
            }

            {
                int s = sql.IndexOf("@{month(");
                nowPlace = s;
                if (s > -1)
                {
                    int e = sql.IndexOf(")}", s);
                    while (e > s && s > -1)
                    {
                        s = s + 8;
                        int per = 0;
                        if (e > s)
                        {
                            per = Convert.ToInt32(sql.Substring(s, e - s));
                        }
                        sql = sql.Replace("@{month(" + per + ")}", nowDt.AddMonths(per).ToString("yyyyMM"));
                        if (per == 0)
                        {
                            sql = sql.Replace("@{month()}", nowDt.AddMonths(per).ToString("yyyyMM"));
                        }

                        s = sql.IndexOf("@{month(");
                        if (nowPlace == s)
                        {
                            return "";
                        }
                        nowPlace = s;
                        if (s > -1)
                        {
                            e = sql.IndexOf(")}", s);
                        }
                    }
                }
            }
            {
                int s = sql.IndexOf("@{year(");
                nowPlace = s;
                if (s > -1)
                {
                    int e = sql.IndexOf(")}", s);
                    while (e > s && s > -1)
                    {
                        s = s + 7;
                        int per = 0;
                        if (e > s)
                        {
                            per = Convert.ToInt32(sql.Substring(s, e - s));
                        }
                        sql = sql.Replace("@{year(" + per + ")}", nowDt.AddYears(per).ToString("yyyy"));
                        if (per == 0)
                        {
                            sql = sql.Replace("@{year()}", nowDt.AddYears(per).ToString("yyyy"));
                        }

                        s = sql.IndexOf("@{year(");
                        if (nowPlace == s)
                        {
                            return "";
                        }
                        nowPlace = s;
                        if (s > -1)
                        {
                            e = sql.IndexOf(")}", s);
                        }
                    }
                }
            }

            {
                int s = sql.IndexOf("@{last_day(");
                nowPlace = s;
                if (s > -1)
                {
                    int e = sql.IndexOf(")}", s);
                    while (e > s && s > -1)
                    {
                        s = s + 11;
                        int per = 0;
                        if (e > s)
                        {
                            per = Convert.ToInt32(sql.Substring(s, e - s));
                        }

                        DateTime temp = new DateTime(nowDt.Year, nowDt.Month, 1);
                        var tmpV = temp.AddMonths(per + 1).AddDays(-1).ToString("yyyyMMdd");

                        sql = sql.Replace("@{last_day(" + per + ")}", tmpV);
                        if (per == 0)
                        {
                            sql = sql.Replace("@{last_day()}", tmpV);
                        }

                        s = sql.IndexOf("@{last_day(");
                        if (nowPlace == s)
                        {
                            return "";
                        }
                        nowPlace = s;
                        if (s > -1)
                        {
                            e = sql.IndexOf(")}", s);
                        }
                    }
                }
            }

            return sql;
        }


        public static string GetSelectScript(string p)
        {
            // @[00-9]  -5   @[00-23,24-40]
            StringBuilder str = new StringBuilder();
            int begin = p.IndexOf("@");//0
            int end = p.IndexOf("]");//6

            //取得参数占位符中的参数
            string temp = p.Substring(begin + 2, end - begin - 2);

            string[] mc = temp.Split(',');

            foreach (var a in mc)
            {
                string[] c = a.Split('-');

                int i1 = 0, i2 = 0, size = 0;
                try
                {
                    i1 = Int32.Parse(c[0]);
                    i2 = Int32.Parse(c[1]);
                }
                catch 
                {
                    //如果不是数字，返回空字符串
                    return str.ToString();
                }
                //是否不超过第二个数
                for (int i = i1; i <= i2; i++)
                {
                    size = c[0].Length;
                    size = size - i.ToString().Length;
                    if (size > 0)
                    {

                        //补齐占位符
                        switch (size)
                        {
                            case 1:
                                str.Append(p.Replace(p.Substring(begin, end - begin + 1), "0" + i.ToString()));
                                break;
                            case 2:
                                str.Append(p.Replace(p.Substring(begin, end - begin + 1), "00" + i.ToString()));
                                break;
                            case 3:
                                str.Append(p.Replace(p.Substring(begin, end - begin + 1), "000" + i.ToString()));
                                break;
                            default:
                                str.Append(p.Replace(p.Substring(begin, end - begin + 1), "0000" + i.ToString()));
                                break;

                        }

                    }
                    else
                    {
                        str.Append(p.Replace(p.Substring(begin, end - begin + 1), i.ToString()));
                    }
                    str.Append(";");
                }
            }
            return str.ToString();
        }

        /// <summary>
        /// 去除HTML标记
        /// </summary>
        /// <param name="NoHTML">包括HTML的源码 </param>
        /// <returns>已经去除后的文字</returns>
        public static string NoHTML(string Htmlstring)
        {
            if (string.IsNullOrEmpty(Htmlstring)) return Htmlstring;
            //删除脚本
            Htmlstring = Regex.Replace(Htmlstring, @"<script[^>]*?>.*?</script>", "", RegexOptions.IgnoreCase);
            //删除HTML
            Htmlstring = Regex.Replace(Htmlstring, @"<(.[^>]*)>", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"([\r\n])[\s]+", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"-->", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"<!--.*", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(quot|#34);", "\"", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(amp|#38);", "&", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(lt|#60);", "<", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(gt|#62);", ">", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(nbsp|#160);", " ", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(iexcl|#161);", "\xa1", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(cent|#162);", "\xa2", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(pound|#163);", "\xa3", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(copy|#169);", "\xa9", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&#(\d+);", "", RegexOptions.IgnoreCase);
            Htmlstring.Replace("<", "");
            Htmlstring.Replace(">", "");
            Htmlstring.Replace("\r\n", "");
            //Htmlstring=HttpContext.Current.Server.HtmlEncode(Htmlstring).Trim();
            return Htmlstring;

        }

        /// <summary>
        /// 分析运行时间
        /// </summary>
        /// <param name="runDataStr"></param>
        /// <returns></returns>
        public DateTime AnalysisRunDate(string runDataStr)
        {
            var reDate = DateTime.Now;
            if (string.IsNullOrEmpty(runDataStr)) return reDate;
            runDataStr = runDataStr.ToLower();
            try
            {
                switch (runDataStr.Substring(runDataStr.Length - 1))
                {
                    case "m":
                        reDate = reDate.AddMonths(Convert.ToInt32(runDataStr.Substring(0, runDataStr.Length - 1)));
                        break;
                    case "d":
                        reDate = reDate.AddDays(Convert.ToInt32(runDataStr.Substring(0, runDataStr.Length - 1)));
                        break;
                    default:
                        if (runDataStr.Length == 6)
                        {
                            runDataStr = runDataStr + "01";
                        }
                        else
                        {
                            if (runDataStr.Length < 6)
                            {
                                return reDate;
                            }
                        }
                        reDate = Convert.ToDateTime(runDataStr.Substring(0, 4) + "-" + runDataStr.Substring(4, 2) + "-" + runDataStr.Substring(6, 2));
                        break;
                }
                return reDate;
            }
            catch
            {
                return reDate;
            }
        }

        public static object WriteFileObj = new object();
        public static void WriteAllText(string path, string contents)
        {
            lock (WriteFileObj)
            {
                System.IO.File.WriteAllText(path, DecodeToStr(contents));
            }
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
                var jSetting = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                jSetting.Converters.Add(dt);
                return JsonConvert.SerializeObject(entity, jSetting);
            }
            catch
            {
                JavaScriptSerializer jss = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
                jss.MaxJsonLength = int.MaxValue;
                var ent = jss.Serialize(entity);
                return ent;
            }
        }

    }

}
