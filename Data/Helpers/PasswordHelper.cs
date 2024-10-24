using System.Security.Cryptography;
using System.Text;

namespace Data.Helpers
{
    public class PasswordHelper
    {
        public static string GenerateSelt()
        {
            byte[] saltBytes = new byte[16];
            using (var provider = new RNGCryptoServiceProvider())
            {
                provider.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string password, string salt)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(salt)))
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashedBytes = hmac.ComputeHash(passwordBytes);
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
        {
            string enteredPasswordHash = HashPassword(enteredPassword, storedSalt);
            return storedHash == enteredPasswordHash;
        }
    }
}
