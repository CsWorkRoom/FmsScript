using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.ScriptService.BLL.Computer
{
    public class FileTool
    {


        /// <summary>

        /// 获取文件属性字典

        /// </summary>

        /// <param name="filePath">文件路径</param>

        /// <returns>属性字典</returns>

        public static List<FileProperty> GetProperties(string mid,string ticks,string md5, string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("指定的文件不存在。", filePath);
            }
            FileInfo dicInfo = new FileInfo(filePath);
            
            //Dictionary<string, string> Properties = new Dictionary<string, string>();
            List<FileProperty> pros = new List<FileProperty>();

            #region 原有属性获取方式
            //PropertyInfo[] propertys = info.GetType().GetProperties();
            //foreach (PropertyInfo item in propertys)
            //{
            //    object svalue = item.GetValue(info, null);//用pi.GetValue获得值
            //    string tvalue = svalue != null ? svalue.ToString() : "";
            //  //  Properties.Add(item.Name, tvalue);

            //    FileProperty pro = new FileProperty();
            //    pro.Id = mid;
            //    pro.MD5 = md5;
            //    pro.PName = item.Name;
            //    pro.PValue = tvalue;
            //    pro.Ticks = ticks;
            //    pros.Add(pro);
            //}
            #endregion

            #region 现有属性获取方式
            FileProperty pro1 = GetEntity(mid, ticks, md5, "Attributes", dicInfo.Attributes.ToString());
            pros.Add(pro1);
            FileProperty pro2 = GetEntity(mid, ticks, md5, "Name", dicInfo.Name.ToString());
            pros.Add(pro2);
            FileProperty pro3 = GetEntity(mid, ticks, md5, "Directory", dicInfo.Directory.ToString());
            pros.Add(pro3);
            FileProperty pro4 = GetEntity(mid, ticks, md5, "LastAccessTime", dicInfo.LastAccessTime.ToString());
            pros.Add(pro4);
            FileProperty pro5 = GetEntity(mid, ticks, md5, "LastWriteTime", dicInfo.LastWriteTime.ToString());
            pros.Add(pro5);
            FileProperty pro6 = GetEntity(mid, ticks, md5, "Length", dicInfo.Length.ToString());
            pros.Add(pro6);
            FileProperty pro7 = GetEntity(mid, ticks, md5, "CreationTime", dicInfo.CreationTime.ToString());
            pros.Add(pro7);
            #endregion
            return pros;
        }
        ///// <summary>
        ///// 存储属性名与其下标（key值均为小写）
        ///// </summary>
        //private static Dictionary<string, int> _propertyIndex = null;
        ///// <summary>
        ///// /// 获取指定文件指定下标的属性值
        ///// </summary>
        ///// <param name="filePath">文件路径</param>
        ///// <param name="index">属性下标</param>
        ///// <returns>属性值</returns>
        //public static string GetPropertyByIndex(string filePath, int index)
        //{
        //    if (!File.Exists(filePath))
        //    {
        //        throw new FileNotFoundException("指定的文件不存在。", filePath);
        //    }
        //    //初始化Shell接口 
        //    Shell32.Shell shell = new Shell32.ShellClass();
        //    //获取文件所在父目录对象 
        //    Folder folder = shell.NameSpace(Path.GetDirectoryName(filePath));
        //    //获取文件对应的FolderItem对象 
        //    FolderItem item = folder.ParseName(Path.GetFileName(filePath));
        //    string value = null;
        //    //获取属性名称 
        //    string key = folder.GetDetailsOf(null, index);
        //    if (false == string.IsNullOrEmpty(key))
        //    {
        //        //获取属性值 
        //        value = folder.GetDetailsOf(item, index);
        //    }
        //    return value;
        //}

        ///// <summary>
        ///// 获取指定文件指定属性名的值
        ///// </summary>
        ///// <param name="filePath">文件路径</param>
        //        /// <param name="propertyName">属性名</param>
        ///// <returns>属性值</returns>
        //public static string GetPropertyEx(string filePath, string propertyName)
        //{
        //    if (_propertyIndex == null)
        //    {
        //        InitPropertyIndex();
        //    }
        //    //转换为小写
        //    string propertyNameLow = propertyName.ToLower();
        //    if (_propertyIndex.ContainsKey(propertyNameLow))
        //    {
        //        int index = _propertyIndex[propertyNameLow];
        //                        return GetPropertyByIndex(filePath, index);
        //    }
        //    return null;
        //}       

        ///// <summary>
        ///// 初始化属性名的下标
        ///// </summary>
        //private static void InitPropertyIndex()
        //{
        //    Dictionary<string, int> propertyIndex = new Dictionary<string, int>();
        //    //获取本代码所在的文件作为临时文件，用于获取属性列表
        //    string tempFile = System.Reflection.Assembly.GetExecutingAssembly().FullName;

        //    Dictionary<string, string> allProperty = GetProperties(tempFile);
        //    if (allProperty != null)
        //    {
        //        int index = 0;
        //        foreach (var item in allProperty.Keys)
        //        {
        //            //属性名统一转换为小写，用于忽略大小写
        //            _propertyIndex.Add(item.ToLower(), index);
        //            index++;
        //        }
        //    }
        //    _propertyIndex = propertyIndex;
        //}


        /// <summary>
        /// 获取文件夹属性
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        public static List<FileProperty> GetDictionaryByDir(string mid, string ticks, string md5, string filePath)
        {
            List<FileProperty> pros = new List<FileProperty>();
            Dictionary<string, string> Properties = new Dictionary<string, string>();
            var dicInfo = new DirectoryInfo(filePath);//获取的目录信息 

            FileProperty pro = GetEntity(mid, ticks, md5, "CreationTime", dicInfo.CreationTime.ToString());
            pros.Add(pro);
            FileProperty pro1 = GetEntity(mid, ticks, md5, "Extension", dicInfo.Extension.ToString());
            pros.Add(pro1);
            FileProperty pro2 = GetEntity(mid, ticks, md5, "FullName", dicInfo.FullName.ToString());
            pros.Add(pro2);
            //FileProperty pro3 = GetEntity(mid, ticks, md5, "LastAccessTime", dicInfo.LastAccessTime.ToString());
            //pros.Add(pro3);
            FileProperty pro4 = GetEntity(mid, ticks, md5, "LastWriteTime", dicInfo.LastWriteTime.ToString());
            pros.Add(pro4);
            FileProperty pro5 = GetEntity(mid, ticks, md5, "Name", dicInfo.Name.ToString());
            pros.Add(pro5);
            //FileProperty pro6 = GetEntity(mid, ticks, md5, "Root", dicInfo.Root.ToString());
            //pros.Add(pro6);
            return pros;
        }

        private static FileProperty GetEntity(string mid, string ticks, string md5,string pname,string pvalue)
        {
            FileProperty pro = new FileProperty();
            pro.Id = mid;
            pro.MD5 = md5;
            pro.PName = pname;
            pro.PValue = pvalue;
            pro.Ticks = ticks;
            return pro;
        }

        /// <summary>
        /// 计算文件的hash值 用于比较两个文件是否相同
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件hash值</returns>
        public static string GetFileHash(string filePath)
        {
            try
            {
                FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }

        public static string GetFileMd5(string filePath)
        {
            try
            {
                FileInfo info = new FileInfo(filePath);
                string size = info.Length.ToString();
                string lasttime = info.LastWriteTime.ToString();
                byte[] fromData = System.Text.Encoding.Unicode.GetBytes(filePath + "_" + size + "_" + lasttime);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(fromData);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }
    }
}
