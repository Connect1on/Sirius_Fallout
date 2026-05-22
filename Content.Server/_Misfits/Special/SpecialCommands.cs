using Content.Shared._Misfits.Special;
using Content.Shared._Misfits.Special.Components;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Misfits.Special;

[AdminCommand(AdminFlags.Debug)]
public sealed class SpecialGetCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "specialget";
    public string Description => "Shows an entity's SPECIAL values.";
    public string Help => "Usage: specialget [entityUid]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var target = args.Length == 0
            ? shell.Player?.AttachedEntity
            : ParseEntity(shell, args[0], _entities);

        if (target == null)
        {
            shell.WriteError("No target entity.");
            return;
        }

        var specialSystem = _entities.System<SharedSpecialSystem>();
        if (!_entities.TryGetComponent<SpecialComponent>(target.Value, out var special))
        {
            shell.WriteError("Target has no SpecialComponent.");
            return;
        }

        foreach (var stat in Content.Shared._Misfits.Special.SpecialStats.All)
        {
            shell.WriteLine($"{stat}: base {specialSystem.GetBase(target.Value, stat, special)}, modifier {specialSystem.GetModifier(target.Value, stat, special)}, effective {specialSystem.GetEffective(target.Value, stat, special)}");
        }
    }

    internal static EntityUid? ParseEntity(IConsoleShell shell, string text, IEntityManager entities)
    {
        if (!NetEntity.TryParse(text, out var netEntity))
        {
            shell.WriteError("Entity UID must be a number.");
            return null;
        }

        if (!entities.TryGetEntity(netEntity, out var target))
        {
            shell.WriteError("Invalid entity UID.");
            return null;
        }

        return target.Value;
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class SpecialSetCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "specialset";
    public string Description => "Sets an entity's base SPECIAL stat.";
    public string Help => "Usage: specialset <entityUid> <strength|perception|endurance|charisma|intelligence|agility|luck> <1-10>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Help);
            return;
        }

        var target = SpecialGetCommand.ParseEntity(shell, args[0], _entities);
        if (target == null)
            return;

        if (!TryParseStat(args[1], out var stat))
        {
            shell.WriteError("Unknown SPECIAL stat.");
            return;
        }

        if (!int.TryParse(args[2], out var value))
        {
            shell.WriteError("Value must be a number.");
            return;
        }

        var specialSystem = _entities.System<SharedSpecialSystem>();
        var special = _entities.EnsureComponent<SpecialComponent>(target.Value);
        if (!specialSystem.TrySetBase(target.Value, stat, value, special))
        {
            shell.WriteError($"Value must be between {SpecialProfile.Minimum} and {SpecialProfile.Maximum}.");
            return;
        }

        shell.WriteLine($"{stat} set to {value}.");
    }

    internal static bool TryParseStat(string text, out SpecialStat stat)
    {
        switch (text.ToLowerInvariant())
        {
            case "s":
            case "str":
            case "strength":
            case "v":
            case "vig":
            case "vigor":
                stat = SpecialStat.Strength;
                return true;
            case "p":
            case "per":
            case "perception":
            case "aw":
            case "aware":
            case "awareness":
                stat = SpecialStat.Perception;
                return true;
            case "e":
            case "end":
            case "endurance":
            case "u":
            case "util":
            case "utility":
                stat = SpecialStat.Endurance;
                return true;
            case "c":
            case "cha":
            case "charisma":
                stat = SpecialStat.Charisma;
                return true;
            case "i":
            case "int":
            case "intelligence":
                stat = SpecialStat.Intelligence;
                return true;
            case "a":
            case "agi":
            case "agility":
            case "t":
            case "tmp":
            case "tempo":
                stat = SpecialStat.Agility;
                return true;
            case "l":
            case "lck":
            case "luck":
                stat = SpecialStat.Luck;
                return true;
            default:
                stat = default;
                return false;
        }
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class SpecialModCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "specialmod";
    public string Description => "Adds a temporary SPECIAL modifier to an entity.";
    public string Help => "Usage: specialmod <entityUid> <strength|perception|endurance|charisma|intelligence|agility|luck> <modifier> [durationSeconds] [source]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 3 or > 5)
        {
            shell.WriteError(Help);
            return;
        }

        var target = SpecialGetCommand.ParseEntity(shell, args[0], _entities);
        if (target == null)
            return;

        if (!SpecialSetCommand.TryParseStat(args[1], out var stat))
        {
            shell.WriteError("Unknown SPECIAL stat.");
            return;
        }

        if (!int.TryParse(args[2], out var modifier))
        {
            shell.WriteError("Modifier must be a number.");
            return;
        }

        TimeSpan? duration = null;
        if (args.Length >= 4)
        {
            if (!float.TryParse(args[3], out var seconds) || seconds <= 0f)
            {
                shell.WriteError("Duration must be a positive number of seconds.");
                return;
            }

            duration = TimeSpan.FromSeconds(seconds);
        }

        var source = args.Length >= 5 ? args[4] : "admin";
        var specialSystem = _entities.System<SharedSpecialSystem>();
        var special = _entities.EnsureComponent<SpecialComponent>(target.Value);

        if (!specialSystem.TryModifyTemporary(target.Value, stat, modifier, duration, source, special))
        {
            shell.WriteError("Could not apply modifier.");
            return;
        }

        shell.WriteLine($"{stat} temporary modifier {modifier:+#;-#;0} applied.");
    }
}
