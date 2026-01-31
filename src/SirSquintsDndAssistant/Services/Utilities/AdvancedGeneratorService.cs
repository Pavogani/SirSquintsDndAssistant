namespace SirSquintsDndAssistant.Services.Utilities;

public interface IAdvancedGeneratorService
{
    NpcPersonality GenerateNpcPersonality();
    QuestHook GenerateQuestHook();
    string GenerateLocationName(string locationType);
    string GenerateShopName();
    string GenerateTavernName();
    string GenerateRumor();
    string GenerateSecret();
}

public class NpcPersonality
{
    public string Trait { get; set; } = string.Empty;
    public string Ideal { get; set; } = string.Empty;
    public string Bond { get; set; } = string.Empty;
    public string Flaw { get; set; } = string.Empty;
    public string Quirk { get; set; } = string.Empty;
    public string Occupation { get; set; } = string.Empty;
    public string Appearance { get; set; } = string.Empty;

    public override string ToString() =>
        $"Occupation: {Occupation}\n" +
        $"Appearance: {Appearance}\n" +
        $"Trait: {Trait}\n" +
        $"Ideal: {Ideal}\n" +
        $"Bond: {Bond}\n" +
        $"Flaw: {Flaw}\n" +
        $"Quirk: {Quirk}";
}

public class QuestHook
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Reward { get; set; } = string.Empty;
    public string Complication { get; set; } = string.Empty;

    public override string ToString() =>
        $"**{Title}** ({Type})\n" +
        $"{Description}\n" +
        $"Reward: {Reward}\n" +
        $"Complication: {Complication}";
}

public class AdvancedGeneratorService : IAdvancedGeneratorService
{
    private readonly Random _random = new();

    #region Personality Data
    private readonly string[] _traits = {
        "Always speaks in whispers",
        "Constantly fidgets with a lucky coin",
        "Speaks their mind without thinking",
        "Overly formal in all interactions",
        "Laughs at inappropriate moments",
        "Very superstitious about everything",
        "Uses big words incorrectly",
        "Always optimistic no matter what",
        "Deeply pessimistic about everything",
        "Talks to their pet/familiar constantly",
        "Compulsively honest",
        "Pathological liar",
        "Easily distracted by shiny things",
        "Never makes eye contact",
        "Incredibly competitive"
    };

    private readonly string[] _ideals = {
        "Freedom: Everyone should be free to live as they choose",
        "Honor: A person's word is their bond",
        "Power: The strong should rule the weak",
        "Knowledge: Understanding is the path to enlightenment",
        "Tradition: The old ways must be preserved",
        "Charity: Helping others is the greatest good",
        "Glory: Fame and recognition matter above all",
        "Justice: The guilty must be punished",
        "Balance: Everything in moderation",
        "Change: Nothing should stay the same"
    };

    private readonly string[] _bonds = {
        "Protecting their family above all else",
        "Avenging a loved one's death",
        "Restoring a lost family heirloom",
        "Finding a lost sibling",
        "Paying off a massive debt",
        "Honoring a promise to a dying friend",
        "Protecting their hometown",
        "Serving their deity faithfully",
        "Proving themselves to a mentor",
        "Keeping a dangerous secret"
    };

    private readonly string[] _flaws = {
        "Can't resist a gamble",
        "Terrified of confined spaces",
        "Trusts no one completely",
        "Addicted to a substance",
        "Easily manipulated by flattery",
        "Holds grudges forever",
        "Arrogant beyond measure",
        "Cowardly when truly threatened",
        "Quick to anger",
        "Obsessed with wealth"
    };

    private readonly string[] _quirks = {
        "Whistles constantly",
        "Collects unusual items",
        "Has an unusual pet",
        "Speaks in riddles",
        "Always eating something",
        "Scratches at an old scar",
        "Counts things obsessively",
        "Has an unusual laugh",
        "Uses strange oaths",
        "Refers to self in third person"
    };

    private readonly string[] _occupations = {
        "Blacksmith", "Innkeeper", "Merchant", "Guard", "Farmer",
        "Scholar", "Priest", "Thief", "Sailor", "Bard",
        "Hunter", "Healer", "Cook", "Messenger", "Stable hand",
        "Librarian", "Brewer", "Tailor", "Jeweler", "Mason"
    };

