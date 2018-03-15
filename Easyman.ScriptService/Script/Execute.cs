using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Librarys.Log;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;

namespace Easyman.ScriptService.Script
{
    /// <summary>
    /// 执行动态脚本
    /// </summary>
    public class Execute
    {
        /// <summary>
        /// 运行脚本
        /// </summary>
        /// <param name="code">脚本代码</param>
        /// <param name="nodeCase">节点实例</param>
        /// <param name="err">错误信息</param>
        /// <returns></returns>
        public static bool Run(string code, BLL.EM_SCRIPT_NODE_CASE.Entity nodeCase, ref ErrorInfo err)
        {
            //动态编译
            CompilerResults cr = CompilerClass(code, ref err);
            if (cr == null)
            {
                BLL.EM_SCRIPT_NODE_CASE_LOG.Instance.Add(nodeCase.ID, 3, "节点脚本编译失败：\r\n" + err.Message, code);
                return false;
            }
            BLL.EM_SCRIPT_NODE_CASE_LOG.Instance.Add(nodeCase.ID, 4, "节点脚本编译成功", "");

            //调用必备函数+执行实例代码
            return ExcuteScriptCaseCode(cr, nodeCase, ref err);
        }

        /// <summary>
        /// 动态编译类
        /// </summary>
        /// <param name="code"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        private static CompilerResults CompilerClass(string code, ref ErrorInfo err)
        {
            string pathServer = AppDomain.CurrentDomain.BaseDirectory + "Easyman.ScriptService.exe";

            #region 判断类库是否存在

            if (!System.IO.File.Exists(pathServer))
            {
                pathServer = AppDomain.CurrentDomain.BaseDirectory + "Bin\\Easyman.ScriptService.exe";
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
            objCompilerParameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            //objCompilerParameters.ReferencedAssemblies.Add(AppDomain.CurrentDomain.BaseDirectory + "Easyman.Librarys.dll");
            objCompilerParameters.ReferencedAssemblies.Add(pathServer);

            objCompilerParameters.GenerateExecutable = false;
            objCompilerParameters.GenerateInMemory = true;

            CompilerResults cr = objICodeCompiler.CompileAssemblyFromSource(objCompilerParameters, code);
            if (cr.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder("编译错误：");
                foreach (CompilerError e in cr.Errors)
                {
                    sb.AppendLine(string.Format("行{0}列{1}：{2}\r\n", e.Line - 12, e.Column, e.ErrorText));
                }
                err.IsError = true;
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
        public static bool ExcuteScriptCaseCode(CompilerResults cr, BLL.EM_SCRIPT_NODE_CASE.Entity nodeCase, ref ErrorInfo err)
        {
            // 通过反射,调用函数
            Assembly objAssembly = cr.CompiledAssembly;
            
            object objScripRunner = objAssembly.CreateInstance("Easyman.ScriptService.Script.ScripRunner");

            if (objScripRunner == null)
            {
                err.IsError = true;
                err.Message = "不能创建脚本的运行实例。";
                return false;
            }

            //初始化节点实例相关数据Initialize()
            var initialize = objScripRunner.GetType().GetMethod("SetScriptNodeCaseID").Invoke(objScripRunner, new object[] { nodeCase.ID });

            //设置启动数据库编号
            var setnowdb = objScripRunner.GetType().GetMethod("setnowdbid").Invoke(objScripRunner, new object[] { nodeCase.DB_SERVER_ID });

            //运行脚本
            var run = objScripRunner.GetType().GetMethod("Run").Invoke(objScripRunner, null);

            #region   获取错误信息GetErr()

            //外部job需要接收内部错误信息
            //用于判断重试次数、用于是否再执行
            var errorMsg = objScripRunner.GetType().GetMethod("GetErrorMessage").Invoke(objScripRunner, null);
            if (string.IsNullOrEmpty(errorMsg.ToString()) == false)
            {
                err.IsError = true;
                err.Message = errorMsg.ToString();
                return false;
            } else 
            {
                var warnMsg = objScripRunner.GetType().GetMethod("GetWarnMessage").Invoke(objScripRunner, null);
                if (string.IsNullOrEmpty(warnMsg.ToString()) == false)
                {
                    err.IsError = false;
                    err.Message = warnMsg.ToString();
                    err.IsWarn = true;
                }                
                return true;
            }
                

            #endregion
        }
    }
}
