using Core.Entities.Error;
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
    public class DeliveryRepository : IDeliveryRepository
    {
        private readonly IConfiguration _configuration;
        public DeliveryRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<List<Order>> GetAll(string sessionID, int top, int skip)
        {
            string url = _configuration["SapCredentials:Url"] + $"/DeliveryNotes?$filter=DocumentStatus eq 'bost_Open'&$orderby=DocEntry desc&$top={top}&$skip={skip}";
            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                using (HttpClient httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Add("Cookie", String.Format("B1SESSION={0}", sessionID));
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<ResponseOrder>(responseBody);
                        return result.value;
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
        }
        public async Task<List<Order>> GetForText(string sessionID, string search)
        {
            string url = _configuration["SapCredentials:Url"] + @$"/DeliveryNotes?$filter=contains(DocNum, '{search}') or contains(DocDate, '{search}') or contains(CardCode, '{search}') or contains(CardName, '{search}')&$orderby=DocEntry desc";
            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                using (HttpClient httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Add("Cookie", String.Format("B1SESSION={0}", sessionID));
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<ResponseOrder>(responseBody);
                        return result.value;
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
        }
        public async Task<Order> GetDeliveryByDocNum(string sessionID, string docNum, string tipoDocumento)
        {
            string url = _configuration["SapCredentials:Url"] + $"/DeliveryNotes?$filter=DocNum eq {docNum}";
            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                using (HttpClient httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Add("Cookie", String.Format("B1SESSION={0}", sessionID));
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<ResponseOrder>(responseBody);
                        return result.value.FirstOrDefault();
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
        }

        
    }
}
