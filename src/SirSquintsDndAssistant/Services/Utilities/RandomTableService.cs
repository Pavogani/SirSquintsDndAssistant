namespace SirSquintsDndAssistant.Services.Utilities;

public interface IRandomTableService
{
    // NPC Generation
    string GetRandomNpcAppearance();
    string GetRandomNpcPersonality();
    string GetRandomNpcSecret();
    string GetRandomNpcOccupation();

    // Tavern/Inn
    string GetRandomTavernName();
    string GetRandomTavernMenu();
    string GetRandomTavernRumor();
    string GetRandomDrink();
    string GetRandomFood();

    // Loot & Treasure
    string GetRandomTrinket();
    string GetRandomBookTitle();
    string GetRandomLetter();
    string GetRandomPocketContents();
    string GetRandomArtObject(int tier);
    string GetRandomGemstone(int tier);

    // Plot & Story
    string GetRandomPlotHook();
    string GetRandomTwist();
    string GetRandomVillainMotivation();
    string GetRandomQuestGiver();

    // Locations
    string GetRandomDungeonFeature();
    string GetRandomWildernessFeature();
    string GetRandomUrbanEncounter();
    string GetRandomShopContents();

    // Combat & Events
    string GetRandomCriticalHitEffect();
    string GetRandomCriticalMissEffect();
    string GetRandomBattlefieldCondition();
    string GetRandomCombatComplication();

    // Comprehensive generators
    FullNpc GenerateFullNpc();
    TavernDetails GenerateFullTavern();
    DungeonRoom GenerateDungeonRoom();
}

public record FullNpc(
    string Name,
    string Race,
    string Occupation,
    string Appearance,
    string Personality,
    string Secret,
    string Voice,
    string Quirk
);

public record TavernDetails(
    string Name,
    string Atmosphere,
    string SpecialtyDrink,
    string SpecialtyFood,
    string Bartender,
    List<string> Rumors,
    string Entertainment
);

public record DungeonRoom(
    string Description,
    string Feature,
    string Hazard,
    string Treasure,
    string Monster
);

public class RandomTableService : IRandomTableService
{
    // ==================== NPC APPEARANCE ====================
    private readonly string[] _npcBuild = { "thin", "muscular", "heavyset", "wiry", "stocky", "lanky", "athletic", "frail", "rotund", "average" };
    private readonly string[] _npcHair = { "bald", "long black hair", "short blonde hair", "curly red hair", "gray braided hair", "wild unkempt hair", "slicked-back hair", "shaved sides", "flowing silver hair", "hair in a topknot" };
    private readonly string[] _npcEyes = { "piercing blue eyes", "warm brown eyes", "striking green eyes", "cold gray eyes", "heterochromatic eyes", "sunken eyes", "bright amber eyes", "pale milky eyes", "dark calculating eyes", "kind crinkled eyes" };
    private readonly string[] _npcDistinguishing = { "a prominent scar", "a tattoo on their neck", "missing an ear", "an eyepatch", "crooked teeth", "a birthmark on their face", "ornate jewelry", "calloused hands", "a limp", "a missing finger" };
    private readonly string[] _npcClothing = { "fine noble attire", "travel-worn clothes", "religious vestments", "practical work clothes", "flashy performer's garb", "military uniform", "ragged peasant clothes", "exotic foreign dress", "leather armor", "scholarly robes" };

    // ==================== NPC PERSONALITY ====================
    private readonly string[] _npcTraits = { "speaks in a whisper", "laughs too loudly", "constantly fidgets", "overly formal", "brutally honest", "pathologically lies", "paranoid", "eternally optimistic", "deeply cynical", "speaks in riddles" };
    private readonly string[] _npcBonds = { "protecting their family", "seeking revenge", "paying off a debt", "finding a lost love", "serving their god", "building their business", "escaping their past", "proving themselves", "finding a cure", "uncovering the truth" };
    private readonly string[] _npcFlaws = { "greedy", "cowardly", "quick to anger", "overly trusting", "secretly addicted", "holds grudges forever", "compulsive gambler", "can't keep secrets", "vain", "jealous" };
    private readonly string[] _npcVoices = { "deep and gravelly", "high and squeaky", "smooth and melodic", "harsh and raspy", "soft and gentle", "booming and loud", "accented and exotic", "monotone", "sing-song", "nervous stutter" };
    private readonly string[] _npcQuirks = { "always eating something", "collects odd trinkets", "talks to themselves", "never makes eye contact", "constantly cleaning", "obsessed with a hobby", "superstitious", "tells bad jokes", "hums constantly", "overshares personal details" };

