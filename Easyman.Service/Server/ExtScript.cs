using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Easyman.Service.Domain;
using Easyman.Service.Common;
using Easyman.Librarys.Log;

namespace Easyman.Service.Server
{
    /// <summary>
    /// 脚本生成、编译及执行
    /// </summary>
    public class ExtScript
    {
        /// <summary>
        /// 执行指定的脚本节点实例
        /// </summary>
        /// <param name="nodeCase">节点实例</param>
        /// <param name="err">错误信息</param>
        public static void ExcuteScriptNodeCase(EM_SCRIPT_NODE_CASE nodeCase, ref ErrorInfo err)
        {
            string classCode = "";

            #region 拼凑脚本节点实例的执行代码

            //获取节本内容
            string csharpCode = ScriptManager.AnalyseCode(nodeCase);
            //替换当前脚本流实例涉及的节点~表(公、私)
            csharpCode = ScriptManager.ReplaceTableNode(nodeCase, csharpCode);

            //拼凑自定义函数字符串(代码暂未实现)
            string csharpFun = ScriptFunManager.GetFunStr();
            //拼凑节点执行代码块
            try
            {
                classCode = GenerateCode(csharpCode, csharpFun);
            }
            catch (Exception e)
            {
                err.IsError = true;
                err.Message = e.Message;
                err.Excep = e;
                return;
            }

            #endregion

            #region 动态编译代码

            CompilerResults cr = CompilerClass(classCode, ref err);//编译
            if (cr == null)
            {
                return;
            }

            #endregion

            #region 执行脚本

            //调用必备函数+执行实例代码
            ExcuteScriptCaseCode(cr, nodeCase, ref err);

            #endregion
        }

        /// <summary>
        /// 拼凑节点执行代码块
        /// </summary>
        /// <param name="csharpCode"></param>
        /// <param name="csharpFun"></param>
        /// <returns></returns>
        public static string GenerateCode(string csharpCode, string csharpFun)
        {

            csharpCode = Fun.ReplaceDataTime(csharpCode, DateTime.Now);

            string code = @"
            using System;
            using Easyman.Service.Server;
            namespace Easyman.Service.Server
            {
                public class ScripRun : ExtScriptHelper
                {
                    public bool Run()
                    {
                        try
                        {
                            //载入脚本内容
                            @(csharpCode)
                            //释放数据库链接
                            Dispose();
                            //启动下一组节点实例
                            //StartNextNodeCase();
                            Dispose();
                            _isRun = false;
                            return true;
                        }
                        catch (Exception err)
                        {
                            //脚本执行失败处理
                            DealErr(err.Message);
                            Dispose();
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
        /// 动态编译类
        /// </summary>
        /// <param name="classCode"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        public static CompilerResults CompilerClass(string classCode, ref ErrorInfo err)
        {
            string pathServer = AppDomain.CurrentDomain.BaseDirectory + "Easyman.Service.dll";

            #region 判断类库是否存在

            if (!System.IO.File.Exists(pathServer))
            {
                pathServer = AppDomain.CurrentDomain.BaseDirectory + "Bin\\Easyman.Service.dll";
            }

            if (!System.IO.File.Exists(pathServer))
            {
                err.IsError = true;
                err.Message = string.Format("类库【{0}】不存在", pathServer);
                return null;
            }
            #endregion

            CSharpCodeProvider objCSharpCodePrivoder = new CSharpCodeProvider();
            ICodeCompiler objICodeCompiler = objCSharpCodePrivoder.CreateCompiler();
            CompilerParameters objCompilerParameters = new CompilerParameters();
            objCompilerParameters.ReferencedAssemblies.Add("System.dll");
            objCompilerParameters.ReferencedAssemblies.Add(pathServer);

            objCompilerParameters.GenerateExecutable = false;
            objCompilerParameters.GenerateInMemory = true;

            CompilerResults cr = objICodeCompiler.CompileAssemblyFromSource(objCompilerParameters, classCode);
            if (cr.Errors.HasErrors)
            {
                err.IsError = true;

                StringBuilder sb = new StringBuilder("编译错误：");
                foreach (CompilerError e in cr.Errors)
                {
                    sb.AppendLine(string.Format("行{0}列{1}：{2} \r\n", e.Line, e.Column, e.ErrorText));
                }
                sb.AppendLine("原代码为：\r\n" + classCode);

                err.Message = sb.ToString();
                return null;
            }
            return cr;
        }

        /// <summary>
        /// 执行节点实例代码内容
        /// </summary>
        /// <param name="cr"></param>
        /// <param name="nodeCase"></param>
        /// <param name="err"></param>
        public static void ExcuteScriptCaseCode(CompilerResults cr, EM_SCRIPT_NODE_CASE nodeCase, ref ErrorInfo err)
        {

            // 通过反射,调用函数
            Assembly objAssembly = cr.CompiledAssembly;
            object objScripRun = objAssembly.CreateInstance("Easyman.Service.Server.ScripRun");
            if (objScripRun == null)
            {
                err.IsError = true;
                err.Message = string.Format("未通过反射创建实例。");
                return;
            }

            //初始化节点实例相关数据Initialize()
            var initialize = objScripRun.GetType().GetMethod("Initialize").Invoke(objScripRun, new object[] { nodeCase.ID });

            //设置启动数据库编号
            var setnowdb = objScripRun.GetType().GetMethod("setnowdbid").Invoke(objScripRun, new object[] { nodeCase.DB_SERVER_ID });

            //运行脚本
            var run = objScripRun.GetType().GetMethod("Run").Invoke(objScripRun, null);

            #region   获取错误信息GetErr()

            //外部job需要接收内部错误信息
            //用于判断重试次数、用于是否再执行
            var errorMsg = objScripRun.GetType().GetMethod("GetError").Invoke(objScripRun, null);
            if (!string.IsNullOrEmpty(errorMsg.ToString()))
            {
                err.IsError = true;
                err.Message = errorMsg.ToString();
                return;
            }

            #endregion

        }

    }
}