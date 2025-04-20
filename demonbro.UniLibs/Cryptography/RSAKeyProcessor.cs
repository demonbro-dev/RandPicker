using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace demonbro.UniLibs.Cryptography
{
    public class RSAKeyProcessor
    {
        /// <summary>
        /// 从PEM格式解码Base64内容
        /// </summary>
        private static byte[] DecodePem(string pem, string header)
        {
            // 移除PEM头尾标记和换行符
            var base64 = Regex.Replace(pem,
                $"-----BEGIN {header}-----|-----END {header}-----|\\n",
                "");

            return Convert.FromBase64String(base64);
        }

        /// <summary>
        /// 加载PEM格式的RSA公钥
        /// </summary>
        public static RSA LoadPublicKey(string publicKeyPem)
        {
            var rsa = RSA.Create();
            var publicKeyBytes = DecodePem(publicKeyPem, "PUBLIC KEY");
            rsa.ImportRSAPublicKey(publicKeyBytes, out _);
            return rsa;
        }

        /// <summary>
        /// 加载PEM格式的RSA私钥
        /// </summary>
        public static RSA LoadPrivateKey(string privateKeyPem)
        {
            var rsa = RSA.Create();
            var privateKeyBytes = DecodePem(privateKeyPem, "RSA PRIVATE KEY");
            rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
            return rsa;
        }

        /// <summary>
        /// 使用私钥解密数据
        /// </summary>
        /// <param name="encryptedData">Base64格式的加密数据</param>
        /// <param name="privateKeyPem">PEM格式私钥</param>
        /// <param name="padding">填充模式（默认PKCS#1）</param>
        public static string Decrypt(string encryptedData, string privateKeyPem, RSAEncryptionPadding padding = null)
        {
            using var rsa = LoadPrivateKey(privateKeyPem);
            var dataBytes = Convert.FromBase64String(encryptedData);
            var result = rsa.Decrypt(dataBytes, padding ?? RSAEncryptionPadding.Pkcs1);
            return Encoding.UTF8.GetString(result);
        }

        /// <summary>
        /// 字节数组版本解密
        /// </summary>
        public static byte[] Decrypt(byte[] encryptedData, string privateKeyPem, RSAEncryptionPadding padding = null)
        {
            using var rsa = LoadPrivateKey(privateKeyPem);
            return rsa.Decrypt(encryptedData, padding ?? RSAEncryptionPadding.Pkcs1);
        }

        /// <summary>
        /// 使用RSA公钥/私钥PEM加密内容
        /// </summary>
        /// <param name="plainText">明文内容</param>
        /// <param name="keyPem">PEM格式的公钥或私钥</param>
        /// <param name="padding">填充模式（默认PKCS#1）</param>
        public static string Encrypt(string plainText, string keyPem, RSAEncryptionPadding padding = null)
        {
            using var rsa = LoadKeyAuto(keyPem);
            var dataBytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = rsa.Encrypt(dataBytes, padding ?? RSAEncryptionPadding.Pkcs1);
            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// 字节数组版本加密
        /// </summary>
        public static byte[] Encrypt(byte[] plainData, string keyPem, RSAEncryptionPadding padding = null)
        {
            using var rsa = LoadKeyAuto(keyPem);
            return rsa.Encrypt(plainData, padding ?? RSAEncryptionPadding.Pkcs1);
        }

        /// <summary>
        /// 自动识别PEM类型加载密钥
        /// </summary>
        private static RSA LoadKeyAuto(string keyPem)
        {
            if (keyPem.Contains("PRIVATE KEY"))
            {
                var rsa = LoadPrivateKey(keyPem);
                // 通过尝试导出公钥验证私钥有效性
                try
                {
                    // 如果私钥没有公钥参数，此操作会抛出异常
                    rsa.ExportRSAPublicKey();
                    return rsa;
                }
                catch (CryptographicException)
                {
                    rsa.Dispose();
                    throw new CryptographicException("私钥不包含有效的公钥参数");
                }
            }

            if (keyPem.Contains("PUBLIC KEY"))
            {
                return LoadPublicKey(keyPem);
            }

            throw new ArgumentException("无法识别的PEM类型，应包含'PUBLIC KEY'或'PRIVATE KEY'");
        }
    }
}