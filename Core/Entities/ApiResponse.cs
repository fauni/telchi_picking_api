using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class ApiResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccessful { get; set; }
        public List<string> ErrorMessages { get; set; }
        public object Resultado { get; set; }
    }
}