    private readonly string[] _appearances = {
        "Missing an eye, wears a patch",
        "Covered in intricate tattoos",
        "Exceptionally tall and thin",
        "Short and stout with a thick beard",
        "Beautiful but with cold eyes",
        "Scarred face with a warm smile",
        "Nervous twitch in left eye",
        "Immaculately groomed at all times",
        "Perpetually disheveled appearance",
        "Has unusual hair color",
        "Walks with a pronounced limp",
        "Constantly chewing on something"
    };
    #endregion

    #region Quest Hook Data
    private readonly string[] _questTypes = {
        "Rescue", "Retrieval", "Investigation", "Escort",
        "Exploration", "Hunt", "Negotiation", "Defense"
    };

    private readonly string[] _questTitles = {
        "The Missing {0}", "Shadows of {0}", "The {0}'s Secret",
        "Hunt for the {0}", "The {0} Conspiracy", "Return of the {0}",
        "The Lost {0}", "Curse of the {0}", "The {0} Gambit"
    };

    private readonly string[] _questSubjects = {
        "Merchant", "Noble", "Artifact", "Dragon", "Wizard",
        "Crown", "Dagger", "Temple", "Forest", "King",
        "Ghost", "Beast", "Curse", "Treasure", "Prisoner"
    };

    private readonly string[] _questDescriptions = {
        "A desperate plea comes from a nearby village - {0} has gone missing under mysterious circumstances.",
        "A wealthy patron offers a substantial reward for the recovery of {0}.",
        "Strange occurrences in {0} have the locals terrified. Someone must investigate.",
        "A caravan needs protection while traveling through {0}.",
        "Ancient ruins in {0} have been discovered, but something guards them.",
        "A dangerous creature has been sighted near {0} and must be dealt with.",
        "Two factions are on the brink of war over {0}. A neutral party is needed.",
        "An impending attack threatens {0}. Defenders are desperately needed."
    };

    private readonly string[] _questLocations = {
        "the Darkwood Forest", "the abandoned mines", "the old castle ruins",
        "the merchant quarter", "the coastal caves", "the mountain pass",
        "the ancient temple", "the noble district", "the swamp lands"
    };

    private readonly string[] _questRewards = {
        "500 gold pieces",
        "1,000 gold pieces",
        "A magical weapon",
        "A favor from a noble",
        "Land ownership deed",
        "Free room and board for life",
        "A rare spellbook",
        "Membership in a prestigious guild"
    };

    private readonly string[] _questComplications = {
        "The quest giver has a hidden agenda",
        "A rival party is also pursuing this objective",
        "Time is running out - there's a strict deadline",
        "The target doesn't want to be found/rescued",
        "A powerful faction opposes your success",
        "The information provided is incomplete or wrong",
        "Weather or natural disasters complicate travel",
        "An innocent will be harmed if the quest succeeds"
    };
    #endregion

    #region Location Data
    private readonly string[] _cityAdjectives = { "New", "Old", "Great", "High", "Low", "North", "South", "East", "West", "Dark", "Bright", "Free" };
    private readonly string[] _cityNouns = { "haven", "port", "gate", "crossing", "ford", "hollow", "reach", "vale", "march", "shire" };
    private readonly string[] _forestNames = { "Darkwood", "Whisperwood", "Thornwood", "Greenwood", "Shadowmere", "Eldertree", "Mistwood", "Ironbark" };
    private readonly string[] _mountainNames = { "Thunder Peak", "Dragon's Spine", "Stormcrest", "Iron Heights", "Frostholm", "Eagle's Nest" };
    private readonly string[] _dungeonNames = { "The Depths", "The Undercrypt", "Halls of", "Tomb of", "Lair of", "Prison of", "Sanctum of" };
    private readonly string[] _dungeonSuffixes = { "the Lost King", "Eternal Night", "the Forgotten", "Madness", "Shadows", "Despair" };

    private readonly string[] _shopPrefixes = { "The", "Old", "Master", "Grand", "Royal", "Honest", "Lucky" };
    private readonly string[] _shopTypes = { "Smithy", "Armory", "Apothecary", "Emporium", "Trading Post", "Bazaar", "Curios" };
    private readonly string[] _shopSuffixes = { "and Sons", "Brothers", "& Co.", "Goods", "Wares", "Supplies" };

    private readonly string[] _tavernAdjectives = { "Rusty", "Golden", "Silver", "Prancing", "Laughing", "Weary", "Jolly", "Drunken", "Sleeping", "Dancing" };
    private readonly string[] _tavernNouns = { "Dragon", "Griffin", "Pony", "Sword", "Shield", "Goblet", "Crown", "Stag", "Boar", "Wolf" };
    #endregion

