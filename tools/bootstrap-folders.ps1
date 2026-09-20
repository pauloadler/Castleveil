$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$folders = @(
  'Assets/_Game/Art/Characters',
  'Assets/_Game/Art/Enemies',
  'Assets/_Game/Art/Environment',
  'Assets/_Game/Art/UI',
  'Assets/_Game/Audio/Music',
  'Assets/_Game/Audio/SFX',
  'Assets/_Game/Data/Characters',
  'Assets/_Game/Data/Enemies',
  'Assets/_Game/Data/Skills',
  'Assets/_Game/Data/Items',
  'Assets/_Game/Materials',
  'Assets/_Game/Prefabs/Characters',
  'Assets/_Game/Prefabs/Enemies',
  'Assets/_Game/Prefabs/Combat',
  'Assets/_Game/Prefabs/UI',
  'Assets/_Game/Scenes',
  'Assets/_Game/Scripts/Core',
  'Assets/_Game/Scripts/Player',
  'Assets/_Game/Scripts/Combat',
  'Assets/_Game/Scripts/Enemies',
  'Assets/_Game/Scripts/Skills',
  'Assets/_Game/Scripts/Stats',
  'Assets/_Game/Scripts/UI',
  'Assets/_Game/Scripts/Editor',
  'Assets/_Game/Settings',
  'Docs'
)

foreach ($folder in $folders) {
    $path = Join-Path $root $folder
    New-Item -ItemType Directory -Force -Path $path | Out-Null
}

Write-Host "ARPG prototype folders are ready at $root"
