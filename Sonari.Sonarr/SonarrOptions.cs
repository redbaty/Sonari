namespace Sonari.Sonarr;

public class SonarrOptions
{
    public Uri? BaseAddress { get; set; }
    
    public string? Key { get; set; }
    
    public int TagId { get; set; }

    public string? Prefix { get; set; } = "job-sonari";
}