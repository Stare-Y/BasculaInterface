using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    /// <summary>Admin/Sudo-only user management (design.md Decision 10) — no self-service, no
    /// bootstrap seeding (design.md Decision 9); the first Sudo account is inserted directly
    /// against the database by the project owner.</summary>
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize(Roles = $"{nameof(Role.Admin)},{nameof(Role.Sudo)}")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request)
        {
            try
            {
                return Ok(await _userService.CreateAsync(request));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new GenericResponse<string> { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new GenericResponse<string> { Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            try
            {
                return Ok(await _userService.GetByIdAsync(id));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new GenericResponse<string> { Message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            return Ok(await _userService.GetAllAsync());
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                return Ok(await _userService.UpdateAsync(id, request));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new GenericResponse<string> { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new GenericResponse<string> { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new GenericResponse<string> { Message = ex.Message });
            }
        }

        [HttpPatch("{id}/Disable")]
        public async Task<IActionResult> Disable(int id)
        {
            try
            {
                await _userService.DisableAsync(id);
                return Ok(new GenericResponse<string> { Data = "Disabled", Message = "Success" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new GenericResponse<string> { Message = ex.Message });
            }
        }
    }
}
