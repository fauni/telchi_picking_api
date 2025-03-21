using BussinessLogic.Logic;
using Core.Entities;
using Core.Entities.Ventas;
using Core.Interfaces;
using Data.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using WebApi.DTOs;

namespace WebApi.Controllers.TransferenciaController
{
    [Route("api/[controller]")]
    [ApiController]
    public class SolicitudTrasladoController : ControllerBase
    {
        private readonly ISolicitudTrasladoRepository _solicitudTrasladoRepository;
        private readonly IDocumentoRepository _documentoRepository;
        private readonly IDetalleDocumentoRepository _detalleDocumentoRepository;
        private readonly ApiResponse _response;

        public SolicitudTrasladoController(ISolicitudTrasladoRepository solicitudTrasladoRepository, IDocumentoRepository documentoRepository, IDetalleDocumentoRepository detalleDocumentoRepository)
        {
            _solicitudTrasladoRepository = solicitudTrasladoRepository;
            _documentoRepository = documentoRepository;
            _detalleDocumentoRepository = detalleDocumentoRepository;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerSolicitudTransferencia(int pageNumber = 1, int pageSize = 10, string? search = null, DateTime? docDate = null)
        {
            // var solicitudes = _solicitudTrasladoRepository.GetAll(pageNumber, pageSize);
            var solicitudes  = _solicitudTrasladoRepository.GetAllSearch(pageNumber, pageSize, search, docDate);

            foreach (var solicitud in solicitudes)
            {
                var documento = await _documentoRepository.GetDocumentByDocNumAsync(solicitud.DocNum.ToString(), "solicitud_traslado");

                // Mapear el documento a DocumentoDto si existe
                if (documento != null)
                {
                    // Obtener los detalles del documento
                    var detalles = await _detalleDocumentoRepository.GetDetallesByDocumentoIdAsync(documento.IdDocumento);
                    documento.Detalles = detalles;
                    solicitud.Documento = documento;

                    foreach (var item_orden in solicitud.Lines)
                    {
                        try
                        {
                            var detalleRelacionado = detalles.FirstOrDefault(d => d.NumeroLinea == item_orden.LineNum);
                            item_orden.DetalleDocumento = detalleRelacionado;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }

                    }
                }
            }

            _response.IsSuccessful = true;
            _response.StatusCode = HttpStatusCode.OK;
            _response.Resultado = solicitudes;
            return Ok(_response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerSolicitudTransferenciaById(int id)
        {
            var solicitud = _solicitudTrasladoRepository.GetByID(id);

            if(solicitud == null)
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.ErrorMessages.Add("No se encontró la solicitud de traslado");
                return NotFound(_response);
            }

            var documento = await _documentoRepository.GetDocumentByDocNumAsync(solicitud.DocNum.ToString(), "solicitud_traslado");

            // Mapear el documento a DocumentoDto si existe
            if (documento != null)
            {
                // Obtener los detalles del documento
                var detalles = await _detalleDocumentoRepository.GetDetallesByDocumentoIdAsync(documento.IdDocumento);
                documento.Detalles = detalles;
                solicitud.Documento = documento;

                foreach (var item_orden in solicitud.Lines)
                {
                    try
                    {
                        var detalleRelacionado = detalles.FirstOrDefault(d => d.NumeroLinea == item_orden.LineNum);
                        item_orden.DetalleDocumento = detalleRelacionado;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }

                }
            }

            _response.IsSuccessful = true;
            _response.StatusCode = HttpStatusCode.OK;
            _response.Resultado = solicitud;
            return Ok(_response);
        }
    }
}
