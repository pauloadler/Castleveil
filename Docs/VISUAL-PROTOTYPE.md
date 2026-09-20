# Visual prototype foundation (Milestone 4.5A)

In the existing Milestone1 scene, run **Tools > ARPG > Setup Visual Prototype**,
then save the scene. Milestone 1, 3 and 4 player/dummy setup must already exist.
The optional HUD is preserved. No compilation or Play Mode validation was run
for this milestone; check these manually in Unity.

The command migrates the scene player's visuals. It unpacks that player's
prefab instance when necessary to reparent the original visual children.
The source Milestone1Player prefab asset is unchanged. Scene edits support Undo;
generated shared assets are retained. Sorting layers are project settings.

```text
Player                         existing gameplay, Rigidbody2D and body collider
  VisualRoot                   Animator, SortingGroup, PlayerAnimationController,
                               PlayerPlaceholderVisual
    CharacterSprite            SpriteRenderer (previously Body)
    FacingMarker               existing optional direction marker
  Attack Hitbox                existing combat collider
  Follow Target                existing Cinemachine target
Visual Prototype Arena
  Grid
    Ground Tilemap
    Decoration Tilemap
    Collision Tilemap          static Rigidbody2D and TilemapCollider2D
    Foreground Tilemap
  Global Light 2D               created only if no active global light exists
```

The arena is a fixed 16 by 12 tile rectangle, with perimeter walls, three
obstacle cells, decoration and two foreground cells. Obstacles avoid the player
and dummy. On first creation the dummy must be within 5 horizontal and 3 vertical
units of the player. Neither actor is repositioned. The old movement reference
grid is hidden. Repeated setup preserves painted maps and existing tile assets.

Sorting layers are added if missing: Ground, Environment, Characters,
Foreground, VFX. Existing layer IDs/order are preserved. If these names already
exist in a different order, arrange them in that order in Tags and Layers.
The shared placeholder material uses URP's Sprite-Lit-Default shader. The global
light includes the new layers; future local Light2D components can target them.
No render pipeline, post-processing or camera-follow settings are replaced.

## Animation contract

`PlayerAnimationController` is presentation-only: it reads velocity, facing,
active melee, dodge and health, and never controls gameplay. Presentation state
priority is Death, Hit, Dodge, Attack, Walk, Idle. Hit duration is configurable
and does not cause hit-stun. No clips or sprite swaps are required yet.

The generated Animator controller contains six empty states, selected using
the integer `VisualState`: Idle=0, Walk=1, Attack=2, Dodge=3, Hit=4, Death=5.
Other optional parameters are:

- `FacingDirection` (integer): North=0, NorthEast=1, East=2, SouthEast=3,
  South=4, SouthWest=5, West=6, NorthWest=7.
- `MoveX`, `MoveY`, `FacingX`, `FacingY` (floats).
- `IsMoving`, `IsAttacking`, `IsDodging` (booleans).

`DirectionResolver.Resolve` uses world XY (+Y north) and keeps a supplied
fallback for zero-length input. The presentation controller exposes equivalent
read-only properties and a `StateChanged` event. Existing assigned Animator
controllers and generated controller edits are preserved on repeat setup.
Root motion is disabled. Animate only visual children in future clips.

## Future assets

- Put character textures/sprites under `Assets/_Game/Art/Characters/` and
  clips/controllers under its `Animations/` folder.
- Replace the SpriteRenderer sprite on `VisualRoot/CharacterSprite`. Keep
  gameplay components on Player. Adjust visual scale/pivot for the art;
  a consistent feet pivot is useful for top-down characters.
- Put tileset textures under `Assets/_Game/Art/Environment/` and Tile assets
  under `Assets/_Game/Art/Environment/Tiles/`. Paint the four existing Tilemaps.
- Use consistent pixels-per-unit and Point filtering for pixel-art textures.
  Enable appropriate collider types on tiles painted on Collision Tilemap.

The setup creates six `Prototype*.asset` Tile assets in the Tiles folder,
`Art/Characters/Animations/PlayerPrototype.controller`, and
`Materials/VisualPrototypeLit.mat`, all under `Assets/_Game/`, with Unity-created
metadata. No third-party assets or final animation clips are included.
