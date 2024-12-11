using Core.Entities.Conteos;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessLogic.Logic
{
    public class ConteoRepository : IConteoRepository
    {
        IConfiguration _configuration;
        IItemRepository _itemRepository;
        public ConteoRepository(IConfiguration configuration, IItemRepository itemRepository)
        {
            _configuration = configuration;
            _itemRepository = itemRepository;
        }

        public async Task<int> CreateConteoFromUserAndWarehouseAsync(Conteo conteo)
        {
            // Obtener los items del almacén
            var items = await _itemRepository.GetItemsByWarehouseAsync(conteo.CodigoAlmacen);

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                // Comenzar la transacción
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Paso 1: Insertar el registro en la tabla Conteo
                        string sqlConteo = @"INSERT INTO Conteo (NombreUsuario, CodigoAlmacen, Comentarios, Estado)
                                     VALUES (@NombreUsuario, @CodigoAlmacen, @Comentarios, @Estado);
                                     SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        // Crear el comando SQL con parámetros
                        using (SqlCommand command = new SqlCommand(sqlConteo, connection, transaction))
                        {
                            // Parámetros para evitar SQL injection
                            command.Parameters.AddWithValue("@NombreUsuario", conteo.NombreUsuario);
                            command.Parameters.AddWithValue("@CodigoAlmacen", conteo.CodigoAlmacen);
                            command.Parameters.AddWithValue("@Comentarios", conteo.Comentarios);
                            command.Parameters.AddWithValue("@Estado", conteo.Estado ?? "Pendiente");

                            // Ejecutar el comando y obtener el ID del nuevo conteo
                            int conteoId = (int)await command.ExecuteScalarAsync();


                            // Paso 2: Insertar los detalles del conteo
                            string sqlDetalleConteo = @"INSERT INTO DetalleConteo (ConteoId, CodigoItem, CodigoBarras, CodigoAlmacen, DescripcionItem, 
                                                 CantidadDisponible, CantidadComprometida, CantidadPendienteDeRecibir, 
                                                 CantidadContada, Estado)
                                                 VALUES (@ConteoId, @CodigoItem, @CodigoBarras, @CodigoAlmacen, @DescripcionItem, 
                                                 @CantidadDisponible, @CantidadComprometida, @CantidadPendienteDeRecibir, 
                                                 @CantidadContada, @Estado);";

                            // Recorrer los items y agregar los detalles del conteo
                            foreach (var item in items)
                            {
                                using (SqlCommand commandDetalle = new SqlCommand(sqlDetalleConteo, connection, transaction))
                                {
                                    // Parámetros para el detalle del conteo
                                    commandDetalle.Parameters.AddWithValue("@ConteoId", conteoId);
                                    commandDetalle.Parameters.AddWithValue("@CodigoItem", item.ItemCode);
                                    commandDetalle.Parameters.AddWithValue("@CodigoBarras", item.CodeBars);
                                    commandDetalle.Parameters.AddWithValue("@CodigoAlmacen", conteo.CodigoAlmacen);
                                    commandDetalle.Parameters.AddWithValue("@DescripcionItem", item.ItemName);
                                    commandDetalle.Parameters.AddWithValue("@CantidadDisponible", item.OnHand);
                                    commandDetalle.Parameters.AddWithValue("@CantidadComprometida", item.IsCommited);
                                    commandDetalle.Parameters.AddWithValue("@CantidadPendienteDeRecibir", item.OnOrder);
                                    commandDetalle.Parameters.AddWithValue("@CantidadContada", 0);  // Se puede poner 0 si aún no se ha contado
                                    commandDetalle.Parameters.AddWithValue("@Estado", "Pendiente");  // Estado inicial del detalle

                                    // Ejecutar el comando para insertar los detalles
                                    await commandDetalle.ExecuteNonQueryAsync();
                                }
                            }

                            // Confirmar la transacción
                            transaction.Commit();

                            // Retornar el ID del conteo creado
                            return conteoId;

                        }
                    }
                    catch (Exception ex)
                    {
                        // Si ocurre un error, se hace rollback de la transacción
                        transaction.Rollback();
                        throw new Exception("Error al crear el conteo y sus detalles", ex);
                    }
                }
            }
        }

        public async Task<List<Conteo>> GetConteosByUserAsync(string nombreUsuario)
        {
            var conteos = new List<Conteo>();
            using(SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                string sql = @"SELECT Id, NombreUsuario, CodigoAlmacen, Comentarios, FechaInicio, 
                              FechaFinalizacion, Estado
                       FROM Conteo
                       WHERE NombreUsuario = @NombreUsuario";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@NombreUsuario", nombreUsuario);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var conteo = new Conteo
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                NombreUsuario = reader.GetString(reader.GetOrdinal("NombreUsuario")),
                                CodigoAlmacen = reader.GetString(reader.GetOrdinal("CodigoAlmacen")),
                                Comentarios = reader.IsDBNull(reader.GetOrdinal("Comentarios"))
                                                ? null
                                                : reader.GetString(reader.GetOrdinal("Comentarios")),
                                FechaInicio = reader.GetDateTime(reader.GetOrdinal("FechaInicio")),
                                FechaFinalizacion = reader.IsDBNull(reader.GetOrdinal("FechaFinalizacion"))
                                                     ? null
                                                     : reader.GetDateTime(reader.GetOrdinal("FechaFinalizacion")),
                                Estado = reader.GetString(reader.GetOrdinal("Estado"))
                            };

                            conteos.Add(conteo);
                        }
                    }
                }
            }
            return conteos;
        }


        public async Task<IEnumerable<DetalleConteo>> GetDetalleConteoByConteoIdAsync(int conteoId)
        {
            var detalles = new List<DetalleConteo>();

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                string sqlQuery = @"SELECT Id, ConteoId, CodigoItem, CodigoBarras, CodigoAlmacen, DescripcionItem, 
                                   CantidadDisponible, CantidadComprometida, CantidadPendienteDeRecibir, 
                                   CantidadContada, Estado
                            FROM DetalleConteo
                            WHERE ConteoId = @ConteoId";

                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.AddWithValue("@ConteoId", conteoId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            detalles.Add(new DetalleConteo
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                ConteoId = reader.GetInt32(reader.GetOrdinal("ConteoId")),
                                CodigoItem = reader.GetString(reader.GetOrdinal("CodigoItem")),
                                CodigoBarras = reader.GetString(reader.GetOrdinal("CodigoBarras")),
                                CodigoAlmacen = reader.GetString(reader.GetOrdinal("CodigoAlmacen")),
                                DescripcionItem = reader.GetString(reader.GetOrdinal("DescripcionItem")),
                                CantidadDisponible = reader.GetDecimal(reader.GetOrdinal("CantidadDisponible")),
                                CantidadComprometida = reader.GetDecimal(reader.GetOrdinal("CantidadComprometida")),
                                CantidadPendienteDeRecibir = reader.GetDecimal(reader.GetOrdinal("CantidadPendienteDeRecibir")),
                                CantidadContada = reader.GetDecimal(reader.GetOrdinal("CantidadContada")),
                                Estado = reader.GetString(reader.GetOrdinal("Estado"))
                            });
                        }
                    }
                }
            }

            return detalles;
        }

        public async Task<IEnumerable<DetalleConteo>> GetDetalleConteoByConteoIdPaginatedAsync(int conteoId, int pageNumber, int pageSize)
        {
            var detalles = new List<DetalleConteo>();

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                string sqlQuery = @"
            SELECT Id, ConteoId, CodigoItem, CodigoBarras, CodigoAlmacen, DescripcionItem, 
                   CantidadDisponible, CantidadComprometida, CantidadPendienteDeRecibir, 
                   CantidadContada, Estado
            FROM DetalleConteo
            WHERE ConteoId = @ConteoId
            ORDER BY Id
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.AddWithValue("@ConteoId", conteoId);
                    command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            detalles.Add(new DetalleConteo
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                ConteoId = reader.GetInt32(reader.GetOrdinal("ConteoId")),
                                CodigoItem = reader.GetString(reader.GetOrdinal("CodigoItem")),
                                CodigoBarras = reader.GetString(reader.GetOrdinal("CodigoBarras")),
                                CodigoAlmacen = reader.GetString(reader.GetOrdinal("CodigoAlmacen")),
                                DescripcionItem = reader.GetString(reader.GetOrdinal("DescripcionItem")),
                                CantidadDisponible = reader.GetDecimal(reader.GetOrdinal("CantidadDisponible")),
                                CantidadComprometida = reader.GetDecimal(reader.GetOrdinal("CantidadComprometida")),
                                CantidadPendienteDeRecibir = reader.GetDecimal(reader.GetOrdinal("CantidadPendienteDeRecibir")),
                                CantidadContada = reader.GetDecimal(reader.GetOrdinal("CantidadContada")),
                                Estado = reader.GetString(reader.GetOrdinal("Estado"))
                            });
                        }
                    }
                }
            }

            return detalles;
        }

        public async Task<IEnumerable<DetalleConteo>> GetDetalleConteoFilteredAsync(
            int conteoId,
            int pageNumber,
            int pageSize,
            string? search = null)
        {
            var detalles = new List<DetalleConteo>();

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                // Construcción dinámica de la consulta
                var sqlQuery = new StringBuilder(@"
                    SELECT Id, ConteoId, CodigoItem, CodigoBarras, CodigoAlmacen, DescripcionItem, 
                    CantidadDisponible, CantidadComprometida, CantidadPendienteDeRecibir, 
                    CantidadContada, Estado FROM DetalleConteo WHERE ConteoId = @ConteoId");

                if (!string.IsNullOrEmpty(search))
                {
                    sqlQuery.Append(" AND (CodigoItem LIKE @search OR DescripcionItem LIKE @search)");
                }

                sqlQuery.Append(@"ORDER BY Id OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");

                using (SqlCommand command = new SqlCommand(sqlQuery.ToString(), connection))
                {
                    command.Parameters.AddWithValue("@ConteoId", conteoId);
                    command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

                    if (!string.IsNullOrEmpty(search))
                    {
                        command.Parameters.AddWithValue("@search", $"%{search}%");
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            detalles.Add(new DetalleConteo
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                ConteoId = reader.GetInt32(reader.GetOrdinal("ConteoId")),
                                CodigoItem = reader.GetString(reader.GetOrdinal("CodigoItem")),
                                CodigoBarras = !reader.IsDBNull(reader.GetOrdinal("CodigoBarras"))? reader.GetString(reader.GetOrdinal("CodigoBarras")): null,
                                CodigoAlmacen = reader.GetString(reader.GetOrdinal("CodigoAlmacen")),
                                DescripcionItem = reader.GetString(reader.GetOrdinal("DescripcionItem")),
                                CantidadDisponible = reader.GetDecimal(reader.GetOrdinal("CantidadDisponible")),
                                CantidadComprometida = reader.GetDecimal(reader.GetOrdinal("CantidadComprometida")),
                                CantidadPendienteDeRecibir = reader.GetDecimal(reader.GetOrdinal("CantidadPendienteDeRecibir")),
                                CantidadContada = reader.GetDecimal(reader.GetOrdinal("CantidadContada")),
                                Estado = reader.GetString(reader.GetOrdinal("Estado"))
                            });
                        }
                    }
                }
            }

            return detalles;
        }

        /// <summary>
        /// Obtiene una lista de detalles del conteo filtrados por ID de conteo y código de barras.
        /// </summary>
        /// <param name="conteoId">El ID del conteo asociado.</param>
        /// <param name="codigoBarras">El código de barras del ítem a buscar.</param>
        /// <returns>Una lista de objetos <see cref="DetalleConteo"/> que cumplen con los criterios de búsqueda.</returns>
        /// <exception cref="Exception">Lanza una excepción si ocurre un error al ejecutar la consulta.</exception>
        public async Task<IEnumerable<DetalleConteo>> GetDetalleConteoByCodigoBarrasAsync(int conteoId, string codigoBarras)
        {
            var detalles = new List<DetalleConteo>();

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                // Consulta SQL para buscar por ConteoId y CodigoBarras
                var sqlQuery = @"
            SELECT Id, ConteoId, CodigoItem, CodigoBarras, CodigoAlmacen, DescripcionItem, 
                   CantidadDisponible, CantidadComprometida, CantidadPendienteDeRecibir, 
                   CantidadContada, Estado 
            FROM DetalleConteo 
            WHERE ConteoId = @ConteoId AND CodigoBarras = @CodigoBarras";

                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    // Parámetros con valores asignados
                    command.Parameters.Add("@ConteoId", SqlDbType.Int).Value = conteoId;
                    command.Parameters.Add("@CodigoBarras", SqlDbType.NVarChar).Value = codigoBarras;

                    try
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                detalles.Add(new DetalleConteo
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    ConteoId = reader.GetInt32(reader.GetOrdinal("ConteoId")),
                                    CodigoItem = reader.GetString(reader.GetOrdinal("CodigoItem")),
                                    CodigoBarras = !reader.IsDBNull(reader.GetOrdinal("CodigoBarras"))
                                        ? reader.GetString(reader.GetOrdinal("CodigoBarras"))
                                        : null,
                                    CodigoAlmacen = reader.GetString(reader.GetOrdinal("CodigoAlmacen")),
                                    DescripcionItem = reader.GetString(reader.GetOrdinal("DescripcionItem")),
                                    CantidadDisponible = reader.GetDecimal(reader.GetOrdinal("CantidadDisponible")),
                                    CantidadComprometida = reader.GetDecimal(reader.GetOrdinal("CantidadComprometida")),
                                    CantidadPendienteDeRecibir = reader.GetDecimal(reader.GetOrdinal("CantidadPendienteDeRecibir")),
                                    CantidadContada = reader.GetDecimal(reader.GetOrdinal("CantidadContada")),
                                    Estado = reader.GetString(reader.GetOrdinal("Estado"))
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Manejo del error
                        throw new Exception("Error al buscar los detalles del conteo por código de barras", ex);
                    }
                }
            }

            return detalles;
        }


        /// <summary>
        /// Elimina un registro de la tabla Conteo y sus registros relacionados en DetalleConteo.
        /// </summary>
        /// <param name="conteoId">Id del registro de conteo que se desea eliminar.</param>
        /// <returns>Un valor booleano que indica si la operación fue exitosa.</returns>
        public async Task<bool> DeleteConteoAsync(int conteoId)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                // Utilizar una transacción para garantizar la consistencia
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Paso 1: Eliminar los detalles relacionados
                        string deleteDetalleQuery = "DELETE FROM DetalleConteo WHERE ConteoId = @ConteoId";
                        using (SqlCommand deleteDetalleCommand = new SqlCommand(deleteDetalleQuery, connection, transaction))
                        {
                            deleteDetalleCommand.Parameters.AddWithValue("@ConteoId", conteoId);
                            await deleteDetalleCommand.ExecuteNonQueryAsync();
                        }

                        // Paso 2: Eliminar el registro en Conteo
                        string deleteConteoQuery = "DELETE FROM Conteo WHERE Id = @Id";
                        using (SqlCommand deleteConteoCommand = new SqlCommand(deleteConteoQuery, connection, transaction))
                        {
                            deleteConteoCommand.Parameters.AddWithValue("@Id", conteoId);
                            int rowsAffected = await deleteConteoCommand.ExecuteNonQueryAsync();

                            // Confirmar la transacción si se eliminó el registro de Conteo
                            transaction.Commit();

                            return rowsAffected > 0; // Retorna true si se eliminó el registro en Conteo
                        }
                    }
                    catch (Exception ex)
                    {
                        // Si ocurre un error, revertir la transacción
                        transaction.Rollback();
                        throw new Exception($"Error al eliminar el conteo con Id {conteoId}: {ex.Message}", ex);
                    }
                }
            }
        }


        /// <summary>
        /// Registra un conteo de ítems en la tabla ConteoItemsInventario, actualizando los detalles del conteo correspondiente.
        /// </summary>
        /// <param name="detalleConteoId">El ID del detalle del conteo al que se asocia este registro.</param>
        /// <param name="usuario">El nombre del usuario que realizó el conteo.</param>
        /// <param name="cantidadAgregada">La cantidad que se agregó en este conteo.</param>
        /// <returns>El ID del registro creado en la tabla ConteoItemsInventario.</returns>
        public async Task<int> RegistrarConteoItemAsync(int detalleConteoId, string usuario, decimal cantidadAgregada)
        {
            // Validación de parámetros de entrada
            if (detalleConteoId <= 0)
                throw new ArgumentException("El ID del detalle de conteo no es válido.", nameof(detalleConteoId));
            if (string.IsNullOrWhiteSpace(usuario))
                throw new ArgumentException("El nombre del usuario es requerido.", nameof(usuario));

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                // Obtener la cantidad contada actual del detalle de conteo
                decimal cantidadContadaAnterior;
                string obtenerCantidadQuery = "SELECT CantidadContada FROM DetalleConteo WHERE Id = @DetalleConteoId";
                using (SqlCommand obtenerCantidadCmd = new SqlCommand(obtenerCantidadQuery, connection))
                {
                    obtenerCantidadCmd.Parameters.AddWithValue("@DetalleConteoId", detalleConteoId);
                    cantidadContadaAnterior = (decimal)await obtenerCantidadCmd.ExecuteScalarAsync();
                }

                // Calcular la nueva cantidad contada
                decimal nuevaCantidadContada = cantidadContadaAnterior + cantidadAgregada;

                // Actualizar la cantidad contada en DetalleConteo
                string actualizarCantidadQuery = @"UPDATE DetalleConteo SET CantidadContada = @NuevaCantidad WHERE Id = @DetalleConteoId";
                using (SqlCommand actualizarCantidadCmd = new SqlCommand(actualizarCantidadQuery, connection))
                {
                    actualizarCantidadCmd.Parameters.AddWithValue("@NuevaCantidad", nuevaCantidadContada);
                    actualizarCantidadCmd.Parameters.AddWithValue("@DetalleConteoId", detalleConteoId);
                    await actualizarCantidadCmd.ExecuteNonQueryAsync();
                }

                // Insertar el registro en ConteoItemsInventario
                string insertarConteoItemQuery = @"INSERT INTO ConteoItemsInventario
                    (IdDetalleConteo, Usuario, FechaHoraConteo, CantidadContada, CantidadContadaAnterior, CantidadAgregada)
                    OUTPUT INSERTED.IdConteo VALUES (@IdDetalleConteo, @Usuario, GETDATE(), @CantidadContada, @CantidadContadaAnterior, @CantidadAgregada)";
                using (SqlCommand insertarConteoItemCmd = new SqlCommand(insertarConteoItemQuery, connection))
                {
                    insertarConteoItemCmd.Parameters.AddWithValue("@IdDetalleConteo", detalleConteoId);
                    insertarConteoItemCmd.Parameters.AddWithValue("@Usuario", usuario);
                    insertarConteoItemCmd.Parameters.AddWithValue("@CantidadContada", nuevaCantidadContada);
                    insertarConteoItemCmd.Parameters.AddWithValue("@CantidadContadaAnterior", cantidadContadaAnterior);
                    insertarConteoItemCmd.Parameters.AddWithValue("@CantidadAgregada", cantidadAgregada);

                    // Obtener el ID del registro recién creado
                    int idConteo = (int)await insertarConteoItemCmd.ExecuteScalarAsync();
                    return idConteo;
                }
            }
        }


    }
}
