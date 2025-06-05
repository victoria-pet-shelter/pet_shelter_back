using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

public class SpeciesDetector
{
    private readonly Dictionary<string, int> _speciesMap;
    private readonly Dictionary<string, List<string>> _keywords;
    private readonly HashSet<string> _loggedBreeds;
    private readonly string _fallbackLogPath;

    public SpeciesDetector(string breedsPath, string keywordsPath, string fallbackLogPath)
    {
        _fallbackLogPath = fallbackLogPath;
        _speciesMap = LoadSpeciesBreeds(breedsPath);
        _keywords = LoadSpeciesKeywords(keywordsPath);
        _loggedBreeds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public int? DetectSpeciesId(string? breed)
    {
        if (string.IsNullOrWhiteSpace(breed))
        {
            return null;
        }

        string trimmedBreed = breed.Trim();

        // 1. Exact match from species_breeds.json
        if (_speciesMap.TryGetValue(trimmedBreed, out int directId))
        {
            return directId;
        }

        // 2. Match from species_keywords.json
        string lowerBreed = trimmedBreed.ToLower();
        foreach (var pair in _keywords)
        {
            foreach (string keyword in pair.Value)
            {
                if (lowerBreed.Contains(keyword.ToLower()))
                {
                    if (int.TryParse(pair.Key, out int keywordId))
                    {
                        return keywordId;
                    }
                }
            }
        }

        // 3. Fallback to "exotic" check
        if (Regex.IsMatch(lowerBreed, @"\b(игуана|хамелеон|геккон|питон|попугай ара|паук|уж|змея|обезьяна|лемур)\b", RegexOptions.IgnoreCase))
        {
            return 9; // exotic
        }

        // 4. Log and return 999 for unknown
        LogUnknownBreed(trimmedBreed);
        return 999;
    }

    private Dictionary<string, int> LoadSpeciesBreeds(string path)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(path))
        {
            return map;
        }

        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);

            foreach (var group in doc.RootElement.EnumerateObject())
            {
                int speciesId = group.Value.GetProperty("species_id").GetInt32();

                foreach (var breed in group.Value.GetProperty("breeds").EnumerateArray())
                {
                    string? name = breed.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        map[name.Trim()] = speciesId;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("⚠️ Failed to parse species_breeds.json: " + ex.Message);
        }

        return map;
    }

    private Dictionary<string, List<string>> LoadSpeciesKeywords(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new Dictionary<string, List<string>>();
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)
                ?? new Dictionary<string, List<string>>();
        }
        catch (Exception ex)
        {
            Console.WriteLine("⚠️ Failed to parse species_keywords.json: " + ex.Message);
            return new Dictionary<string, List<string>>();
        }
    }

    private void LogUnknownBreed(string breed)
    {
        try
        {
            if (_loggedBreeds.Contains(breed))
            {
                return;
            }

            EnsureLogFileExists();
            File.AppendAllText(_fallbackLogPath, breed + Environment.NewLine);
            _loggedBreeds.Add(breed);
        }
        catch (Exception ex)
        {
            Console.WriteLine("⚠️ Failed to log unknown breed: " + ex.Message);
        }
    }

    private void EnsureLogFileExists()
    {
        try
        {
            string? directory = Path.GetDirectoryName(_fallbackLogPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(_fallbackLogPath))
            {
                File.WriteAllText(_fallbackLogPath, $"🗂️ Unknown breeds log started at {DateTime.Now}\n\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("⚠️ Failed to ensure log file exists: " + ex.Message);
        }
    }
}
