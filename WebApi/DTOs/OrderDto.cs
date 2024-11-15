using Core.Entities.Picking;
using Core.Entities.Ventas;

namespace WebApi.DTOs
{
    public class OrderDto
    {
        public int? DocEntry { get; set; }
        public long? DocNum { get; set; }
        public string DocType { get; set; }
        public DateTimeOffset? DocDate { get; set; }
        public DateTimeOffset? DocDueDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public double? DocTotal { get; set; }
        public string DocCurrency { get; set; }
        public string Comments { get; set; }

        public Documento Documento { get; set; }
        public List<DocumentLineOrderDto> DocumentLines { get; set; }
    }

    public class DocumentLineOrderDto
    {
        public int? LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        public double? Quantity { get; set; }
        public double? Price { get; set; }
        public double? PriceAfterVat { get; set; }
        public string Currency { get; set; }
        public DetalleDocumento DetalleDocumento { get; set; }
        public List<LineTaxJurisdictionDto> LineTaxJurisdictions { get; set; }
    }

    public class LineTaxJurisdictionDto
    {
        public string JurisdictionCode { get; set; }
        public double? TaxAmount { get; set; }
        public double? TaxRate { get; set; }
    }
}
