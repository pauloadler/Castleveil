---
name: arpg-combat-system
description: Build or change the prototype ARPG combat foundation: health, damage calculation, hitbox/hurtbox interactions, attacks, knockback, hit-stun, critical hits, stamina costs, skill definitions, cooldowns and the two prototype skills. Do not use for enemy decision-making or scene construction.
---

# ARPG Combat System

## Required primitives
Prefer small, explicit types such as:
- `IDamageable`
- `DamageInfo`
- `DamageResult` when useful
- `Health`
- `Hitbox`
- `Hurtbox`
- `CharacterStats`
- `AttackDefinition` or equivalent data where it reduces duplication
- `SkillDefinition : ScriptableObject`
- `SkillController`

## Prototype stats
Support at least:
- MaxHealth
- AttackDamage
- Armor
- MoveSpeed
- CriticalChance
- CriticalDamage
- MaxStamina
- StaminaRegeneration

## Damage rules
- Keep raw attack data separate from target health mutation.
- Resolve critical hit and mitigation in one clearly testable location.
- Clamp invalid values where appropriate.
- Do not let callers directly mutate target HP for normal combat.
- Avoid hidden scene lookups (`FindObjectOfType`, string searches) in combat hot paths.

## Hit detection
- Activate hitboxes only during attack windows.
- Do not continuously call overlap queries in `Update` for melee detection.
- Prevent the same attack instance from repeatedly damaging the same target unless explicitly configured.
- Make team/faction filtering explicit enough to prevent self-hits.

## Prototype attacks
Implement support for:
- basic melee attack
- heavy melee attack
- configurable cooldown/recovery
- knockback
- simple hit-stun

## Prototype skills
`Whirlwind`:
- circular/local area damage around the player;
- cooldown;
- stamina cost;
- configurable damage multiplier.

`Projectile`:
- spawns a projectile toward current aim direction;
- projectile owns movement/lifetime/hit handling;
- cooldown;
- stamina cost;
- configurable damage multiplier.

`SkillDefinition` must expose at least:
- Id
- DisplayName
- Cooldown
- StaminaCost
- DamageMultiplier

## Tests
When practical, add EditMode tests for:
- health clamping and death transition
- deterministic damage/armor math excluding random critical selection or by injecting/controlling randomness
- XP-independent combat calculations
- cooldown state if it can be tested without scene timing

## Completion checks
- Player and enemies use the same damage contract.
- Attack windows cannot multi-hit accidentally.
- Stats are not duplicated as magic numbers across MonoBehaviours.
- Combat code does not depend on a specific scene hierarchy.
