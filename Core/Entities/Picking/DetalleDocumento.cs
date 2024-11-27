using Core.Entities.Picking;
using System.Collections.Generic;

public class DetalleDocumento
{
    public int IdDetalle { get; set; }
    public int IdDocumento { get; set; }
    public int NumeroLinea { get; set; }
    public string CodigoItem { get; set; } = string.Empty;
    public string DescripcionItem { get; set; } = string.Empty;
    public decimal CantidadEsperada { get; set; }
    public decimal CantidadContada { get; set; }
    public string Estado { get; set; }
    public string CodigoBarras { get; set; }

    // Lista para ConteoItems si deseas cargar los conteos
    public List<ConteoItems> Conteos { get; set; } = new List<ConteoItems>();
}
