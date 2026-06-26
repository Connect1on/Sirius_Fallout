using Content.Server._Misfits.SpecialStats.Components;
using Content.Server.Speech;
using Content.Shared.Examine;
using Robust.Shared.Random;

namespace Content.Server._Misfits.SpecialStats;

/// <summary>
/// Presents low Charisma through examine text and light speech awkwardness.
/// </summary>
public sealed class SpecialLowCharismaSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly string[] AwkwardOpeners =
    [
        "Ну, ",
        "Зырь, ",
        "Типа, ",
        "Бля, ",
        "Короче, ",
        "Слышь, ",
        "Кхе-кхе, ",
        "Ой жёске, ",
    ];

    private static readonly string[] AwkwardClosers =
    [
        ", Нахуй",
        ", Или типа того",
        ", Хуё-моё",
        ", Ну или я хз",
        ", Ёпта",
        ", Походу",
        ", Така хуйня, малята",
        ", Вот так вот",
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpecialLowCharismaComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SpecialLowCharismaComponent, AccentGetEvent>(OnAccent);
    }

    private void OnExamined(Entity<SpecialLowCharismaComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var locId = ent.Comp.Charisma <= 2
            ? "special-low-charisma-examine-very-low"
            : "special-low-charisma-examine-low";

        args.PushMarkup(Loc.GetString(locId, ("user", ent.Owner)));
    }

    private void OnAccent(Entity<SpecialLowCharismaComponent> ent, ref AccentGetEvent args)
    {
        if (ent.Comp.Charisma > 2 || string.IsNullOrWhiteSpace(args.Message))
            return;

        args.Message = Accentuate(args.Message, ent.Comp.Charisma);
    }

    private string Accentuate(string message, int charisma)
    {
        var openerChance = charisma <= 1 ? 0.30f : 0.15f;
        var closerChance = charisma <= 1 ? 0.25f : 0.10f;
        var stutterChance = charisma <= 1 ? 0.15f : 0.05f;

        message = message.Trim();

        if (_random.Prob(stutterChance))
            message = StutterFirstWord(message);

        if (_random.Prob(openerChance))
            message = _random.Pick(AwkwardOpeners) + message;

        if (_random.Prob(closerChance))
            message = AddCloser(message, _random.Pick(AwkwardClosers));

        return message;
    }

    private static string StutterFirstWord(string message)
    {
        var firstLetter = -1;
        for (var i = 0; i < message.Length; i++)
        {
            if (!char.IsLetter(message[i]))
                continue;

            firstLetter = i;
            break;
        }

        if (firstLetter < 0)
            return message;

        return message.Insert(firstLetter, $"{message[firstLetter]}-");
    }

    private static string AddCloser(string message, string closer)
    {
        if (message.Length == 0)
            return message;

        var last = message[^1];
        if (last is '.' or '!' or '?')
            return message[..^1] + closer + last;

        return message + closer;
    }
}
