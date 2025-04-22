using Core.Entities.SolicitudTraslado;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interfaces
{
    public interface ITransferenciaStockRepository
    {
        List<OWTQ> GetAll(int pageNumber, int pageSize);
        List<OWTQ> GetAllSearch(int pageNumber, int pageSize, string? search, DateTime? docDate);
        OWTQ GetByID(int id);
    }
}