    // ==================== NPC SECRETS ====================
    private readonly string[] _npcSecrets = {
        "is actually a spy for a rival faction",
        "murdered someone years ago and got away with it",
        "is secretly royalty in hiding",
        "is dying of an incurable disease",
        "is a werewolf",
        "embezzled a fortune and hid it",
        "is in love with someone forbidden",
        "witnessed a crime and is being hunted",
        "made a deal with a devil",
        "is an imposter who replaced the real person",
        "knows the location of a powerful artifact",
        "is a former adventurer with a dark past",
        "is being blackmailed",
        "has a secret child",
        "is plotting against their employer"
    };

    // ==================== NPC OCCUPATIONS ====================
    private readonly string[] _npcOccupations = {
        "Blacksmith", "Baker", "Innkeeper", "Guard", "Merchant", "Scholar", "Priest", "Farmer",
        "Hunter", "Fisher", "Carpenter", "Mason", "Weaver", "Tanner", "Apothecary", "Herbalist",
        "Scribe", "Messenger", "Sailor", "Soldier", "Entertainer", "Thief", "Beggar", "Noble",
        "Servant", "Cook", "Stable hand", "Gravedigger", "Rat catcher", "Street vendor",
        "Lamplighter", "Midwife", "Barber", "Cobbler", "Potter", "Brewer", "Jeweler", "Tailor"
    };

    // ==================== TAVERN ====================
    private readonly string[] _tavernAdjectives = { "Rusty", "Golden", "Silver", "Broken", "Dancing", "Sleeping", "Wandering", "Prancing", "Drunken", "Lucky", "Weary", "Jolly", "Salty", "Crimson", "Shady" };
    private readonly string[] _tavernNouns = { "Dragon", "Unicorn", "Griffin", "Sword", "Tankard", "Barrel", "Maiden", "Knight", "Wizard", "Raven", "Stag", "Boar", "Serpent", "Anchor", "Crown" };
    private readonly string[] _tavernAtmosphere = {
        "Loud and rowdy with arm-wrestling contests",
        "Quiet and cozy with a crackling fireplace",
        "Smoky and mysterious with hooded patrons",
        "Lively with a bard playing upbeat tunes",
        "Tense, with two factions eyeing each other",
        "Nearly empty, with a nervous bartender",
        "Packed wall-to-wall with celebrating locals",
        "Dark and seedy, deals happening in corners"
    };
    private readonly string[] _drinks = {
        "Dwarven Firewhiskey (burns going down)", "Elven Moonwine (silvery and sweet)",
        "Goblin Grog (questionable ingredients)", "Dragon's Breath Ale (spicy and strong)",
        "Halfling Honeymead (smooth and golden)", "Orcish Bloodrum (thick and bitter)",
        "Wizard's Brew (changes color)", "Sailor's Reserve (salty and dark)",
        "Forest Berry Cider", "Local Lager", "Imported Wine", "Mystery Punch"
    };
    private readonly string[] _foods = {
        "Hearty beef stew with crusty bread", "Roasted whole chicken with herbs",
        "Fish and chips with tartar sauce", "Meat pie with gravy",
        "Vegetable soup with fresh bread", "Grilled sausages with sauerkraut",
        "Shepherd's pie", "Cheese and bread platter", "Roasted boar with apples",
        "Mystery meat skewers (don't ask)", "Spiced lamb with rice", "Fried eggs and bacon"
    };
    private readonly string[] _tavernRumors = {
        "They say the old ruins outside town have become active again at night",
        "A merchant's caravan went missing on the north road last week",
        "The baron's daughter has been acting strange since the new moon",
        "Someone's been stealing corpses from the cemetery",
        "A dragon was spotted flying over the mountains to the east",
        "The well water has been tasting strange lately",
        "They found a body in the river - and it wasn't human",
        "The old wizard's tower has lights in it again, but he died years ago",
        "Goblins have been seen in the forest, more organized than usual",
        "A stranger in a black cloak has been asking questions about the party",
        "The temple has been receiving dark omens",
        "Pirates have been raiding ships closer to shore than ever before"
    };
    private readonly string[] _entertainment = {
        "A bard singing heroic ballads", "A card game with high stakes",
        "Arm wrestling tournament", "A storyteller sharing local legends",
        "Darts competition", "A juggler and fire-eater",
        "No entertainment - the mood is somber", "An argument about politics",
        "A traveling merchant showing exotic wares", "A fortune teller reading palms"
    };

