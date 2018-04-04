using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Easyman.Librarys.ApiRequest
{
    public class Request
    {
        //body是要传递的参数,格式"roleId=1&uid=2"
        //post的cotentType填写:
        //"application/x-www-form-urlencoded"
        //soap填写:"text/xml; charset=utf-8"
        public static string PostHttp(string url, string body, string contentType)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

            httpWebRequest.ContentType = contentType;
            httpWebRequest.Method = "POST";
            //httpWebRequest.Timeout = 20000;

            byte[] btBodys = Encoding.UTF8.GetBytes(body);
            httpWebRequest.ContentLength = btBodys.Length;
            httpWebRequest.GetRequestStream().Write(btBodys, 0, btBodys.Length);

            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
            string responseContent = streamReader.ReadToEnd();

            httpWebResponse.Close();
            streamReader.Close();
            httpWebRequest.Abort();
            httpWebResponse.Close();

            return responseContent;
        }
        //public static string GetHttp(string url, HttpContext httpContext)
        //{
        //    string queryString = "?";

        //    foreach (string key in httpContext.Request.QueryString.AllKeys)
        //    {
        //        queryString += key + "=" + httpContext.Request.QueryString[key] + "&";
        //    }

        //    queryString = queryString.Substring(0, queryString.Length - 1);

        //    HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url + queryString);

        //    httpWebRequest.ContentType = "application/json";
        //    httpWebRequest.Method = "GET";
        //    httpWebRequest.Timeout = 20000;

        //    //byte[] btBodys = Encoding.UTF8.GetBytes(body);
        //    //httpWebRequest.ContentLength = btBodys.Length;
        //    //httpWebRequest.GetRequestStream().Write(btBodys, 0, btBodys.Length);

        //    HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
        //    StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
        //    string responseContent = streamReader.ReadToEnd();

        //    httpWebResponse.Close();
        //    streamReader.Close();

        //    return responseContent;
        //}

        public static string GetHttp(string Url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            request.Timeout = 18000000;//等待5小时

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }

        /// <summary>
        /// 用于检查IP地址或域名是否可以使用TCP/IP协议访问(使用Ping命令),true表示Ping成功,false表示Ping失败 
        /// </summary>
        /// <param name="strIpOrDName">输入参数,表示IP地址或域名</param>
        /// <returns></returns>
        public static bool PingIP(string DoNameOrIP)
        {
            bool res = true;
            using (Ping objPingSender = new Ping())
            {
                try
                {
                    PingOptions objPinOptions = new PingOptions();
                    objPinOptions.DontFragment = true;
                    string data = "";
                    byte[] buffer = Encoding.UTF8.GetBytes(data);
                    int intTimeout = 1000;
                    PingReply objPinReply = objPingSender.Send(DoNameOrIP, intTimeout, buffer, objPinOptions);
                    string strInfo = objPinReply.Status.ToString();
                    if (strInfo == "Success")
                    {
                        //objPingSender.Dispose();//释放资源
                        //return true;
                    }
                    else
                    {
                        res = false; 
                        //return false;
                    }
                }
                catch (Exception)
                {
                    res = false;
                    //return false;
                }
            }
            return res;
        }

        /// <summary>
        /// 复制大文件
        /// </summary>
        /// <param name="fromPath">源文件的路径</param>
        /// <param name="toPath">文件保存的路径</param>
        /// <param name="eachReadLength">每次读取的长度</param>
        /// <returns>是否复制成功</returns>
        public static bool CopyFile(string fromPath, string toPath, int eachReadLength)
        {
            //将源文件 读取成文件流
            FileStream fromFile = new FileStream(fromPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            //已追加的方式 写入文件流          
            FileStream toFile = new FileStream(toPath, FileMode.Append, FileAccess.Write);
            //实际读取的文件长度
            int toCopyLength = 0;
            //如果每次读取的长度小于 源文件的长度 分段读取
            if (eachReadLength < fromFile.Length)
            {
                byte[] buffer = new byte[eachReadLength];
                long copied = 0;
                while (copied <= fromFile.Length - eachReadLength)
                {
                    toCopyLength = fromFile.Read(buffer, 0, eachReadLength);
                    fromFile.Flush();
                    toFile.Write(buffer, 0, eachReadLength);
                    toFile.Flush();
                    //流的当前位置
                    toFile.Position = fromFile.Position;
                    copied += toCopyLength;

                }
                int left = (int)(fromFile.Length - copied);
                toCopyLength = fromFile.Read(buffer, 0, left);
                fromFile.Flush();
                toFile.Write(buffer, 0, left);
                toFile.Flush();

            }
            else
            {
                //如果每次拷贝的文件长度大于源文件的长度 则将实际文件长度直接拷贝
                byte[] buffer = new byte[fromFile.Length];
                fromFile.Read(buffer, 0, buffer.Length);
                fromFile.Flush();
                toFile.Write(buffer, 0, buffer.Length);
                toFile.Flush();

            }
            fromFile.Close();
            toFile.Close();
            return true;
        }
    }

}
