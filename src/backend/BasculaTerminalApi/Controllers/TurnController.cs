using Core.Application.DTOs;
using Core.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    // All-GET controller — every action stays open under the fallback-authenticated policy
    // (design.md Decision 3 of add-user-authentication-and-audit-log).
    [AllowAnonymous]
    [ApiController]
    [Route("api/[Controller]")]
    public class TurnController : ControllerBase
    {
        private readonly ITurnService _turnService;
        public TurnController(ITurnService turnService)
        {
            _turnService = turnService;
        }

        [HttpGet]
        public async Task<ActionResult<TurnDto>> GetTurn([FromQuery] int? weightId = null, [FromQuery] string? description = null)
        {
            TurnDto turn = await _turnService.GetTurn(weightId, description);
            return Ok(turn);
        }
    }
}
