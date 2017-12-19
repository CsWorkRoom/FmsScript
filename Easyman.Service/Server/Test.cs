using System;
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
                StartNextNodeCase();
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
}