    // ==================== TRINKETS ====================
    private readonly string[] _trinkets = {
        "A mummified goblin hand", "A crystal that faintly glows in moonlight",
        "A gold coin from an unknown empire", "A diary written in a strange language",
        "A tiny mechanical spider that doesn't work", "A vial of glowing liquid",
        "A feather that never gets dirty", "An old key to an unknown lock",
        "A small music box that plays an eerie tune", "A stone that's always warm",
        "A preserved fairy wing", "A doll made of corn husks",
        "A compass that points to something other than north", "A mirror that shows a slightly different reflection",
        "A jar of pickled fingers", "A letter addressed to someone you've never heard of",
        "A wanted poster for someone who looks like you", "A tooth from a large creature",
        "A flask that's always full of salt water", "A single playing card that always returns to your pocket"
    };

    // ==================== BOOK TITLES ====================
    private readonly string[] _bookTitles = {
        "The Forbidden Arts of Necromancy", "A Traveler's Guide to the Underdark",
        "Cooking with Monsters: 101 Recipes", "The Complete History of the Dwarven Kingdoms",
        "Love in the Time of Dragons", "Tax Law and You: A Merchant's Guide",
        "The Autobiography of a Mind Flayer", "Herbalism for Beginners",
        "The Art of War Against Goblins", "Poetry of the Elven Courts",
        "Theories on Planar Travel", "The Rise and Fall of the Lich King",
        "A Children's Book of Dangerous Beasts", "The Journal of a Mad Wizard",
        "Forbidden Love Between Races", "The Secret Language of Thieves"
    };

    // ==================== LETTERS ====================
    private readonly string[] _letters = {
        "A love letter never delivered, now decades old",
        "A blackmail note demanding gold for silence",
        "Orders from a military commander, marked urgent",
        "A coded message that seems important",
        "A child's drawing with 'I miss you' written on it",
        "A final letter from someone facing execution",
        "Business correspondence about a shady deal",
        "A map with X marking a spot, no other context",
        "A warning: 'They know. Run.'",
        "An invitation to a secret society meeting",
        "A bounty notice for someone the party knows",
        "A recipe passed down through generations"
    };

    // ==================== POCKET CONTENTS ====================
    private readonly string[] _pocketContents = {
        "3 copper pieces and some lint", "A half-eaten piece of jerky",
        "A love note from someone named 'M'", "A small knife, well-used",
        "A religious symbol on a leather cord", "Dice made of bone",
        "A crumpled wanted poster", "Keys to an unknown door",
        "A pouch of fragrant herbs", "A child's toy soldier",
        "A receipt for a suspicious purchase", "A lock of hair tied with ribbon",
        "A tooth wrapped in cloth", "A small mirror",
        "A folded piece of silk", "Gambling tokens from a local establishment"
    };

    // ==================== ART OBJECTS BY TIER ====================
    private readonly string[][] _artObjects = {
        new[] { // Tier 1 (25 gp)
            "Silver ewer", "Carved bone statuette", "Small gold bracelet",
            "Cloth-of-gold vestments", "Black velvet mask with silver thread",
            "Copper chalice with silver filigree", "Pair of engraved bone dice",
            "Small mirror in painted wooden frame", "Embroidered silk handkerchief"
        },
        new[] { // Tier 2 (250 gp)
            "Gold ring with bloodstone", "Carved ivory statuette",
            "Large gold bracelet", "Silver necklace with gemstone pendant",
            "Bronze crown", "Silk robe with gold embroidery",
            "Well-made tapestry", "Brass mug with jade inlay",
            "Box of turquoise animal figurines"
        },
        new[] { // Tier 3 (750 gp)
            "Silver chalice with moonstones", "Silver-plated longsword with jet in hilt",
            "Carved harp of exotic wood", "Small gold idol",
            "Gold dragon comb with red garnet eye", "Obsidian statuette with gold fittings",
            "Gold bird cage with electrum filigree", "Painted gold war mask"
        },
        new[] { // Tier 4 (2500 gp)
            "Fine gold chain with fire opal", "Old masterpiece painting",
            "Embroidered and bejeweled glove", "Jeweled anklet",
            "Gold music box", "Gold circlet with four aquamarines",
            "Eye patch with mock eye of sapphire and moonstone",
            "Necklace of small pink pearls"
        }
    };

