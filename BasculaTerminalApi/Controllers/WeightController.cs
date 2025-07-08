using Core.Application.DTOs;
using Core.Application.Interfaces;
using Core.Application.Services;
using Core.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    [ApiController]
    [Route("[Controller]")]
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
                WeightEntry newEntry = await _weightRepo.CreateAsync(weightEntryDto);

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
                return Ok(weightEntry);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving weight entry: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int top = 21, [FromQuery] uint page = 1)
        {
            try
            {
                IEnumerable<WeightEntry> weightEntries = await _weightRepo.GetAllAsync(top, page);
                if (weightEntries == null || !weightEntries.Any())
                {
                    return NotFound("No weight entries found.");
                }
                return Ok(weightEntries);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving weight entries: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromBody] WeightEntryDto weightEntryDto, int id)
        {
            try
            {
                await _weightRepo.UpdateAsync(weightEntryDto, id);
                return NoContent();
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
