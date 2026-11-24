using System;
using System.Security.Cryptography;

namespace Joymart.Models
{
    public static class PasswordHelper
    {
        // Tạo chuỗi Salt ngẫu nhiên 16 byte dạng Base64
        public static string CreateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        // Băm mật khẩu kết hợp với Salt bằng PBKDF2
        public static string Hash(string password, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000))
            {
                byte[] hashBytes = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(hashBytes);
            }
        }

        // Kiểm tra mật khẩu khi đăng nhập
        public static bool Verify(string password, string salt, string storedHash)
        {
            if (string.IsNullOrEmpty(salt) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(password))
            {
                return false;
            }
            try
            {
                string newHash = Hash(password, salt);
                return newHash == storedHash;
            }
            catch
            {
                return false;
            }
        }
    }
}