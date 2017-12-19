using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easyman.Service.Domain;

namespace Easyman.Service.Server
{
    public class ScriptFunManager
    {
        /// <summary>
        /// 获取脚本自定义函数集合
        /// </summary>
        /// <returns></returns>
        public static IList<EM_SCRIPT_FUNCTION> GetFunctionList()
        {
            using (DBEntities db = new DBEntities())
            {
                return db.EM_SCRIPT_FUNCTION.ToList();
            }
        }


        /// <summary>
        /// 将获取配置的扩展函数拼凑为str，以备动态编译
        /// 以后的需要的外部引用都在helper中进行引用
        /// </summary>
        /// <returns></returns>
        public static string GetFunStr()
        {
            string funstr = "";
            var funList = GetFunctionList().Where(p => p.STATUS == (short)PubEnum.ScriptStatus.Open);
            if (funList != null && funList.Count() > 0)
            {
                foreach (var fun in funList)
                {
                    funstr += fun.CONTENT;
                    funstr += "\r\n";
                }
            }
            return funstr;
        }

       
    }
}
