# Prototype Architecture

## Goal
A modular Unity 2D isometric action-RPG prototype with minimal infrastructure and a fast gameplay iteration loop.

## Runtime areas

### Core
Cross-cutting runtime primitives that do not belong to one actor type.

### Player
Input orchestration, movement, facing, dodge and player-specific action coordination.

### Combat
Damage contracts, hitboxes/hurtboxes, attacks, health and combat results shared by player and enemies.

### Stats
Reusable character stat/value objects and stamina/experience-related primitives where appropriate.

### Skills
Skill definitions and reusable skill execution/cooldown logic.

### Enemies
Enemy state machines, targeting, movement decisions and boss behavior.

### UI
HUD presentation and game-over/restart presentation. UI should observe gameplay state instead of owning it.

### Editor
Editor-only automation, especially `Tools > ARPG > Build Prototype`.

## Data strategy
Use ScriptableObjects for tunable definitions such as enemy data and skill data. Never store mutable per-instance combat state in shared ScriptableObject assets.

## Prototype philosophy
Prefer explicit, boring code over reusable frameworks until a second real use case proves an abstraction is needed.
