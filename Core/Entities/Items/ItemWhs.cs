using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Items
{
    public class ItemWhs
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string CodeBars { get; set; }
        public decimal OnHand { get; set; }
        public decimal IsCommited { get; set; }
        public decimal OnOrder { get; set; }
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
    }
}
