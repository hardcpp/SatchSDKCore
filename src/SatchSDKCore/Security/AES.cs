using System;
using System.IO;
using System.Security.Cryptography;

namespace SSC.Security;

/// <summary>
/// AES
/// </summary>
internal class AES
{
    private const int IV_SIZE = 16;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Perform an AES CBC encryption with IV inlined at the begining
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="data">Data</param>
    /// <returns>Encrypted data with the IV inlined at the begining</returns>
    /// <exception cref="ArgumentException"></exception>
    public static byte[] EncryptCBCInlineIV(byte[] key, byte[] data)
    {
        if (key == null || key.Length < 6)
            throw new ArgumentException("Invalid key");

        if (data == null || data.Length < 1)
            throw new ArgumentException("Invalid data");

        try
        {
            byte[] cipherData;
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;

                var cipher = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, cipher, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(data);
                    }

                    cipherData = memoryStream.ToArray();
                }

                var resultBytes = new byte[aes.IV.Length + cipherData.Length];
                Array.Copy(aes.IV, 0, resultBytes, 0, aes.IV.Length);
                Array.Copy(cipherData, 0, resultBytes, aes.IV.Length, cipherData.Length);

                return resultBytes;
            }
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error, "[CP_API_SDK.Security][EasyAES.EasyCBCEncrypt] Error:");
            Logging.Log(ELogSeverity.Error, exception);

            throw;
        }
    }
    /// <summary>
    /// Perform an AES CBC decryption from data with IV inlined at the begining
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="data">Data</param>
    /// <returns>Decrypted data</returns>
    /// <exception cref="ArgumentException"></exception>
    public static byte[] DecryptCBCInlineIV(byte[] key, byte[] data)
    {
        if (key == null || key.Length < 6)
            throw new ArgumentException("Invalid key");

        if (data == null || data.Length < (IV_SIZE + 1))
            throw new ArgumentException("Invalid data");

        try
        {
            var iv = new byte[IV_SIZE];
            var cipherData = new byte[data.Length - IV_SIZE];

            Array.Copy(data, 0, iv, 0, IV_SIZE);
            Array.Copy(data, IV_SIZE, cipherData, 0, cipherData.Length);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(cipherData, 0, cipherData.Length);
                    }

                    return memoryStream.ToArray();
                }
            }
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error, "[CP_API_SDK.Security][EasyAES.EasyCBCDecrypt] Error:");
            Logging.Log(ELogSeverity.Error, exception);

            throw;
        }
    }
}
