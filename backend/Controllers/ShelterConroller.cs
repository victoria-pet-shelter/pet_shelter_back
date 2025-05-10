using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Dtos;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;

namespace Controllers
{
    [ApiController]
    [Route("shelters")]
    public class SheltersController : ControllerBase
    {
        private readonly AppDbContext db;

        public SheltersController(AppDbContext db)
        {
            this.db = db;
        }

        // Get all shelters
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            List<Shelters> shelters = await db.Shelters.ToListAsync();
            return Ok(shelters);
        }

        // Get a shelter by id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            Shelters shelter = await db.Shelters.FindAsync(id);

            if (shelter == null)
            {
                return NotFound("Shelter not found.");
            }

            return Ok(shelter);
        }

        // Create shelter (Only shelter_owner)
        [Authorize(Roles = "shelter_owner")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ShelterCreateDto dto)
        {
            string? userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            Guid userId;
            bool parsed = Guid.TryParse(userIdString, out userId);

            if (!parsed)
            {
                return Unauthorized();
            }

            Shelters shelter = new Shelters();
            shelter.id = Guid.NewGuid();
            shelter.shelter_owner_id = userId;
            shelter.name = dto.name;
            shelter.address = dto.address;
            shelter.phone = dto.phone;
            shelter.email = dto.email;
            shelter.description = dto.description;
            shelter.created_at = DateTime.UtcNow;

            db.Shelters.Add(shelter);
            await db.SaveChangesAsync();

            return Ok(shelter);
        }

        // Update shelter (Only owner)
        [Authorize(Roles = "shelter_owner")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ShelterUpdateDto dto)
        {
            Shelters shelter = await db.Shelters.FindAsync(id);

            if (shelter == null)
            {
                return NotFound("Shelter not found.");
            }

            string? userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            Guid userId;
            bool parsed = Guid.TryParse(userIdString, out userId);

            if (!parsed)
            {
                return Unauthorized();
            }

            if (shelter.shelter_owner_id != userId)
            {
                return Forbid();
            }

            shelter.name = dto.name;
            shelter.address = dto.address;
            shelter.phone = dto.phone;
            shelter.email = dto.email;
            shelter.description = dto.description;

            await db.SaveChangesAsync();

            return Ok(shelter);
        }

        // Delete shelter (Only owner)
        [Authorize(Roles = "shelter_owner")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            Shelters shelter = await db.Shelters.FindAsync(id);

            if (shelter == null)
            {
                return NotFound("Shelter not found.");
            }

            string? userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            Guid userId;
            bool parsed = Guid.TryParse(userIdString, out userId);

            if (!parsed)
            {
                return Unauthorized();
            }

            if (shelter.shelter_owner_id != userId)
            {
                return Forbid();
            }

            db.Shelters.Remove(shelter);
            await db.SaveChangesAsync();

            return Ok("Shelter deleted.");
        }
    }
}
