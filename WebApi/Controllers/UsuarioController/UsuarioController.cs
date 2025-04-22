using Data.Interfaces;
using Core.Entities.Login;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Core.Entities;
using System.Net;

namespace WebApi.Controllers.UsuarioController
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        protected ApiResponse _response;
        public UsuarioController(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
            _response = new ApiResponse();
        }

        [HttpGet]
        [Authorize]
        [Route("listado")]
        public async Task<ActionResult<ApiResponse>> ObtenerUsuarios()
        {
            var usuarios = _usuarioRepository.ObtenerUsuarios();
            _response.IsSuccessful = true;
            _response.Resultado = usuarios;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpPost]
        [Route("register")]
        public IActionResult RegistrarUsuario([FromBody] Usuario usuario)
        {
            bool success = _usuarioRepository.InsertarUsuario(usuario);
            if (success) { 
                return Ok("Usuario registrado exitosamente.");
            } else {
                return StatusCode(500, "Error al registrar el usuario.");
            }
        }

        [HttpPost]
        [Route("cambiar-clave")]
        public IActionResult CambiarClave([FromBody] LoginModel usuario)
        {
            _usuarioRepository.ResetearClave(usuario);
            return Ok();
        }
    }
}
