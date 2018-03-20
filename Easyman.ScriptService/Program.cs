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
