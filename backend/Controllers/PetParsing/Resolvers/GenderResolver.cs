using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using Models;
using System;

public class GenderResolver
{
    private readonly AppDbContext _db;

    public GenderResolver(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int?> ResolveGenderAsync(string? description, string? title = null)
    {
        string combined = ((description ?? "") + " " + (title ?? "")).ToLower();

        if (string.IsNullOrWhiteSpace(combined))
            return null;

        if (Regex.IsMatch(combined, @"\b(девочка|meitene|female|сука|girl|женский|she)\b"))
            return await EnsureGenderExistsAsync("female");

        if (Regex.IsMatch(combined, @"\b(мальчик|puika|male|кобель|boy|мужской|he)\b"))
            return await EnsureGenderExistsAsync("male");

        return null;
    }

    private async Task<int> EnsureGenderExistsAsync(string name)
    {
        var existing = await _db.Genders.FirstOrDefaultAsync(g => g.name == name);
        if (existing != null)
            return existing.id;

        try
        {
            var gender = new Genders { name = name };
            _db.Genders.Add(gender);
            await _db.SaveChangesAsync();
            return gender.id;
        }
        catch (DbUpdateException)
        {
            var existingRetry = await _db.Genders.FirstOrDefaultAsync(g => g.name == name);
            if (existingRetry != null)
                return existingRetry.id;

            throw;
        }
    }
}
