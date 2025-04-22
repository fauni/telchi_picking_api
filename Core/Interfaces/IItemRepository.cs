using Core.Entities.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IItemRepository
    {
        Task<List<Item>> GetAll(String sessionID, int top);

        Task<List<ItemWhs>> GetItemsByWarehouseAsync(string whsCode);

        Task<Item> GetByCode(String sessionID, string code);
    }
}
