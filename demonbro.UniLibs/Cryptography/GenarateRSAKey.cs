using System;
using System.Security.Cryptography;
using System.Text;

namespace demonbro.UniLibs.Cryptography
{
    internal class GenarateRSAKey
    {
        /// <summary>
        /// 生成RSA密钥对
        /// </summary>
        /// <param name="keySize">密钥长度（1024/2048/4096）</param>
        /// <returns>包含公钥和私钥的元组</returns>
        public static (string publicKey, string privateKey) Generate(int keySize = 2048)
        {
            // 验证密钥长度
            if (keySize != 1024 && keySize != 2048 && keySize != 4096)
                throw new ArgumentException("Invalid key size. Valid sizes: 1024, 2048, 4096");

            using var rsa = RSA.Create(keySize);

            // 导出私钥（PKCS#1格式）
            var privateKeyBytes = rsa.ExportRSAPrivateKey();
            var privateKey = FormatPem(Convert.ToBase64String(privateKeyBytes), "RSA PRIVATE KEY");

            // 导出公钥（PKCS#1格式）
            var publicKeyBytes = rsa.ExportRSAPublicKey();
            var publicKey = FormatPem(Convert.ToBase64String(publicKeyBytes), "PUBLIC KEY");

            return (publicKey, privateKey);
        }

        /// <summary>
        /// 格式化PEM字符串
        /// </summary>
        private static string FormatPem(string keyBase64, string header)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"-----BEGIN {header}-----");

            // 每64个字符换行
            for (var i = 0; i < keyBase64.Length; i += 64)
            {
                var length = Math.Min(64, keyBase64.Length - i);
                sb.AppendLine(keyBase64.Substring(i, length));
            }

            sb.AppendLine($"-----END {header}-----");
            return sb.ToString();
        }
    }
}