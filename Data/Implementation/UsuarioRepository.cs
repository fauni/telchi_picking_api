using Core.Entities.Login;
using Data.Helpers;
using Data.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Implementation
{
    public class UsuarioRepository : IUsuarioRepository
    {
        IConfiguration _configuration;

        public UsuarioRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void ResetearClave(LoginModel usuario)
        {
            // Validación básica del parámetro de entrada
            if (usuario == null || string.IsNullOrWhiteSpace(usuario.Username))
            {
                throw new ArgumentException("El usuario no puede estar vacío");
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    // Primero buscamos si el usuario existe y obtenemos sus datos
                    Usuario usuarioEncontrado = null;
                    string selectQuery = @"
                        SELECT Id, Nombres, ApellidoPaterno, ApellidoMaterno, Email 
                        FROM Usuarios 
                        WHERE Usuario = @Username 
                        AND EstaActivo = 1 
                        AND EstaBloqueado = 0";

                    conn.Open();
                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, conn))
                    {
                        selectCommand.Parameters.AddWithValue("@Username", usuario.Username);

                        using (SqlDataReader reader = selectCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                usuarioEncontrado = new Usuario
                                {
                                    Id = (int)reader["Id"],
                                    Nombres = reader["Nombres"].ToString(),
                                    ApellidoPaterno = reader["ApellidoPaterno"].ToString(),
                                    ApellidoMaterno = reader["ApellidoMaterno"].ToString(),
                                    Email = reader["Email"].ToString()
                                };
                            }
                        }
                    }

                    // Si no encontramos el usuario, lanzamos excepción
                    if (usuarioEncontrado == null)
                    {
                        throw new KeyNotFoundException("Usuario no encontrado o no está activo");
                    }

                    // Generamos el nuevo salt y hash de la contraseña
                    string salt = PasswordHelper.GenerateSelt();
                    string passwordHash = PasswordHelper.HashPassword(usuario.Password, salt);

                    // Actualizamos la contraseña en la base de datos
                    string updateQuery = @"
                        UPDATE Usuarios 
                        SET PasswordHash = @PasswordHash, PasswordSalt = @Salt, FechaModificacion = GETDATE()
                        WHERE Id = @UserId";

                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, conn))
                    {
                        updateCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        updateCommand.Parameters.AddWithValue("@Salt", salt);
                        updateCommand.Parameters.AddWithValue("@UserId", usuarioEncontrado.Id);

                        int rowsAffected = updateCommand.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            throw new Exception("No se pudo actualizar la contraseña");
                        }
                    }

                    // Aquí podrías agregar el envío de email de notificación
                    // Ejemplo: _emailService.SendPasswordResetConfirmation(usuarioEncontrado.Email);
                }
            }
            catch (Exception ex)
            {
                // Mejor manejo de errores
                throw new Exception($"Error al resetear la clave: {ex.Message}", ex);
            }
        }

        public bool InsertarUsuario(Usuario usuario)
        {
            string salt = PasswordHelper.GenerateSelt();
            string passwordHash = PasswordHelper.HashPassword(usuario.PasswordHash, salt);
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();

                    string query = @"INSERT INTO Usuarios 
                                (ApellidoPaterno, ApellidoMaterno, Nombres, Usuario, Email, PasswordHash, PasswordSalt, EstaBloqueado, EstaActivo, FechaCreacion) 
                                VALUES 
                                (@ApellidoPaterno, @ApellidoMaterno, @Nombres, @Usuario, @Email, @PasswordHash, @PasswordSalt, 0, 1, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        // Asignamos los valores a los parámetros SQL
                        cmd.Parameters.AddWithValue("@ApellidoPaterno", usuario.ApellidoPaterno);
                        cmd.Parameters.AddWithValue("@ApellidoMaterno", usuario.ApellidoMaterno);
                        cmd.Parameters.AddWithValue("@Nombres", usuario.Nombres);
                        cmd.Parameters.AddWithValue("@Usuario", usuario.UsuarioNombre);
                        cmd.Parameters.AddWithValue("@Email", usuario.Email);
                        cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        cmd.Parameters.AddWithValue("@PasswordSalt", salt);

                        // Ejecutamos el comando
                        int rowsAffected = cmd.ExecuteNonQuery();

                        // Si una fila fue insertada correctamente, retornamos true
                        return rowsAffected > 0;
                    }
                }
            }
            catch(Exception ex) {
                // TODO: Agregar manejo de errores
                throw new Exception(ex.Message);
                // return false;
            }
        }

        public List<Usuario> ObtenerUsuarios()
        {
            var usuarios = new List<Usuario>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    conn.Open();
                    string query = "SELECT Id, ApellidoPaterno, ApellidoMaterno, Nombres, Usuario, Email, EstaActivo FROM Usuarios";
                    using (SqlCommand cmd = new SqlCommand(query, conn)) 
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read()) 
                            {
                                var usuario = new Usuario
                                {
                                    Id = (int)reader["Id"],
                                    ApellidoPaterno = reader["ApellidoPaterno2"].ToString(),
                                    ApellidoMaterno = reader["ApellidoMaterno"].ToString(),
                                    Nombres = reader["Nombres"].ToString(),
                                    UsuarioNombre = reader["Usuario"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    EstaActivo = (bool)reader["EstaActivo"]
                                };
                                usuarios.Add(usuario);

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ocurrio un error al obtener los datos: " + ex.Message);
            }

            return usuarios;
        }
    }
}
