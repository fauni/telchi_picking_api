using Core.Entities.Almacenes;
using Data.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Implementation
{
    public class AlmacenRepository : IAlmacenRepository
    {
        IConfiguration _configuration;
        string _connectionString;

        public AlmacenRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }
        public List<Almacen> ObtenerAlmacenes()
        {
            var almacenes = new List<Almacen>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT Id, Codigo, Nombre FROM Almacen", connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        almacenes.Add(new Almacen
                        {
                            Id = reader.GetInt32(0),
                            Codigo = reader.GetString(1),
                            Nombre = reader.GetString(2)
                        });
                    }
                }
            }

            return almacenes;
        }

        public List<Almacen> ObtenerAlmacenesPorUsuario(int idUsuario)
        {
            var almacenes = new List<Almacen>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    @"SELECT A.Id, A.Codigo, A.Nombre
                      FROM Almacen A
                      INNER JOIN UsuarioAlmacen UA ON A.Id = UA.AlmacenId
                      WHERE UA.UsuarioId = @UsuarioId", connection);

                command.Parameters.Add(new SqlParameter("@UsuarioId", SqlDbType.Int) { Value = idUsuario });

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        almacenes.Add(new Almacen
                        {
                            Id = reader.GetInt32(0),
                            Codigo = reader.GetString(1),
                            Nombre = reader.GetString(2)
                        });
                    }
                }
            }

            return almacenes;
        }
    }
}
