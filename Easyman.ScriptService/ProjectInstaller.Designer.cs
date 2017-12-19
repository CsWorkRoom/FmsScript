using System.IO;
using System.Text;
using System.Xml;

namespace Easyman.ScriptService
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        /// <summary>
        /// 服务名称
        /// </summary>
        private string _serviceName = "EasymanServiceV3";
        private string _displayName = "EasymanServiceV3";
        private string _description = "联宇创新开发三部数据库脚本服务程序第三版";

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller1
            // 
            this.serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller1.Password = null;
            this.serviceProcessInstaller1.Username = null;

            //加载服务配置信息
            GetServiceInfo();
            this.serviceInstaller1.ServiceName = _serviceName;
            this.serviceInstaller1.DisplayName = _displayName;
            this.serviceInstaller1.Description = _description;

            this.serviceInstaller1.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller1,
            this.serviceInstaller1});

            //更新批处理文件
            UpdateServiceNameInBatFiles("Easyman3ScriptService", _serviceName);
        }

        #endregion

        #region 获取服务名
        /// <summary> 
        /// 通过读xml的方式，读取配置文件.config中的服务名ServiceName
        /// </summary> 
        private void GetServiceInfo()
        {
            string root = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string xmlfile = root + ".config";

            if (File.Exists(xmlfile))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(xmlfile);
                XmlNode appSettings = doc.SelectSingleNode("configuration/appSettings");
                int i = 0;
                foreach (XmlNode node in appSettings.ChildNodes)
                {
                    if (node.Name != "add")
                    {
                        continue;
                    }
                    XmlElement element = (XmlElement)node;
                    string key = element.GetAttribute("key");
                    switch (key)
                    {
                        case "ServiceName":
                            _serviceName = element.GetAttribute("value");
                            i++;
                            break;
                        case "DisplayName":
                            _displayName = element.GetAttribute("value");
                            i++;
                            break;
                        case "Description":
                            _description = element.GetAttribute("value");
                            i++;
                            break;
                    }

                    if (i >= 3)
                    {
                        break;
                    }
                }
                doc = null;
            }
            else
            {
                throw new FileNotFoundException("未能找到服务名称配置文件" + xmlfile);
            }
        }

        #endregion

        #region 更新批处理文件中的服务名
        /// <summary>
        /// 更新批处理文件中的服务名
        /// </summary>
        /// <param name="oldServiceName">原服务名</param>
        /// <param name="newServiceName">新服务名</param>
        private static void UpdateServiceNameInBatFiles(string oldServiceName, string newServiceName)
        {
            string exeFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string path = exeFile.Remove(exeFile.LastIndexOf('\\') + 1);
            string os = "set ServiceName=" + oldServiceName;
            string ns = "set ServiceName=" + newServiceName;
            if (Directory.Exists(path))
            {
                DirectoryInfo di = new DirectoryInfo(path);
                Encoding encoding = Encoding.GetEncoding("GB2312");
                foreach (FileInfo fi in di.GetFiles("*.bat"))
                {
                    if (fi.FullName.Contains("安装"))
                    {
                        continue;
                    }
                    string[] lines = File.ReadAllLines(fi.FullName, encoding);
                    for(int i=0;i<lines.Length;i++)
                    {
                        if (lines[i].StartsWith("set ServiceName="))
                        {
                            lines[i] = "set ServiceName=" + newServiceName;
                        }
                    }

                    File.WriteAllLines(fi.FullName, lines, encoding);
                }
            }
        }
        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;
        private System.ServiceProcess.ServiceInstaller serviceInstaller1;
    }
}