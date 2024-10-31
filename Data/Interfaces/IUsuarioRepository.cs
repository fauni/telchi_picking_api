using Core.Entities.Login;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interfaces
{
    public interface IUsuarioRepository
    {
        bool InsertarUsuario(Usuario usuario);
        List<Usuario> ObtenerUsuarios();
    }
}
