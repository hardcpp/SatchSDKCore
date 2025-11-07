using System;
using System.IO;
using System.Security.Cryptography;

namespace SSC.Security;

/// <summary>
/// AES
/// </summary>
internal class AES
{
    private const int c_IVSize = 16;

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
            byte[] l_CipherData;
            var l_AES = Aes.Create();
            l_AES.Key = key;
            l_AES.GenerateIV();
            l_AES.Mode = CipherMode.CBC;

            var l_Cipher = l_AES.CreateEncryptor(l_AES.Key, l_AES.IV);

            using (MemoryStream l_MemoryStream = new MemoryStream())
            {
                using (CryptoStream l_CryptoStream = new CryptoStream(l_MemoryStream, l_Cipher, CryptoStreamMode.Write))
                {
                    l_CryptoStream.Write(data);
                }

                l_CipherData = l_MemoryStream.ToArray();
            }

            var l_ResultBytes = new byte[l_AES.IV.Length + l_CipherData.Length];
            Array.Copy(l_AES.IV, 0, l_ResultBytes, 0, l_AES.IV.Length);
            Array.Copy(l_CipherData, 0, l_ResultBytes, l_AES.IV.Length, l_CipherData.Length);

            return l_ResultBytes;
        }
        catch (Exception l_Exception)
        {
            Logging.Log(ELogSeverity.Error, $"[CP_API_SDK.Security][EasyAES.EasyCBCEncrypt] Error:");
            Logging.Log(ELogSeverity.Error, l_Exception);

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

        if (data == null || data.Length < (c_IVSize + 1))
            throw new ArgumentException("Invalid data");

        try
        {
            var l_IV = new byte[c_IVSize];
            var l_CipherData = new byte[data.Length - c_IVSize];

            Array.Copy(data, 0, l_IV, 0, c_IVSize);
            Array.Copy(data, c_IVSize, l_CipherData, 0, l_CipherData.Length);

            var l_AES = Aes.Create();
            l_AES.Key = key;
            l_AES.IV = l_IV;
            l_AES.Mode = CipherMode.CBC;

            using (MemoryStream l_MemoryStream = new MemoryStream())
            {
                using (CryptoStream l_CryptoStream = new CryptoStream(l_MemoryStream, l_AES.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    l_CryptoStream.Write(l_CipherData, 0, l_CipherData.Length);
                }

                return l_MemoryStream.ToArray();
            }
        }
        catch (Exception l_Exception)
        {
            Logging.Log(ELogSeverity.Error, $"[CP_API_SDK.Security][EasyAES.EasyCBCDecrypt] Error:");
            Logging.Log(ELogSeverity.Error, l_Exception);

            throw;
        }
    }
}