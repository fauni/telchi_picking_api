using Data.Interfaces;
using Core.Entities.Login;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Usuario
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        public UsuarioController(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        [HttpPost]
        [Route("register")]
        public IActionResult RegistrarUsuario([FromBody] Core.Entities.Login.Usuario usuario)
        {
            bool success = _usuarioRepository.InsertarUsuario(usuario);
            if (success) { 
                return Ok("Usuario registrado exitosamente.");
            } else {
                return StatusCode(500, "Error al registrar el usuario.");
            }
        }
    }
}
