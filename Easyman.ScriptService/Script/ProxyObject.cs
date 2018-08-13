using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.ScriptService.Script
{
    /// <summary>
    /// 
    /// </summary>
    /// 
    [Serializable]
    public class ProxyObject : MarshalByRefObject
    {
        Assembly assembly = null;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dllName"></param>
        /// <returns></returns>
        public void LoadAssembly(string dllName)
        {
             byte[] file = File.ReadAllBytes(dllName);
            assembly = Assembly.Load(file); Assembly.LoadFrom(dllName);
            // file = null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullClassName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool Invoke(string fullClassName,  object caseID,  object dbID)
        {
            if (assembly == null)
                return false;
            Type tp = assembly.GetType(fullClassName);
            if (tp == null)
                return false;
            Object obj = Activator.CreateInstance(tp);
            //初始化节点实例相关数据Initialize()
            MethodInfo methodInit = tp.GetMethod("SetScriptNodeCaseID");
            methodInit.Invoke(obj,new object[] { caseID } );
            //设置启动数据库编号
            MethodInfo methodDb = tp.GetMethod("setnowdbid");
            methodDb.Invoke(obj, new object[] { dbID });
            //运行脚本
            MethodInfo methodRun = tp.GetMethod("Run");
            methodRun.Invoke(obj, null);
            return true;
        }
    }
}

