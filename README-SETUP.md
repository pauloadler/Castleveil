# ARPG Codex Starter

This starter is intended to be copied into the ROOT of a Unity 6.3 LTS Universal 2D project.

## Contents
- `AGENTS.md`: persistent Codex rules for this repository.
- `.agents/skills/`: repository-local Codex skills.
- `Docs/`: architecture, prototype scope and TODO list.
- `Assets/_Game/`: desired project-owned folder layout.
- `tools/bootstrap-folders.ps1`: idempotent folder bootstrap script.

## Recommended order
1. Install Git.
2. Install Unity Hub.
3. Install Unity 6.3 LTS with Windows Build Support (IL2CPP) if you plan to create Windows builds.
4. Create a `Universal 2D` project.
5. Install/confirm Input System and Cinemachine in Package Manager.
6. Initialize Git in the Unity project root.
7. Copy the contents of this starter into that same root.
8. Commit the clean baseline.
9. Open Codex from the repository root.
10. Ask Codex to run `$unity-project-bootstrap` and inspect the real project before implementing gameplay.

## First Codex task
Use:

```text
Use $unity-project-bootstrap.
Inspect this Unity project and normalize only the repository structure and required packages for milestone 1.
Do not implement gameplay yet.
Report the Unity version, package status, files changed, and any Unity Editor action I still need to perform manually.
```

## Second Codex task
After the project opens cleanly:

```text
Use $arpg-player-controller and $unity-code-verification.
Implement only milestone 1 player input, 8-direction movement and mouse-facing.
Do not implement attacks, dodge or skills yet.
Keep the change small and verify compilation before stopping.
```