    // ==================== GEMSTONES BY TIER ====================
    private readonly string[][] _gemstones = {
        new[] { "Azurite", "Banded agate", "Blue quartz", "Eye agate", "Hematite", "Lapis lazuli", "Malachite", "Moss agate", "Obsidian", "Tiger eye", "Turquoise" },
        new[] { "Bloodstone", "Carnelian", "Chalcedony", "Chrysoprase", "Citrine", "Jasper", "Moonstone", "Onyx", "Zircon", "Sardonyx", "Star rose quartz" },
        new[] { "Amber", "Amethyst", "Chrysoberyl", "Coral", "Garnet", "Jade", "Jet", "Pearl", "Spinel", "Tourmaline" },
        new[] { "Alexandrite", "Aquamarine", "Black pearl", "Blue spinel", "Peridot", "Topaz", "Diamond", "Emerald", "Ruby", "Sapphire", "Star sapphire" }
    };

    // ==================== PLOT HOOKS ====================
    private readonly string[] _plotHooks = {
        "A child approaches the party claiming their village has been taken over by dopplegangers",
        "The party receives an anonymous letter warning them of an assassination attempt",
        "A dying man stumbles into their camp and whispers a cryptic message before expiring",
        "Local animals have been behaving strangely, all moving in one direction",
        "The party's likeness appears on new wanted posters for crimes they didn't commit",
        "An earthquake reveals the entrance to an ancient underground complex",
        "A solar eclipse triggers strange magical phenomena across the land",
        "The party finds a treasure map on a body floating in a river",
        "A merchant offers enormous payment for retrieving a 'simple family heirloom'",
        "A ghost begs the party to solve their murder",
        "Two feuding nobles both try to hire the party for the same job",
        "A plague is spreading, and the local healer has vanished",
        "An old debt comes due - someone saved a party member's life long ago",
        "A child has been born with strange powers that are attracting dangerous attention",
        "A ship washes ashore with no crew but cargo intact"
    };

    // ==================== PLOT TWISTS ====================
    private readonly string[] _plotTwists = {
        "The villain is actually the hero's long-lost sibling",
        "The helpful NPC has been manipulating the party from the start",
        "The monster is actually the transformed victim they were sent to rescue",
        "The treasure was fake - the real prize was information",
        "The party has been working for the villain all along without knowing it",
        "The dead person they were investigating faked their death",
        "Two separate plots are actually connected",
        "The prophesied hero is actually the villain",
        "The safe haven has been compromised",
        "Time has passed differently than the party realized",
        "The cure is worse than the disease",
        "The trusted authority figure is the source of the problem"
    };

    // ==================== VILLAIN MOTIVATIONS ====================
    private readonly string[] _villainMotivations = {
        "Revenge for a past wrong, real or perceived",
        "To resurrect a dead loved one at any cost",
        "World domination, believing only they can bring order",
        "To prove their superiority over those who doubted them",
        "Desperate to cure their own affliction",
        "Serving a dark god's incomprehensible will",
        "Protecting their people through extreme measures",
        "Accumulating power to feel safe",
        "Destroying what they cannot have",
        "Following a prophecy they believe they must fulfill",
        "Escaping a fate worse than death",
        "Testing worthy opponents for some greater purpose"
    };

    // ==================== DUNGEON FEATURES ====================
    private readonly string[] _dungeonFeatures = {
        "A pit trap with rusty spikes at the bottom",
        "Ancient murals depicting a forgotten civilization",
        "A fountain with water that glows faintly blue",
        "Chains hanging from the ceiling, some still have bones attached",
        "A massive statue with gemstone eyes",
        "A floor covered in strange mushrooms",
        "An altar stained with old blood",
        "A collapsed section revealing natural caves",
        "Prison cells with skeletal remains inside",
        "A magical circle carved into the floor",
        "A bottomless chasm with a narrow bridge",
        "A room where gravity seems reversed"
    };

