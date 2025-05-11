using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _tripsService;

        public TripsController(ITripsService tripsService)
        {
            _tripsService = tripsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTrips()
        {
            var trips = await _tripsService.GetTrips();
            return Ok(trips);
        }
        
        [HttpGet("/api/clients/{id}/trips")]
        public async Task<IActionResult> GetTripsForClient(int id)
        {
            var trips = await _tripsService.GetClientTrips(id);
            if (trips == null || trips.Count == 0)
                return NotFound($"Client with ID {id} has no trips.");

            return Ok(trips);
        }

        
        [HttpPost("/api/clients")]
        public async Task<IActionResult> AddClient([FromBody] ClientDTO client)
        {
            if (string.IsNullOrWhiteSpace(client.FirstName) ||
                string.IsNullOrWhiteSpace(client.LastName) ||
                string.IsNullOrWhiteSpace(client.Pesel))
            {
                return BadRequest("FirstName, LastName, and Pesel are required.");
            }

            int newId = await _tripsService.AddClient(client);
            return CreatedAtAction(nameof(GetTripsForClient), new { id = newId }, new { id = newId });
        }

        [HttpPut("/api/clients/{id}/trips/{tripId}")]
        public async Task<IActionResult> RegisterClientToTrip(int id, int tripId)
        {
            var success = await _tripsService.RegisterClientForTrip(id, tripId);
            if (!success)
                return BadRequest("Registration failed: client or trip may not exist, or conditions were not met.");

            return Ok("Client successfully registered for the trip.");
        }

        [HttpDelete("/api/clients/{id}/trips/{tripId}")]
        public async Task<IActionResult> DeleteClientFromTrip(int id, int tripId)
        {
            var success = await _tripsService.DeleteClientFromTrip(id, tripId);
            if (!success)
                return NotFound("Client is not registered for this trip.");

            return NoContent(); // 204 — успешно, но без тела
        }

        

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrip(int id)
        {
            return Ok();
        }
    }
}
