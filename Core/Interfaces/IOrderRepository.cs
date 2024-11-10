﻿using Core.Entities.Ventas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetAll(String sessionID, int top, int skip);
        Task<List<Order>> GetForText(String sessionID, string search);
    }
}
