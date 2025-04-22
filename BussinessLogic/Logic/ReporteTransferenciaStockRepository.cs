using Core.Entities.Reportes;
using Core.Interfaces;
using Data.Interfaces;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BussinessLogic.Logic
{
    public class ReporteTransferenciaStockRepository : IReporteTransferenciaStockRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ITransferenciaStockRepository _transferenciaStockRepository;
        private readonly ISolicitudTrasladoRepository _solicitudTrasladoRepository;

        public ReporteTransferenciaStockRepository(
            IConfiguration configuration, 
            ITransferenciaStockRepository transferenciaStockRepository, 
            ISolicitudTrasladoRepository solicitudTrasladoRepository
        ){
            _configuration = configuration;
            _transferenciaStockRepository = transferenciaStockRepository;
            _solicitudTrasladoRepository = solicitudTrasladoRepository;
        }

        public MemoryStream GenerarReporte(string sessionID, int docEntry, string tipoDocumento)
        {
            var data = tipoDocumento == "solicitud_traslado" 
                ? _solicitudTrasladoRepository.GetByID(docEntry)
                : _transferenciaStockRepository.GetByID(docEntry);
            var detalle = this.ObtenerDetalle(docEntry, tipoDocumento);

            var stream = new MemoryStream();
            using (var writer = new PdfWriter(stream))
            {
                using (var pdf = new PdfDocument(writer))
                {
                    using (var document = new iText.Layout.Document(pdf))
                    {
                        string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "telchi.png");

                        // Agregar metadatos al documento(opcional)
                        pdf.GetDocumentInfo().SetTitle("Reporte de Transferencia de Stock"); // Modificar
                        PdfFont fuenteTabla = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
                        // Definir la fuente para la cabecera en negrita
                        PdfFont fuenteCabecera = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

                        if (File.Exists(imagePath))
                        {
                            ImageData imageData = ImageDataFactory.Create(imagePath);
                            Image logo = new Image(imageData).SetWidth(80).SetFixedPosition(20, pdf.GetDefaultPageSize().GetTop() - 50);
                            document.Add(logo);
                        }

                        string textoTitulo = "";
                        switch (tipoDocumento)
                        {
                            case "solicitud_traslado": textoTitulo = "SOLICITUD DE TRASLADO"; break;
                            case "transferencia_stock": textoTitulo = "TRANSFERENCIA DE STOCK"; break;
                        }
                        // Titulo Centrado
                        Paragraph titulo = new Paragraph($"{textoTitulo} Nro. {data.DocNum}")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(12)
                            .SetFont(fuenteCabecera)
                            .SetMarginTop(20);
                        document.Add(titulo);


                        #region CABECERA

                        // Crear una tabla con dos columnas
                        Table tableHeader = new Table(new float[] { 1, 2 })
                            .UseAllAvailableWidth();

                        // Definir el borde común
                        Border commonBorder = new SolidBorder(1);
                        string formato = "dd/MM/yyyy";
                        string fechaDocumento = data.DocDate.ToShortDateString();

                        // Añadir la celda de "Código de Cliente" que abarca dos filas
                        Cell fromWhsCell = new Cell(1, 2)
                            .Add(new Paragraph($"De Almacen  : {data.Filler}").SetFontSize(9).SetFont(fuenteCabecera))
                            .SetBorder(Border.NO_BORDER);
                        Cell toWhsCell = new Cell(1, 2)
                            .Add(new Paragraph($"A Almacen  : {data.ToWhsCode}").SetFontSize(9).SetFont(fuenteCabecera))
                            .SetBorder(Border.NO_BORDER);
                        Cell fechaDelDocumentoCell = new Cell(1, 2)
                            .Add(new Paragraph($"Fecha del Documento: {fechaDocumento}").SetFontSize(9).SetFont(fuenteCabecera))
                            .SetBorder(Border.NO_BORDER);
                        // Añadir la celda "Código de Cliente" al inicio de la tabla
                        tableHeader.AddCell(fromWhsCell);
                        tableHeader.AddCell(toWhsCell);
                        tableHeader.AddCell(fechaDelDocumentoCell);


                        // Añadir la tabla al documento
                        document.Add(tableHeader);

                        #endregion

                        // TABLA CON DATOS
                        Table tablaDetalle = new Table(7).UseAllAvailableWidth();

                        // Definir la fuente para el contenido de la tabla con un tamaño reducido

                        float tamanoFuenteCabecera = 9; // Tamaño de fuente normal
                        float tamanoFuenteTabla = 8; // Tamaño de fuente reducido


                        // Agregar las celdas de la cabecera con formato bold
                        tablaDetalle.AddHeaderCell(new Cell().Add(new Paragraph("#").SetFont(fuenteCabecera).SetFontSize(tamanoFuenteCabecera)));
                        tablaDetalle.AddHeaderCell(new Cell().Add(new Paragraph("Código").SetFont(fuenteCabecera).SetFontSize(tamanoFuenteCabecera)));
                        tablaDetalle.AddHeaderCell(new Cell().Add(new Paragraph("Descripción").SetFont(fuenteCabecera).SetFontSize(tamanoFuenteCabecera)));
                        tablaDetalle.AddHeaderCell(new Cell().Add(new Paragraph("Fecha Vcto.").SetFont(fuenteCabecera).SetFontSize(tamanoFuenteCabecera)));
                        tablaDetalle.AddHeaderCell(new Cell().Add(new Paragraph("Cantidad").SetFont(fuenteCabecera).SetFontSize(tamanoFuenteCabecera)));
                        tablaDetalle.AddHeaderCell(new Cell().Add(new Paragraph("Cant. Contada").SetFont(fuenteCabecera).SetFontSize(tamanoFuenteCabecera)));
                        tablaDetalle.AddHeaderCell(new Cell().Add(new Paragraph("Usuario").SetFont(fuenteCabecera).SetFontSize(tamanoFuenteCabecera)));

                        int count = 0;
                        foreach (var item in detalle)
                        {
                            count++;
                            // Agregamos el index del foreach y el contenido con el tamaño de fuente reducido
                            tablaDetalle.AddCell(new Cell().Add(new Paragraph(count.ToString()).SetFont(fuenteTabla).SetFontSize(tamanoFuenteTabla)));
                            tablaDetalle.AddCell(new Cell().Add(new Paragraph(item.CodigoItem).SetFont(fuenteTabla).SetFontSize(tamanoFuenteTabla)));
                            tablaDetalle.AddCell(new Cell().Add(new Paragraph(item.DescripcionItem).SetFont(fuenteTabla).SetFontSize(tamanoFuenteTabla)));
                            tablaDetalle.AddCell(new Cell().Add(new Paragraph(((DateTime)item.FechaVencimiento).ToShortDateString()).SetFont(fuenteTabla).SetFontSize(tamanoFuenteTabla)));
                            tablaDetalle.AddCell(new Cell().Add(new Paragraph(item.CantidadEsperada.ToString()).SetFont(fuenteTabla).SetFontSize(tamanoFuenteTabla)));
                            tablaDetalle.AddCell(new Cell().Add(new Paragraph(item.CantidadAgregada.ToString()).SetFont(fuenteTabla).SetFontSize(tamanoFuenteTabla)));
                            tablaDetalle.AddCell(new Cell().Add(new Paragraph(item.Usuario).SetFont(fuenteTabla).SetFontSize(tamanoFuenteTabla)));
                        }

                        document.Add(tablaDetalle);


                        // Crear una tabla para la información del empleado y condiciones de pago
                        Table footerTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 3 })).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);

                        footerTable.AddCell(new Cell().Add(new Paragraph("Comentarios:").SetFont(fuenteCabecera)).SetBorder(Border.NO_BORDER).SetFontSize(10));
                        footerTable.AddCell(new Cell().Add(new Paragraph($"{data.Comments}")).SetBorder(Border.NO_BORDER).SetFontSize(10));

                        // footerTable.AddCell(new Cell(1, 2).Add(new Paragraph($"{imagePath}").SetFont(fuenteCabecera)).SetBorder(Border.NO_BORDER).SetFontSize(10));
                        // footerTable.AddCell(new Cell(1, 2).Add(new Paragraph($"{data.Comments}")).SetBorder(Border.NO_BORDER).SetFontSize(10));

                        // Asegurarse de que no hay bordes
                        footerTable.SetBorder(Border.NO_BORDER);

                        document.Add(footerTable);
                    }
                }
            }
            return stream;
        }

        public List<DetalleReporte> ObtenerDetalle(int docEntry, string tipoDocumento)
        {
            string sql = @$"select T1.CodigoItem, T1.DescripcionItem, T1.CantidadEsperada, T1.CantidadContada, T2.FechaVencimiento, T2.CantidadAgregada, T2.Usuario
                from Documento T0
                inner join DetalleDocumento T1 on T0.IdDocumento = T1.IdDocumento
                inner join ConteoItems T2 ON t2.IdDetalle = T1.IdDetalle
                where T0.TipoDocumento = '{tipoDocumento}' and T0.DocEntry= '{docEntry}'";

            // realizamos la consulta y la obtenemos en un List<DetalleReporte>
            var detalleReporte = new List<DetalleReporte>();
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detalle = new DetalleReporte
                            {
                                CodigoItem = reader["CodigoItem"].ToString(),
                                DescripcionItem = reader["DescripcionItem"].ToString(),
                                CantidadEsperada = Convert.ToDecimal(reader["CantidadEsperada"]),
                                CantidadContada = Convert.ToDecimal(reader["CantidadContada"]),
                                FechaVencimiento = Convert.ToDateTime(reader["FechaVencimiento"]),
                                CantidadAgregada = Convert.ToDecimal(reader["CantidadAgregada"]),
                                Usuario = reader["Usuario"].ToString()
                            };
                            detalleReporte.Add(detalle);
                        }
                    }
                }
            }
            return detalleReporte;
        }
    }

}
