using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace HubPasswordHasher
{
    public enum HashAlgorithmType
    {
        MD5,
        SHA256,
        SHA512
    }

    public class SaltedHash
    {
        // And PEPPER AND MALT VINEGAR!
        public static string GenerateRandomSalt()
        {
            byte[] saltBytes;
            int minSaltSize = 4;
            int maxSaltSize = 8;

            Random random = new Random();
            int saltSize = random.Next(minSaltSize, maxSaltSize);
            saltBytes = new byte[saltSize];

            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetNonZeroBytes(saltBytes);

            return Convert.ToBase64String(saltBytes);
        }

        public static string GenerateHash(HashAlgorithmType type, string plainText)
        {
            return GenerateHash(type, plainText, null);
        }

        public static string GenerateHash(HashAlgorithmType type, string plainText, string salt)
        {
            byte[] saltBytes;

            if (salt == null)
                saltBytes = Encoding.UTF8.GetBytes(GenerateRandomSalt());
            else
                saltBytes = Encoding.UTF8.GetBytes(salt);

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] plainTextWithSaltBytes = new byte[plainTextBytes.Length + saltBytes.Length];

            for (int i = 0; i < plainTextBytes.Length; i++)
                plainTextWithSaltBytes[i] = plainTextBytes[i];

            for (int i = 0; i < saltBytes.Length; i++)
                plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];

            HashAlgorithm hash;
            switch (type)
            {
                case HashAlgorithmType.MD5:
                    hash = new MD5CryptoServiceProvider();
                    break;

                case HashAlgorithmType.SHA256:
                    hash = new SHA256Managed();
                    break;

                case HashAlgorithmType.SHA512:
                    hash = new SHA512Managed();
                    break;
                default:
                    hash = new MD5CryptoServiceProvider();
                    break;
            }

            byte[] hashBytes = hash.ComputeHash(plainTextWithSaltBytes);
            byte[] hashWithSaltBytes = new byte[hashBytes.Length + saltBytes.Length];

            for (int i = 0; i < hashBytes.Length; i++)
                hashWithSaltBytes[i] = hashBytes[i];

            for (int i = 0; i < saltBytes.Length; i++)
                hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];

            string hashValue = Convert.ToBase64String(hashWithSaltBytes);

            return hashValue;
        }

        public static bool VerifyHash(HashAlgorithmType type, string plainText, string salt, string expectedHashValue)
        {
            string hashValue = GenerateHash(type, plainText, salt);
            return (string.Compare(hashValue, expectedHashValue, false) == 0);
        }
    }
}
