namespace SirSquintsDndAssistant.Services.Reference;

public interface IRulesReferenceService
{
    List<QuickRule> GetAbilityCheckRules();
    List<QuickRule> GetCombatRules();
    List<QuickRule> GetSpellcastingRules();
    List<QuickRule> GetMovementRules();
    List<QuickRule> GetRestRules();
    List<QuickRule> GetConditionRules();
    List<QuickRule> SearchRules(string query);
    List<RuleCategory> GetAllCategories();
}

public class QuickRule
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
}

public class RuleCategory
{
    public string Name { get; set; } = string.Empty;
    public List<QuickRule> Rules { get; set; } = new();
}

public class RulesReferenceService : IRulesReferenceService
{
    private readonly List<QuickRule> _allRules;

    public RulesReferenceService()
    {
        _allRules = InitializeRules();
    }

    private List<QuickRule> InitializeRules()
    {
        return new List<QuickRule>
        {
            // Ability Checks
            new() { Name = "Ability Check", Category = "Ability Checks", Description = "Roll d20 + ability modifier. If proficient in a relevant skill, add proficiency bonus.", Example = "d20 + DEX modifier for a Stealth check" },
            new() { Name = "Advantage", Category = "Ability Checks", Description = "Roll 2d20 and take the higher result. Granted by favorable circumstances.", Example = "Attacking a prone enemy in melee gives advantage" },
            new() { Name = "Disadvantage", Category = "Ability Checks", Description = "Roll 2d20 and take the lower result. Imposed by unfavorable circumstances.", Example = "Attacking while blinded gives disadvantage" },
            new() { Name = "Passive Checks", Category = "Ability Checks", Description = "10 + all modifiers that normally apply to the check. Used when no active roll is needed.", Example = "Passive Perception = 10 + WIS modifier + proficiency (if proficient)" },
            new() { Name = "Contest", Category = "Ability Checks", Description = "Both creatures make ability checks. Higher result wins. Ties mean no change.", Example = "Grappling: Athletics vs Athletics or Acrobatics" },

            // Combat
            new() { Name = "Initiative", Category = "Combat", Description = "Roll d20 + DEX modifier at start of combat. Determines turn order (highest first).", Example = "Fighter rolls 15 + 2 DEX = 17 initiative" },
            new() { Name = "Attack Roll", Category = "Combat", Description = "d20 + ability modifier + proficiency bonus (if proficient with weapon). Meet or beat AC to hit.", Example = "d20 + STR + proficiency vs target's AC" },
            new() { Name = "Critical Hit", Category = "Combat", Description = "Natural 20 on attack roll. Roll all damage dice twice. Always hits regardless of AC.", Example = "2d8 + STR for a longsword crit" },
            new() { Name = "Critical Miss", Category = "Combat", Description = "Natural 1 on attack roll. Attack automatically misses regardless of modifiers.", Example = "Rolling a 1 even with +15 to hit still misses" },
            new() { Name = "Cover", Category = "Combat", Description = "Half cover: +2 AC and DEX saves. Three-quarters cover: +5 AC and DEX saves. Full cover: can't be targeted.", Example = "Behind a low wall provides half cover" },
            new() { Name = "Opportunity Attack", Category = "Combat", Description = "Reaction attack when hostile creature leaves your reach. Uses your reaction.", Example = "Enemy moves away, you can attack as a reaction" },
            new() { Name = "Two-Weapon Fighting", Category = "Combat", Description = "When attacking with a light weapon, use bonus action to attack with another light weapon. No ability modifier to damage (unless negative).", Example = "Attack with shortsword, bonus action attack with dagger" },
            new() { Name = "Grappling", Category = "Combat", Description = "Replace one attack. Athletics check vs target's Athletics or Acrobatics. Success reduces target's speed to 0.", Example = "Athletics vs Acrobatics to grab and hold" },
            new() { Name = "Shoving", Category = "Combat", Description = "Replace one attack. Athletics check vs target's Athletics or Acrobatics. Push 5 ft or knock prone.", Example = "Push enemy off cliff or knock prone for advantage" },
            new() { Name = "Prone", Category = "Combat", Description = "Melee attacks have advantage, ranged attacks have disadvantage. Crawling costs double movement. Standing costs half movement.", Example = "Knocked prone, melee attackers have advantage on you" },

            // Damage and Healing
            new() { Name = "Damage Types", Category = "Damage", Description = "Acid, Bludgeoning, Cold, Fire, Force, Lightning, Necrotic, Piercing, Poison, Psychic, Radiant, Slashing, Thunder", Example = "Fireball deals fire damage" },
            new() { Name = "Resistance", Category = "Damage", Description = "Take half damage from the damage type (rounded down).", Example = "10 fire damage becomes 5 with fire resistance" },
            new() { Name = "Vulnerability", Category = "Damage", Description = "Take double damage from the damage type.", Example = "10 fire damage becomes 20 with fire vulnerability" },
            new() { Name = "Immunity", Category = "Damage", Description = "Take no damage from the damage type.", Example = "Fire elemental is immune to fire damage" },
            new() { Name = "Healing", Category = "Damage", Description = "Regain hit points equal to healing amount. Cannot exceed maximum HP.", Example = "Cure Wounds heals 1d8 + spellcasting modifier" },
            new() { Name = "Temporary HP", Category = "Damage", Description = "Extra HP that absorb damage first. Don't stack - take higher value. Lost before real HP.", Example = "False Life grants 1d4+4 temp HP" },
            new() { Name = "Instant Death", Category = "Damage", Description = "If damage reduces you to 0 HP and remaining damage equals or exceeds your max HP, you die instantly.", Example = "30 HP wizard takes 65 damage and dies instantly" },

            // Death and Dying
            new() { Name = "Death Saving Throws", Category = "Death", Description = "At 0 HP, roll d20 on your turn. 10+ = success, 9- = failure. 3 successes = stable. 3 failures = death. Natural 20 = regain 1 HP. Natural 1 = 2 failures.", Example = "Roll 12 = 1 success toward stabilization" },
            new() { Name = "Stabilizing", Category = "Death", Description = "Medicine check DC 10 or any healing stabilizes creature at 0 HP. Stable creature regains 1 HP after 1d4 hours.", Example = "DC 10 Medicine check or healing word" },
            new() { Name = "Damage at 0 HP", Category = "Death", Description = "Taking damage while at 0 HP causes a death save failure. Critical hits cause 2 failures.", Example = "Attacked while unconscious = automatic failure" },

            // Spellcasting
            new() { Name = "Spell Save DC", Category = "Spellcasting", Description = "8 + proficiency bonus + spellcasting ability modifier", Example = "Wizard with +4 INT and +3 proficiency has DC 15" },
            new() { Name = "Spell Attack", Category = "Spellcasting", Description = "d20 + proficiency bonus + spellcasting ability modifier", Example = "Fire Bolt attack = d20 + proficiency + INT" },
            new() { Name = "Concentration", Category = "Spellcasting", Description = "Some spells require concentration. Taking damage requires CON save (DC 10 or half damage, whichever is higher). Only one concentration spell at a time.", Example = "Taking 22 damage = DC 11 CON save to maintain" },
            new() { Name = "Casting Time", Category = "Spellcasting", Description = "Action, Bonus Action, Reaction, or longer. Bonus action spell limits other spells to cantrips only that turn.", Example = "Healing Word (bonus) + Fire Bolt (cantrip) is allowed" },
            new() { Name = "Components", Category = "Spellcasting", Description = "V = Verbal (speaking). S = Somatic (hand gestures). M = Material (components or focus).", Example = "Fireball requires V, S, M" },
            new() { Name = "Upcasting", Category = "Spellcasting", Description = "Some spells can be cast using a higher-level slot for enhanced effect.", Example = "Cure Wounds at 2nd level heals 2d8 + modifier" },

            // Movement
            new() { Name = "Difficult Terrain", Category = "Movement", Description = "Costs 2 feet of movement for every 1 foot moved.", Example = "30 ft speed = 15 ft in difficult terrain" },
            new() { Name = "Climbing/Swimming", Category = "Movement", Description = "Costs 2 feet of movement unless you have a climb/swim speed.", Example = "30 ft speed = 15 ft climbing" },
            new() { Name = "Crawling", Category = "Movement", Description = "Costs 2 feet of movement for every 1 foot moved.", Example = "Prone creature crawls at half speed" },
            new() { Name = "Jumping", Category = "Movement", Description = "Long jump = STR score feet (with 10 ft running start). High jump = 3 + STR modifier feet (with 10 ft running start).", Example = "STR 16 = 16 ft long jump, 6 ft high jump" },
            new() { Name = "Falling", Category = "Movement", Description = "Take 1d6 bludgeoning damage per 10 feet fallen, to a maximum of 20d6.", Example = "40 ft fall = 4d6 bludgeoning damage" },
            new() { Name = "Dash", Category = "Movement", Description = "Action to gain extra movement equal to your speed.", Example = "30 ft speed + Dash = 60 ft total" },
            new() { Name = "Disengage", Category = "Movement", Description = "Action to prevent opportunity attacks for the rest of your turn.", Example = "Use Disengage then move away safely" },

            // Resting
            new() { Name = "Short Rest", Category = "Resting", Description = "At least 1 hour of downtime. Can spend Hit Dice to heal (roll + CON modifier per die).", Example = "Fighter spends 1 Hit Die = 1d10 + CON modifier healing" },
            new() { Name = "Long Rest", Category = "Resting", Description = "At least 8 hours (must sleep 6). Regain all HP and up to half your Hit Dice. Can only benefit once per 24 hours.", Example = "Full HP and spell slots restored" },
            new() { Name = "Hit Dice", Category = "Resting", Description = "Number equals your level. Die size based on class (d6 to d12). Regain half on long rest.", Example = "Level 5 Fighter has 5d10 Hit Dice" },

            // Actions in Combat
            new() { Name = "Attack Action", Category = "Actions", Description = "Make a melee or ranged attack. Extra Attack allows multiple attacks.", Example = "Fighter with Extra Attack makes 2 attacks" },
            new() { Name = "Cast a Spell", Category = "Actions", Description = "Cast a spell with a casting time of 1 action.", Example = "Cast Fireball (1 action)" },
            new() { Name = "Dodge", Category = "Actions", Description = "Attack rolls against you have disadvantage. DEX saves have advantage. Lost if incapacitated or speed is 0.", Example = "Use Dodge when expecting heavy attacks" },
            new() { Name = "Help", Category = "Actions", Description = "Give advantage on next ability check or attack roll to an ally within 5 ft (against target within 5 ft of you).", Example = "Help rogue pick a lock for advantage" },
            new() { Name = "Hide", Category = "Actions", Description = "Stealth check to become hidden. Must be unseen and unheard.", Example = "Dexterity (Stealth) vs Passive Perception" },
            new() { Name = "Ready", Category = "Actions", Description = "Prepare an action with a trigger. Uses your reaction when triggered.", Example = "Ready attack for when enemy enters doorway" },
            new() { Name = "Search", Category = "Actions", Description = "Perception or Investigation check to find something.", Example = "Search for hidden doors or traps" },
            new() { Name = "Use an Object", Category = "Actions", Description = "Interact with an object that requires an action (potion, tool, etc.).", Example = "Drink a healing potion as an action" }
        };
    }

