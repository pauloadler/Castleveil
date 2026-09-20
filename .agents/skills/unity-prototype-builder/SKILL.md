---
name: unity-prototype-builder
description: Create or maintain Unity Editor tooling that automatically builds the ARPG prototype assets, ScriptableObjects, prefabs, HUD and PrototypeDungeon scene. Use when scene/prefab/data setup would otherwise require fragile manual YAML editing or repetitive Unity Editor work.
---

# Unity Prototype Builder

## Goal
Provide an Editor command:

`Tools > ARPG > Build Prototype`

that assembles the milestone-1 playable prototype using placeholder visuals.

## Rules
- Put Editor-only code under `Assets/_Game/Scripts/Editor/`.
- Never place `UnityEditor` references in runtime assemblies/files.
- Prefer Unity serialization APIs and prefab/scene APIs over raw YAML edits.
- Make repeated execution idempotent where practical.
- Use stable asset paths under `Assets/_Game/`.
- Do not overwrite user-authored assets unless the builder owns them and the behavior is documented.

## Builder responsibilities
The command should be able to create/configure, as appropriate:
1. required prototype ScriptableObjects;
2. placeholder materials/sprites if needed;
3. Player prefab/components;
4. MeleeEnemy prefab;
5. RangedEnemy prefab;
6. EliteEnemy prefab;
7. PrototypeBoss prefab;
8. combat projectile prefab(s);
9. Canvas/HUD;
10. `PrototypeDungeon` scene;
11. camera/Cinemachine setup;
12. player spawn;
13. walls/obstacles;
14. enemy encounters;
15. boss arena;
16. Game Over/Restart UI;
17. save modified assets/scenes and report results.

## Safety
- Validate package/component availability before using package-specific types.
- If an asset already exists, update only builder-owned serialized fields or skip it deliberately.
- Log actionable errors that tell the developer what is missing.
- Do not create hundreds of anonymous placeholder assets when a few reusable placeholders are enough.

## Verification
After generation:
- scene opens without missing scripts;
- prefabs have required runtime components;
- ScriptableObject references are populated;
- no Editor-only type leaks into runtime code;
- repeated builder execution does not duplicate encounters/UI indefinitely.
