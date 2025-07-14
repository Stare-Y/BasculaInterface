using Core.Application.DTOs;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Services;
using Core.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class WeightController : ControllerBase, IWeightService<IActionResult>
    {
        private readonly IWeightRepo _weightRepo = null!;
        public WeightController(IWeightRepo weightRepo)
        {
            _weightRepo = weightRepo;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WeightEntryDto weightEntryDto)
        {
            try
            {
                WeightEntry newEntry = await _weightRepo.CreateAsync(weightEntryDto.ConvertToBaseEntry());

                return CreatedAtAction(nameof(GetById), new { id = newEntry.Id }, newEntry);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating weight entry: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                WeightEntry? weightEntry = await _weightRepo.GetByIdAsync(id);
                if (weightEntry == null)
                {
                    return NotFound($"Weight entry with ID {id} not found.");
                }

                WeightEntryDto weightEntryDto = weightEntry.ConvertToDto();

                return Ok(weightEntryDto);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving weight entry: {ex.Message}");
            }
        }

        [HttpGet("Pending")]
        public async Task<IActionResult> GetPendingWeights([FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            try
            {
                IEnumerable<WeightEntry> pendingWeights = await _weightRepo.GetPendingWeights(top, page);
                if (pendingWeights == null || !pendingWeights.Any())
                {
                    return NotFound("No pending weight entries found.");
                }
                IEnumerable<WeightEntryDto> dtos = pendingWeights.ConvertRangeToDto();
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving pending weight entries: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int top = 30, [FromQuery] uint page = 1)
        {
            try
            {
                IEnumerable<WeightEntry> weightEntries = await _weightRepo.GetAllAsync(top, page);

                if (weightEntries == null || !weightEntries.Any())
                {
                    return NotFound("No weight entries found.");
                }

                IEnumerable<WeightEntryDto> dtos = weightEntries.ConvertRangeToDto();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving weight entries: {ex.Message}");
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] WeightEntryDto weightEntryDto)
        {
            try
            {
                await _weightRepo.UpdateAsync(weightEntryDto.ConvertToBaseEntry());
                return Ok("Update Successful :D");
            }
            catch (KeyNotFoundException knfEx)
            {
                return NotFound(knfEx.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating weight entry: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                bool deleted = await _weightRepo.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"Weight entry with ID {id} not found.");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error deleting weight entry: {ex.Message}");
            }
        }
    }
}
