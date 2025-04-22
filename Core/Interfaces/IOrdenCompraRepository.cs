using Core.Entities.Ventas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IOrdenCompraRepository
    {
        Task<List<Order>> GetAll(string sessionID, int top, int skip);
        Task<List<Order>> GetForText(string sessionID, string search);
        Task<Order> GetOrdenCompraByDocNum(string sessionID, string docNum, string tipoDocumento);
    }
}
