using Core.Entities.Picking;
using Core.Entities.Ventas;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessLogic.Logic
{
    public class DocumentoRepository : IDocumentoRepository
    {
        IConfiguration _configuration;

        public DocumentoRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /*
        public async Task ActualizarEstadoDocumentoAsync(int idDocumento)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                // Verificar el estado de los items en DetalleDocumento
                string estadoDocumento = "P"; // Estado inicial
                string query = "SELECT * FROM DetalleDocumento WHERE IdDocumento = @IdDocumento";
                bool todosCompletados = true;
                bool algunEnProceso = false;

                using(SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdDocumento", idDocumento);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while(await reader.ReadAsync())
                        {
                            string estadoItem = reader["Estado"].ToString();
                            if(estadoItem == "En Progreso")
                            {
                                algunEnProceso = true;
                                todosCompletados = false;
                            } else if(estadoItem == "Pendiente")
                            {
                                todosCompletados = false;
                            } else if(estadoItem == "Completado" && !algunEnProceso)
                            {
                                // No hacemos cambios a algunEnProceso o todos completados
                            }
                        }
                    }

                    // Determinar el estado final del documento
                    if (todosCompletados)
                    {
                        estadoDocumento = "F";
                    } else if (algunEnProceso)
                    {
                        estadoDocumento = "I";
                    } else
                    {
                        estadoDocumento = "P";
                    }

                    // Paso 2: Actualizar el estado del documento y la fecha de modificación si cambió el estado
                    string updateQuery = "UPDATE Documento SET EstadoConteo = @Estado, FechaFinalizacion = @FechaFinalizacion WHERE IdDocumento = @IdDocumento";
                    using(SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@Estado", estadoDocumento);
                        updateCommand.Parameters.AddWithValue("@FechaFinalizacion", DateTime.Now);
                        updateCommand.Parameters.AddWithValue("@IdDocumento", idDocumento);
                        await updateCommand.ExecuteNonQueryAsync();
                    }
                }
            }
        }
        */
        public async Task ActualizarEstadoDocumentoAsync(int idDocumento)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                // Paso 1: Obtener los estados únicos de los detalles del documento
                string query = "SELECT DISTINCT Estado FROM DetalleDocumento WHERE IdDocumento = @IdDocumento";
                var estados = new HashSet<string>();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdDocumento", idDocumento);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            estados.Add(reader["Estado"].ToString());
                        }
                    }
                }

                // Paso 2: Determinar el estado del documento basado en los estados de detalle
                string estadoDocumento;

                if (estados.Contains("Pendiente") && estados.Contains("Completado"))
                {
                    estadoDocumento = "I"; // En Proceso (indica que tiene ambos estados)
                }
                else if (estados.Count == 1 && estados.Contains("Completado"))
                {
                    estadoDocumento = "F"; // Todos los detalles están completados
                }
                else
                {
                    estadoDocumento = "P"; // Solo tiene detalles pendientes
                }

                // Paso 3: Actualizar el estado del documento y la fecha de modificación
                string updateQuery = "UPDATE Documento SET EstadoConteo = @Estado, FechaFinalizacion = @FechaFinalizacion WHERE IdDocumento = @IdDocumento";
                using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@Estado", estadoDocumento);
                    updateCommand.Parameters.AddWithValue("@FechaFinalizacion", DateTime.Now);
                    updateCommand.Parameters.AddWithValue("@IdDocumento", idDocumento);
                    await updateCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<int> CreateDocumentFromOrderAsync(Order order, string tipoDocumento)
        {
            using(SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Paso 1: Insertar el registro en la tabla documento
                        string sqlDocumento = "INSERT INTO Documento (TipoDocumento, NumeroDocumento, FechaInicio, EstadoConteo) " +
                                      "VALUES (@TipoDocumento, @NumeroDocumento, @FechaInicio, @EstadoConteo); " +
                                      "SELECT SCOPE_IDENTITY();";

                        SqlCommand commandDocumento = new SqlCommand(sqlDocumento, connection, transaction);
                        commandDocumento.Parameters.AddWithValue("@TipoDocumento", tipoDocumento);
                        commandDocumento.Parameters.AddWithValue("@NumeroDocumento", order.DocNum);
                        commandDocumento.Parameters.AddWithValue("@FechaInicio", DateTime.Now); // Fecha actual de creación
                        commandDocumento.Parameters.AddWithValue("@EstadoConteo", 'P'); // Estado inicial 'Pendiente'

                        var result = await commandDocumento.ExecuteScalarAsync();
                        int documentId = Convert.ToInt32(result);

                        // Paso 2: Insertar cada línea en la tabla Detalle Documento
                        string sqlDetalle = "INSERT INTO DetalleDocumento (IdDocumento, NumeroLinea ,CodigoItem, DescripcionItem, CantidadEsperada, CantidadContada, Estado) " +
                                    "VALUES (@IdDocumento, @NumeroLinea, @CodigoItem, @DescripcionItem, @CantidadEsperada, @CantidadContada, @Estado);";

                        foreach (var line in order.DocumentLines)
                        {
                            SqlCommand commandDetalle = new SqlCommand(sqlDetalle, connection, transaction);
                            commandDetalle.Parameters.AddWithValue("@IdDocumento", documentId);
                            commandDetalle.Parameters.AddWithValue("@NumeroLinea", line.LineNum);
                            commandDetalle.Parameters.AddWithValue("@CodigoItem", line.ItemCode);
                            commandDetalle.Parameters.AddWithValue("@DescripcionItem", line.ItemDescription);
                            commandDetalle.Parameters.AddWithValue("@CantidadEsperada", line.Quantity);
                            commandDetalle.Parameters.AddWithValue("@CantidadContada", 0); // Iniciar en 0
                            commandDetalle.Parameters.AddWithValue("@Estado", "Pendiente"); // Estado inicial 'Pendiente'

                            await commandDetalle.ExecuteNonQueryAsync();
                        }

                        // Confirmar transacción
                        transaction.Commit();
                        return documentId; // Devuelve el ID del documento creado

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<Documento> GetDocumentByDocNumAsync(string id)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string sql = "SELECT * FROM Documento WHERE NumeroDocumento = @Id";
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Documento
                        {
                            IdDocumento = (int)reader["IdDocumento"],
                            TipoDocumento = reader["TipoDocumento"].ToString(),
                            NumeroDocumento = reader["NumeroDocumento"].ToString(),
                            FechaInicio = reader["FechaInicio"] as DateTime?,
                            FechaFinalizacion = reader["FechaFinalizacion"] as DateTime?,
                            EstadoConteo = (reader["EstadoConteo"]).ToString()
                        };
                    }
                }
            }
            return null;
        }

        public async Task<List<Documento>> GetDocumentosPorOrderIdAsync(int orderId)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "SELECT * FROM Documento WHERE OrderId = @OrderId"; // Suponiendo que tienes OrderId en la tabla Documento
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@OrderId", orderId);

                await connection.OpenAsync();
                var documentos = new List<Documento>();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        documentos.Add(new Documento
                        {
                            IdDocumento = (int)reader["IdDocumento"],
                            TipoDocumento = reader["TipoDocumento"].ToString(),
                            NumeroDocumento = reader["NumeroDocumento"].ToString(),
                            FechaInicio = reader["FechaInicio"] as DateTime?,
                            FechaFinalizacion = reader["FechaFinalizacion"] as DateTime?,
                            EstadoConteo = reader["EstadoConteo"].ToString()
                        });
                    }
                }
                return documentos;
            }
        }

        public async Task<int> InsertDocumentAsync(Documento documento)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string sql = "INSERT INTO Documento (TipoDocumento, NumeroDocumento, FechaInicio, FechaFinalizacion, EstadoConteo) " +
                             "VALUES (@TipoDocumento, @NumeroDocumento, @FechaInicio, @FechaFinalizacion, @EstadoConteo); " +
                             "SELECT SCOPE_IDENTITY();";

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@TipoDocumento", documento.TipoDocumento);
                command.Parameters.AddWithValue("@NumeroDocumento", documento.NumeroDocumento);
                command.Parameters.AddWithValue("@FechaInicio", documento.FechaInicio ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FechaFinalizacion", documento.FechaFinalizacion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@EstadoConteo", documento.EstadoConteo);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                int newId = Convert.ToInt32(result);
                
                documento.IdDocumento = newId;

                return newId;
            }
        }
    }
}
