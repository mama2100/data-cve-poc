using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.IO;

namespace UpSkope
{
    internal class Crypto
    {
        private readonly byte[] key;
        private readonly byte[] iv;

        public Crypto()
        {
            // Get key from device ID
            string deviceId = DeviceId.GenerateDeviceUid();
            key = Encoding.UTF8.GetBytes(deviceId)
                .Take(32)
                .ToArray();

            // Get IV from ProductID
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false))
            {
                string productId = key?.GetValue("ProductID") as string ?? string.Empty;
                iv = Encoding.UTF8.GetBytes(productId)
                    .Take(16)
                    .ToArray();
            }
        }

        public string EncryptString(string input)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(input);
                    }

                    byte[] encrypted = msEncrypt.ToArray();
                    string base64 = Convert.ToBase64String(encrypted);
                    return base64;
                }
            }
        }

        public string DecryptString(string input)
        {
            byte[] cipherText = Convert.FromBase64String(input);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var msDecrypt = new MemoryStream(cipherText))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
    }
}