    // ==================== WILDERNESS FEATURES ====================
    private readonly string[] _wildernessFeatures = {
        "A circle of standing stones humming with energy",
        "The remains of a battlefield, weapons and bones scattered",
        "A massive tree with a door carved into its trunk",
        "A crystal-clear pool with something glinting at the bottom",
        "An abandoned campsite with signs of a struggle",
        "A bridge made of a single fallen giant tree",
        "Strange lights dancing in the distance",
        "A shrine to an unknown deity, offerings still fresh",
        "Animal bones arranged in an unnatural pattern",
        "A cave entrance hidden behind a waterfall",
        "An old well that seems much deeper than it should be",
        "Tracks from a creature of enormous size"
    };

    // ==================== URBAN ENCOUNTERS ====================
    private readonly string[] _urbanEncounters = {
        "A pickpocket tries their luck on a party member",
        "A street preacher warns of coming doom",
        "Two merchant guilds are having a public argument",
        "A noble's carriage blocks the street, guards are rude",
        "A child has lost their pet and asks for help finding it",
        "A public execution is drawing a crowd",
        "A fire breaks out in a nearby building",
        "Someone is handing out pamphlets for a 'business opportunity'",
        "A brawl spills out of a nearby tavern",
        "A mysterious merchant offers unusual wares from their cart",
        "City guard are searching everyone for contraband",
        "A funeral procession blocks the main road"
    };

    // ==================== CRITICAL HIT EFFECTS ====================
    private readonly string[] _criticalHitEffects = {
        "Devastating blow! The enemy is knocked prone.",
        "Precision strike! The enemy is disoriented and has disadvantage on their next attack.",
        "Brutal hit! The enemy begins bleeding (1d4 damage at start of their turns).",
        "Stunning blow! The enemy loses their reaction until their next turn.",
        "Crushing impact! The enemy's armor/hide is damaged (-1 AC until repaired).",
        "Vicious strike! The enemy is frightened of you until end of your next turn.",
        "Perfect timing! You may make an additional attack as a bonus action.",
        "Overwhelming force! Push the enemy 10 feet in a direction of your choice.",
        "Crippling blow! The enemy's speed is reduced by half until end of their next turn.",
        "Masterful strike! The attack deals maximum damage."
    };

    // ==================== CRITICAL MISS EFFECTS ====================
    private readonly string[] _criticalMissEffects = {
        "Weapon slips! Your weapon flies 10 feet in a random direction.",
        "Off balance! You fall prone.",
        "Friendly fire! Make an attack roll against the nearest ally.",
        "Opening! The enemy may use their reaction to make an opportunity attack.",
        "Tangled! Your movement speed is 0 until end of your next turn.",
        "Distracted! You have disadvantage on your next attack.",
        "Equipment failure! A non-weapon item falls from your person.",
        "Embarrassing! You're flustered, enemies have advantage against you until end of your next turn.",
        "Strained! You take 1d4 damage from pulling a muscle.",
        "Nothing happens - just a clean miss."
    };

    // ==================== BATTLEFIELD CONDITIONS ====================
    private readonly string[] _battlefieldConditions = {
        "Heavy rain (-2 to ranged attacks, disadvantage on Perception)",
        "Thick fog (heavily obscured beyond 30 feet)",
        "Strong wind (disadvantage on ranged attacks, extinguishes flames)",
        "Extreme heat (Constitution save DC 10 each hour or gain exhaustion)",
        "Ankle-deep water (difficult terrain, disadvantage on Stealth)",
        "Scattered debris (difficult terrain, Dex save DC 12 or fall prone when running)",
        "Magical darkness (light sources are dimmed by one step)",
        "Toxic spores (Constitution save DC 13 or poisoned until end of next turn)",
        "Unstable ground (loud noise may cause collapse)",
        "Dimensional instability (on a natural 1, teleport 10 feet in random direction)"
    };

    // ==================== COMBAT COMPLICATIONS ====================
    private readonly string[] _combatComplications = {
        "Reinforcements arrive for the enemy",
        "The floor/ceiling begins to collapse",
        "A new faction enters the fight with their own agenda",
        "One enemy attempts to flee to warn others",
        "An innocent bystander wanders into the area",
        "Environmental hazard activates (fire, electricity, etc.)",
        "One enemy reveals a hidden powerful ability",
        "An ally becomes incapacitated or separated",
        "The enemy's leader calls for parley",
        "An enemy switches sides mid-battle",
        "A precious item is in danger of being destroyed",
        "The location begins flooding/filling with gas"
    };

