namespace SirSquintsDndAssistant.Services.Utilities;

public class NameGenerator
{
    private readonly Random _random = new();
    private readonly string[] _humanFirst = { "Aric", "Bran", "Cole", "Dain", "Erik", "Finn", "Lyra", "Mara", "Nara", "Rena", "Sara", "Tara" };
    private readonly string[] _humanLast = { "Blackwood", "Stormwind", "Ironforge", "Silverhand", "Brightblade", "Swiftarrow" };
    private readonly string[] _elfNames = { "Aelrindel", "Faelyn", "Galadriel", "Haldir", "Legolas", "Miriel", "Sylvari", "Thranduil" };
    private readonly string[] _dwarfNames = { "Balin", "Gimli", "Thorin", "Bofur", "Dain", "Durin", "Gloin" };
    private readonly string[] _tavernNames = { "The Prancing Pony", "The Dragon's Breath", "The Rusty Sword", "The Golden Goblet", "The Weary Traveler" };

    public string GenerateHumanName() => $"{_humanFirst[_random.Next(_humanFirst.Length)]} {_humanLast[_random.Next(_humanLast.Length)]}";
    public string GenerateElfName() => _elfNames[_random.Next(_elfNames.Length)];
    public string GenerateDwarfName() => _dwarfNames[_random.Next(_dwarfNames.Length)];
    public string GenerateTavernName() => _tavernNames[_random.Next(_tavernNames.Length)];
}
