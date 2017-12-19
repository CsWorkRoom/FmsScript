using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Easyman.Librarys.Cache
{
    public class BCache
    {
        /// <summary>
        /// 添加缓存 默认 按绝对过期时间存储
        /// </summary>
        /// <param name="cacheKey">键值</param>
        /// <param name="data">数据</param>
        /// <param name="timeOut">超时时间,单位：分</param>
        /// <param name="isAbsolute">是否按绝对时间缓存:超过多少秒后直接过期、相对时间：超过多少时间不调用就失效，默认按绝对时间缓存</param>
        public static void Add(string cacheKey, object data, int timeOut, bool isAbsolute = true)
        {
            timeOut = Math.Max(1, timeOut);

            if (isAbsolute != false)
            {
                HttpRuntime.Cache.Insert(cacheKey, data, null, DateTime.Now.AddMinutes(timeOut), TimeSpan.Zero);
            }
            else
            {
                HttpRuntime.Cache.Insert(cacheKey, data, null, System.Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(timeOut));
            }
        }

        /// <summary>
        /// 删除缓存数据
        /// </summary>
        /// <param name="cacheKey">键值</param>
        public static void Remove(string cacheKey)
        {
            HttpRuntime.Cache.Remove(cacheKey);
        }

        /// <summary>
        /// 获取缓存数据
        /// </summary>
        /// <param name="cacheKey">键值</param>
        /// <returns>找到则返回 object数据,否则为null </returns>
        public static object Get(string cacheKey)
        {
            return HttpRuntime.Cache.Get(cacheKey);
        }
    }
}
