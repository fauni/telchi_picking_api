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
    public class PurchaseOrderRepository: IPurchaseOrderRepository
    {
        private readonly IConfiguration _configuration;

        public PurchaseOrderRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<Order>> GetAll(string sessionID, int top, int skip)
        {
            string url = _configuration["SapCredentials:Url"] + $"/PurchaseInvoices?$filter=DocumentStatus eq 'bost_Open'&$orderby=DocEntry desc&$top={top}&$skip={skip}";
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
            string url = _configuration["SapCredentials:Url"] + @$"/PurchaseInvoices?$filter=contains(DocNum, '{search}') or contains(DocDate, '{search}') or contains(CardCode, '{search}') or contains(CardName, '{search}')&$orderby=DocEntry desc";

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

        public async Task<Order> GetOrderByDocNum(string sessionID, string docNum, string tipoDocumento)
        {
            string url = "";
            if (tipoDocumento == "orden_venta")
            {
                url = _configuration["SapCredentials:Url"] + $"/Orders?$filter=DocNum eq {docNum}";
            }
            else if (tipoDocumento == "factura")
            {
                url = _configuration["SapCredentials:Url"] + $"/Invoices?$filter=DocNum eq {docNum}"; // Obtener Documentos por Factura
            }
            else if (tipoDocumento == "factura_compra")
            {
                url = _configuration["SapCredentials:Url"] + $"/PurchaseInvoices?$filter=DocNum eq {docNum}"; // Obtener documentos de factura de compra
            }
            else
            {
                // TODO: Agregar metodos para los demas tipos de documento
            }

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
                            throw new ApiException((int)response.StatusCode, "No se encontró la factura de compra con el DocNum especificado.");
                        }
                    }
                    else
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        throw new ApiException((int)response.StatusCode, $"Error al obtener la factura de compra: {errorResponse}");
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
