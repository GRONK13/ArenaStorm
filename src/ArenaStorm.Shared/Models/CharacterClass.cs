namespace ArenaStorm.Shared.Models;

/// <summary>
/// Playable character archetypes available in ArenaStorm.
/// </summary>
public enum CharacterClass : byte
{
    /// <summary>Swift ranged damage dealer.</summary>
    Ranger = 0,

    /// <summary>High-health defensive juggernaut.</summary>
    Tank = 1,

    /// <summary>Potent ranged spellcaster.</summary>
    Mage = 2,

    /// <summary>Balanced close-range combatant.</summary>
    Warrior = 3
}

/// <summary>
/// Base stats and attributes for a playable character archetype.
/// </summary>
/// <param name="Class">The character class enum identifier.</param>
/// <param name="Name">Human-readable class name.</param>
/// <param name="MaxHealth">Starting maximum health capacity.</param>
/// <param name="MoveSpeed">Base movement speed in units per second.</param>
/// <param name="AttackDamage">Base damage per primary attack.</param>
/// <param name="AttackRange">Effective range of attacks in units.</param>
/// <param name="AttackCooldown">Time in seconds between consecutive attacks.</param>
/// <param name="Description">Descriptive overview of the class playstyle.</param>
public record CharacterClassData(
    CharacterClass Class,
    string Name,
    short MaxHealth,
    float MoveSpeed,
    float AttackDamage,
    float AttackRange,
    float AttackCooldown,
    string Description);

/// <summary>
/// Static registry providing stat definitions for all playable character classes.
/// </summary>
public static class CharacterClassRegistry
{
    /// <summary>
    /// Stat definitions indexed by character class.
    /// </summary>
    public static readonly Dictionary<CharacterClass, CharacterClassData> Classes = new()
    {
        [CharacterClass.Ranger] = new CharacterClassData(
            Class: CharacterClass.Ranger,
            Name: "Ranger",
            MaxHealth: 100,
            MoveSpeed: 5.0f,
            AttackDamage: 15f,
            AttackRange: 500f,
            AttackCooldown: 0.2f,
            Description: "Swift ranged specialist delivering sustained fire from a safe distance."
        ),
        [CharacterClass.Tank] = new CharacterClassData(
            Class: CharacterClass.Tank,
            Name: "Tank",
            MaxHealth: 180,
            MoveSpeed: 3.5f,
            AttackDamage: 40f,
            AttackRange: 200f,
            AttackCooldown: 0.8f,
            Description: "Armored frontliner capable of absorbing immense damage while controlling space."
        ),
        [CharacterClass.Mage] = new CharacterClassData(
            Class: CharacterClass.Mage,
            Name: "Mage",
            MaxHealth: 80,
            MoveSpeed: 4.5f,
            AttackDamage: 35f,
            AttackRange: 400f,
            AttackCooldown: 0.6f,
            Description: "Arcane spellcaster unleashing devastating burst attacks from afar."
        ),
        [CharacterClass.Warrior] = new CharacterClassData(
            Class: CharacterClass.Warrior,
            Name: "Warrior",
            MaxHealth: 130,
            MoveSpeed: 4.0f,
            AttackDamage: 25f,
            AttackRange: 80f,
            AttackCooldown: 0.4f,
            Description: "Aggressive melee brawler excelling at close-quarters pressure."
        )
    };

    /// <summary>
    /// Retrieves the static definition data for a specific character class.
    /// </summary>
    /// <param name="c">The character class archetype.</param>
    /// <returns>The class configuration data.</returns>
    public static CharacterClassData Get(CharacterClass c)
    {
        return Classes.TryGetValue(c, out var data)
            ? data
            : Classes[CharacterClass.Ranger];
    }
}
