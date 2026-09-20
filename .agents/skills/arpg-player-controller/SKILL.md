---
name: arpg-player-controller
description: Implement or modify the ARPG player's Input System bindings, 8-direction movement, mouse-facing, movement locking, dodge, stamina interaction, and player-controller orchestration. Use only for player control/movement concerns, not damage formulas or enemy AI.
---

# ARPG Player Controller

## Design goals
Keep player control responsive and modular. Do not concentrate input, locomotion, attacks, stamina and animation in one class.

## Preferred components
- `PlayerController`: coordinates high-level player state only.
- `PlayerInputReader`: converts Input System actions into game-facing values/events.
- `PlayerMovement`: movement and facing.
- `PlayerDodge`: dodge state, distance, duration, cooldown and invulnerability request.
- `PlayerCombat`: attack requests; detailed combat belongs to `$arpg-combat-system`.
- `PlayerSkills`: skill activation requests.

## Input baseline
- WASD: move
- Mouse position: aim/facing
- LMB: basic attack
- RMB: heavy attack
- Space: dodge
- Q: Whirlwind
- E: Projectile
- Esc: pause placeholder / future pause handling

## Movement requirements
- 8-direction movement.
- Movement must be frame-rate independent.
- Normalize/clamp diagonal input so diagonal movement is not faster.
- Keep facing/aim direction independent from movement direction.
- Centralize movement speed in stats/configuration.
- Use Rigidbody2D consistently if physics movement is chosen; do not mix transform teleport movement with dynamic physics without a clear reason.

## Dodge requirements
Expose/configure:
- duration
- distance or speed
- staminaCost
- cooldown
- invulnerabilityDuration

Rules:
- Dodge must fail cleanly when stamina/cooldown prevents it.
- Dodge state should prevent incompatible actions only for the required interval.
- Invulnerability should be communicated through a narrow interface/event to health/hurtbox logic instead of hard-coding damage logic into PlayerDodge.

## Acceptance checks
- WASD produces equal-speed 8-direction movement.
- Mouse facing is stable at all movement directions.
- Dodge consumes stamina once, obeys cooldown and ends reliably.
- Player classes stay focused and testable.
