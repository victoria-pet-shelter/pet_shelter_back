namespace Models;

public class MultilangBreed
{
    public string? en { get; set; }
    public string? lv { get; set; }
    public string? ru { get; set; }
}

public class SpeciesEntry
{
    public int species_id { get; set; }
    public List<MultilangBreed> breeds { get; set; } = new();
}