using Core.Entities.Picking;
using Core.Entities.Sap;
using Core.Entities.Ventas;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection.Metadata;
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

        public async Task<string> ProcessErrorResponseAsync(string jsonResponse)
        {
            try
            {
                // Parsear el JSON para extraer el mensaje
                var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(jsonResponse);
                string messageValue = jsonObject["error"]?["message"]?["value"]?.ToString();

                if (!string.IsNullOrEmpty(messageValue))
                {
                    return messageValue; // Devolver solo el campo "value"
                }
                else
                {
                    return "No se encontró el mensaje de error.";
                }
            }
            catch (Exception ex)
            {
                // Manejar excepciones en caso de que el formato no sea válido
                return $"Error al procesar la respuesta: {ex.Message}";
            }
        }


        public async Task<ResultadoActualizacionSap> ActualizarConteoOrdenSap(string sessionID, int docEntry, List<DetalleDocumentoToSap> detalle, string tipoDocumento)
        {
            string url = "";
            if (tipoDocumento == "orden_venta")
            {
                url = _configuration["SapCredentials:Url"] + $"/Orders({docEntry})";
            }
            else if (tipoDocumento == "factura")
            {
                url = _configuration["SapCredentials:Url"] + $"/Invoices({docEntry})";
            }
            else if (tipoDocumento == "factura_compra")
            {
                url = _configuration["SapCredentials:Url"] + $"/PurchaseInvoices({docEntry})";
            }
            else 
            {
                // TODO: Es necesario completar los métodos 
                url = _configuration["SapCredentials:Url"] + $"/Invoices({docEntry})"; // Aqui la url para transferencias completar
            }
            
            try
            {
                HttpClientHandler handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                using (HttpClient httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Add("Cookie", $"B1SESSION={sessionID}");

                    Core.Entities.Sap.Document document = new Core.Entities.Sap.Document
                    {
                        DocumentLines = detalle.Select(d => new Core.Entities.Sap.DocumentLine
                        {
                            LineNum = d.NumeroLinea,
                            U_PCK_CantContada = (int)d.TotalCantidadContada,
                            U_PCK_ContUsuarios = d.UsuariosParticipantes
                        }).ToList()
                    };

                    var json = JsonConvert.SerializeObject(document);
                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await httpClient.PatchAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        await ActualizarEstadoActualizacionSapAsync(docEntry, tipoDocumento);
                        return new ResultadoActualizacionSap
                        {
                            Exito = true,
                            Mensaje = "Actualización realizada correctamente",
                            CodigoEstado = response.StatusCode.ToString()
                        };
                    }
                    else
                    {
                        string errorMessage = await response.Content.ReadAsStringAsync();
                        string mensajeDeError = await ProcessErrorResponseAsync(errorMessage);
                        return new ResultadoActualizacionSap
                        {
                            Exito = false,
                            Mensaje = $"Error al actualizar en SAP: {mensajeDeError}",
                            CodigoEstado = response.StatusCode.ToString()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResultadoActualizacionSap
                {
                    Exito = false,
                    Mensaje = $"Error en ActualizarConteoOrdenSap: {ex.Message}",
                    CodigoEstado = "Exception"
                };
            }
        }

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
                else if (estados.Contains("En Progreso")) 
                {
                    estadoDocumento = "I";
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

        public async Task ActualizarEstadoActualizacionSapAsync(int docEntry, string tipoDocumento)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                string updateQuery = "UPDATE Documento SET ActualizadoSap = @ActualizadoSap WHERE DocEntry = @DocEntry and TipoDocumento=@TipoDocumento";
                using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@ActualizadoSap", "Y");
                    updateCommand.Parameters.AddWithValue("@FechaFinalizacion", DateTime.Now);
                    updateCommand.Parameters.AddWithValue("@DocEntry", docEntry);
                    updateCommand.Parameters.AddWithValue("@TipoDocumento", tipoDocumento);
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
                        string sqlDocumento = "INSERT INTO Documento (TipoDocumento, NumeroDocumento, FechaInicio, EstadoConteo, DocEntry) " +
                                      "VALUES (@TipoDocumento, @NumeroDocumento, @FechaInicio, @EstadoConteo, @DocEntry); " +
                                      "SELECT SCOPE_IDENTITY();";

                        SqlCommand commandDocumento = new SqlCommand(sqlDocumento, connection, transaction);
                        commandDocumento.Parameters.AddWithValue("@TipoDocumento", tipoDocumento);
                        commandDocumento.Parameters.AddWithValue("@NumeroDocumento", order.DocNum);
                        commandDocumento.Parameters.AddWithValue("@FechaInicio", DateTime.Now); // Fecha actual de creación
                        commandDocumento.Parameters.AddWithValue("@EstadoConteo", 'P'); // Estado inicial 'Pendiente'
                        commandDocumento.Parameters.AddWithValue("@DocEntry", order.DocEntry);

                        var result = await commandDocumento.ExecuteScalarAsync();
                        int documentId = Convert.ToInt32(result);

                        // Paso 2: Insertar cada línea en la tabla Detalle Documento
                        string sqlDetalle = "INSERT INTO DetalleDocumento (IdDocumento, NumeroLinea ,CodigoItem, DescripcionItem, CantidadEsperada, CantidadContada, Estado, CodigoBarras) " +
                                    "VALUES (@IdDocumento, @NumeroLinea, @CodigoItem, @DescripcionItem, @CantidadEsperada, @CantidadContada, @Estado, @CodigoBarras);";

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
                            commandDetalle.Parameters.Add(new SqlParameter("@CodigoBarras", SqlDbType.NVarChar)
                            {
                                Value = line.BarCode ?? (object)DBNull.Value
                            }); // Maneja el nulo

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

        public async Task<Documento> GetDocumentByDocNumAsync(string id, string tipoDocumento)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string sql = "SELECT * FROM Documento WHERE NumeroDocumento = @Id and TipoDocumento = @TipoDocumento";
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@TipoDocumento", tipoDocumento);

                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Documento
                        {
                            DocEntry = (int)reader["DocEntry"],
                            IdDocumento = (int)reader["IdDocumento"],
                            TipoDocumento = reader["TipoDocumento"].ToString(),
                            NumeroDocumento = reader["NumeroDocumento"].ToString(),
                            FechaInicio = reader["FechaInicio"] as DateTime?,
                            FechaFinalizacion = reader["FechaFinalizacion"] as DateTime?,
                            EstadoConteo = (reader["EstadoConteo"]).ToString(),
                            ActualizadoSap = reader["ActualizadoSap"].ToString()
                        };
                    }
                }
            }
            return null;
        }

        // TODO: Revisar este metodo GetDocumentosPorOrderIdAsync si se utiliza o no
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
                            EstadoConteo = reader["EstadoConteo"].ToString(),
                            ActualizadoSap = reader["ActualizadoSap"].ToString()
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
                string sql = "INSERT INTO Documento (TipoDocumento, NumeroDocumento, FechaInicio, FechaFinalizacion, EstadoConteo, DocEntry) " +
                             "VALUES (@TipoDocumento, @NumeroDocumento, @FechaInicio, @FechaFinalizacion, @EstadoConteo, @DocEntry); " +
                             "SELECT SCOPE_IDENTITY();";

                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@TipoDocumento", documento.TipoDocumento);
                command.Parameters.AddWithValue("@NumeroDocumento", documento.NumeroDocumento);
                command.Parameters.AddWithValue("@FechaInicio", documento.FechaInicio ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FechaFinalizacion", documento.FechaFinalizacion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@EstadoConteo", documento.EstadoConteo);
                command.Parameters.AddWithValue("@DocEntry", documento.DocEntry);

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                int newId = Convert.ToInt32(result);
                
                documento.IdDocumento = newId;

                return newId;
            }
        }


        public async Task<List<DetalleDocumentoToSap>> ObtenerDetalleDocumentoPorNumeroAsync(string numeroDocumento, string tipoDocumento)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = @"SELECT 
                    d.DocEntry,
                    d.NumeroDocumento,
                    dd.IdDetalle, 
                    dd.CodigoItem,
                    dd.NumeroLinea,
                    dd.CantidadContada AS TotalCantidadContada,
                    -- ISNULL(SUM(c.CantidadContada), 0) AS TotalCantidadContada,
                    STUFF((
	                    SELECT DISTINCT ', ' + c2.Usuario
	                    FROM ConteoItems c2
	                    WHERE c2.IdDetalle = dd.IdDetalle
                        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS UsuariosParticipantes
                FROM Documento d
                INNER JOIN DetalleDocumento dd ON d.IdDocumento = dd.IdDocumento
                LEFT JOIN ConteoItems c ON dd.IdDetalle = c.IdDetalle
                WHERE d.NumeroDocumento = @NumeroDocumento and d.TipoDocumento = @TipoDocumento
                GROUP BY d.DocEntry, d.NumeroDocumento, dd.IdDetalle, dd.CodigoItem, dd.NumeroLinea, dd.CantidadContada";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@NumeroDocumento", numeroDocumento);
                command.Parameters.AddWithValue("@TipoDocumento", tipoDocumento);

                await connection.OpenAsync();
                var detalles = new List<DetalleDocumentoToSap>();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        detalles.Add(new DetalleDocumentoToSap
                        {
                            DocEntry = (int)reader["DocEntry"],
                            NumeroDocumento = Convert.ToInt64(reader["NumeroDocumento"]),
                            IdDetalle = (int)reader["IdDetalle"],
                            CodigoItem = reader["CodigoItem"].ToString(),
                            NumeroLinea = (int)reader["NumeroLinea"],
                            TotalCantidadContada = Convert.ToDecimal(reader["TotalCantidadContada"]), 
                            UsuariosParticipantes = reader["UsuariosParticipantes"].ToString()
                        });
                    }
                }
                return detalles;
            }
        }
    }
}
