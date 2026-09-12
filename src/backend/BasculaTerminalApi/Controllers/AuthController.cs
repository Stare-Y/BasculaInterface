using Core.Application.DTOs;
using Core.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("Login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            LoginResponse? response = await _authService.LoginAsync(request.Identifier, request.Password);

            if (response is null)
            {
                return Unauthorized(new GenericResponse<string> { Message = "Credenciales inválidas." });
            }

            return Ok(response);
        }
    }
}
