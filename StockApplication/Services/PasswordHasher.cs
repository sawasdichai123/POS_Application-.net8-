// Let's create a PasswordHasher class for secure password management

using System;
using System.Security.Cryptography;
using System.Text;

namespace StockApplication.Services
{
    public class PasswordHasher
    {
        // Size of salt
        private const int SaltSize = 16;
        
        // Size of hash
        private const int HashSize = 20;
        
        // Number of iterations
        private const int Iterations = 10000;
        
        public static string HashPassword(string password)
        {
            // Create salt
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            
            // Create hash
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations);
            var hash = pbkdf2.GetBytes(HashSize);
            
            // Combine salt and hash
            var hashBytes = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, hashBytes, 0, SaltSize);
            Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);
            
            // Convert to base64
            var base64Hash = Convert.ToBase64String(hashBytes);
            
            return base64Hash;
        }
        
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            // Convert base64-encoded hash to byte array
            var hashBytes = Convert.FromBase64String(hashedPassword);
            
            // Get salt from hash
            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);
            
            // Create hash with given salt
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations);
            var hash = pbkdf2.GetBytes(HashSize);
            
            // Compare results
            for (var i = 0; i < HashSize; i++)
            {
                if (hashBytes[i + SaltSize] != hash[i])
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}