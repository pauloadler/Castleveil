---
name: unity-project-bootstrap
description: Bootstrap or normalize this Unity 2D ARPG repository: inspect Unity/package versions, create the agreed Assets/_Game folder layout, confirm URP 2D/Input System/Cinemachine setup, and prepare the project for prototype development. Use for initial setup or project-structure changes; do not use for gameplay feature implementation.
---

# Unity Project Bootstrap

Use this skill when initializing or repairing the repository structure for the ARPG prototype.

## Procedure
1. Inspect `ProjectSettings/ProjectVersion.txt` and `Packages/manifest.json` before making changes.
2. Confirm the project uses Unity 6.3 LTS or document the actual installed version; never silently upgrade the Editor version.
3. Confirm the project was created from Universal 2D or otherwise has a working URP 2D renderer.
4. Confirm these packages are available before gameplay work:
   - Input System
   - Cinemachine
   - 2D packages required by the chosen template
5. Keep all owned assets under `Assets/_Game/`.
6. Ensure this structure exists:

```text
Assets/_Game/
  Art/Characters
  Art/Enemies
  Art/Environment
  Art/UI
  Audio/Music
  Audio/SFX
  Data/Characters
  Data/Enemies
  Data/Skills
  Data/Items
  Materials
  Prefabs/Characters
  Prefabs/Enemies
  Prefabs/Combat
  Prefabs/UI
  Scenes
  Scripts/Core
  Scripts/Player
  Scripts/Combat
  Scripts/Enemies
  Scripts/Skills
  Scripts/Stats
  Scripts/UI
  Scripts/Editor
  Settings
```

7. Do not add gameplay systems during bootstrap.
8. Do not edit Unity YAML by hand just to create scenes or prefabs.
9. If editor-created assets are needed, prefer a later Editor builder task using `$unity-prototype-builder`.
10. Report any missing package or manual Unity Editor action explicitly.

## Completion checklist
- Unity version identified.
- `manifest.json` inspected.
- URP 2D status known.
- Input System status known.
- Cinemachine status known.
- Folder layout present.
- No unrelated gameplay code added.
