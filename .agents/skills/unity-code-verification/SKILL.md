---
name: unity-code-verification
description: Verify Unity/C# changes before completion: inspect compiler errors, run available EditMode tests or safe batch-mode checks, validate runtime-vs-Editor assembly boundaries, review diffs, and report any manual Play Mode validation still required. Use before declaring Unity tasks complete.
---

# Unity Code Verification

Run this skill before declaring a Unity implementation task complete.

## Verification sequence
1. Review the diff for unrelated changes, accidental generated files and secrets.
2. Confirm no code was added under generated Unity folders.
3. Check C# compile errors using the best available local mechanism.
4. Run relevant automated tests when present and practical.
5. Ensure runtime code does not import `UnityEditor`.
6. Inspect missing-reference risks for newly created serialized fields/assets.
7. Check for obvious per-frame allocation or scene-search anti-patterns in gameplay hot paths.
8. Confirm namespaces and folder ownership match project conventions.
9. If Unity Editor or Play Mode validation cannot be run from the current environment, say exactly what the developer must validate manually instead of claiming success.

## Do not
- Do not mask compile errors with disabled code unless explicitly justified.
- Do not delete tests merely to make verification pass.
- Do not claim Play Mode behavior was verified when only static compilation was checked.

## Completion report
Summarize:
- files changed;
- compile status;
- tests run and results;
- remaining manual Unity validation, if any;
- known limitations introduced by the current change.
