// In Util/EncryptionHelper.cs (or similar shared location)
using System;
using System.IO;
using System.Security.Cryptography;

namespace StellaLearningBackend.Util // Or a more general namespace
{
    public static class EncryptionHelper
    {
        // AES block size in bytes (128 bits)
        private const int AesBlockSize = 16;

        // Encrypts data using AES (CBC mode). Prepends IV to the output.
        public static byte[] EncryptData(byte[] key, byte[] dataToEncrypt)
        {
            if (key == null || (key.Length != 16 && key.Length != 24 && key.Length != 32))
                throw new ArgumentException("Invalid key size for AES.", nameof(key));
            if (dataToEncrypt == null)
                throw new ArgumentNullException(nameof(dataToEncrypt));

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV(); // Generate a unique IV for this encryption operation
            var iv = aes.IV;

            using var memoryStream = new MemoryStream();
            // Prepend the IV to the ciphertext for easy retrieval during decryption
            memoryStream.Write(iv, 0, iv.Length); // IV is always 16 bytes for AES
            using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cryptoStream.Write(dataToEncrypt, 0, dataToEncrypt.Length);
                cryptoStream.FlushFinalBlock(); // Ensure all data is written
            }
            return memoryStream.ToArray();
        }

        // Decrypts data encrypted by EncryptData (AES CBC mode with prepended IV).
        public static byte[] DecryptData(byte[] key, byte[] dataToDecrypt)
        {
            if (key == null || (key.Length != 16 && key.Length != 24 && key.Length != 32))
                throw new ArgumentException("Invalid key size for AES.", nameof(key));
            // Ensure data includes at least IV bytes (16 for AES)
            if (dataToDecrypt == null || dataToDecrypt.Length <= AesBlockSize)
                throw new ArgumentException($"Invalid data length ({dataToDecrypt?.Length ?? 0} bytes) for decryption. Must be > {AesBlockSize} bytes.", nameof(dataToDecrypt));

            using var aes = Aes.Create();
            aes.Key = key;

            // Read the IV from the beginning of the data (first 16 bytes)
            byte[] iv = new byte[AesBlockSize];
            Buffer.BlockCopy(dataToDecrypt, 0, iv, 0, iv.Length);
            aes.IV = iv;

            // Create a MemoryStream for the actual ciphertext (excluding the IV)
            using var ciphertextStream = new MemoryStream(dataToDecrypt, iv.Length, dataToDecrypt.Length - iv.Length);
            using var memoryStream = new MemoryStream(); // Output for decrypted data

            // Decrypt using CryptoStream in Read mode
            using (var cryptoStream = new CryptoStream(ciphertextStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                cryptoStream.CopyTo(memoryStream); // Read decrypted data into the output stream
            }
            return memoryStream.ToArray();
        }
    }
}