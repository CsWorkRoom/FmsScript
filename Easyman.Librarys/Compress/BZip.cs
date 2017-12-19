using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.Librarys.Compress
{
    /// <summary>
    /// ZIP压缩与解压
    /// </summary>
    public static class BZip
    {
        /// <summary>
        /// 解压zip文件到指定路径
        /// </summary>
        /// <param name="zipFileName">zip文件</param>
        /// <param name="dstFileName">解压路径</param>
        /// <returns></returns>
        public static bool UnZip(string zipFileName, string dstFileName)
        {
            if (!File.Exists(zipFileName))
            {
                return false;
            }
            ZipEntry theEntry;
            ZipInputStream s = new ZipInputStream(File.OpenRead(zipFileName));
            while ((theEntry = s.GetNextEntry()) != null)
            {
                string directoryName = dstFileName;//Path.GetDirectoryName(dstFileName);
                directoryName += Path.GetDirectoryName(theEntry.Name);
                string fileName = Path.GetFileName(theEntry.Name);
                //生成解压目录
                Directory.CreateDirectory(directoryName);
                if (fileName != String.Empty)
                {
                    //解压文件到指定的目录
                    FileStream streamWriter = File.Create(dstFileName + theEntry.Name);
                    int size = 2048;
                    byte[] data = new byte[2048];
                    while (true)
                    {
                        size = s.Read(data, 0, data.Length);
                        if (size > 0)
                        {
                            streamWriter.Write(data, 0, size);
                        }
                        else
                        {
                            break;
                        }
                    }
                    streamWriter.Close();
                }
            }
            s.Close();
            return true;
        }
        /// <summary>
        /// 取zip文件中的指定文件
        /// Android *.apk AndroidManifest.xml
        /// iOS *.ipa iTunesMetadata.plist
        /// WP *.* AppManifest.xaml
        /// </summary>
        /// <param name="zipFileName">zip文件</param>
        /// <param name="innerFileName">需要取的文件名</param>
        /// <param name="fuzzySame">模糊比较文件名</param>
        /// <returns></returns>
        public static byte[] ReadInnerFileBytes(string zipFileName, string innerFileName, bool fuzzySame)
        {
            if (!File.Exists(zipFileName))
            {
                return null;
            }

            innerFileName = innerFileName.ToLower();
            using (ZipInputStream s = new ZipInputStream(File.OpenRead(zipFileName)))
            {
                ZipEntry entry;//AndroidManifest.xml
                while ((entry = s.GetNextEntry()) != null)
                {
                    string srcName = entry.Name.ToLower();

                    if (entry.Name == innerFileName || fuzzySame && srcName.IndexOf(innerFileName) >= 0)
                    {
                        List<byte> dyns = null;
                        byte[] buff = new byte[10240];
                        bool isFirst = true;

                        while (true)
                        {
                            int size = s.Read(buff, 0, 10240);
                            if (size > 0)
                            {
                                if (isFirst && size < 10240)
                                {
                                    byte[] rr = new byte[size];
                                    Array.Copy(buff, rr, size);
                                    return rr;
                                }
                                isFirst = false;
                                if (dyns == null)
                                    dyns = new List<byte>(10240 * 2);
                                if (size == 10240)
                                    dyns.AddRange(buff);
                                else
                                {
                                    for (int i = 0; i < size; i++)
                                        dyns.Add(buff[i]);
                                }
                            }
                            else
                                break;
                        }

                        return dyns != null ? dyns.ToArray() : null;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 压缩文件
        /// </summary>
        /// <param name="sourceFile">待压缩文件</param>
        /// <param name="destinationFile">目标文件</param>
        public static void CompressFile(string sourceFile, string destinationFile)
        {
            if (!File.Exists(sourceFile))
                throw new FileNotFoundException();

            FileStream sourceStream = null;
            FileStream destinationStream = null;
            GZipStream compressedStream = null;
            try
            {
                // Read the bytes from the source file into a byte array
                sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);

                // Read the source stream values into the buffer
                byte[] buffer = new byte[sourceStream.Length];
                int checkCounter = sourceStream.Read(buffer, 0, buffer.Length);

                if (checkCounter != buffer.Length)
                    throw new ApplicationException();

                // Open the FileStream to write to
                destinationStream = new FileStream(destinationFile, FileMode.OpenOrCreate, FileAccess.Write);

                // Create a compression stream pointing to the destiantion stream
                compressedStream = new GZipStream(destinationStream, CompressionMode.Compress, true);

                // Now write the compressed data to the destination file
                compressedStream.Write(buffer, 0, buffer.Length);
            }
            finally
            {
                // Make sure we allways close all streams
                if (sourceStream != null)
                    sourceStream.Close();

                if (compressedStream != null)
                    compressedStream.Close();

                if (destinationStream != null)
                    destinationStream.Close();
            }
        }

        /// <summary>
        /// 解压缩文件
        /// </summary>
        /// <param name="sourceFile">已压缩文件</param>
        /// <param name="destinationFile">目标文件</param>
        public static void DecompressFile(string sourceFile, string destinationFile)
        {
            if (!File.Exists(sourceFile))
                throw new FileNotFoundException();

            // Create the streams and byte arrays needed
            FileStream sourceStream = null;
            FileStream destinationStream = null;
            GZipStream decompressedStream = null;

            try
            {
                // Read in the compressed source stream
                sourceStream = new FileStream(sourceFile, FileMode.Open);

                // Create a compression stream pointing to the destiantion stream
                decompressedStream = new GZipStream(sourceStream, CompressionMode.Decompress, true);

                // Read the footer to determine the length of the destiantion file
                byte[] quartetBuffer = new byte[4];
                int position = (int)sourceStream.Length - 4;
                sourceStream.Position = position;
                sourceStream.Read(quartetBuffer, 0, 4);
                sourceStream.Position = 0;
                int checkLength = BitConverter.ToInt32(quartetBuffer, 0);

                byte[] buffer = new byte[checkLength + 100];

                int offset = 0;
                int total = 0;

                // Read the compressed data into the buffer
                while (true)
                {
                    int bytesRead = decompressedStream.Read(buffer, offset, 100);

                    if (bytesRead == 0)
                        break;

                    offset += bytesRead;
                    total += bytesRead;
                }

                // Now write everything to the destination file
                destinationStream = new FileStream(destinationFile, FileMode.Create);
                destinationStream.Write(buffer, 0, total);

                // and flush everyhting to clean out the buffer
                destinationStream.Flush();
            }
            finally
            {
                // Make sure we allways close all streams
                if (sourceStream != null)
                    sourceStream.Close();

                if (decompressedStream != null)
                    decompressedStream.Close();

                if (destinationStream != null)
                    destinationStream.Close();
            }

        }

        /// <summary>
        /// 压缩字符串
        /// </summary>
        public static byte[] CompressString(string source)
        {
            if (string.IsNullOrEmpty(source))
                return null;
            if (source.Length > 128)
            {
                MemoryStream destinationStream = new MemoryStream();
                using (GZipStream gzip = new GZipStream(destinationStream, CompressionMode.Compress))
                {
                    byte[] buf = Encoding.UTF8.GetBytes(source);
                    gzip.Write(buf, 0, buf.Length);
                    gzip.Flush();
                }
                return destinationStream.ToArray();
            }
            else
            {
                byte[] temp = Encoding.UTF8.GetBytes(source);
                byte[] result = new byte[temp.Length + 4];
                result[0] = 0x0;
                result[1] = 0x0;
                result[2] = 0x0;
                result[3] = 0x0;
                for (int i = 0; i < temp.Length; i++)
                    result[i + 4] = temp[i];
                return result;
            }
        }
        /// <summary>
        /// 解压字符串
        /// </summary>
        public static string DecompressString(byte[] source)
        {
            if (source == null || source.Length == 0)
                return string.Empty;
            if (source.Length < 4)
                throw new NotImplementedException();
            if (source[0] == 0 && source[1] == 0 && source[2] == 0 && source[3] == 0)
                return Encoding.UTF8.GetString(source, 4, source.Length - 4);
            GZipStream gzip = new GZipStream(new MemoryStream(source), CompressionMode.Decompress);
            using (StreamReader reader = new StreamReader(gzip))
            {
                return reader.ReadToEnd();
            }
        }

        public static bool ZipFileMain(string zipDir, string zipFile)
        {
            string[] filenames = Directory.GetFiles(zipDir, "*.*", SearchOption.AllDirectories);

            Crc32 crc = new Crc32();
            ZipOutputStream s = new ZipOutputStream(File.Create(zipFile));

            s.SetLevel(6); // 0 - store only to 9 - means best compression

            foreach (string file in filenames)
            {
                //打开压缩文件
                FileStream fs = File.OpenRead(file);

                byte[] buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
                ZipEntry entry = new ZipEntry(file.Replace(zipDir, ""));
                entry.DateTime = DateTime.Now;
                entry.Size = fs.Length;
                fs.Close();

                crc.Reset();
                crc.Update(buffer);

                entry.Crc = crc.Value;

                s.PutNextEntry(entry);

                s.Write(buffer, 0, buffer.Length);
            }
            s.Finish();
            s.Close();
            return true;
        }

        /// <summary>
        /// 压文件列表
        /// </summary>
        /// <param name="pathList">文件列表</param>
        /// <param name="zipFile">压文件</param>
        /// <returns></returns>
        public static bool ZipFileList(Dictionary<string, string> pathList, string zipFile)
        {

            Crc32 crc = new Crc32();
            ZipOutputStream s = new ZipOutputStream(File.Create(zipFile));

            s.SetLevel(6); // 0 - store only to 9 - means best compression

            foreach (var file in pathList)
            {
                if (!File.Exists(file.Key))
                {
                    continue;
                }

                //打开压缩文件
                FileStream fs = File.OpenRead(file.Key);

                byte[] buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
                ZipEntry entry = new ZipEntry(file.Value);
                entry.DateTime = DateTime.Now;
                entry.Size = fs.Length;
                fs.Close();

                crc.Reset();
                crc.Update(buffer);

                entry.Crc = crc.Value;

                s.PutNextEntry(entry);

                s.Write(buffer, 0, buffer.Length);
            }
            s.Finish();
            s.Close();
            return true;
        }
    }
}
