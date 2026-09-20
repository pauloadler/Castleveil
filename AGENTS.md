# ARPG Prototype — Codex Project Instructions

## Project goal
Build a small, playable 2D isometric action-RPG prototype in Unity 6.6 (Editor 6000.6.2f1) using C# and URP 2D.

References such as Stoneshard, Drakantos and Dimraeth are references for perspective, readability and combat feel only. Do not copy code, assets, characters, maps, names or other protected content.

## Scope for the first milestone
The first milestone is a 5–10 minute playable prototype containing:
- 8-direction player movement
- mouse-facing / aiming
- basic melee attack
- heavy attack
- dodge with stamina and brief invulnerability
- two skills: Whirlwind and Projectile
- health, stamina and simple stats
- melee and ranged enemies
- one elite enemy
- one simple boss
- XP and level-up
- HUD for HP, stamina, XP, level and skill cooldowns
- player death, Game Over and Restart
- one prototype dungeon / arena using placeholder art

Stop when these acceptance criteria are met. Do not expand scope without an explicit request.

## Technology constraints
Use:
- Unity 6.6 (Editor 6000.6.2f1)
- C#
- Universal 2D / URP 2D
- Unity Input System
- Cinemachine when useful
- ScriptableObjects for tunable game data

Do not introduce during the prototype unless explicitly requested:
- multiplayer/networking
- ECS/DOTS
- Addressables
- DI frameworks
- backend/database
- procedural generation
- crafting
- quests/dialogue
- skill trees
- complex inventory
- save-game progression

## Code rules
- Use namespaces beginning with `Game`.
- Prefer composition over large MonoBehaviours.
- Keep PlayerController as an orchestrator, not a god class.
- Separate input, movement, combat, dodge, skills, health and stats.
- Prefer serialized configuration or ScriptableObjects over magic numbers.
- Avoid premature abstraction and generic frameworks without a real use case.
- Avoid allocations in hot gameplay loops where practical.
- Do not run physics overlap queries every Update for attack detection.
- Keep gameplay code deterministic enough to be testable where practical.
- Add comments only when they explain intent or non-obvious constraints.

## Unity asset rules
- Do not hand-edit `.unity`, `.prefab` or `.asset` YAML unless there is no safer alternative.
- Prefer Editor tooling for scene/prefab generation.
- Keep project-owned content under `Assets/_Game/`.
- Do not modify generated folders such as `Library/`, `Temp/`, `Logs/` or `obj/`.

## Required skills
Use the matching repository skill before substantial work:
- Initial setup / folders / packages -> `$unity-project-bootstrap`
- Player input, movement, facing or dodge -> `$arpg-player-controller`
- Damage, hitboxes, stats, attacks or skills -> `$arpg-combat-system`
- Enemy behavior, state machines or boss logic -> `$arpg-enemy-ai`
- Automatic scene/prefab/data construction -> `$unity-prototype-builder`
- Before declaring a task complete -> `$unity-code-verification`

## Work style
1. Inspect existing code and package versions before changing anything.
2. State a short implementation plan for non-trivial work.
3. Make the smallest coherent change that satisfies the task.
4. Verify compilation/tests after relevant changes.
5. Fix regressions introduced by the change before stopping.
6. Update `Docs/TODO.md` only for real remaining work; do not use TODOs as a substitute for implementation.
7. Do not silently add production dependencies.

## Prototype acceptance criteria
The milestone is complete when, from `PrototypeDungeon`, the player can:
1. press Play;
2. move with WASD in 8 directions;
3. aim/facing follows the mouse;
4. perform basic and heavy attacks;
5. dodge using stamina;
6. use Q and E skills;
7. fight melee and ranged enemies;
8. take and deal damage;
9. kill enemies and gain XP;
10. level up;
11. fight a boss;
12. die;
13. restart the run.
