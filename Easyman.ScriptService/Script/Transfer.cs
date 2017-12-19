using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Librarys.Log;
using System.Text.RegularExpressions;

namespace Easyman.ScriptService.Script
{
    /// <summary>
    /// 脚本转换类，将脚本中的变量、函数等作替换处理
    /// </summary>
    public class Transfer
    {
        /// <summary>
        /// 转换脚本
        /// </summary>
        /// <param name="nodeCaseEntity">节点实例</param>
        /// <param name="err">错误信息</param>
        /// <returns>转换后的脚本</returns>
        public static string Trans(BLL.EM_SCRIPT_NODE_CASE.Entity nodeCaseEntity, ref ErrorInfo err)
        {
            if (nodeCaseEntity == null)
            {
                return string.Empty;
            }

            string code = nodeCaseEntity.CONTENT;
            string functions = string.Empty;
            try
            {
                //若为创建表，替换表名
                if (nodeCaseEntity.SCRIPT_MODEL == (short)Enums.ScriptModel.CreateTb)
                {
                    code = TransCreateTable(code, nodeCaseEntity.E_TABLE_NAME, nodeCaseEntity.TABLE_SUFFIX, nodeCaseEntity.TABLE_TYPE);
                }

                //替换当前脚本流实例涉及的节点~表(公、私)
                code = ReplaceTableNames(nodeCaseEntity.SCRIPT_CASE_ID, code);

                //替换自定义函数
                code = ReplaceFunctions('@', code);

                //拼凑自定义函数字符串(代码暂未实现)
                functions = BLL.EM_SCRIPT_FUNCTION.Instance.GetAllFunctionsToString();

                //拼凑节点执行代码块
                return GenerateCode(code, functions);
            }
            catch (Exception ex)
            {
                err.IsError = true;
                err.Message = string.Format("脚本流【{0}】的实例【{1}】中的节点【{2}】的实例【{3}】转换脚本，错误信息为：\r\n{3}\r\n原始脚本代码为：\r\n{4}", nodeCaseEntity.SCRIPT_ID, nodeCaseEntity.SCRIPT_CASE_ID, nodeCaseEntity.SCRIPT_NODE_ID, nodeCaseEntity.ID, ex.ToString(), code);
                return string.Empty;
            }
        }

