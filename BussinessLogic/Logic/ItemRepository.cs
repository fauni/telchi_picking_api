using Core.Entities.Error;
using Core.Entities.Items;
using Core.Entities.Ventas;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sap.Data.Hana;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessLogic.Logic
{
    public class ItemRepository : IItemRepository
    {
        private readonly IConfiguration _configuration;
        public ItemRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<List<Item>> GetAll(string sessionID, int top)
        {
            string baseUrl = _configuration["SapCredentials:Url"] + "/Items?filter=WarehouseCode eq '2A-DAN'";
            List<Item> allItems = new List<Item>();
            int skip = 0;
            bool hasMoreItems = true;

            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (HttpClient httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Add("Cookie", $"B1SESSION={sessionID}");

                    while (hasMoreItems)
                    {
                        string url = $"{baseUrl}?$top={top}&$skip={skip}";
                        HttpResponseMessage response = await httpClient.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            var result = JsonConvert.DeserializeObject<ResponseItem>(responseBody);

                            if (result.value.Count > 0)
                            {
                                allItems.AddRange(result.value);
                                skip += top; // Increment skip for the next page
                                if(allItems.Count == 300 || allItems.Count == 500 || allItems.Count == 800)
                                {
                                    int esvalido = 0;
                                }
                            }
                            else
                            {
                                hasMoreItems = false; // Stop if no more items are returned
                            }
                        }
                        else
                        {
                            var errorResponse = await response.Content.ReadAsStringAsync();
                            throw new ApiException((int)response.StatusCode, errorResponse);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is ApiException apiEx)
                {
                    throw apiEx;
                }
                throw new ApiException(500, "An unexpected error occurred: " + ex.Message);
            }

            return allItems; // Return the aggregated list of items
        }

        public async Task<Item> GetByCode(string sessionID, string code)
        {
            string baseUrl = _configuration["SapCredentials:Url"] + $"/Items('{code}')?$select=ItemCode,ItemName,BarCode";
            Item item = new Item();

            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using (HttpClient httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Add("Cookie", $"B1SESSION={sessionID}");

                    HttpResponseMessage response = await httpClient.GetAsync(baseUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<Item>(responseBody);
                        item = result;
                    }
                    else
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        throw new ApiException((int)response.StatusCode, errorResponse);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is ApiException apiEx)
                {
                    throw apiEx;
                }
                throw new ApiException(500, "An unexpected error occurred: " + ex.Message);
            }

            return item; // Return the aggregated list of items
        }

        public async Task<List<ItemWhs>> GetItemsByWarehouseAsync(string whsCode)
        {
            string connectionString = _configuration.GetConnectionString("SapHanaConnection");
            var items = new List<ItemWhs>();

            using (var connection = new HanaConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                    SELECT 
                        T0.""ItemCode"", 
                        T0.""ItemName"", 
                        T0.""CodeBars"",
                        T1.""OnHand"", 
                        T1.""IsCommited"", 
                        T1.""OnOrder"", 
                        T2.""WhsCode"",
                        T2.""WhsName""
                    FROM 
                        ""TELCHI"".""OITM"" T0
                    INNER JOIN 
                        ""TELCHI"".""OITW"" T1 ON T0.""ItemCode"" = T1.""ItemCode""
                    INNER JOIN 
                        ""TELCHI"".""OWHS"" T2 ON T1.""WhsCode"" = T2.""WhsCode""
                    WHERE 
                        T2.""WhsCode"" = :WhsCode
                    ORDER BY 
                        T0.""ItemCode"";
                ";



                using (var command = new HanaCommand(query, connection))
                {
                    command.Parameters.Add(new HanaParameter("WhsCode", whsCode));
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            items.Add(new ItemWhs
                            {
                                ItemCode = reader["ItemCode"].ToString(),
                                ItemName = reader["ItemName"].ToString(),
                                CodeBars = reader["CodeBars"].ToString(),
                                OnHand = reader["OnHand"] != DBNull.Value ? Convert.ToDecimal(reader["OnHand"]) : 0,
                                IsCommited = reader["IsCommited"] != DBNull.Value ? Convert.ToDecimal(reader["IsCommited"]) : 0,
                                OnOrder = reader["OnOrder"] != DBNull.Value ? Convert.ToDecimal(reader["OnOrder"]) : 0,
                                WhsCode = reader["WhsCode"].ToString(),
                                WhsName = reader["WhsName"].ToString()
                            });
                        }
                    }
                }
            }
            return items;
        }
    }
}
