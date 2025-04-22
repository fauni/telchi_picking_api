using BussinessLogic.Logic;
using Core.Entities;
using Core.Entities.Items;
using Core.Interfaces;
using Data.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace WebApi.Controllers.TransferenciaController
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransferenciaStockController : ControllerBase
    {
        private readonly ITransferenciaStockRepository _transferenciaStockRepository;
        private readonly IDocumentoRepository _documentoRepository;
        private readonly IDetalleDocumentoRepository _detalleDocumentoRepository;
        private readonly IReporteTransferenciaStockRepository _reporteRepository;
        private readonly IItemRepository _itemRepository;
        private readonly ApiResponse _response;

        public TransferenciaStockController(
            ITransferenciaStockRepository transferenciaStockRepository,
            IDocumentoRepository documentoRepository,
            IDetalleDocumentoRepository detalleDocumentoRepository,
            IReporteTransferenciaStockRepository reporteRepository,
            IItemRepository itemRepository
            )
        {
            _transferenciaStockRepository = transferenciaStockRepository;
            _documentoRepository = documentoRepository;
            _detalleDocumentoRepository = detalleDocumentoRepository;
            _reporteRepository = reporteRepository;
            _itemRepository = itemRepository;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTransferenciaStock(int pageNumber = 1, int pageSize = 10, string? search = null, DateTime? docDate = null)
        {
            var transferencias = _transferenciaStockRepository.GetAllSearch(pageNumber, pageSize, search, docDate);
            foreach (var transferencia in transferencias)
            {
                var documento = await _documentoRepository.GetDocumentByDocNumAsync(transferencia.DocNum.ToString(), "transferencia_stock");

                // Mapear el documento a DocumentoDto si existe
                if (documento != null)
                {
                    // Obtener los detalles del documento
                    var detalles = await _detalleDocumentoRepository.GetDetallesByDocumentoIdAsync(documento.IdDocumento);
                    documento.Detalles = detalles;
                    transferencia.Documento = documento;

                    foreach (var item_orden in transferencia.Lines)
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
            _response.Resultado = transferencias;
            return Ok(_response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerTransferenciaStockById(int id)
        {
            var sessionID = Request.Headers["SessionID"];

            var transferencia = _transferenciaStockRepository.GetByID(id);
            await _documentoRepository.ActualizaItemsDocumentoConteoTransferenciaStock(transferencia, "transferencia_stock");
            if(transferencia == null)
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.ErrorMessages.Add("Transferencia no encontrada.");
                return NotFound(_response);
            }

            var documento = await _documentoRepository.GetDocumentByDocNumAsync(transferencia.DocNum.ToString(), "transferencia_stock");
            // Mapear el documento a DocumentoDto si existe
            if (documento != null)
            {
                // Obtener los detalles del documento
                var detalles = await _detalleDocumentoRepository.GetDetallesByDocumentoIdAsync(documento.IdDocumento);
                documento.Detalles = detalles;
                transferencia.Documento = documento;

                foreach (var item_orden in transferencia.Lines)
                {   
                    try
                    {
                        var detalleRelacionado = detalles.FirstOrDefault(d => d.NumeroLinea == item_orden.LineNum);
                        item_orden.DetalleDocumento = detalleRelacionado;
                        if (!String.IsNullOrEmpty(sessionID))
                        {
                            var item = _itemRepository.GetByCode(sessionID, item_orden.ItemCode).Result;
                            item_orden.CodeBars = item.BarCode;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }
            }
            _response.IsSuccessful = true;
            _response.StatusCode = HttpStatusCode.OK;
            _response.Resultado = transferencia;
            return Ok(_response);
        }

        [HttpGet("GenerarReporte")]
        public async Task<IActionResult> GenerarReporte([FromQuery] int docEntry, string tipoDocumento)
        {
            try
            {
                var sessionID = Request.Headers["SessionID"];
                var pdfStream = _reporteRepository.GenerarReporte(sessionID, docEntry, tipoDocumento);
                return File(pdfStream.ToArray(), "application/pdf", "Reporte.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }
    }
}
