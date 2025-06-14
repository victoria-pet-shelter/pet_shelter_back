using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Controllers;

[ApiController]
[Route("pets")]
public class PetsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PetsController(AppDbContext db)
    {
        _db = db;
    }

    private Guid? GetUserId()
    {
        string? userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? shelterId, [FromQuery] int? speciesId, [FromQuery] string? name, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 100) pageSize = 10;

        var query = _db.Pets
            .Include(p => p.Species)
            .Include(p => p.Breed)
            .Include(p => p.Gender)
            .Include(p => p.Shelter)
            .AsQueryable();

        if (shelterId != null)
            query = query.Where(p => p.shelter_id == shelterId);
        if (speciesId != null)
            query = query.Where(p => p.species_id == speciesId);
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(p => p.name != null && p.name.ToLower().Contains(name.ToLower()));

        int totalCount = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pets = await query
            .OrderByDescending(p => p.created_at)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { currentPage = page, pageSize, totalCount, totalPages, pets });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var pet = await _db.Pets
            .Include(p => p.Species)
            .Include(p => p.Breed)
            .Include(p => p.Gender)
            .Include(p => p.Shelter)
            .FirstOrDefaultAsync(p => p.id == id);

        if (pet == null)
            return NotFound("Pet not found");

        return Ok(pet);
    }

    [Authorize(Roles = "shelter_owner")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Pets dto)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var shelter = await _db.Shelters.FirstOrDefaultAsync(s => s.id == dto.shelter_id && s.shelter_owner_id == userId);
        if (shelter == null)
            return Forbid("Shelter not found or you don't own it.");

        dto.id = Guid.NewGuid();
        dto.created_at = DateTime.UtcNow;

        using var transaction = await _db.Database.BeginTransactionAsync();
        await _db.Pets.AddAsync(dto);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(dto);
    }

    [Authorize(Roles = "shelter_owner")]
    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] Pets patch)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var pet = await _db.Pets.FirstOrDefaultAsync(p => p.id == id);
        if (pet == null)
            return NotFound("Pet not found");

        var shelter = await _db.Shelters.FirstOrDefaultAsync(s => s.id == pet.shelter_id);
        if (shelter == null || shelter.shelter_owner_id != userId)
            return Forbid("You do not own this shelter.");

        // Apply updates
        if (!string.IsNullOrWhiteSpace(patch.name)) pet.name = patch.name;
        if (!string.IsNullOrWhiteSpace(patch.description)) pet.description = patch.description;
        if (!string.IsNullOrWhiteSpace(patch.image)) pet.image = patch.image;
        if (!string.IsNullOrWhiteSpace(patch.color)) pet.color = patch.color;
        if (!string.IsNullOrWhiteSpace(patch.category)) pet.category = patch.category;
        if (!string.IsNullOrWhiteSpace(patch.cena)) pet.cena = patch.cena;
        if (!string.IsNullOrWhiteSpace(patch.external_url)) pet.external_url = patch.external_url;
        if (patch.age != null) pet.age = patch.age;
        if (patch.gender_id != null) pet.gender_id = patch.gender_id;
        if (patch.breed_id != 0) pet.breed_id = patch.breed_id;
        if (patch.species_id != 0) pet.species_id = patch.species_id;

        using var transaction = await _db.Database.BeginTransactionAsync();
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(pet);
    }

    [Authorize(Roles = "shelter_owner")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var pet = await _db.Pets.FirstOrDefaultAsync(p => p.id == id);
        if (pet == null)
            return NotFound("Pet not found");

        var shelter = await _db.Shelters.FirstOrDefaultAsync(s => s.id == pet.shelter_id);
        if (shelter == null || shelter.shelter_owner_id != userId)
            return Forbid("You do not own this shelter.");

        using var transaction = await _db.Database.BeginTransactionAsync();
        _db.Pets.Remove(pet);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok("Pet deleted.");
    }
}
