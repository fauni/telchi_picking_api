using Core.Entities.SolicitudTraslado;
using Data.Interfaces;
using Microsoft.Extensions.Configuration;
using Sap.Data.Hana;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Implementation
{
    public class SolicitudTrasladoRepository : ISolicitudTrasladoRepository
    {
        IConfiguration _configuration;
        string _connectionString;

        public SolicitudTrasladoRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("SapHanaConnection");
        }

        public List<OWTQ> GetAll(int pageNumber, int pageSize)
        {
            List<OWTQ> solicitudes = new List<OWTQ>();

            try
            {
                using (HanaConnection connection = new HanaConnection(_connectionString))
                {
                    connection.Open();

                    int offset = (pageNumber - 1) * pageSize;
                    string sql = $"SELECT \"DocEntry\", \"DocNum\", \"Series\",\"DocDate\", \"DocDueDate\", \"Comments\", \"JrnlMemo\", \"Filler\",\"ToWhsCode\", \"DocStatus\" " +
                        $"FROM \"TELCHI_QAS_EC\".\"OWTQ\" order by \"DocEntry\" desc LIMIT {pageSize} OFFSET {offset}";

                    using (HanaCommand command = new HanaCommand(sql, connection))
                    {
                        using (HanaDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                OWTQ solicitud = new OWTQ
                                {
                                    DocEntry = reader.GetInt32(reader.GetOrdinal("DocEntry")),
                                    DocNum = reader.GetInt32(reader.GetOrdinal("DocNum")),
                                    Series = reader.GetInt32(reader.GetOrdinal("Series")),
                                    DocDate = reader.GetDateTime(reader.GetOrdinal("DocDate")),
                                    DocDueDate = reader.GetDateTime(reader.GetOrdinal("DocDueDate")),
                                    Comments = reader.IsDBNull(reader.GetOrdinal("Comments")) ? null : reader.GetString(reader.GetOrdinal("Comments")),
                                    JrnlMemo = reader.IsDBNull(reader.GetOrdinal("JrnlMemo")) ? null : reader.GetString(reader.GetOrdinal("JrnlMemo")),
                                    Filler = reader.GetString(reader.GetOrdinal("Filler")),
                                    ToWhsCode = reader.GetString(reader.GetOrdinal("ToWhsCode")),
                                    DocStatus = reader.GetString(reader.GetOrdinal("DocStatus")),
                                    Lines = new List<WTQ1>()
                                };

                                // Obtener las líneas para la solicitud actual 
                                string sqlLines = "SELECT \"DocEntry\", \"LineNum\", \"LineStatus\", \"ItemCode\", \"Dscription\", \"CodeBars\", \"Quantity\", \"FromWhsCod\", \"WhsCode\", \"U_PCK_CantContada\", \"U_PCK_ContUsuarios\" " +
                                "FROM \"TELCHI_QAS_EC\".\"WTQ1\" " +
                                $"WHERE \"DocEntry\" = {solicitud.DocEntry}";

                                using (HanaCommand commandLines = new HanaCommand(sqlLines, connection))
                                {
                                    using (HanaDataReader readerLines = commandLines.ExecuteReader())
                                    {
                                        // Aseguramos que solicitud.Lines esté inicializado antes del bucle
                                        solicitud.Lines ??= new List<WTQ1>();
                                        while (readerLines.Read())
                                        {
                                            WTQ1 line = new WTQ1();
                                            line.DocEntry = readerLines.GetInt32(readerLines.GetOrdinal("DocEntry"));
                                            line.LineNum = readerLines.GetInt32(readerLines.GetOrdinal("LineNum"));
                                            line.LineStatus = readerLines.GetString(readerLines.GetOrdinal("LineStatus"));
                                            line.ItemCode = readerLines.GetString(readerLines.GetOrdinal("ItemCode"));
                                            line.Dscription = readerLines.GetString(readerLines.GetOrdinal("Dscription"));
                                            line.CodeBars = readerLines.IsDBNull(readerLines.GetOrdinal("CodeBars")) ? null : readerLines.GetString(readerLines.GetOrdinal("CodeBars"));
                                            line.Quantity = readerLines.GetDecimal(readerLines.GetOrdinal("Quantity"));
                                            line.FromWhsCod = readerLines.GetString(readerLines.GetOrdinal("FromWhsCod"));
                                            line.WhsCode = readerLines.GetString(readerLines.GetOrdinal("WhsCode"));
                                            line.U_PCK_CantContada = readerLines.IsDBNull(readerLines.GetOrdinal("U_PCK_CantContada")) ? (decimal?)null : readerLines.GetDecimal(readerLines.GetOrdinal("U_PCK_CantContada"));
                                            line.U_PCK_ContUsuarios = readerLines.IsDBNull(readerLines.GetOrdinal("U_PCK_ContUsuarios")) ? null : readerLines.GetString(readerLines.GetOrdinal("U_PCK_ContUsuarios"));
                                            solicitud.Lines.Add(line);
                                        }
                                    }
                                }

                                solicitudes.Add(solicitud);
                            }
                        }
                    }
                }

                return solicitudes;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public List<OWTQ> GetAllSearch(int pageNumber, int pageSize, string? search = null, DateTime? docDate = null)
        {
            List<OWTQ> solicitudes = new List<OWTQ>();

            try
            {
                using (HanaConnection connection = new HanaConnection(_connectionString))
                {
                    connection.Open();

                    int offset = (pageNumber - 1) * pageSize;

                    // Construcción dinámica de la consulta con filtros opcionales
                    string sql = "SELECT \"DocEntry\", \"DocNum\", \"Series\", \"DocDate\", \"DocDueDate\", \"Comments\", \"JrnlMemo\", \"Filler\", \"ToWhsCode\", \"DocStatus\" " +
                                 "FROM \"TELCHI_QAS_EC\".\"OWTQ\" ";

                    List<string> filters = new List<string>();

                    if (!string.IsNullOrEmpty(search))
                        filters.Add($"\"DocNum\" like '%{search.Replace("'", "''")}%'"); // Escapar comillas simples para evitar SQL Injection

                    if (docDate.HasValue)
                        filters.Add($"\"DocDate\" = '{docDate.Value:yyyy-MM-dd}'");

                    if (filters.Count > 0)
                        sql += "WHERE " + string.Join(" AND ", filters);

                    sql += $" ORDER BY \"DocEntry\" DESC LIMIT {pageSize} OFFSET {offset}";

                    using (HanaCommand command = new HanaCommand(sql, connection))
                    {
                        using (HanaDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                OWTQ solicitud = new OWTQ
                                {
                                    DocEntry = reader.GetInt32(reader.GetOrdinal("DocEntry")),
                                    DocNum = reader.GetInt32(reader.GetOrdinal("DocNum")),
                                    Series = reader.GetInt32(reader.GetOrdinal("Series")),
                                    DocDate = reader.GetDateTime(reader.GetOrdinal("DocDate")),
                                    DocDueDate = reader.GetDateTime(reader.GetOrdinal("DocDueDate")),
                                    Comments = reader.IsDBNull(reader.GetOrdinal("Comments")) ? null : reader.GetString(reader.GetOrdinal("Comments")),
                                    JrnlMemo = reader.IsDBNull(reader.GetOrdinal("JrnlMemo")) ? null : reader.GetString(reader.GetOrdinal("JrnlMemo")),
                                    Filler = reader.GetString(reader.GetOrdinal("Filler")),
                                    ToWhsCode = reader.GetString(reader.GetOrdinal("ToWhsCode")),
                                    DocStatus = reader.GetString(reader.GetOrdinal("DocStatus")),
                                    Lines = new List<WTQ1>()
                                };

                                // Obtener las líneas para la solicitud actual 
                                string sqlLines = "SELECT \"DocEntry\", \"LineNum\", \"LineStatus\", \"ItemCode\", \"Dscription\", \"CodeBars\", \"Quantity\", \"FromWhsCod\", \"WhsCode\", \"U_PCK_CantContada\", \"U_PCK_ContUsuarios\" " +
                                "FROM \"TELCHI_QAS_EC\".\"WTQ1\" " +
                                $"WHERE \"DocEntry\" = {solicitud.DocEntry}";

                                using (HanaCommand commandLines = new HanaCommand(sqlLines, connection))
                                {
                                    using (HanaDataReader readerLines = commandLines.ExecuteReader())
                                    {
                                        // Aseguramos que solicitud.Lines esté inicializado antes del bucle
                                        solicitud.Lines ??= new List<WTQ1>();
                                        while (readerLines.Read())
                                        {
                                            WTQ1 line = new WTQ1();
                                            line.DocEntry = readerLines.GetInt32(readerLines.GetOrdinal("DocEntry"));
                                            line.LineNum = readerLines.GetInt32(readerLines.GetOrdinal("LineNum"));
                                            line.LineStatus = readerLines.GetString(readerLines.GetOrdinal("LineStatus"));
                                            line.ItemCode = readerLines.GetString(readerLines.GetOrdinal("ItemCode"));
                                            line.Dscription = readerLines.GetString(readerLines.GetOrdinal("Dscription"));
                                            line.CodeBars = readerLines.IsDBNull(readerLines.GetOrdinal("CodeBars")) ? null : readerLines.GetString(readerLines.GetOrdinal("CodeBars"));
                                            line.Quantity = readerLines.GetDecimal(readerLines.GetOrdinal("Quantity"));
                                            line.FromWhsCod = readerLines.GetString(readerLines.GetOrdinal("FromWhsCod"));
                                            line.WhsCode = readerLines.GetString(readerLines.GetOrdinal("WhsCode"));
                                            line.U_PCK_CantContada = readerLines.IsDBNull(readerLines.GetOrdinal("U_PCK_CantContada")) ? (decimal?)null : readerLines.GetDecimal(readerLines.GetOrdinal("U_PCK_CantContada"));
                                            line.U_PCK_ContUsuarios = readerLines.IsDBNull(readerLines.GetOrdinal("U_PCK_ContUsuarios")) ? null : readerLines.GetString(readerLines.GetOrdinal("U_PCK_ContUsuarios"));
                                            solicitud.Lines.Add(line);
                                        }
                                    }
                                }

                                solicitudes.Add(solicitud);
                            }
                        }
                    }
                }

                return solicitudes;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public OWTQ GetByID(int id)
         {
            OWTQ solicitud = null;

            using (HanaConnection connection = new HanaConnection(_connectionString))
            {
                connection.Open();
                // string sql = "SELECT \"DocEntry\", \"DocNum\", \"Series\",\"DocDate\", \"DocDueDate\", \"Comments\", \"JrnlMemo\", \"Filler\",\"ToWhsCode\", \"DocStatus\" FROM \"TELCHI_QAS_EC\".\"OWTQ\" WHERE \"DocEntry\" = @DocEntry";
                string sql = $"SELECT \"DocEntry\", \"DocNum\", \"Series\", \"DocDate\", \"DocDueDate\", \"Comments\", \"JrnlMemo\", \"Filler\", \"ToWhsCode\", \"DocStatus\" " +
                    $"FROM \"TELCHI_QAS_EC\".\"OWTQ\" " +
                    $"WHERE \"DocEntry\" = {id}";

                using (HanaCommand command = new HanaCommand(sql, connection)) 
                { 
                    // command.Parameters.Add("@DocEntry", HanaDbType.Integer, 11).Value = id;

                    using (HanaDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            solicitud = new OWTQ
                            {
                                DocEntry = reader.GetInt32(reader.GetOrdinal("DocEntry")),
                                DocNum = reader.GetInt32(reader.GetOrdinal("DocNum")),
                                Series = reader.GetInt32(reader.GetOrdinal("Series")),
                                DocDate = reader.GetDateTime(reader.GetOrdinal("DocDate")),
                                DocDueDate = reader.GetDateTime(reader.GetOrdinal("DocDueDate")),
                                Comments = reader.IsDBNull(reader.GetOrdinal("Comments")) ? null : reader.GetString(reader.GetOrdinal("Comments")),
                                JrnlMemo = reader.GetString(reader.GetOrdinal("JrnlMemo")),
                                Filler = reader.GetString(reader.GetOrdinal("Filler")),
                                ToWhsCode = reader.GetString(reader.GetOrdinal("ToWhsCode")),
                                DocStatus = reader.GetString(reader.GetOrdinal("DocStatus"))
                            };
                        }
                    }
                }

                if (solicitud != null)
                {
                    string sqlLines = "SELECT \"DocEntry\", \"LineNum\", \"LineStatus\", \"ItemCode\", \"Dscription\", \"CodeBars\", \"Quantity\", \"FromWhsCod\", \"WhsCode\", \"U_PCK_CantContada\", \"U_PCK_ContUsuarios\" " +
                        "FROM \"TELCHI_QAS_EC\".\"WTQ1\" " +
                        $"WHERE \"DocEntry\" = {id}";

                    using (HanaCommand commandLines = new HanaCommand(sqlLines, connection))
                    {
                        using(HanaDataReader readerLines = commandLines.ExecuteReader())
                        {
                            // Aseguramos que solicitud.Lines esté inicializado antes del bucle
                            solicitud.Lines ??= new List<WTQ1>(); 
                            while (readerLines.Read())
                            {
                                WTQ1 line = new WTQ1();
                                line.DocEntry = readerLines.GetInt32(readerLines.GetOrdinal("DocEntry"));
                                line.LineNum = readerLines.GetInt32(readerLines.GetOrdinal("LineNum"));
                                line.LineStatus = readerLines.GetString(readerLines.GetOrdinal("LineStatus"));
                                line.ItemCode = readerLines.GetString(readerLines.GetOrdinal("ItemCode"));
                                line.Dscription = readerLines.GetString(readerLines.GetOrdinal("Dscription"));
                                line.CodeBars = readerLines.IsDBNull(readerLines.GetOrdinal("CodeBars")) ? null : readerLines.GetString(readerLines.GetOrdinal("CodeBars"));
                                line.Quantity = readerLines.GetDecimal(readerLines.GetOrdinal("Quantity"));
                                line.FromWhsCod = readerLines.GetString(readerLines.GetOrdinal("FromWhsCod"));
                                line.WhsCode = readerLines.GetString(readerLines.GetOrdinal("WhsCode"));
                                line.U_PCK_CantContada = readerLines.IsDBNull(readerLines.GetOrdinal("U_PCK_CantContada")) ? (decimal?)null : readerLines.GetDecimal(readerLines.GetOrdinal("U_PCK_CantContada"));
                                line.U_PCK_ContUsuarios = readerLines.IsDBNull(readerLines.GetOrdinal("U_PCK_ContUsuarios")) ? null : readerLines.GetString(readerLines.GetOrdinal("U_PCK_ContUsuarios"));
                                solicitud.Lines.Add(line);
                            }
                        }
                    }
                }
            }
            return solicitud;
        }
    }
}
