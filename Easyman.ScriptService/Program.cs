using Easyman.Librarys.ApiRequest;
using System;
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
}
