// In Util/EncryptionHelper.cs (or similar shared location)
using System.Security.Cryptography;

namespace AyanamisTower.WebAPI.Util // Or a more general namespace
{
    /// <summary>
    /// Provides helper methods for AES encryption and decryption.
    /// </summary>
    public static class EncryptionHelper
    {
        // AES block size in bytes (128 bits)
        private const int AesBlockSize = 16;

        /// <summary>
        /// Encrypts the provided data using AES (CBC mode) with the specified key.
        /// The Initialization Vector (IV) is generated uniquely for each operation and prepended to the resulting ciphertext.
        /// </summary>
        /// <param name="key">The secret key for AES encryption. Must be 16, 24, or 32 bytes long (corresponding to AES-128, AES-192, or AES-256).</param>
        /// <param name="dataToEncrypt">The byte array containing the data to be encrypted.</param>
        /// <returns>A byte array containing the 16-byte IV followed by the encrypted ciphertext.</returns>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="key"/> is null or has an invalid size (not 16, 24, or 32 bytes).</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dataToEncrypt"/> is null.</exception>
        public static byte[] EncryptData(byte[] key, byte[] dataToEncrypt)
        {
            if (key == null || (key.Length != 16 && key.Length != 24 && key.Length != 32))
                throw new ArgumentException("Invalid key size for AES.", nameof(key));
            ArgumentNullException.ThrowIfNull(dataToEncrypt);

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

        /// <summary>
        /// Decrypts the specified data using AES with the provided key.
        /// Assumes the data was encrypted using <see cref="EncryptData"/> and includes a 16-byte IV prepended to the ciphertext.
        /// </summary>
        /// <param name="key">The secret key used for encryption. Must be 16, 24, or 32 bytes long.</param>
        /// <param name="dataToDecrypt">The data to decrypt, which must include the 16-byte IV prepended to the actual ciphertext.</param>
        /// <returns>A byte array containing the original decrypted data.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dataToDecrypt"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the <paramref name="key"/> is null or has an invalid size (not 16, 24, or 32 bytes).
        /// Thrown if <paramref name="dataToDecrypt"/> is not long enough to contain the IV (i.e., length is less than or equal to 16 bytes).
        /// </exception>
        /// <remarks>
        /// This method extracts the IV from the first 16 bytes of <paramref name="dataToDecrypt"/>.
        /// </remarks>
        public static byte[] DecryptData(byte[] key, byte[] dataToDecrypt)
        {
            if (key == null || (key.Length != 16 && key.Length != 24 && key.Length != 32))
                if (key == null || (key.Length != 16 && key.Length != 24 && key.Length != 32))
                    throw new ArgumentException("Invalid key size for AES.", nameof(key));
            // Ensure data includes at least IV bytes (16 for AES)
            ArgumentNullException.ThrowIfNull(dataToDecrypt);
            if (dataToDecrypt.Length <= AesBlockSize)
                throw new ArgumentException($"Invalid data length ({dataToDecrypt.Length} bytes) for decryption. Must be > {AesBlockSize} bytes.", nameof(dataToDecrypt));
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