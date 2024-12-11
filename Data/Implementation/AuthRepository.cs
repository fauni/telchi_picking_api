using Core.Entities.Almacenes;
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
        private readonly IAlmacenRepository _almacenRepository;
        public AuthRepository(IConfiguration configuration, IAlmacenRepository almacenRepository)
        {
            _configuration = configuration;
            _almacenRepository = almacenRepository;
        }

        public async Task<SapAuthResponse> AuthenticateWithSapB1Async()
        {
            var sapAuthUrl = _configuration["SapCredentials:Url"] + $"/Login";
            var credentials = new
            {
                CompanyDB = _configuration["SapCredentials:CompanyDB"],
                Password = _configuration["SapCredentials:Password"],
                UserName = _configuration["SapCredentials:UserName"],
                Language = "23"
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
                                var usuario = new Usuario
                                {
                                    Id = (int)reader["Id"],
                                    Nombres = reader["Nombres"].ToString(),
                                    ApellidoPaterno = reader["ApellidoPaterno"].ToString(),
                                    ApellidoMaterno = reader["ApellidoMaterno"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    UsuarioNombre = username
                                };

                                // Obtener los almacenes asignados al usuario
                                usuario.Almacenes = _almacenRepository.ObtenerAlmacenesPorUsuario(usuario.Id);
                                
                                return usuario;
                            }
                        }
                    }
                    return null;
                }
            }
        }
    }
}