    public List<QuickRule> GetAbilityCheckRules() =>
        _allRules.Where(r => r.Category == "Ability Checks").ToList();

    public List<QuickRule> GetCombatRules() =>
        _allRules.Where(r => r.Category == "Combat" || r.Category == "Actions").ToList();

    public List<QuickRule> GetSpellcastingRules() =>
        _allRules.Where(r => r.Category == "Spellcasting").ToList();

    public List<QuickRule> GetMovementRules() =>
        _allRules.Where(r => r.Category == "Movement").ToList();

    public List<QuickRule> GetRestRules() =>
        _allRules.Where(r => r.Category == "Resting").ToList();

    public List<QuickRule> GetConditionRules() =>
        _allRules.Where(r => r.Category == "Damage" || r.Category == "Death").ToList();

    public List<QuickRule> SearchRules(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _allRules;

        var lowerQuery = query.ToLower();
        return _allRules.Where(r =>
            r.Name.ToLower().Contains(lowerQuery) ||
            r.Category.ToLower().Contains(lowerQuery) ||
            r.Description.ToLower().Contains(lowerQuery))
            .ToList();
    }

    public List<RuleCategory> GetAllCategories()
    {
        return _allRules
            .GroupBy(r => r.Category)
            .Select(g => new RuleCategory
            {
                Name = g.Key,
                Rules = g.ToList()
            })
            .OrderBy(c => c.Name)
            .ToList();
    }
}
