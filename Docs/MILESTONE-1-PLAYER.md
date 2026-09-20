# Milestone 1: player movement and facing

Requires Unity 6.6, Editor 6000.6.2f1, with the repository's Input System,
Cinemachine and URP packages. This task covers movement, facing and camera
follow only; the larger prototype checklist remains in TODO.md.

## Setup

1. Allow Unity to finish importing and compiling.
2. Outside Play Mode, run **Tools > ARPG > Setup Milestone 1**.
3. Save any currently edited scene if prompted.
4. The command opens `Assets/_Game/Scenes/Milestone1.unity`. Press Play and
   click the Game view to give it keyboard focus.

On its first run the command creates these assets through Unity Editor APIs:

- `Assets/_Game/Art/Characters/Milestone1Square.png`
- `Assets/_Game/Prefabs/Characters/Milestone1Player.prefab`
- `Assets/_Game/Scenes/Milestone1.unity`

Unity also generates their `.meta` files. The source Input Actions asset is
`Assets/_Game/Settings/Milestone1.inputactions`; only Move and Pointer actions
are defined. The command does not modify build profiles or global input settings.
It preserves existing assets. If the scene already exists, it simply opens it,
without duplicating objects or resetting Inspector changes.

## Components

- `PlayerInputReader` owns a private action-asset instance and exposes clamped
  movement input and mouse screen position. Its action state is cleared when
  disabled, and movement is suppressed when the application loses focus.
- `PlayerMovement` sets dynamic Rigidbody2D velocity in FixedUpdate, in world
  units per second. Unity's physics integration applies the timestep. Change
  **Movement Speed** in the Inspector (default: 5). Diagonals are clamped to
  the same speed as cardinal movement. Physics rotation is frozen and gravity
  is zero in the generated prefab.
- `PlayerMovement.MouseWorldPosition` is the cursor's intersection with the
  player's XY plane. `FacingDirection` is a normalized world-space Vector2,
  independent of travel direction, for later combat use. It retains its last
  direction when the pointer is at the player's center or unavailable.
- `PlayerPlaceholderVisual` positions a gold marker toward the mouse; there
  is no animation system. The body and collider do not rotate.
- A Cinemachine camera follows the player's child **Follow Target** using a
  position composer. The main camera is orthographic. Camera follow runs in
  LateUpdate over the interpolated player pose, before mouse world projection.

## Manual Play Mode checks

1. Hold W, A, S and D separately, then all four diagonal combinations. Check
   that releasing the keys stops the player and opposite keys cancel.
2. Compare equal-duration cardinal and diagonal travel using the world grid
   or the player's Transform position. At speed 5, five seconds of cardinal
   movement covers about 25 units; diagonal travel covers about 17.68 units
   on each axis (25 total), allowing for key timing.
3. Repeat movement with Game-view VSync on and off, or at different frame
   rates. Equal-time world distance should remain consistent.
4. Move the mouse around a stationary player and while moving in every
   direction. The gold marker should follow the cursor independently of WASD.
   Put the pointer at the player center: the last facing direction should stay
   stable. Pan by moving the player and check cursor alignment during follow.
5. Verify the camera follows smoothly, the player does not fall or rotate,
   and the grid scrolls. The grid is a reference, not a bounded arena.
6. Switch focus away and return, then disable/re-enable PlayerInputReader in
   Play Mode. Verify no stuck movement and that input resumes normally.
7. Change Movement Speed (including zero) and verify the effect. Disable
   PlayerMovement while moving and verify its velocity becomes zero.
8. Stop Play Mode, run setup again, and check there is still one Player,
   one Main Camera and one Player Follow Camera, with Inspector edits preserved.
9. Check the Console for errors.

No combat, dodge, stamina, enemies, XP, inventory or gameplay animation is included.

## Verification performed

The source compiled with Unity 6000.6.2f1 in an isolated project copy. The
setup command successfully saved its scene, prefab and sprite through Unity
APIs. A temporary automated Play Mode harness passed checks for setup
idempotence, component/asset references, input normalization and cancellation,
equal two-second cardinal/diagonal travel at 50 and 100 Hz physics steps,
stopping on release/focus loss/disable, mouse world projection, and retaining
facing at zero aim distance.

The headless Editor also emitted exceptions from its internal
`UnityEditor.Search.SearchDatabase` startup indexing, outside the player code.
No C# compiler errors remained. Visual rendering and camera smoothness still
need the manual checks above; the temporary harness is not a repository test suite.