    // ==================== IMPLEMENTATION ====================

    public string GetRandomNpcAppearance()
    {
        var build = _npcBuild[Random.Shared.Next(_npcBuild.Length)];
        var hair = _npcHair[Random.Shared.Next(_npcHair.Length)];
        var eyes = _npcEyes[Random.Shared.Next(_npcEyes.Length)];
        var distinguishing = _npcDistinguishing[Random.Shared.Next(_npcDistinguishing.Length)];
        var clothing = _npcClothing[Random.Shared.Next(_npcClothing.Length)];

        return $"A {build} figure with {hair}, {eyes}, {distinguishing}, wearing {clothing}.";
    }

    public string GetRandomNpcPersonality()
    {
        var trait = _npcTraits[Random.Shared.Next(_npcTraits.Length)];
        var bond = _npcBonds[Random.Shared.Next(_npcBonds.Length)];
        var flaw = _npcFlaws[Random.Shared.Next(_npcFlaws.Length)];

        return $"They {trait}. Motivated by {bond}. Their flaw: {flaw}.";
    }

    public string GetRandomNpcSecret() => _npcSecrets[Random.Shared.Next(_npcSecrets.Length)];
    public string GetRandomNpcOccupation() => _npcOccupations[Random.Shared.Next(_npcOccupations.Length)];

    public string GetRandomTavernName()
    {
        var adj = _tavernAdjectives[Random.Shared.Next(_tavernAdjectives.Length)];
        var noun = _tavernNouns[Random.Shared.Next(_tavernNouns.Length)];
        return $"The {adj} {noun}";
    }

    public string GetRandomTavernMenu()
    {
        var drink = _drinks[Random.Shared.Next(_drinks.Length)];
        var food = _foods[Random.Shared.Next(_foods.Length)];
        return $"Today's Special: {food}\nDrink of the Day: {drink}";
    }

    public string GetRandomTavernRumor() => _tavernRumors[Random.Shared.Next(_tavernRumors.Length)];
    public string GetRandomDrink() => _drinks[Random.Shared.Next(_drinks.Length)];
    public string GetRandomFood() => _foods[Random.Shared.Next(_foods.Length)];
    public string GetRandomTrinket() => _trinkets[Random.Shared.Next(_trinkets.Length)];
    public string GetRandomBookTitle() => _bookTitles[Random.Shared.Next(_bookTitles.Length)];
    public string GetRandomLetter() => _letters[Random.Shared.Next(_letters.Length)];
    public string GetRandomPocketContents() => _pocketContents[Random.Shared.Next(_pocketContents.Length)];

    public string GetRandomArtObject(int tier)
    {
        tier = Math.Clamp(tier, 1, 4) - 1;
        return _artObjects[tier][Random.Shared.Next(_artObjects[tier].Length)];
    }

    public string GetRandomGemstone(int tier)
    {
        tier = Math.Clamp(tier, 1, 4) - 1;
        return _gemstones[tier][Random.Shared.Next(_gemstones[tier].Length)];
    }

    public string GetRandomPlotHook() => _plotHooks[Random.Shared.Next(_plotHooks.Length)];
    public string GetRandomTwist() => _plotTwists[Random.Shared.Next(_plotTwists.Length)];
    public string GetRandomVillainMotivation() => _villainMotivations[Random.Shared.Next(_villainMotivations.Length)];
    public string GetRandomQuestGiver() => $"A {GetRandomNpcOccupation().ToLower()} who {_npcTraits[Random.Shared.Next(_npcTraits.Length)]}";
    public string GetRandomDungeonFeature() => _dungeonFeatures[Random.Shared.Next(_dungeonFeatures.Length)];
    public string GetRandomWildernessFeature() => _wildernessFeatures[Random.Shared.Next(_wildernessFeatures.Length)];
    public string GetRandomUrbanEncounter() => _urbanEncounters[Random.Shared.Next(_urbanEncounters.Length)];

    public string GetRandomShopContents()
    {
        var items = new List<string>();
        var count = Random.Shared.Next(3, 7);
        for (int i = 0; i < count; i++)
        {
            items.Add(_trinkets[Random.Shared.Next(_trinkets.Length)]);
        }
        return string.Join("\n", items.Select((item, idx) => $"{idx + 1}. {item}"));
    }

