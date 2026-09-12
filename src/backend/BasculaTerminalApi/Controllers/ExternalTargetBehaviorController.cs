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
    public class ExternalTargetBehaviorController : ControllerBase
    {
        private readonly ILogger<ExternalTargetBehaviorController> _logger;
        private readonly IExternalTargetBehaviorService _externalTargetBehaviorService;

        public ExternalTargetBehaviorController(ILogger<ExternalTargetBehaviorController> logger,
            IExternalTargetBehaviorService externalTargetBehaviorService)
        {
            _logger = logger;
            _externalTargetBehaviorService = externalTargetBehaviorService;
        }

        [HttpGet("Available")]
        public async Task<ActionResult<IEnumerable<ExternalTargetBehaviorDto>>> GetAll()
        {
            return Ok(await _externalTargetBehaviorService.GetAllAsync());
        }

        /// <summary>Hidden behaviors used as almacén targets for pedido conversion (design.md Decision 5).</summary>
        [HttpGet("AlmacenTargets")]
        public async Task<ActionResult<IEnumerable<ExternalTargetBehaviorDto>>> GetAlmacenTargets()
        {
            return Ok(await _externalTargetBehaviorService.GetAlmacenTargetsAsync());
        }

        [HttpGet("ById")]
        public async Task<ActionResult<ExternalTargetBehaviorDto>> GetById([FromQuery] int id)
        {
            return Ok(await _externalTargetBehaviorService.GetByIdAsync(id));
        }
    }
}
