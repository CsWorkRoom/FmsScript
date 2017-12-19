using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Easyman.TestForm
{
    public class ClassAsync
    {

        public async Task<string> MethodAsync(DateTime dt)
        {
            await Task.Delay(1000);
            System.Threading.Thread.Sleep(2000);
            string s = "进入时间：" + dt.ToString("yyyy-MM-dd HH:mm:ss.fff") + "返回时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            MessageBox.Show(s.ToString());
            return s;
        }

        public int Test(int i)
        {
            return 1;
        }

        public int Test(int i, bool b = true)
        {
            return b ? 2 : 3;
        }
    }
}
