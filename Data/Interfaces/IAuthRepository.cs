using Core.Entities.Login;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interfaces
{
    public interface IAuthRepository
    {
        Task<Usuario> ValidateUserAsync(string username, string password);
        Task<SapAuthResponse> AuthenticateWithSapB1Async();
    }
}
