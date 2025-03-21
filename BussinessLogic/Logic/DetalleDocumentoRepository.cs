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
    public class DetalleDocumentoRepository: IDetalleDocumentoRepository
    {
        private readonly IConfiguration _configuration;

        public DetalleDocumentoRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Obtener todos los detalles por ID de Documento
        public async Task<List<DetalleDocumento>> GetDetallesByDocumentoIdAsync(int documentoId)
        {
            var detalles = new List<DetalleDocumento>();

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string sql = "SELECT * FROM DetalleDocumento WHERE idDocumento = @documentoId";
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@documentoId", documentoId);

                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var detalle = new DetalleDocumento
                        {
                            IdDetalle = (int)reader["idDetalle"],
                            IdDocumento = (int)reader["idDocumento"],
                            CodigoItem = reader["codigoItem"].ToString(),
                            DescripcionItem = reader["descripcionItem"].ToString(),
                            CantidadEsperada = Convert.ToDecimal(reader["cantidadEsperada"]),
                            CantidadContada = Convert.ToDecimal(reader["cantidadContada"]),
                            Estado = reader["estado"].ToString(),
                            CodigoBarras = reader["CodigoBarras"].ToString(),
                            NumeroLinea = (int)reader["numeroLinea"]
                        };
                        detalles.Add(detalle);
                    }
                }
            }

            return detalles;
        }

        // Insertar un nuevo detalle de documento
        public async Task<int> InsertDetalleDocumentoAsync(DetalleDocumento detalle)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string sql = "INSERT INTO DetalleDocumento (idDocumento, numeroLinea, codigoItem, descripcionItem, cantidadEsperada, cantidadContada) " +
                             "VALUES (@idDocumento, @numeroLinea, @codigoItem, @descripcionItem, @cantidadEsperada, @cantidadContada); " +
                             "SELECT SCOPE_IDENTITY();";

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@idDocumento", detalle.IdDocumento);
                command.Parameters.AddWithValue("@numeroLinea", detalle.NumeroLinea);
                command.Parameters.AddWithValue("@codigoItem", detalle.CodigoItem);
                command.Parameters.AddWithValue("@descripcionItem", detalle.DescripcionItem);
                command.Parameters.AddWithValue("@cantidadEsperada", detalle.CantidadEsperada);
                command.Parameters.AddWithValue("@cantidadContada", detalle.CantidadContada);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }

        // Actualizar un detalle de documento existente
        public async Task<bool> UpdateDetalleDocumentoAsync(DetalleDocumento detalle)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string sql = "UPDATE DetalleDocumento SET codigoItem = @codigoItem, descripcionItem = @descripcionItem, " +
                             "cantidadEsperada = @cantidadEsperada, cantidadContada = @cantidadContada " +
                             "WHERE idDetalle = @idDetalle";

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@codigoItem", detalle.CodigoItem);
                command.Parameters.AddWithValue("@descripcionItem", detalle.DescripcionItem);
                command.Parameters.AddWithValue("@cantidadEsperada", detalle.CantidadEsperada);
                command.Parameters.AddWithValue("@cantidadContada", detalle.CantidadContada);
                command.Parameters.AddWithValue("@idDetalle", detalle.IdDetalle);

                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }

        // Eliminar un detalle de documento por su id
        public async Task<bool> DeleteDetalleDocumentoAsync(int detalleId)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string sql = "DELETE FROM DetalleDocumento WHERE idDetalle = @idDetalle";
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@idDetalle", detalleId);

                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
        }

        public async Task<int> InsertConteoItemAsync(ConteoItems conteo)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string sql = "INSERT INTO ConteoItems (IdDetalle, Usuario, FechaHoraConteo, CantidadContada, CantidadContadaAnterior, CantidadAgregada) " +
                             "VALUES (@IdDetalle, @Usuario, @FechaHoraConteo, @CantidadContada, @CantidadContadaAnterior, @CantidadAgregada); " +
                             "SELECT SCOPE_IDENTITY();";

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@IdDetalle", conteo.IdDetalle);
                command.Parameters.AddWithValue("@Usuario", conteo.Usuario);
                command.Parameters.AddWithValue("@FechaHoraConteo", conteo.FechaHoraConteo == default(DateTime) ? DateTime.Now : conteo.FechaHoraConteo);
                command.Parameters.AddWithValue("@CantidadContada", conteo.CantidadContada);
                command.Parameters.AddWithValue("@CantidadContadaAnterior", conteo.CantidadContadaAnterior);
                command.Parameters.AddWithValue("@CantidadAgregada", conteo.CantidadAgregada);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }

        public async Task<bool> ActualizarCantidadContadaAsync(int idDetalle, decimal cantidadAgregada, string usuario)
        {
            using(SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Paso 1: Obtener la cantidad contada actual y la cantidad esperada
                        decimal cantidadContadaAnterior = 0;
                        decimal cantidadEsperada = 0;

                        string selectSql = "SELECT CantidadContada, CantidadEsperada FROM DetalleDocumento WHERE IdDetalle = @IdDetalle";
                        using (SqlCommand selectCommand = new SqlCommand(selectSql, connection, transaction))
                        {
                            selectCommand.Parameters.AddWithValue("@IdDetalle", idDetalle);
                            using (var reader = await selectCommand.ExecuteReaderAsync()) 
                            {
                                if (await reader.ReadAsync()) 
                                {
                                    cantidadContadaAnterior = (decimal)reader["CantidadContada"];
                                    cantidadEsperada = (decimal)reader["CantidadEsperada"];
                                } else
                                {
                                    return false; // Si no se encuentra el detalle, retornar false
                                }
                            }
                        }

                        // Calcular la nueva cantidad contada
                        decimal nuevaCantidad = cantidadContadaAnterior + cantidadAgregada;

                        // Determinar el nuevo estado
                        string nuevoEstado = nuevaCantidad >= cantidadEsperada ? "Completado" : "En Progreso";


                        // Paso 2: Actualizar la cantidad contada en DetalleDocumentos
                        string updateSql = "UPDATE DetalleDocumento SET CantidadContada = @NuevaCantidad, Estado = @Estado WHERE IdDetalle = @IdDetalle";
                        using (SqlCommand updateCommand = new SqlCommand(updateSql, connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@NuevaCantidad", nuevaCantidad);
                            updateCommand.Parameters.AddWithValue("@Estado", nuevoEstado);
                            updateCommand.Parameters.AddWithValue("@IdDetalle", idDetalle);
                            await updateCommand.ExecuteNonQueryAsync(); // Revisar la diferencia con ExecuteScalarAsync
                        }

                        // Paso 3: Insertar un registro en ConteoItems
                        string insertSql = "INSERT INTO ConteoItems (IdDetalle, Usuario, FechaHoraConteo, CantidadContada, CantidadContadaAnterior, CantidadAgregada) " +
                                   "VALUES (@IdDetalle, @Usuario, @FechaHoraConteo, @CantidadContada, @CantidadContadaAnterior, @CantidadAgregada)";
                        using (SqlCommand insertCommand = new SqlCommand(insertSql, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@IdDetalle", idDetalle);
                            insertCommand.Parameters.AddWithValue("@Usuario", usuario);
                            insertCommand.Parameters.AddWithValue("@FechaHoraConteo", DateTime.Now);
                            insertCommand.Parameters.AddWithValue("@CantidadContada", nuevaCantidad);
                            insertCommand.Parameters.AddWithValue("@CantidadContadaAnterior", cantidadContadaAnterior);
                            insertCommand.Parameters.AddWithValue("@CantidadAgregada", cantidadAgregada);
                            await insertCommand.ExecuteNonQueryAsync();
                        }

                        // Confirmar transacción
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;   
                    }
                }
            }
        }

        /// <summary>
        /// Reiniciar la Cantidad Contada de un detalle de documento a 0.
        /// </summary>
        /// <param name="idDetalle"></param>
        /// <param name="cantidadAgregada"></param>
        /// <param name="usuario"></param>
        /// <returns></returns>
        public async Task<bool> ReiniciarCantidadContadaAsync(int idDetalle, decimal cantidadAgregada, string usuario)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Paso 1: Obtener la cantidad contada actual y la cantidad esperada
                        decimal cantidadContadaAnterior = 0;
                        decimal cantidadEsperada = 0;

                        string selectSql = "SELECT CantidadContada, CantidadEsperada FROM DetalleDocumento WHERE IdDetalle = @IdDetalle";
                        using (SqlCommand selectCommand = new SqlCommand(selectSql, connection, transaction))
                        {
                            selectCommand.Parameters.AddWithValue("@IdDetalle", idDetalle);
                            using (var reader = await selectCommand.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    cantidadContadaAnterior = (decimal)reader["CantidadContada"];
                                    cantidadEsperada = (decimal)reader["CantidadEsperada"];
                                }
                                else
                                {
                                    return false; // Si no se encuentra el detalle, retornar false
                                }
                            }
                        }

                        // Calcular la nueva cantidad contada
                        decimal nuevaCantidad = 0; // cantidadContadaAnterior + cantidadAgregada;

                        // Determinar el nuevo estado
                        string nuevoEstado = "Pendiente";// nuevaCantidad >= cantidadEsperada ? "Completado" : "En Progreso";


                        // Paso 2: Actualizar la cantidad contada en DetalleDocumentos
                        string updateSql = "UPDATE DetalleDocumento SET CantidadContada = @NuevaCantidad, Estado = @Estado WHERE IdDetalle = @IdDetalle";
                        using (SqlCommand updateCommand = new SqlCommand(updateSql, connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@NuevaCantidad", nuevaCantidad);
                            updateCommand.Parameters.AddWithValue("@Estado", nuevoEstado);
                            updateCommand.Parameters.AddWithValue("@IdDetalle", idDetalle);
                            await updateCommand.ExecuteNonQueryAsync(); // Revisar la diferencia con ExecuteScalarAsync
                        }

                        // Paso 3: Insertar un registro en ConteoItems
                        string insertSql = "INSERT INTO ConteoItems (IdDetalle, Usuario, FechaHoraConteo, CantidadContada, CantidadContadaAnterior, CantidadAgregada) " +
                                   "VALUES (@IdDetalle, @Usuario, @FechaHoraConteo, @CantidadContada, @CantidadContadaAnterior, @CantidadAgregada)";
                        using (SqlCommand insertCommand = new SqlCommand(insertSql, connection, transaction))
                        {
                            insertCommand.Parameters.AddWithValue("@IdDetalle", idDetalle);
                            insertCommand.Parameters.AddWithValue("@Usuario", usuario);
                            insertCommand.Parameters.AddWithValue("@FechaHoraConteo", DateTime.Now);
                            insertCommand.Parameters.AddWithValue("@CantidadContada", nuevaCantidad);
                            insertCommand.Parameters.AddWithValue("@CantidadContadaAnterior", cantidadContadaAnterior);
                            insertCommand.Parameters.AddWithValue("@CantidadAgregada", cantidadAgregada);
                            await insertCommand.ExecuteNonQueryAsync();
                        }

                        // Confirmar transacción
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public async Task<int> ObtenerIdDocumentoPorDetalleAsync(int idDetalle)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = "SELECT IdDocumento FROM DetalleDocumento WHERE IdDetalle = @IdDetalle";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@IdDetalle", idDetalle);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();

                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
                else
                {
                    throw new Exception("No se encontró un documento para el detalle especificado.");
                }
            }
        }
    }
}
