using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Easyman.Common.Helper
{
    /// <summary>
    /// 加解密帮助类
    /// </summary>
    public static class EncryptHelper
    {
        /// <summary>
        /// AES秘钥
        /// </summary>
        private const string aesKey = "Easyman-easyman3";

        /// <summary>
        /// AES偏移向量
        /// </summary>
        private const string aesIV = "Easyman-easyman3";

        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="toEncryptString">待加密的明文</param>
        /// <returns></returns>
        public static string AesEncrypt(string toEncryptString)
        {
            var rijndaelCipher = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = 128,
                BlockSize = 128
            };

            var toEncryptBytes = Encoding.UTF8.GetBytes(toEncryptString);
            var keyBytes = Encoding.UTF8.GetBytes(aesKey);
            var ivBytes = Encoding.UTF8.GetBytes(aesIV);

            rijndaelCipher.Key = keyBytes;
            rijndaelCipher.IV = ivBytes;

            ICryptoTransform transform = rijndaelCipher.CreateEncryptor();
            byte[] cipherBytes = transform.TransformFinalBlock(toEncryptBytes, 0, toEncryptBytes.Length);
            return Convert.ToBase64String(cipherBytes);
        }

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="toDecrpt"></param>
        /// <returns></returns>
        public static string AesDecrpt(string toDecrpt)
        {
            var key = Encoding.UTF8.GetBytes(aesKey);
            var encryptedData = Convert.FromBase64String(toDecrpt);
            var iv = Encoding.UTF8.GetBytes(aesIV);

            var rDel = new RijndaelManaged
            {
                Key = key,
                IV = iv,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };

            var cTransform = rDel.CreateDecryptor();
            var resultArray = cTransform.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

            return Encoding.UTF8.GetString(resultArray);
        }
    }
}