        /// <summary>
        /// 替换创建表的脚本
        /// </summary>
        /// <param name="script">脚本</param>
        /// <param name="eTableName">表名</param>
        /// <param name="tableSuffix">表后缀</param>
        /// <param name="tableType">表类型</param>
        /// <returns></returns>
        private static string TransCreateTable(string script, string eTableName, long? tableSuffix, short? tableType)
        {
            if (string.IsNullOrWhiteSpace(script))
            {
                return string.Empty;
            }

            string retStr = "";
            string code = "";
            string tableName = eTableName;
            if (tableType == (short)Enums.TableType.Private)
            {
                tableName += "_" + tableSuffix;
            }

            //替换表名，删除末尾分号
            code = script.Replace("@{CURR_TB}", tableName).TrimEnd(new char[] { ' ', ';' });

            //先删除同名表，再执行脚本
            retStr = string.Format(@"if(is_table_exists(""{0}""))
                        drop_table(""{0}"");
                    execute(""{1}"");", tableName, code);

            return retStr;
        }

        /// <summary>
        /// 替换当前脚本流实例涉及的节点表
        /// </summary>
        /// <param name="nodeCaseID">脚本流实例ID</param>
        /// <param name="content">当前节点的脚本</param>
        /// <param name="err">错误信息</param>
        private static string ReplaceTableNames(long nodeCaseID, string content)
        {
            //获取当前实例下的节点实例
            IList<BLL.EM_SCRIPT_NODE_CASE.Entity> nodeCaseList = BLL.EM_SCRIPT_NODE_CASE.Instance.GetListByScriptCaseID(nodeCaseID);
            if (nodeCaseList != null && nodeCaseList.Count > 0)
            {
                for (int i = 0; i < nodeCaseList.Count; i++)
                {
                    var ncase = nodeCaseList[i];
                    if (ncase.SCRIPT_MODEL == (short)Enums.ScriptModel.CreateTb)
                    {
                        string tbName = ncase.E_TABLE_NAME;
                        if (ncase.TABLE_TYPE == (short)Enums.TableType.Private)
                        {
                            tbName = ncase.E_TABLE_NAME + "_" + ncase.TABLE_SUFFIX;
                        }
                        //替换占位符表
                        content = content.Replace("@{" + ncase.E_TABLE_NAME + "}", tbName);
                    }
                }
            }

            return content;
        }

        /// <summary>
        /// 替换脚本中的自定义函数
        /// </summary>
        /// <param name="prechar">前缀字符（@表示可变，$表示固定）</param>
        /// <param name="code">脚本内容</param>
        /// <returns>替换之后的脚本</returns>
        public static string ReplaceFunctions(char prechar, string code)
        {
            Regex regFunction = new Regex("\\" + prechar + @"{(?<fun>\w+)\((?<par>.*?)\)}");
            Match match = regFunction.Match(code);
            while (match.Success)
            {
                string fun = match.Result("${fun}");
                string par = match.Result("${par}");
                string result = GetFunctionResult(fun, par);
                //替换
                if (string.IsNullOrWhiteSpace(result) == false)
                {
                    code = code.Replace(prechar + "{" + fun + "(" + par + ")}", result);
                }

                match = match.NextMatch();
            }

            return code;
        }

        /// <summary>
        /// 获取日期函数执行结果
        /// </summary>
        /// <param name="function">函数名</param>
        /// <param name="para">参数</param>
        /// <returns></returns>
        private static string GetFunctionResult(string function, string para)
        {
            int p = 0;
            int.TryParse(para, out p);
            switch(function)
            {
                case "day":
                    return Base.day(p);
                case "day_of_month":
                    return Base.day_of_month(p).ToString();
                case "day_of_month2":
                    return Base.day_of_month2(p);
                case "last_day":
                    return Base.last_day(p);
                case "month":
                    return Base.month(p);
                case "month_of_year":
                    return Base.month_of_year(p).ToString();
                case "month_of_year2":
                    return Base.month_of_year2(p);
                case "year":
                    return Base.year(p).ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// 拼凑节点执行代码块
        /// </summary>
        /// <param name="csharpCode"></param>
        /// <param name="csharpFun"></param>
        /// <returns></returns>
        private static string GenerateCode(string csharpCode, string csharpFun)
        {
            csharpCode = ReplaceDataTime(csharpCode, DateTime.Now);

            string code = @"
            using System;
            //using Easyman.ScriptService.Script;
            namespace Easyman.ScriptService.Script
            {
                public class ScripRunner : Base
                {
                    public bool Run()
                    {
                        try
                        {
                            //载入脚本内容
                            @(csharpCode)
                            return true;
                        }
                        catch (Exception err)
                        {
                            //脚本执行失败处理
                            WriteErrorMessage(err.ToString(), 3);
                            return false;
                        }
                    }
                    //加载自定义函数
                    @(csharpFun)
                }
            }";
            code = code.Replace("@(csharpCode)", csharpCode);
            code = code.Replace("@(csharpFun)", csharpFun);
            return code;
        }

        /// <summary>
        /// 替换@{day(0)}、@{month(0)}、@{years(0)},@{last_day()}
        /// </summary>
        /// <param name="inString"></param>
        /// <param name="nowDatetime"></param>
        /// <returns></returns>
        private static string ReplaceDataTime(string inString, DateTime nowDatetime)
        {

            inString = inString.Replace("@{day}", "@{day(0)}");
            inString = inString.Replace("@{month}", "@{month(0)}");
            inString = inString.Replace("@{years}", "@{year(0)}");
            inString = inString.Replace("@{years", "@{year");
            inString = inString.Replace("@{last_day}", "@{last_day(0)}");

            var sql = inString;
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
                        sql = sql.Replace("@{day(" + per + ")}", nowDatetime.AddDays(per).ToString("yyyyMMdd"));
                        if (per == 0)
                        {
                            sql = sql.Replace("@{day()}", nowDatetime.AddDays(per).ToString("yyyyMMdd"));
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
                        sql = sql.Replace("@{month(" + per + ")}", nowDatetime.AddMonths(per).ToString("yyyyMM"));
                        if (per == 0)
                        {
                            sql = sql.Replace("@{month()}", nowDatetime.AddMonths(per).ToString("yyyyMM"));
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
                        sql = sql.Replace("@{year(" + per + ")}", nowDatetime.AddYears(per).ToString("yyyy"));
                        if (per == 0)
                        {
                            sql = sql.Replace("@{year()}", nowDatetime.AddYears(per).ToString("yyyy"));
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

                        DateTime temp = new DateTime(nowDatetime.Year, nowDatetime.Month, 1);
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
    }
}
