using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using Models;
using Dtos;
using Validation;

namespace Controllers;

[ApiController]
[Route("shelters")]
public class SheltersController : ControllerBase
{
    private readonly AppDbContext db;

    public SheltersController(AppDbContext dbContext)
    {
        db = dbContext;
    }

    private Guid? GetUserId()
    {
        string? userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        Guid parsedId;
        bool isValid = Guid.TryParse(userIdString, out parsedId);

        if (isValid)
            return parsedId;
        else
            return null;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        List<Shelters> shelters = await db.Shelters
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(shelters);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        Shelters? shelter = await db.Shelters
            .Include(s => s.Owner)
            .FirstOrDefaultAsync(s => s.id == id);

        if (shelter == null)
            return NotFound("Shelter not found.");

        return Ok(shelter);
    }

    [Authorize(Roles = "shelter_owner")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ShelterCreateDto dto)
    {
        ShelterValidator validator = new ShelterValidator();
        Dictionary<string, string> errors = validator.Validate(dto);

        if (errors.Count > 0)
            return BadRequest(new { errors });

        Guid? userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            Shelters newShelter = new Shelters
            {
                id = Guid.NewGuid(),
                shelter_owner_id = userId.Value,
                name = dto.name,
                address = dto.address,
                phone = dto.phone,
                email = dto.email,
                description = dto.description,
                created_at = DateTime.UtcNow
            };

            await db.Shelters.AddAsync(newShelter);
            await db.SaveChangesAsync();

            return Ok(newShelter);
        }
        catch (Exception ex)
            return Problem("Error: " + ex.Message);
    }

    [Authorize(Roles = "shelter_owner")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ShelterUpdateDto dto)
    {
        Shelters? shelter = await db.Shelters.FindAsync(id);
        if (shelter == null)
            return NotFound("Shelter not found.");
        

        Guid? userId = GetUserId();
        if (userId == null || shelter.shelter_owner_id != userId.Value)
            return Forbid();

        ShelterValidator validator = new ShelterValidator();
        Dictionary<string, string> errors = validator.Validate(dto);

        if (errors.Count > 0)
            return BadRequest(new { errors });

        try
        {
            shelter.name = dto.name;
            shelter.address = dto.address;
            shelter.phone = dto.phone;
            shelter.email = dto.email;
            shelter.description = dto.description;

            await db.SaveChangesAsync();

            return Ok(shelter);
        }
        catch (Exception ex)
            return Problem("Error: " + ex.Message);
    }

    [Authorize(Roles = "shelter_owner")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Shelters? shelter = await db.Shelters.FindAsync(id);
        if (shelter == null)
            return NotFound("Shelter not found.");

        Guid? userId = GetUserId();
        if (userId == null || shelter.shelter_owner_id != userId.Value)
            return Forbid();

        try
        {
            db.Shelters.Remove(shelter);
            await db.SaveChangesAsync();

            return Ok(new { message = "Shelter deleted." });
        }
        catch (Exception ex)
        {
            return Problem("Error: " + ex.Message);
        }
    }
}
