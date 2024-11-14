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
    public class OrderRepository : IOrderRepository
    {
        private readonly IConfiguration _configuration;
        public OrderRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<Order>> GetAll(string sessionID, int top, int skip)
        {
            string url = _configuration["SapCredentials:Url"] + $"/Orders?$orderby=DocEntry desc&$top={top}&$skip={skip}";
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
                        // var codeError = new CodeErrorException((int)response.StatusCode, errorResponse);
                        // return null, codeError);
                        throw new Exception(errorResponse);
                    }
                }
            }
            catch (Exception ex) {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<Order>> GetForText(string sessionID, string search)
        {
            string url = _configuration["SapCredentials:Url"] + @$"/Orders?$filter=contains(DocNum, '{search}')
                or contains(DocDate, '{search}') or contains(CardCode, '{search}') or contains(CardName, '{search}')&$orderby=DocEntry desc";
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
                        // var codeError = new CodeErrorException((int)response.StatusCode, errorResponse);
                        // return null, codeError);
                        throw new Exception(errorResponse);
                    }
                }
            } catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<Order> GetOrderByDocNum(string sessionID, string docNum)
        {
            string url = _configuration["SapCredentials:Url"] + $"/Orders?$filter=DocNum eq {docNum}";
            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                using (HttpClient httpClient = new HttpClient(handler))
                {
                    // Configurar la sesión en la cabecera de la solicitud
                    httpClient.DefaultRequestHeaders.Add("Cookie", $"B1SESSION={sessionID}");

                    // Enviar la solicitud GET
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                     
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<ResponseOrder>(responseBody);

                        // Verificar si se obtuvo al menos una orden
                        if (result.value != null && result.value.Count > 0)
                        {
                            return result.value.First();
                        }
                        else
                        {
                            throw new Exception("No se encontró una orden con el DocNum especificado.");
                        }
                    }
                    else
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Error al obtener la orden: {errorResponse}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
