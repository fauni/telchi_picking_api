﻿using Core.Entities.Login;
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
                return false;
            }
        }
    }
}
