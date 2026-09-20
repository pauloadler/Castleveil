---
name: arpg-enemy-ai
description: Implement or modify prototype enemy and boss AI for the isometric ARPG, including Idle/Chase/Attack/Recover/Dead states, melee/ranged behavior, telegraphs, elite variants and a simple boss. Use for enemy decision-making; do not use for generic combat math or project bootstrap.
---

# ARPG Enemy AI

## Baseline state model
Use a small explicit state machine with these conceptual states:
- Idle
- Chase
- Attack
- Recover
- Dead

Do not build a large generic AI framework for the prototype.

## Enemy data
Use `EnemyDefinition : ScriptableObject` or equivalent data for tunable values such as:
- max health
- damage
- move speed
- detection range
- attack range
- attack cooldown
- XP reward

Keep transient state in runtime components, not ScriptableObject assets.

## Melee enemy
- Detect player within configured range.
- Chase until attack range.
- Telegraph briefly when appropriate.
- Attack through the shared combat system.
- Enter Recover before attacking again.
- Stop all combat behavior after death.

## Ranged enemy
- Prefer a configured engagement range.
- Maintain sensible distance where practical without complex pathfinding.
- Fire projectiles through the shared combat primitives.
- Obey cooldown/recovery.

## Elite enemy
For the prototype, reuse the normal architecture with adjusted stats plus one meaningful behavior difference. Do not create an entirely separate framework.

## Boss
Prototype boss must have exactly enough behavior to validate the loop:
- melee attack
- telegraphed area attack
- charge OR projectile attack

No multi-phase architecture is required for milestone 1 unless explicitly requested.

## Readability rules
- Dangerous attacks need visible anticipation/telegraph hooks even if visuals are placeholders.
- State transitions must be understandable from code and debug logs/gizmos where useful.
- Avoid per-frame expensive scene-wide searches.

## Completion checks
- AI cannot attack after death.
- Cooldowns/recover state prevent attack spam.
- Melee and ranged enemies exercise different combat paths.
- Boss uses the same health/damage contracts as other actors.
