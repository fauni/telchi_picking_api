using Core.Entities;
using Core.Entities.Login;
using Data.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace WebApi.Controllers.Login
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        private readonly IJwtTokenRepository _jwtTokenRepository;

        public AuthController(IAuthRepository authRepository, IJwtTokenRepository jwtTokenRepository)
        {
            _authRepository = authRepository;
            _jwtTokenRepository = jwtTokenRepository;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var usuario = await _authRepository.ValidateUserAsync(model.Username, model.Password);

            if (usuario != null)
            {
                var token = _jwtTokenRepository.GenerateToken(model.Username);

                // Autenticación en SAP Business One
                var sapSession = await _authRepository.AuthenticateWithSapB1Async();
                if (sapSession == null)
                {
                    return StatusCode(500, new ApiResponse()
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        IsSuccessful = false,
                        ErrorMessages = new List<string>() {
                        "Autenticación fallida en SAP B1"
                    }
                    });
                }

                return Ok(new ApiResponse
                {
                    StatusCode = HttpStatusCode.OK,
                    IsSuccessful = true,
                    Resultado = new
                    {
                        Token = token,
                        Usuario = usuario,
                        SapSessionId = sapSession.SessionId,
                        SapSessionTimeout = sapSession.SessionTimeout
                    }
                });
            }
            return Unauthorized(new ApiResponse
            {
                StatusCode = HttpStatusCode.Unauthorized,
                IsSuccessful = false,
                ErrorMessages = new List<string> { "Usuario o contraseña inválido" }
            });
        }
    }
}
