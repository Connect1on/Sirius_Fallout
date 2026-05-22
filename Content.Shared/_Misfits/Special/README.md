# SPECIAL tuning

Base values live on `HumanoidCharacterProfile.Special` and are copied to `SpecialComponent` when the character spawns.
The persistent player-data row mirrors these values for character info/history, but character setup remains authoritative.
Runtime systems should query `SharedSpecialSystem` instead of reading fields directly:

- `GetBase(entity, stat)` for character-creation values.
- `GetModifier(entity, stat)` for temporary modifier totals.
- `GetEffective(entity, stat)` for gameplay-safe values clamped to 1-10.
- `GetCurvedEffectDelta(entity, stat)` for gameplay effects that should scale non-linearly around 5.
- `HasRequirement(entity, stat, minimum)` for perks, weapons, or future skill gates.
- `TryModifyTemporary(entity, stat, modifier, duration, source)` for drugs, chems, injuries, perks, or equipment.

Balance values are in `Resources/Prototypes/_Misfits/Special/special_tuning.yml`.
Initial effects are deliberately small because SS14 combat is real-time.
Most gameplay effects use a curved delta from the effective stat instead of a flat point-for-point delta:

- 1: -5
- 2: -3.5
- 3: -2.25
- 4: -1
- 5: 0
- 6: +1
- 7: +2.25
- 8: +3.75
- 9: +5.5
- 10: +7.5

The tuning values below are multiplied by that curved delta:

- Strength changes melee damage by `strengthMeleeDamageMultiplierPerPoint`.
- Perception changes ranged spread/recoil by `perceptionSpreadReductionPerPoint`.
- Endurance changes stamina crit threshold by `enduranceStaminaCritThresholdPerPoint`.
- Charisma changes character-creation loadout points by the curved delta times 2, rounded away from zero.
- Intelligence changes crafting delay on a fixed curve: 1 blocks hand crafting, 5 is normal speed, 9 is 80% faster, and 10 is instant only for lathe production. Hand construction keeps a small nonzero delay floor.
- Agility changes movement speed by `agilityMovementSpeedMultiplierPerPoint`.
- Luck changes critical-hit and lucky-scavenge chance.
