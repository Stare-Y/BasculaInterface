using Core.Application.DTOs;
using Core.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
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

        [HttpGet("ById")]
        public async Task<ActionResult<ExternalTargetBehaviorDto>> GetById([FromQuery] int id)
        {
            return Ok(await _externalTargetBehaviorService.GetByIdAsync(id));
        }
    }
}
