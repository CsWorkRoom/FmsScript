using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Librarys.Log;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;
using RemoteAccess;
using System.IO;
using System.Security.Cryptography;
using System.Data;
using Easyman.Librarys.DBHelper;
using Easyman.Librarys.ApiRequest;

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

            CompilerResults cr = null;//初始化
            using (CSharpCodeProvider objCSharpCodePrivoder = new CSharpCodeProvider())
            {
                ICodeCompiler objICodeCompiler = objCSharpCodePrivoder.CreateCompiler();
                CompilerParameters objCompilerParameters = new CompilerParameters();
                objCompilerParameters.ReferencedAssemblies.Add("System.dll");
                objCompilerParameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
                //objCompilerParameters.ReferencedAssemblies.Add(AppDomain.CurrentDomain.BaseDirectory + "Easyman.Librarys.dll");
                objCompilerParameters.ReferencedAssemblies.Add(pathServer);

                objCompilerParameters.GenerateExecutable = false;
                objCompilerParameters.GenerateInMemory = true;

                cr = objICodeCompiler.CompileAssemblyFromSource(objCompilerParameters, code);
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
                objCSharpCodePrivoder.Dispose();//手动释放
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

        #region 20180414新版动态编辑和方法的调用

        public static bool NewRun(string code, BLL.EM_SCRIPT_NODE_CASE.Entity nodeCase, List<KV> monitList, ref ErrorInfo err)
        {
            //动态编译
            string dllName = NewCompilerClass(code, ref err);
            if (string.IsNullOrEmpty(dllName))
            {
                BLL.EM_SCRIPT_NODE_CASE_LOG.Instance.Add(nodeCase.ID, 3, "节点脚本编译失败：\r\n" + err.Message, code);
                return false;
            }
            BLL.EM_SCRIPT_NODE_CASE_LOG.Instance.Add(nodeCase.ID, 4, "节点脚本编译成功:" + dllName, "");

            //调用必备函数+执行实例代码
            return NewExcuteScriptCaseCode(dllName, nodeCase, monitList, ref err);
        }

        /// <summary>
        /// 动态编译类
        /// </summary>
        /// <param name="code"></param>
        /// <param name="err"></param>
        /// <returns></returns>
        private static string NewCompilerClass(string code, ref ErrorInfo err)
        {
            string assemblyName = "";
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

            using (CSharpCodeProvider objCSharpCodePrivoder = new CSharpCodeProvider())
            {
                //生成程序集名(动态)
                string assemblyNameTemp = AppDomain.CurrentDomain.BaseDirectory + "dlls\\DynamicalCode_" + DateTime.Now.Ticks + ".dll";

                //ICodeCompiler objICodeCompiler = objCSharpCodePrivoder.CreateCompiler();
                CompilerParameters objCompilerParameters = new CompilerParameters();
                objCompilerParameters.ReferencedAssemblies.Add("System.dll");
                objCompilerParameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");

                //objCompilerParameters.ReferencedAssemblies.Add(AppDomain.CurrentDomain.BaseDirectory + "Easyman.Librarys.dll");
                objCompilerParameters.ReferencedAssemblies.Add(pathServer);

                //objCompilerParameters.GenerateExecutable = false;
                objCompilerParameters.GenerateInMemory = false;
                objCompilerParameters.OutputAssembly = assemblyNameTemp;

                CompilerResults cr = objCSharpCodePrivoder.CompileAssemblyFromSource(objCompilerParameters, code);
                if (cr.Errors.HasErrors)
                {
                    StringBuilder sb = new StringBuilder("编译错误：");
                    foreach (CompilerError e in cr.Errors)
                    {
                        sb.AppendLine(string.Format("行{0}列{1}：{2}\r\n", e.Line - 12, e.Column, e.ErrorText));
                    }
                    err.IsError = true;
                    err.Message = sb.ToString();

                    //return null;
                }
                else
                {
                    assemblyName = assemblyNameTemp;//赋值返回变量
                }
                objCSharpCodePrivoder.Dispose();//手动释放
            }
            return assemblyName;
        }

        public static bool NewExcuteScriptCaseCode(string dllName, BLL.EM_SCRIPT_NODE_CASE.Entity nodeCase, List<KV> monitList, ref ErrorInfo err)
        {
            bool resBool = true;
           // string dllPath = dllName;
           // AppDomain ad = AppDomain.CurrentDomain;

           // ProxyObject obj = (ProxyObject)ad.CreateInstanceFromAndUnwrap(System.AppDomain.CurrentDomain.BaseDirectory + "Easyman.ScriptService.exe", "Easyman.ScriptService.Script.ProxyObject");
           // obj.LoadAssembly(dllPath);
           // resBool=obj.Invoke("Easyman.ScriptService.Script.ScripRunner", nodeCase.ID, nodeCase.DB_SERVER_ID);
           //// AppDomain.Unload(ad);
            //DoAbsoluteDeleteFile(dllName, ref err);
            #region 原模式

            AppDomain objAppDomain = null;
            IRemoteInterface objRemote = null;
            try
            {
                // 0. Create an addtional AppDomain  
                //AppDomainSetup objSetup = new AppDomainSetup();
                //objSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
                //objAppDomain = AppDomain.CreateDomain("MyAppDomain", null, objSetup);



                objAppDomain = AppDomain.CreateDomain("Domain_"+DateTime.Now.Ticks.ToString());//.CurrentDomain;

                // 4. Invoke the method by using Reflection  
                RemoteLoaderFactory factory = (RemoteLoaderFactory)objAppDomain.CreateInstance("Easyman.ScriptService", "RemoteAccess.RemoteLoaderFactory").Unwrap();

                // with help of factory, create a real 'LiveClass' instance  
                object objObject = factory.Create(dllName, "Easyman.ScriptService.Script.ScripRunner", null);

                if (objObject == null)
                {
                    resBool = false;
                    err.IsError = true;
                    err.Message = "创建实例【Easyman.ScriptService.Script.ScripRunner】失败";
                }

                // *** Cast object to remote interface, avoid loading type info  
                objRemote = (IRemoteInterface)objObject;

                //初始化节点实例相关数据Initialize()
                objRemote.Invoke("SetScriptNodeCaseID", new object[] { nodeCase.ID });
                //设置启动数据库编号
                objRemote.Invoke("setnowdbid", new object[] { nodeCase.DB_SERVER_ID });
                //设置启动数据库编号
                if (monitList != null&&monitList.Count()>0)
                    objRemote.Invoke("SetMonitFileList", new object[] { monitList.Select(p => p.K).ToList() });
                //运行脚本
                objRemote.Invoke("Run", null);

                #region   获取错误信息GetErr()

                //外部job需要接收内部错误信息
                //用于判断重试次数、用于是否再执行
                var errorMsg = (string)objRemote.Invoke("GetErrorMessage", null);

                if (!string.IsNullOrEmpty(errorMsg.ToString()))
                {
                    err.IsError = true;
                    err.Message = errorMsg.ToString();
                    resBool = false;
                }
                else
                {
                    var warnMsg = (string)objRemote.Invoke("GetWarnMessage", null);
                    if (!string.IsNullOrEmpty(warnMsg.ToString()))
                    {
                        err.IsError = false;
                        err.Message = warnMsg.ToString();
                        err.IsWarn = true;
                    }
                }

                ////Dispose the objects and unload the generated DLLs.  
                //objRemote = null;
                //AppDomain.Unload(objAppDomain);
                //System.IO.File.Delete(dllName);

                #endregion
            }
            catch (Exception ex)
            {
                resBool = false;
                err.IsError = true;
                err.Message = ex.Message;
            }
            finally
            {
                //Dispose the objects and unload the generated DLLs.  
                //objRemote = null;
                AppDomain.Unload(objAppDomain);
                // System.IO.File.Delete(dllName);
                DoAbsoluteDeleteFile(dllName,ref err);
            }

            //     BLL.EM_SCRIPT_NODE_CASE_LOG.Instance.Add(nodeCase.ID, 3, "执行错误：\r\n" + err.Message,"");
            #endregion
            return resBool;
        }

        #endregion



        #region 删除生成后的DLL
        public static void DoAbsoluteDeleteFile(object filePath, ref ErrorInfo err)
        {
            try
            {
                string filename = filePath.ToString();

                if (string.IsNullOrEmpty(filename))
                {
                    return;
                }

                if (File.Exists(filename))
                {
                    File.SetAttributes(filename, FileAttributes.Normal);

                    double sectors = Math.Ceiling(new FileInfo(filename).Length / 512.0);

                    byte[] dummyBuffer = new byte[512];

                    RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

                    FileStream inputStream = new FileStream(filename, FileMode.Open);

                    inputStream.Position = 0;

                    for (int sectorsWritten = 0; sectorsWritten < sectors; sectorsWritten++)
                    {
                        rng.GetBytes(dummyBuffer);

                        inputStream.Write(dummyBuffer, 0, dummyBuffer.Length);

                        sectorsWritten++;
                    }

                    inputStream.SetLength(0);

                    inputStream.Close();

                    DateTime dt = new DateTime(2049, 1, 1, 0, 0, 0);

                    File.SetCreationTime(filename, dt);

                    File.SetLastAccessTime(filename, dt);

                    File.SetLastWriteTime(filename, dt);

                    File.Delete(filename);


                }
            }
            catch (Exception e)
            {
                err.Message = e.Message;
                err.IsError = true;
            }
        }

        #endregion
    }
}
