using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using Models;
using System;
using Dtos;

namespace Controllers;

[ApiController]
[Route("users")]
[Authorize(Roles = "admin")] // only admins
public class UsersController : ControllerBase
{
    private readonly AppDbContext db;

    public UsersController(AppDbContext context)
    {
        db = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? role, [FromQuery] string? name, [FromQuery] string? email, [FromQuery] string? sort = "name")
    {
        var query = db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.role == role);

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(u => u.name != null && u.name.ToLower().Contains(name.ToLower()));

        if (!string.IsNullOrWhiteSpace(email))
        {
            string? encryptedEmail = EncryptionService.Encrypt(email);
            query = query.Where(u => u.email == encryptedEmail);
        }

        // Sort
        query = sort switch
        {
            "created" => query.OrderByDescending(u => u.id),
            "email" => query.OrderBy(u => u.email),
            _ => query.OrderBy(u => u.name)
        };

        var users = await query.ToListAsync();

        foreach (var user in users)
        {
            if (!string.IsNullOrWhiteSpace(user.email))
                user.email = EncryptionService.Decrypt(user.email);

            if (!string.IsNullOrWhiteSpace(user.phone))
                user.phone = EncryptionService.Decrypt(user.phone);
        }

        return Ok(users);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] UserUpdateDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.id == id);
        if (user == null)
            return NotFound("User not found.");

        if (!string.IsNullOrWhiteSpace(dto.email))
        {
            string? encryptedEmail = EncryptionService.Encrypt(dto.email);
            bool emailExists = await db.Users.AnyAsync(u => u.email == encryptedEmail && u.id != id);
            if (emailExists)
                return BadRequest("Email is already taken.");

            user.email = encryptedEmail;
        }

        if (!string.IsNullOrWhiteSpace(dto.name))
            user.name = dto.name;

        if (!string.IsNullOrWhiteSpace(dto.phone))
            user.phone = EncryptionService.Encrypt(dto.phone);

        if (!string.IsNullOrWhiteSpace(dto.role))
            user.role = dto.role;

        // Transaction
        using var transaction = await db.Database.BeginTransactionAsync();
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok("User updated.");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.id == id);
        if (user == null)
            return NotFound("User not found.");

        if (!string.IsNullOrWhiteSpace(user.email))
            user.email = EncryptionService.Decrypt(user.email);

        if (!string.IsNullOrWhiteSpace(user.phone))
            user.phone = EncryptionService.Decrypt(user.phone);

        return Ok(user);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null)
            return NotFound("User not found.");
            
        // Transaction
        using var transaction = await db.Database.BeginTransactionAsync();
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new { message = "User deleted." });
    }
}
