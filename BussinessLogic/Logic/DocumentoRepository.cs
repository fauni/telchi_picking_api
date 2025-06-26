using Core.Entities.Picking;
using Core.Entities.Sap;
using Core.Entities.SolicitudTraslado;
using Core.Entities.Ventas;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.ConstrainedExecution;
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

        public async Task<ResultadoActualizacionSap> GuardarConteoTransferenciaStock(string sessionID, int docEntry, string tipoDocumento)
        {
            string url = _configuration["SapCredentials:Url"] + "/U_TOM_CONTEO";
            var detalle = ObtenerDetalleDocumentoPorDocEntry(docEntry, tipoDocumento);

            try
            {
                using (var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
                using (var httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Add("Cookie", $"B1SESSION={sessionID}");
                    httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation"); // Para recibir la respuesta completa

                    foreach (var item in detalle)
                    {
                        // Verificar si el registro ya existe
                        var checkUrl = $"{url}?$filter=U_DocType eq '{item.DocEntry}'";
                        var checkResponse = await httpClient.GetAsync(checkUrl);

                        if (checkResponse.IsSuccessStatusCode)
                        {
                            var existingData = await checkResponse.Content.ReadAsStringAsync();
                            var exists = JObject.Parse(existingData)["value"]?.Any() == true;

                            HttpResponseMessage response;
                            var conteoData = new
                            {
                                Code = item.IdDetalle,
                                Name = $"transferencia_stock_{item.NumeroDocumento}_{item.NumeroLinea}",
                                U_DocType = docEntry,
                                U_ItemCode = item.CodigoItem,
                                U_QuantityCounted = item.TotalCantidadContada,
                                U_Username = item.UsuariosParticipantes
                            };
                            string json = JsonConvert.SerializeObject(conteoData);

                            if (exists)
                            {
                                var updateUrl = $"{url}('{item.IdDetalle}')";
                                // ACTUALIZAR (PUT para reemplazo completo o PATCH para parcial)
                                response = await httpClient.PatchAsync(updateUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                            }
                            else
                            {
                                // INSERTAR (POST)
                                response = await httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
                            }

                            if (!response.IsSuccessStatusCode)
                            {
                                var error = await response.Content.ReadAsStringAsync();
                                throw new Exception($"Error {(exists ? "actualizando" : "creando")} registro: {error}");
                            }
                        }
                    }

                    await ActualizarEstadoActualizacionSapAsync(docEntry, tipoDocumento);
                    return new ResultadoActualizacionSap { Exito = true, Mensaje = "Operación completada" };
                }
            }
            catch (Exception ex)
            {
                return new ResultadoActualizacionSap { Exito = false, Mensaje = ex.Message };
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
            else if(tipoDocumento == "solicitud_traslado")
            {
                url = _configuration["SapCredentials:Url"] + $"/InventoryTransferRequests({docEntry})";
            } else if(tipoDocumento == "orden_compra")
            {
                url = _configuration["SapCredentials:Url"] + $"/PurchaseOrders({docEntry})"; // Aqui la url para transferencias completar
            } else if (tipoDocumento == "entregas")
            {
                url = _configuration["SapCredentials:Url"] + $"/DeliveryNotes({docEntry})"; // Aqui la url para transferencias completar
            }
            else 
            {
                url = ""; // Revisar
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
                    string json;

                    if(tipoDocumento == "solicitud_traslado")
                    {
                        var documentSolicitud = new DocumentSolicitudTraslado
                        {
                            StockTransferLines = detalle.Select(d => new Core.Entities.Sap.DocumentLine
                            {
                                LineNum = d.NumeroLinea,
                                ItemCode = d.CodigoItem,
                                U_PCK_CantContada = (int)d.TotalCantidadContada,
                                U_PCK_ContUsuarios = d.UsuariosParticipantes
                            }).ToList()
                        };
                        json = JsonConvert.SerializeObject(documentSolicitud);
                    }
                    else
                    {
                        var document = new Core.Entities.Sap.Document
                        {
                            DocumentLines = detalle.Select(d => new Core.Entities.Sap.DocumentLine
                            {
                                LineNum = d.NumeroLinea,
                                ItemCode = d.CodigoItem,
                                U_PCK_CantContada = (int)d.TotalCantidadContada,
                                U_PCK_ContUsuarios = d.UsuariosParticipantes
                            }).ToList()
                        };
                        json = JsonConvert.SerializeObject(document);
                    }
                    
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

        public async Task ActualizaItemsDocumentoConteo(Order order, string tipoDocumento)
        {
            string doc_num = order.DocNum.ToString() ?? string.Empty;
            List<DetalleDocumentoToSap> detalleDocumento = await ObtenerDetalleDocumentoPorNumeroAsync(doc_num, tipoDocumento);

            int idDocumento;

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                using (SqlCommand getIdCommand = new SqlCommand("SELECT IdDocumento FROM Documento WHERE NumeroDocumento = @NumeroDocumento AND TipoDocumento = @TipoDocumento", connection))
                {
                    getIdCommand.Parameters.AddWithValue("@NumeroDocumento", doc_num);
                    getIdCommand.Parameters.AddWithValue("@TipoDocumento", tipoDocumento);
                    object result = await getIdCommand.ExecuteScalarAsync();
                    idDocumento = result != null ? Convert.ToInt32(result) : 0;
                }
            }

            if (idDocumento > 0)
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            await this.EliminarDuplicadosDetalleDocumentoAsync(idDocumento);
                            // Insertar o actualizar ítems de la orden
                            foreach (var detalleOrder in order.DocumentLines)
                            {
                                var existe = detalleDocumento.Any(d => d.CodigoItem == detalleOrder.ItemCode);
                                if (!existe)
                                {
                                    string insertSql = @"
                                INSERT INTO DetalleDocumento 
                                (IdDocumento, NumeroLinea, CodigoItem, DescripcionItem, CantidadEsperada, CantidadContada, Estado, CodigoBarras) 
                                VALUES 
                                (@IdDocumento, @NumeroLinea, @CodigoItem, @DescripcionItem, @CantidadEsperada, @CantidadContada, @Estado, @CodigoBarras);";

                                    using (SqlCommand cmd = new SqlCommand(insertSql, connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@IdDocumento", idDocumento);
                                        cmd.Parameters.AddWithValue("@NumeroLinea", detalleOrder.LineNum);
                                        cmd.Parameters.AddWithValue("@CodigoItem", detalleOrder.ItemCode);
                                        cmd.Parameters.AddWithValue("@DescripcionItem", detalleOrder.ItemDescription ?? string.Empty);
                                        cmd.Parameters.AddWithValue("@CantidadEsperada", detalleOrder.Quantity);
                                        cmd.Parameters.AddWithValue("@CantidadContada", 0);
                                        cmd.Parameters.AddWithValue("@Estado", "Pendiente");
                                        cmd.Parameters.Add(new SqlParameter("@CodigoBarras", SqlDbType.NVarChar)
                                        {
                                            Value = detalleOrder.BarCode ?? (object)DBNull.Value
                                        });

                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else
                                {
                                    var detalle = detalleDocumento.Find(d => d.CodigoItem == detalleOrder.ItemCode);
                                    string updateSql = "UPDATE DetalleDocumento SET CantidadEsperada = @CantidadEsperada WHERE IdDetalle = @IdDetalle";

                                    using (SqlCommand cmd = new SqlCommand(updateSql, connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@CantidadEsperada", detalleOrder.Quantity);
                                        cmd.Parameters.AddWithValue("@IdDetalle", detalle.IdDetalle);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            // 🔥 Eliminar ítems que ya no están en la nueva orden
                            var codigosNuevos = order.DocumentLines.Select(l => l.ItemCode).ToHashSet();
                            var codigosActuales = detalleDocumento.Select(d => d.CodigoItem).ToList();
                            var codigosParaEliminar = codigosActuales.Except(codigosNuevos).ToList();

                            foreach (var codigoEliminar in codigosParaEliminar)
                            {
                                var detalleEliminar = detalleDocumento.FirstOrDefault(d => d.CodigoItem == codigoEliminar);
                                if (detalleEliminar != null)
                                {
                                    const string deleteSql = "DELETE FROM DetalleDocumento WHERE IdDetalle = @IdDetalle";
                                    using (var cmd = new SqlCommand(deleteSql, connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@IdDetalle", detalleEliminar.IdDetalle);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            transaction.Commit();
                            await ActualizarEstadoDocumentoAsync(idDocumento);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception("Error al actualizar ítems del documento de conteo.", ex);
                        }
                    }
                }
            }
        }

        public async Task EliminarDuplicadosDetalleDocumentoAsync(int idDocumento)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Paso 1: Obtener todos los detalles del documento
                        var detalleDocumento = new List<(int IdDetalle, string CodigoItem)>();

                        using (SqlCommand cmd = new SqlCommand("SELECT IdDetalle, CodigoItem FROM DetalleDocumento WHERE IdDocumento = @IdDocumento", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@IdDocumento", idDocumento);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    int idDetalle = reader.GetInt32(0);
                                    string codigoItem = reader.GetString(1);
                                    detalleDocumento.Add((idDetalle, codigoItem));
                                }
                            }
                        }

                        // Paso 2: Agrupar por CodigoItem y eliminar duplicados
                        var duplicados = detalleDocumento
                            .GroupBy(d => d.CodigoItem)
                            .SelectMany(g => g.Skip(1)) // Conserva el primero, elimina los demás
                            .ToList();

                        foreach (var dup in duplicados)
                        {
                            using (SqlCommand deleteCmd = new SqlCommand("DELETE FROM DetalleDocumento WHERE IdDetalle = @IdDetalle", connection, transaction))
                            {
                                deleteCmd.Parameters.AddWithValue("@IdDetalle", dup.IdDetalle);
                                await deleteCmd.ExecuteNonQueryAsync();
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception("Error al eliminar duplicados en DetalleDocumento.", ex);
                    }
                }
            }
        }

        //public async Task ActualizaItemsDocumentoConteo(Order order, string tipoDocumento)
        //{
        //    string doc_num = order.DocNum.ToString() ?? String.Empty;
        //     List<DetalleDocumentoToSap> detalleDocumento = await ObtenerDetalleDocumentoPorNumeroAsync(doc_num, tipoDocumento);

        //    // Obtenemos el Id del documento
        //    int idDocumento;

        //    using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        //    {
        //        await connection.OpenAsync();
        //        using (SqlCommand getIdCommand = new SqlCommand($"SELECT IdDocumento FROM Documento WHERE NumeroDocumento = @NumeroDocumento and TipoDocumento = @TipoDocumento", connection))
        //        {
        //            getIdCommand.Parameters.AddWithValue("@NumeroDocumento", doc_num);
        //            getIdCommand.Parameters.AddWithValue("@TipoDocumento", tipoDocumento);
        //            object result = await getIdCommand.ExecuteScalarAsync();
        //            idDocumento = result != null ? Convert.ToInt32(result) : 0;
        //        }
        //    }

        //    if (idDocumento > 0) 
        //    {
        //        using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        //        {
        //            await connection.OpenAsync();
        //            using (SqlTransaction transaction = connection.BeginTransaction())
        //            {

        //                try
        //                {
        //                    foreach (var detalleOrder in order.DocumentLines)
        //                    {
        //                        var existe = detalleDocumento.Any(d => d.CodigoItem == detalleOrder.ItemCode);
        //                        if (!existe)
        //                        {
        //                            string sqlDetalle = "INSERT INTO DetalleDocumento (IdDocumento, NumeroLinea ,CodigoItem, DescripcionItem, CantidadEsperada, CantidadContada, Estado, CodigoBarras) " +
        //                                "VALUES (@IdDocumento, @NumeroLinea, @CodigoItem, @DescripcionItem, @CantidadEsperada, @CantidadContada, @Estado, @CodigoBarras);";

        //                            SqlCommand commandDetalle = new SqlCommand(sqlDetalle, connection, transaction);
        //                            commandDetalle.Parameters.AddWithValue("@IdDocumento", idDocumento);
        //                            commandDetalle.Parameters.AddWithValue("@NumeroLinea", detalleOrder.LineNum);
        //                            commandDetalle.Parameters.AddWithValue("@CodigoItem", detalleOrder.ItemCode);
        //                            commandDetalle.Parameters.AddWithValue("@DescripcionItem", detalleOrder.ItemDescription);
        //                            commandDetalle.Parameters.AddWithValue("@CantidadEsperada", detalleOrder.Quantity);
        //                            commandDetalle.Parameters.AddWithValue("@CantidadContada", 0); // Iniciar en 0
        //                            commandDetalle.Parameters.AddWithValue("@Estado", "Pendiente"); // Estado inicial 'Pendiente'
        //                            commandDetalle.Parameters.Add(new SqlParameter("@CodigoBarras", SqlDbType.NVarChar)
        //                            {
        //                                Value = detalleOrder.BarCode ?? (object)DBNull.Value
        //                            }); // Maneja el nulo

        //                            await commandDetalle.ExecuteNonQueryAsync();
        //                        } else
        //                        {
        //                            var detalle = detalleDocumento.Find(d => d.CodigoItem == detalleOrder.ItemCode);
        //                            string sqlDetalle = $"UPDATE DetalleDocumento SET CantidadEsperada = @CantidadEsperada WHERE IdDetalle = {detalle.IdDetalle}";
        //                            SqlCommand commandDetalle = new SqlCommand(sqlDetalle, connection, transaction);
        //                            commandDetalle.Parameters.AddWithValue("@CantidadEsperada", detalleOrder.Quantity);
        //                            await commandDetalle.ExecuteNonQueryAsync();
        //                        }
        //                    }
        //                    transaction.Commit();
        //                    await ActualizarEstadoDocumentoAsync(idDocumento);
        //                }
        //                catch (Exception ex)
        //                {
        //                    transaction.Rollback();
        //                    throw;
        //                }
        //            }
        //        }
        //    }   
        //}

        public async Task ActualizaItemsDocumentoConteoSolicitud(OWTQ solicitud, string tipoDocumento)
        {
            string doc_num = solicitud.DocNum.ToString() ?? string.Empty;
            List<DetalleDocumentoToSap> detalleDocumento = await ObtenerDetalleDocumentoPorNumeroAsync(doc_num, tipoDocumento);

            int idDocumento = 0;

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                using (SqlCommand getIdCommand = new SqlCommand("SELECT IdDocumento FROM Documento WHERE NumeroDocumento = @NumeroDocumento AND TipoDocumento = 'solicitud_traslado'", connection))
                {
                    getIdCommand.Parameters.AddWithValue("@NumeroDocumento", doc_num);
                    object result = await getIdCommand.ExecuteScalarAsync();
                    idDocumento = result != null ? Convert.ToInt32(result) : 0;
                }

                if (idDocumento > 0)
                {
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var codigosNuevos = solicitud.Lines.Select(l => l.ItemCode).ToHashSet();

                            foreach (var detalleOrder in solicitud.Lines)
                            {
                                var existente = detalleDocumento.FirstOrDefault(d => d.CodigoItem == detalleOrder.ItemCode);
                                if (existente == null)
                                {
                                    const string insertSql = @"
                                INSERT INTO DetalleDocumento 
                                (IdDocumento, NumeroLinea, CodigoItem, DescripcionItem, CantidadEsperada, CantidadContada, Estado, CodigoBarras) 
                                VALUES 
                                (@IdDocumento, @NumeroLinea, @CodigoItem, @DescripcionItem, @CantidadEsperada, @CantidadContada, @Estado, @CodigoBarras);";

                                    using (var cmd = new SqlCommand(insertSql, connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@IdDocumento", idDocumento);
                                        cmd.Parameters.AddWithValue("@NumeroLinea", detalleOrder.LineNum);
                                        cmd.Parameters.AddWithValue("@CodigoItem", detalleOrder.ItemCode);
                                        cmd.Parameters.AddWithValue("@DescripcionItem", detalleOrder.Dscription ?? string.Empty);
                                        cmd.Parameters.AddWithValue("@CantidadEsperada", detalleOrder.Quantity);
                                        cmd.Parameters.AddWithValue("@CantidadContada", 0);
                                        cmd.Parameters.AddWithValue("@Estado", "Pendiente");
                                        cmd.Parameters.AddWithValue("@CodigoBarras", detalleOrder.CodeBars ?? (object)DBNull.Value);

                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                                else
                                {
                                    const string updateSql = @"
                                UPDATE DetalleDocumento 
                                SET CantidadEsperada = @CantidadEsperada 
                                WHERE IdDetalle = @IdDetalle";

                                    using (var cmd = new SqlCommand(updateSql, connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@CantidadEsperada", detalleOrder.Quantity);
                                        cmd.Parameters.AddWithValue("@IdDetalle", existente.IdDetalle);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            // 🔥 ELIMINAR los ítems que ya no están en la nueva solicitud
                            var codigosActuales = detalleDocumento.Select(d => d.CodigoItem).ToList();
                            var codigosParaEliminar = codigosActuales.Except(codigosNuevos).ToList();

                            foreach (var codigoEliminar in codigosParaEliminar)
                            {
                                const string deleteSql = "DELETE FROM DetalleDocumento WHERE IdDocumento = @IdDocumento AND CodigoItem = @CodigoItem";
                                using (var cmd = new SqlCommand(deleteSql, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@IdDocumento", idDocumento);
                                    cmd.Parameters.AddWithValue("@CodigoItem", codigoEliminar);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception("Error en la sincronización de ítems del documento", ex);
                        }
                    }
                }
            }
        }

        //public async Task ActualizaItemsDocumentoConteoSolicitud(OWTQ solicitud, string tipoDocumento)
        //{
        //    string doc_num = solicitud.DocNum.ToString() ?? String.Empty;
        //    List<DetalleDocumentoToSap> detalleDocumento = await ObtenerDetalleDocumentoPorNumeroAsync(doc_num, tipoDocumento);

        //    // Obtenemos el Id del documento
        //    int idDocumento;

        //    using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        //    {
        //        await connection.OpenAsync();
        //        using (SqlCommand getIdCommand = new SqlCommand("SELECT IdDocumento FROM Documento WHERE NumeroDocumento = @NumeroDocumento and TipoDocumento = 'solicitud_traslado'", connection))
        //        {
        //            getIdCommand.Parameters.AddWithValue("@NumeroDocumento", doc_num);
        //            object result = await getIdCommand.ExecuteScalarAsync();
        //            idDocumento = result != null ? Convert.ToInt32(result) : 0;
        //        }
        //    }

        //    if (idDocumento > 0)
        //    {
        //        using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        //        {
        //            await connection.OpenAsync();
        //            using (SqlTransaction transaction = connection.BeginTransaction())
        //            {

        //                try
        //                {
        //                    foreach (var detalleOrder in solicitud.Lines)
        //                    {
        //                        var existe = detalleDocumento.Any(d => d.CodigoItem == detalleOrder.ItemCode);
        //                        if (!existe)
        //                        {
        //                            string sqlDetalle = "INSERT INTO DetalleDocumento (IdDocumento, NumeroLinea ,CodigoItem, DescripcionItem, CantidadEsperada, CantidadContada, Estado, CodigoBarras) " +
        //                                "VALUES (@IdDocumento, @NumeroLinea, @CodigoItem, @DescripcionItem, @CantidadEsperada, @CantidadContada, @Estado, @CodigoBarras);";

        //                            SqlCommand commandDetalle = new SqlCommand(sqlDetalle, connection, transaction);
        //                            commandDetalle.Parameters.AddWithValue("@IdDocumento", idDocumento);
        //                            commandDetalle.Parameters.AddWithValue("@NumeroLinea", detalleOrder.LineNum);
        //                            commandDetalle.Parameters.AddWithValue("@CodigoItem", detalleOrder.ItemCode);
        //                            commandDetalle.Parameters.AddWithValue("@DescripcionItem", detalleOrder.Dscription);
        //                            commandDetalle.Parameters.AddWithValue("@CantidadEsperada", detalleOrder.Quantity);
        //                            commandDetalle.Parameters.AddWithValue("@CantidadContada", 0); // Iniciar en 0
        //                            commandDetalle.Parameters.AddWithValue("@Estado", "Pendiente"); // Estado inicial 'Pendiente'
        //                            commandDetalle.Parameters.Add(new SqlParameter("@CodigoBarras", SqlDbType.NVarChar)
        //                            {
        //                                Value = detalleOrder.CodeBars ?? (object)DBNull.Value
        //                            }); // Maneja el nulo

        //                            await commandDetalle.ExecuteNonQueryAsync();
        //                        }
        //                        else
        //                        {
        //                            var detalle = detalleDocumento.Find(d => d.CodigoItem == detalleOrder.ItemCode);
        //                            string sqlDetalle = $"UPDATE DetalleDocumento SET CantidadEsperada = @CantidadEsperada WHERE IdDetalle = {detalle.IdDetalle}";
        //                            SqlCommand commandDetalle = new SqlCommand(sqlDetalle, connection, transaction);
        //                            commandDetalle.Parameters.AddWithValue("@CantidadEsperada", detalleOrder.Quantity);
        //                            await commandDetalle.ExecuteNonQueryAsync();
        //                        }
        //                    }
        //                    transaction.Commit();
        //                }
        //                catch (Exception ex)
        //                {
        //                    transaction.Rollback();
        //                    throw;
        //                }
        //            }
        //        }
        //    }
        //}

        public async Task ActualizaItemsDocumentoConteoTransferenciaStock(OWTQ solicitud, string tipoDocumento)
        {
            string doc_num = solicitud.DocNum.ToString() ?? String.Empty;
            List<DetalleDocumentoToSap> detalleDocumento = await ObtenerDetalleDocumentoPorNumeroAsync(doc_num, tipoDocumento);

            // Obtenemos el Id del documento
            int idDocumento;

            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (SqlCommand getIdCommand = new SqlCommand("SELECT IdDocumento FROM Documento WHERE NumeroDocumento = @NumeroDocumento and TipoDocumento = 'transferencia_stock'", connection))
                {
                    getIdCommand.Parameters.AddWithValue("@NumeroDocumento", doc_num);
                    object result = await getIdCommand.ExecuteScalarAsync();
                    idDocumento = result != null ? Convert.ToInt32(result) : 0;
                }
            }

            if (idDocumento > 0)
            {
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {

                        try
                        {
                            foreach (var detalleOrder in solicitud.Lines)
                            {
                                var existe = detalleDocumento.Any(d => d.CodigoItem == detalleOrder.ItemCode);
                                if (!existe)
                                {
                                    string sqlDetalle = "INSERT INTO DetalleDocumento (IdDocumento, NumeroLinea ,CodigoItem, DescripcionItem, CantidadEsperada, CantidadContada, Estado, CodigoBarras) " +
                                        "VALUES (@IdDocumento, @NumeroLinea, @CodigoItem, @DescripcionItem, @CantidadEsperada, @CantidadContada, @Estado, @CodigoBarras);";

                                    SqlCommand commandDetalle = new SqlCommand(sqlDetalle, connection, transaction);
                                    commandDetalle.Parameters.AddWithValue("@IdDocumento", idDocumento);
                                    commandDetalle.Parameters.AddWithValue("@NumeroLinea", detalleOrder.LineNum);
                                    commandDetalle.Parameters.AddWithValue("@CodigoItem", detalleOrder.ItemCode);
                                    commandDetalle.Parameters.AddWithValue("@DescripcionItem", detalleOrder.Dscription);
                                    commandDetalle.Parameters.AddWithValue("@CantidadEsperada", detalleOrder.Quantity);
                                    commandDetalle.Parameters.AddWithValue("@CantidadContada", 0); // Iniciar en 0
                                    commandDetalle.Parameters.AddWithValue("@Estado", "Pendiente"); // Estado inicial 'Pendiente'
                                    commandDetalle.Parameters.Add(new SqlParameter("@CodigoBarras", SqlDbType.NVarChar)
                                    {
                                        Value = detalleOrder.CodeBars ?? (object)DBNull.Value
                                    }); // Maneja el nulo

                                    await commandDetalle.ExecuteNonQueryAsync();
                                }
                                else
                                {
                                    var detalle = detalleDocumento.Find(d => d.CodigoItem == detalleOrder.ItemCode);
                                    string sqlDetalle = $"UPDATE DetalleDocumento SET CantidadEsperada = @CantidadEsperada WHERE IdDetalle = {detalle.IdDetalle}";
                                    SqlCommand commandDetalle = new SqlCommand(sqlDetalle, connection, transaction);
                                    commandDetalle.Parameters.AddWithValue("@CantidadEsperada", detalleOrder.Quantity);
                                    await commandDetalle.ExecuteNonQueryAsync();
                                }
                            }
                            transaction.Commit();
                            await ActualizarEstadoDocumentoAsync(idDocumento);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
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

        public async Task<int> CreateDocumentFromSolicitudAsync(OWTQ solicitud)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
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
                        commandDocumento.Parameters.AddWithValue("@TipoDocumento", "solicitud_traslado");
                        commandDocumento.Parameters.AddWithValue("@NumeroDocumento", solicitud.DocNum);
                        commandDocumento.Parameters.AddWithValue("@FechaInicio", DateTime.Now); // Fecha actual de creación
                        commandDocumento.Parameters.AddWithValue("@EstadoConteo", 'P'); // Estado inicial 'Pendiente'
                        commandDocumento.Parameters.AddWithValue("@DocEntry", solicitud.DocEntry);

                        var result = await commandDocumento.ExecuteScalarAsync();
                        int documentId = Convert.ToInt32(result);

                        // Paso 2: Insertar cada línea en la tabla Detalle Documento
                        string sqlDetalle = "INSERT INTO DetalleDocumento (IdDocumento, NumeroLinea ,CodigoItem, DescripcionItem, CantidadEsperada, CantidadContada, Estado, CodigoBarras) " +
                                    "VALUES (@IdDocumento, @NumeroLinea, @CodigoItem, @DescripcionItem, @CantidadEsperada, @CantidadContada, @Estado, @CodigoBarras);";

                        foreach (var line in solicitud.Lines)
                        {
                            SqlCommand commandDetalle = new SqlCommand(sqlDetalle, connection, transaction);
                            commandDetalle.Parameters.AddWithValue("@IdDocumento", documentId);
                            commandDetalle.Parameters.AddWithValue("@NumeroLinea", line.LineNum);
                            commandDetalle.Parameters.AddWithValue("@CodigoItem", line.ItemCode);
                            commandDetalle.Parameters.AddWithValue("@DescripcionItem", line.Dscription);
                            commandDetalle.Parameters.AddWithValue("@CantidadEsperada", line.Quantity);
                            commandDetalle.Parameters.AddWithValue("@CantidadContada", 0); // Iniciar en 0
                            commandDetalle.Parameters.AddWithValue("@Estado", "Pendiente"); // Estado inicial 'Pendiente'
                            commandDetalle.Parameters.Add(new SqlParameter("@CodigoBarras", SqlDbType.NVarChar)
                            {
                                Value = line.CodeBars ?? (object)DBNull.Value
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

        public async Task<int> CreateDocumentFromTransferenciaAsync(OWTQ transferencia)
        {
            using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
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
                        commandDocumento.Parameters.AddWithValue("@TipoDocumento", "transferencia_stock");
                        commandDocumento.Parameters.AddWithValue("@NumeroDocumento", transferencia.DocNum);
                        commandDocumento.Parameters.AddWithValue("@FechaInicio", DateTime.Now); // Fecha actual de creación
                        commandDocumento.Parameters.AddWithValue("@EstadoConteo", 'P'); // Estado inicial 'Pendiente'
                        commandDocumento.Parameters.AddWithValue("@DocEntry", transferencia.DocEntry);

                        var result = await commandDocumento.ExecuteScalarAsync();
                        int documentId = Convert.ToInt32(result);

                        // Paso 2: Insertar cada línea en la tabla Detalle Documento
                        string sqlDetalle = "INSERT INTO DetalleDocumento (IdDocumento, NumeroLinea ,CodigoItem, DescripcionItem, CantidadEsperada, CantidadContada, Estado, CodigoBarras) " +
                                    "VALUES (@IdDocumento, @NumeroLinea, @CodigoItem, @DescripcionItem, @CantidadEsperada, @CantidadContada, @Estado, @CodigoBarras);";
                        
                        foreach (var line in transferencia.Lines)
                        {
                            SqlCommand commandDetalle = new SqlCommand(sqlDetalle, connection, transaction);
                            commandDetalle.Parameters.AddWithValue("@IdDocumento", documentId);
                            commandDetalle.Parameters.AddWithValue("@NumeroLinea", line.LineNum);
                            commandDetalle.Parameters.AddWithValue("@CodigoItem", line.ItemCode);
                            commandDetalle.Parameters.AddWithValue("@DescripcionItem", line.Dscription);
                            commandDetalle.Parameters.AddWithValue("@CantidadEsperada", line.Quantity);
                            commandDetalle.Parameters.AddWithValue("@CantidadContada", 0); // Iniciar en 0
                            commandDetalle.Parameters.AddWithValue("@Estado", "Pendiente"); // Estado inicial 'Pendiente'
                            commandDetalle.Parameters.Add(new SqlParameter("@CodigoBarras", SqlDbType.NVarChar)
                            {
                                Value = line.CodeBars ?? (object)DBNull.Value
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

        public List<DetalleDocumentoToSap> ObtenerDetalleDocumentoPorDocEntry(int docEntry, string tipoDocumento)
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
                    STUFF((
	                    SELECT DISTINCT ', ' + c2.Usuario
	                    FROM ConteoItems c2
	                    WHERE c2.IdDetalle = dd.IdDetalle
                        FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS UsuariosParticipantes
                FROM Documento d
                INNER JOIN DetalleDocumento dd ON d.IdDocumento = dd.IdDocumento
                LEFT JOIN ConteoItems c ON dd.IdDetalle = c.IdDetalle
                WHERE d.DocEntry = @DocEntry and d.TipoDocumento = @TipoDocumento
                GROUP BY d.DocEntry, d.NumeroDocumento, dd.IdDetalle, dd.CodigoItem, dd.NumeroLinea, dd.CantidadContada";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@DocEntry", docEntry);
                command.Parameters.AddWithValue("@TipoDocumento", tipoDocumento);

                connection.Open();
                var detalles = new List<DetalleDocumentoToSap>();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
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
