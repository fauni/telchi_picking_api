using System;

public class ConteoItems
{
    public int IdConteo { get; set; }
    public int IdDetalle { get; set; }
    public string Usuario { get; set; }
    public DateTime FechaHoraConteo { get; set; }
    public decimal CantidadContada { get; set; }
    public decimal CantidadContadaAnterior { get; set; }
    public decimal CantidadAgregada { get; set; }
}
