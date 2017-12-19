using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.ScriptService
{
    public partial class EasymanScriptService : ServiceBase
    {
        public EasymanScriptService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Main.Start();
        }

        protected override void OnStop()
        {
            Main.Stop();
        }
    }
}
