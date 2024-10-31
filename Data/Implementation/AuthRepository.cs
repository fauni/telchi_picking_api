using Core.Entities.Login;
using Data.Helpers;
using Data.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace Data.Implementation
{
    public class AuthRepository: IAuthRepository
    {
        private readonly IConfiguration _configuration;
        public AuthRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<SapAuthResponse> AuthenticateWithSapB1Async()
        {
            var sapAuthUrl = _configuration["SapCredentials:Url"];
            var credentials = new
            {
                CompanyDB = _configuration["SapCredentials:CompanyDB"],
                Password = _configuration["SapCredentials:Password"],
                UserName = _configuration["SapCredentials:UserName"]
            };
            // Configurar el HttpClientHandler para ignorar errores de certificado
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            using (var httpClient = new HttpClient(handler))
            {
                var content = new StringContent(JsonSerializer.Serialize(credentials), encoding: Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(sapAuthUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var sapResponseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<SapAuthResponse>(sapResponseContent);
                }

                return null;
            }
                
        }

        public async Task<Usuario> ValidateUserAsync(string username, string password)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "SELECT * FROM Usuarios WHERE Usuario = @Username AND EstaActivo = 1 AND EstaBloqueado = 0";
                using (SqlCommand command = new SqlCommand(query, connection)) 
                { 
                    command.Parameters.AddWithValue("@Username", username);
                    await connection.OpenAsync();
                    using (SqlDataReader reader = command.ExecuteReader()) 
                    {
                        if (reader.Read()) 
                        {
                            string storedHash = reader["PasswordHash"]?.ToString();
                            string storedSalt = reader["PasswordSalt"]?.ToString();

                            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                            {
                                return null;
                            }

                            string inputHash = PasswordHelper.HashPassword(password, storedSalt);
                            if (storedHash == inputHash)
                            {
                                return new Usuario
                                {
                                    Id = (int)reader["Id"],
                                    Nombres = reader["Nombres"].ToString(),
                                    ApellidoPaterno = reader["ApellidoPaterno"].ToString(),
                                    ApellidoMaterno = reader["ApellidoMaterno"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    UsuarioNombre = username,
                                };
                            }
                        }
                    }
                    return null;
                }
                //SqlCommand cmd = new SqlCommand(query, connection);
                //cmd.Parameters.AddWithValue("@Username", username);

                //await connection.OpenAsync();
                //SqlDataReader reader = await cmd.ExecuteReaderAsync();

                //if (reader.Read())
                //{
                //    string storedHash = reader["PasswordHash"].ToString();
                //    string storedSalt = reader["PasswordSalt"].ToString();
                //    string inputHash = PasswordHelper.HashPassword(password, storedSalt);
                //    return storedHash == inputHash;
                //}
                //return false;
            }
        }
    }
}