    public string GetRandomCriticalHitEffect() => _criticalHitEffects[Random.Shared.Next(_criticalHitEffects.Length)];
    public string GetRandomCriticalMissEffect() => _criticalMissEffects[Random.Shared.Next(_criticalMissEffects.Length)];
    public string GetRandomBattlefieldCondition() => _battlefieldConditions[Random.Shared.Next(_battlefieldConditions.Length)];
    public string GetRandomCombatComplication() => _combatComplications[Random.Shared.Next(_combatComplications.Length)];

    public FullNpc GenerateFullNpc()
    {
        var names = new[] { "Aldric", "Brynn", "Cedric", "Dara", "Eldrin", "Fiona", "Gareth", "Helena", "Iona", "Jasper", "Kira", "Liam", "Mira", "Nolan", "Ophelia", "Pierce", "Quinn", "Rosa", "Silas", "Thea", "Ulric", "Vera", "Willem", "Xena", "Yosef", "Zara" };
        var races = new[] { "Human", "Elf", "Dwarf", "Halfling", "Half-Elf", "Gnome", "Half-Orc", "Tiefling", "Dragonborn" };

        return new FullNpc(
            Name: names[Random.Shared.Next(names.Length)],
            Race: races[Random.Shared.Next(races.Length)],
            Occupation: GetRandomNpcOccupation(),
            Appearance: GetRandomNpcAppearance(),
            Personality: GetRandomNpcPersonality(),
            Secret: GetRandomNpcSecret(),
            Voice: _npcVoices[Random.Shared.Next(_npcVoices.Length)],
            Quirk: _npcQuirks[Random.Shared.Next(_npcQuirks.Length)]
        );
    }

    public TavernDetails GenerateFullTavern()
    {
        var rumors = new List<string>();
        var rumorCount = Random.Shared.Next(2, 5);
        for (int i = 0; i < rumorCount; i++)
        {
            rumors.Add(GetRandomTavernRumor());
        }

        return new TavernDetails(
            Name: GetRandomTavernName(),
            Atmosphere: _tavernAtmosphere[Random.Shared.Next(_tavernAtmosphere.Length)],
            SpecialtyDrink: GetRandomDrink(),
            SpecialtyFood: GetRandomFood(),
            Bartender: GetRandomNpcAppearance(),
            Rumors: rumors,
            Entertainment: _entertainment[Random.Shared.Next(_entertainment.Length)]
        );
    }

    public DungeonRoom GenerateDungeonRoom()
    {
        var roomDescriptions = new[] {
            "A cramped storage room with crates and barrels",
            "A grand hall with pillars and a vaulted ceiling",
            "A narrow corridor with alcoves on each side",
            "A circular chamber with a domed roof",
            "A natural cave with stalactites dripping water",
            "An old bedroom with rotting furniture",
            "A torture chamber with rusty implements",
            "A library with moldy books on sagging shelves",
            "An armory with empty weapon racks",
            "A shrine with a defaced altar"
        };

        var hazards = new[] {
            "None apparent", "Unstable ceiling (may collapse)",
            "Poison gas seeping from cracks", "Floor trap (pressure plate)",
            "Magical ward (triggers alarm)", "Slippery floor (difficult terrain)",
            "Extreme cold", "Darkness that resists light", "Spore-filled air"
        };

        var treasures = new[] {
            "Nothing of value", "A few scattered coins (2d10 gp)",
            "A locked chest (DC 15)", "A hidden compartment (DC 18 to find)",
            "Valuable art object", "Magical item (roll on table)",
            "Important documents", "Keys to another room"
        };

        var monsters = new[] {
            "Empty", "Signs of recent occupation but empty now",
            "Undead (skeletons/zombies)", "Vermin (rats/spiders)",
            "Ooze/slime creature", "Goblinoids", "Cultists",
            "A lone powerful creature", "Trapped spirit/ghost"
        };

        return new DungeonRoom(
            Description: roomDescriptions[Random.Shared.Next(roomDescriptions.Length)],
            Feature: GetRandomDungeonFeature(),
            Hazard: hazards[Random.Shared.Next(hazards.Length)],
            Treasure: treasures[Random.Shared.Next(treasures.Length)],
            Monster: monsters[Random.Shared.Next(monsters.Length)]
        );
    }
}
