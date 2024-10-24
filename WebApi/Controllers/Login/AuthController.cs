using Core.Entities.Login;
using Data.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
            var isValidUser = await _authRepository.ValidateUserAsync(model.Username, model.Password);
            if (isValidUser)
            {
                var token = _jwtTokenRepository.GenerateToken(model.Username);
                return Ok(new
                {
                    Token = token
                });
            }
            return Unauthorized("Invalid username or password");
        }
    }
}
