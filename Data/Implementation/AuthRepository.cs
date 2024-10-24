using Data.Helpers;
using Data.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace Data.Implementation
{
    public class AuthRepository: IAuthRepository
    {
        IConfiguration _configuration;
        public AuthRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "SELECT PasswordHash, PasswordSalt FROM Usuarios WHERE Usuario = @Username AND EstaActivo = 1 AND EstaBloqueado = 0";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Username", username);

                await connection.OpenAsync();
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (reader.Read())
                {
                    string storedHash = reader["PasswordHash"].ToString();
                    string storedSalt = reader["PasswordSalt"].ToString();
                    string inputHash = PasswordHelper.HashPassword(password, storedSalt);
                    return storedHash == inputHash;
                }
                return false;
            }
        }
    }
}