    #region Rumor/Secret Data
    private readonly string[] _rumors = {
        "They say the {0} has been seen in {1}.",
        "I heard that {0} is secretly working for {1}.",
        "Word is that {0} is hidden somewhere in {1}.",
        "People are whispering about strange lights coming from {1}.",
        "A traveler mentioned that {0} was spotted heading toward {1}.",
        "The guards have been questioning everyone about {0}.",
        "An old woman predicted that {0} would bring doom to {1}.",
        "Merchants are refusing to trade near {1} because of {0}."
    };

    private readonly string[] _rumorSubjects = {
        "a dragon", "the missing heir", "a dark cult", "an ancient artifact",
        "a powerful wizard", "undead creatures", "goblin raiders", "the assassin"
    };

    private readonly string[] _secrets = {
        "The local lord is actually a lycanthrope.",
        "The temple's high priest lost their faith years ago.",
        "A secret passage connects the inn to the old castle.",
        "The merchant guild is a front for a thieves' guild.",
        "The town was built on an ancient burial ground.",
        "One of the guards is a spy for a neighboring kingdom.",
        "The mayor is being blackmailed by unknown forces.",
        "A powerful demon is sealed beneath the town square.",
        "The local hero's famous victory was actually faked.",
        "The well in town square leads to an underground complex."
    };
    #endregion

    public NpcPersonality GenerateNpcPersonality()
    {
        return new NpcPersonality
        {
            Trait = _traits[_random.Next(_traits.Length)],
            Ideal = _ideals[_random.Next(_ideals.Length)],
            Bond = _bonds[_random.Next(_bonds.Length)],
            Flaw = _flaws[_random.Next(_flaws.Length)],
            Quirk = _quirks[_random.Next(_quirks.Length)],
            Occupation = _occupations[_random.Next(_occupations.Length)],
            Appearance = _appearances[_random.Next(_appearances.Length)]
        };
    }

    public QuestHook GenerateQuestHook()
    {
        var type = _questTypes[_random.Next(_questTypes.Length)];
        var subject = _questSubjects[_random.Next(_questSubjects.Length)];
        var titleTemplate = _questTitles[_random.Next(_questTitles.Length)];
        var descTemplate = _questDescriptions[_random.Next(_questDescriptions.Length)];
        var location = _questLocations[_random.Next(_questLocations.Length)];

        return new QuestHook
        {
            Title = string.Format(titleTemplate, subject),
            Type = type,
            Description = string.Format(descTemplate, location),
            Reward = _questRewards[_random.Next(_questRewards.Length)],
            Complication = _questComplications[_random.Next(_questComplications.Length)]
        };
    }

    public string GenerateLocationName(string locationType)
    {
        return locationType.ToLower() switch
        {
            "city" or "town" => $"{_cityAdjectives[_random.Next(_cityAdjectives.Length)]}{_cityNouns[_random.Next(_cityNouns.Length)]}",
            "forest" => _forestNames[_random.Next(_forestNames.Length)],
            "mountain" => _mountainNames[_random.Next(_mountainNames.Length)],
            "dungeon" => $"{_dungeonNames[_random.Next(_dungeonNames.Length)]} {_dungeonSuffixes[_random.Next(_dungeonSuffixes.Length)]}",
            _ => $"{_cityAdjectives[_random.Next(_cityAdjectives.Length)]}{_cityNouns[_random.Next(_cityNouns.Length)]}"
        };
    }

    public string GenerateShopName()
    {
        var prefix = _shopPrefixes[_random.Next(_shopPrefixes.Length)];
        var type = _shopTypes[_random.Next(_shopTypes.Length)];
        var suffix = _random.Next(2) == 0 ? "" : $" {_shopSuffixes[_random.Next(_shopSuffixes.Length)]}";
        return $"{prefix} {type}{suffix}";
    }

    public string GenerateTavernName()
    {
        var adj = _tavernAdjectives[_random.Next(_tavernAdjectives.Length)];
        var noun = _tavernNouns[_random.Next(_tavernNouns.Length)];
        return $"The {adj} {noun}";
    }

    public string GenerateRumor()
    {
        var template = _rumors[_random.Next(_rumors.Length)];
        var subject = _rumorSubjects[_random.Next(_rumorSubjects.Length)];
        var location = _questLocations[_random.Next(_questLocations.Length)];
        return string.Format(template, subject, location);
    }

    public string GenerateSecret()
    {
        return _secrets[_random.Next(_secrets.Length)];
    }
}
