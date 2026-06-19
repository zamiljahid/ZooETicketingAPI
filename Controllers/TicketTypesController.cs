using Microsoft.AspNetCore.Mvc;
using ZooTicketing.API.DTOs;
using ZooTicketing.API.Interfaces;
namespace ZooTicketing.API.Controllers;
[ApiController][Route("api/ticket-types")]
public class TicketTypesController(ITicketTypeRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() {
        try { return Ok(await repo.GetAllAsync()); }
        catch(Exception ex){ return StatusCode(500,$"Failed to fetch ticket types: {ex.Message}"); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id) {
        try {
            if(id<=0) return BadRequest("Invalid ID.");
            var r=await repo.GetByIdAsync(id);
            return r is null ? NotFound($"Ticket type {id} not found.") : Ok(r);
        }
        catch(Exception ex){ return StatusCode(500,$"Error: {ex.Message}"); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(TicketTypeRequest req) {
        try {
            if(string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");
            if(req.Price<0) return BadRequest("Price cannot be negative.");
            return Ok(await repo.CreateAsync(req));
        }
        catch(InvalidOperationException ex){ return Conflict(ex.Message); }
        catch(Exception ex){ return StatusCode(500,$"Failed to create ticket type: {ex.Message}"); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, TicketTypeRequest req) {
        try {
            if(id<=0) return BadRequest("Invalid ID.");
            if(string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");
            if(req.Price<0) return BadRequest("Price cannot be negative.");
            return await repo.UpdateAsync(id,req) ? Ok("Updated.") : NotFound($"Ticket type {id} not found.");
        }
        catch(InvalidOperationException ex){ return Conflict(ex.Message); }
        catch(Exception ex){ return StatusCode(500,$"Failed to update: {ex.Message}"); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id) {
        try {
            if(id<=0) return BadRequest("Invalid ID.");
            return await repo.DeleteAsync(id) ? Ok("Deleted.") : NotFound($"Ticket type {id} not found.");
        }
        catch(InvalidOperationException ex){ return Conflict(ex.Message); }
        catch(Exception ex){ return StatusCode(500,$"Failed to delete: {ex.Message}"); }
    }
}
