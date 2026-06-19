using Microsoft.AspNetCore.Mvc;
using ZooTicketing.API.DTOs;
using ZooTicketing.API.Interfaces;
namespace ZooTicketing.API.Controllers;
[ApiController][Route("api/orders")]
public class OrdersController(IOrderRepository orders,ITicketRepository tickets) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() {
        try { return Ok(await orders.GetAllAsync()); }
        catch(Exception ex){ return StatusCode(500,$"Failed to fetch orders: {ex.Message}"); }
    }

    [HttpGet("user/{uid}")]
    public async Task<IActionResult> ByUser(int uid) {
        try {
            if(uid<=0) return BadRequest("Invalid user ID.");
            return Ok(await orders.GetByUserAsync(uid));
        }
        catch(InvalidOperationException ex){ return BadRequest(ex.Message); }
        catch(Exception ex){ return StatusCode(500,$"Error: {ex.Message}"); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id) {
        try {
            if(id<=0) return BadRequest("Invalid order ID.");
            var r=await orders.GetByIdAsync(id);
            return r is null ? NotFound($"Order {id} not found.") : Ok(r);
        }
        catch(Exception ex){ return StatusCode(500,$"Error: {ex.Message}"); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest req) {
        try {
            if(req.Tickets==null||req.Tickets.Count==0) return BadRequest("At least one ticket is required.");
            if(req.VisitDate<DateOnly.FromDateTime(DateTime.Today)) return BadRequest("Visit date cannot be in the past.");
            var result=await orders.CreateAsync(req,null);
            return Ok(result);
        }
        catch(InvalidOperationException ex){ return BadRequest(ex.Message); }
        catch(Exception ex){ return StatusCode(500,$"Failed to create order: {ex.Message}"); }
    }

    [HttpGet("tickets/{id}")]
    public async Task<IActionResult> GetTicket(int id) {
        try {
            if(id<=0) return BadRequest("Invalid ticket ID.");
            var r=await tickets.GetByIdAsync(id);
            return r is null ? NotFound($"Ticket {id} not found.") : Ok(r);
        }
        catch(Exception ex){ return StatusCode(500,$"Error: {ex.Message}"); }
    }

    [HttpGet("tickets/token/{token}")]
    public async Task<IActionResult> ByToken(Guid token) {
        try {
            if(token==Guid.Empty) return BadRequest("Invalid token.");
            var r=await tickets.GetByTokenAsync(token);
            return r is null ? NotFound("Ticket not found.") : Ok(r);
        }
        catch(Exception ex){ return StatusCode(500,$"Error: {ex.Message}"); }
    }

    [HttpDelete("tickets/{id}")]
    public async Task<IActionResult> Cancel(int id) {
        try {
            if(id<=0) return BadRequest("Invalid ticket ID.");
            return await tickets.CancelAsync(id) ? Ok("Ticket cancelled.") : NotFound($"Ticket {id} not found.");
        }
        catch(InvalidOperationException ex){ return BadRequest(ex.Message); }
        catch(Exception ex){ return StatusCode(500,$"Failed to cancel ticket: {ex.Message}"); }
    }
}
