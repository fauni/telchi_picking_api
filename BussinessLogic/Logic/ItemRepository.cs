using Core.Entities.Error;
using Core.Entities.Items;
using Core.Entities.Ventas;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
    }
}
