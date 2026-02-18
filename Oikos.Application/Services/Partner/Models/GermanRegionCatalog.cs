namespace Oikos.Application.Services.Partner.Models
{
    public record RegionOption(string Code, string Name);

    public static class GermanRegionCatalog
    {
        public static readonly IReadOnlyList<RegionOption> Regions = new List<RegionOption>
        {
            new("BW", "Baden-Württemberg"),
            new("BY", "Bayern"),
            new("BE", "Berlin"),
            new("BB", "Brandenburg"),
            new("HB", "Bremen"),
            new("HH", "Hamburg"),
            new("HE", "Hessen"),
            new("MV", "Mecklenburg-Vorpommern"),
            new("NI", "Niedersachsen"),
            new("NW", "Nordrhein-Westfalen"),
            new("RP", "Rheinland-Pfalz"),
            new("SL", "Saarland"),
            new("SN", "Sachsen"),
            new("ST", "Sachsen-Anhalt"),
            new("SH", "Schleswig-Holstein"),
            new("TH", "Thüringen")
        };
    }
}
