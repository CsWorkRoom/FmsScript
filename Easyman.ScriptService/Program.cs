using Easyman.Librarys.ApiRequest;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.ScriptService
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main()
        {
            //Script.Base bs = new Script.Base();
            //bs.CopyFileToServer();
            //Request.PostHttp("http://localhost:6235/File/GetFileListByFolder", "folderId=21", "application/x-www-form-urlencoded");
            //Request.GetHttp("http://localhost:6235/api/services/api/MonitFile/UpFileByMonitFile", "monitFileId=53");
#if DEBUG
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.Run(new TestForm());
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new EasymanScriptService()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }

        static void testapi()
        {
            Request.GetHttp("http://localhost:6235/api/services/api/MonitFile/UpFileByMonitFile", "monitFileId=53");
        }
    }

    public class global
    {
        ///// <summary>
        ///// 待上传的文件集合列表
        ///// </summary>
        //public static List<long> list = new List<long>();

        /// <summary>
        /// 待监控文件列表
        /// </summary>
        public static List<KV> monitKVList = new List<KV>();

        /// <summary>
        /// 未在线ip
        /// </summary>
        public static List<KV> ipNotList = new List<KV>();

        /// <summary>
        /// 对不在线终端操作
        /// </summary>
        /// <param name="opType"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public static List<KV> OpIpNotList(string opType, KV info=null)
        {
            lock (ipNotList)
            {
                if (opType == "add")
                {
                    if (!ipNotList.Exists(e => e.K == info.K))                    
                    {
                        ipNotList.Add(info);
                    }
                    return ipNotList;

                }
                else if (opType == "remove")
                {
                    if (ipNotList.Exists(e => e.K == info.K))
                    {                        
                        ipNotList.Remove(info);
                    }
                    return ipNotList;
                }
                else if (opType == "getall")
                {
                    return ipNotList;
                }
                else if (opType == "get")
                {                   
                        return null;
                }
                else
                {
                    return ipNotList;
                }
            }
        }
        /// <summary>
        /// 获取未在线ip数量
        /// </summary>
        /// <returns></returns>
        public static int GetIpNotCount()
        {
            lock (ipNotList)
            {
                if (ipNotList != null && ipNotList.Count > 0)
                {
                    return ipNotList.Count;
                }
                else return 0;
            }
        }
        /// <summary>
        /// 对文件上传列表进行操作
        /// </summary>
        /// <param name="opType"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public static List<KV> OpMonitKVList(string opType, KV info=null,int num=5,List<KV> monitList=null)
        {
            lock (monitKVList)
            {
                if (opType == "add")
                {
                    if (!monitKVList.Exists(e => e.K == info.K))
                    {
                        monitKVList.Add(info);
                    }
                    return monitKVList;

                }
                else if (opType == "remove")
                {
                    if (monitList != null && monitList.Count > 0)
                    {
                        foreach (KV kv in monitList)
                        {
                            if (monitKVList.Exists(e => e.K == kv.K))
                            {
                                monitKVList.Remove(info);
                            }
                        }                           
                    }                   
                    return monitKVList;
                }
                else if (opType == "getall")
                {
                    return monitKVList;
                }
                else if (opType == "take")
                {
                    var  kvLs = global.monitKVList.Where(e=>e.Status!=5).Take(num).ToList();

                    foreach (KV kv in kvLs)
                    {
                        int index = monitKVList.FindIndex(m => m == kv);
                        monitKVList[index].Status = 5;                       
                    }
                   // monitKVList.RemoveAll(p => kvLs.Exists(e => e.K == p.K));//从内存中移除
                    return kvLs;
                }
                else
                {
                    return monitKVList;
                }
            }
        }
        /// <summary>
        /// 获取待监控文件数量
        /// </summary>
        /// <returns></returns>
        public static int GetMonitKVCount()
        {
            lock (monitKVList)
            {
                if (monitKVList != null && monitKVList.Count > 0)
                {
                    return monitKVList.Count;
                }
                else return 0;
            }
        }

        //public static List<string> ipList = new List<string>();

        ///// <summary>
        ///// 未在线的ip集合（需要定期做验证）
        ///// </summary>
        //public static Dictionary<long, string> ipList = new Dictionary<long, string>();

        ///// <summary>
        ///// 待上传的监控文件id集合
        ///// key：monitFileId, value：ip
        ///// </summary>
        //public static Dictionary<long, string> monitFileIdList = new Dictionary<long, string>();


    }
}
