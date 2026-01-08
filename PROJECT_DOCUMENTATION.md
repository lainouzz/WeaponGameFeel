# PROJECT DOCUMENTATION
## Extraction-Style FPS Game (Working Title)
**Last Updated:** 2025-01-06
**Status:** Early Development / Prototype

---

## 📋 PROJECT OVERVIEW

This is an extraction-style FPS game built in Unity. Players enter a level, collect loot, fight enemies in waves, and must extract before dying with as much loot as possible, sell the loot and upgrade.
Death results in losing all collected items. The project is in early development with many systems in prototype/placeholder state.
Player can also gamble collected credits at a slot machine. cuz we all are degenerate gamblers :)

**Target Platform:** PC
**Unity/Editor Version:** 2022+ (New Input System) 6000.0.24f1
**Framework:** .NET Framework 4.7.1

---

## ✅ COMPLETED SYSTEMS

### 1. Player Movement (`PlayerMovement.cs`)
- [x] Basic WASD movement with CharacterController
- [x] Sprint (Shift)
- [x] Crouch (Toggle)
- [x] Jump
- [x] Footstep audio system (left/right foot alternation)
- [x] Movement state flags (IsMoving, IsSprinting, IsCrouching)
- [x] `canMove` flag for disabling movement (used by slot machine, etc.)

### 2. Camera System (`CameraMovement.cs`)
- [x] Mouse look
- [x] Procedural camera recoil (Battlefield-style)
- [x] Recoil recovery

### 3. Weapon System (`Weapon.cs`)
- [x] State machine architecture (Idle, Firing, Reloading, Inspecting, Drawing, Holstering, Switching)
- [x] Raycast shooting with damage
- [x] Bullet spread (hipfire, ADS, movement-based)
- [x] Sustained fire spread accumulation
- [x] ADS (Aim Down Sights) with smooth transitions
- [x] Recoil system (visual weapon kick + camera recoil)
- [x] Magazine drop during reload (physical object)
- [x] Muzzle flash particles
- [x] Ammo system (magazine + reserve)
- [x] Holster/unholster system (H key)
- [x] Weapon sway based on mouse input
- [x] Impact force on hit objects
- [x] Sound handler integration

### 4. Weapon Inspection (`Inspect.cs`)
- [x] Hold to inspect (rotate weapon with mouse)
- [x] Weight-based rotation speed
- [x] Extended roll for viewing other side
- [x] Smooth return to original position

### 5. Inventory System (`PlayerInventoryManager.cs`)
- [x] Item storage with quantity limits
- [x] Credits (currency) system
- [x] Save/Load to JSON file
- [x] Auto-save on quit/pause
- [x] UI integration with ItemUI prefabs
- [x] Right-click to transfer items (for vendor)
- [x] Clear inventory on death

### 6. Item System
- [x] `ItemData` ScriptableObject (id, name, icon, price, quantity limit)
- [x] `ItemDatabase` for lookup by ID
- [x] `ItemPickup` world object
- [x] `ItemPickupBehavior` raycast-based pickup (E key)
- [x] Quantity display in UI

### 7. Vendor/Reforge Station (`ReforgeStationMenuController.cs`, `VendorSellManager.cs`)
- [x] Transfer items from inventory to sell queue
- [x] Sell items for credits
- [x] Stat upgrade purchasing (Health)

### 8. Player Stats System (`StatsManager.cs`, `BaseStat.cs`, `HealthStat.cs`)
- [x] Abstract `BaseStat` class with events
- [x] `HealthStat` with damage, healing, regeneration
- [x] Upgrade system (spend credits to increase max HP)
- [x] Death event handling
- [ ] Stamina stat (structure exists, not fully implemented)

### 9. Extraction System (`ExtractManager.cs`, `ExtractionZone.cs`)
- [x] Extraction zone trigger detection
- [x] Countdown timer with UI text
- [x] Cancel extraction on leaving zone
- [x] Load main menu on successful extract
- [ ] Proper extraction rewards/logic

### 10. Gambling - Slot Machine (`SlotMachine.cs`)
- [x] Distance-based interaction (E to open)
- [x] Bet adjustment (Up/Down arrows)
- [x] Spin with random outcome (Space)
- [x] Win multipliers (2x, 3x, 5x jackpot)
- [x] Credit spending/winning
- [x] Player movement disabled while using
- [x] Debug logging
- [ ] Proper visual slot spinning

### 11. Debug System (`Debugging.cs`)
- [x] Toggle debug mode (X key)
- [x] Give credits (G key)
- [x] Kill player / clear inventory (K key)
- [x] Heal player (H key)
- [x] Damage player (J key)

### 12. UI Systems
- [x] Ammo display
- [x] Credits display
- [x] Inventory panel (Tab to toggle)
- [x] Extraction timer text
- [x] Slot machine UI
- [x] Upgrade cost/level display
- [ ] Full game UI (menus, HUD, etc.)

---

## 🔧 TODO LIST - REMAINING WORK

### HIGH PRIORITY

#### Loot Spawning System
- [ ] Create `LootSpawnZone` component for defining spawn areas
- [ ] Create `LootSpawner` manager for handling spawns
- [ ] Percentage-based spawn chance per item
- [ ] Spawn points on shelves, tables, containers
- [ ] Loot rarity system
- [ ] Randomized loot tables per zone

#### Enemy System
- [ ] Base enemy AI class
- [ ] Wave spawn system
- [ ] Enemy health/damage
- [ ] Enemy pathfinding (NavMesh)
- [ ] Different enemy types
- [ ] Enemy drop loot on death

#### Player Spawn System
- [ ] Spawn point selection
- [ ] Spawn protection
- [ ] Respawn logic (if applicable)

#### Death/Game Over Logic
- [ ] Death screen UI
- [ ] Inventory loss confirmation
- [ ] Return to main menu or respawn option
- [ ] Death animation/effect

#### Proper Extraction Logic
- [ ] Extraction rewards screen
- [ ] Keep extracted items permanently
- [ ] Extraction statistics
- [ ] Multiple extraction points per level

### MEDIUM PRIORITY

#### Complete Player Stats
- [ ] Stamina implementation (sprinting costs stamina)
- [ ] Weapon damage stat (scales weapon damage)
- [ ] Movement speed stat
- [ ] Additional stats as needed
- [ ] Stats save/load

#### UI Overhaul
- [ ] Main menu design
- [ ] Pause menu
- [ ] Settings menu (audio, graphics, controls)
- [ ] HUD redesign (health bar, stamina bar, compass)
- [ ] Inventory redesign
- [ ] Death screen
- [ ] Extraction success screen
- [ ] Loading screens

#### Slot Machine Improvements
- [ ] Proper 3D slot machine model
- [ ] Animated spinning reels
- [ ] Sound effects
- [ ] Visual feedback for wins
- [ ] Multiple machine types/odds

#### Additional Weapons
- [ ] At least 1 more weapon (pistol, shotgun, etc.)
- [ ] Weapon switching system
- [ ] Weapon pickups in world
- [ ] Weapon stats variation

#### Skill Tree (Optional)
- [ ] Skill tree UI
- [ ] Skill point earning
- [ ] Passive abilities
- [ ] Active abilities

### LOW PRIORITY / POLISH

#### Animations
- [ ] First-person hand animations
- [ ] Weapon reload animations
- [ ] Weapon draw/holster animations
- [ ] Inspect animation polish
- [ ] ADS animation

#### Audio (SFX)
- [ ] Weapon fire sounds (per weapon)
- [ ] Reload sounds
- [ ] UI sounds (button clicks, inventory)
- [ ] Pickup sounds
- [ ] Ambient sounds
- [ ] Enemy sounds
- [ ] Extraction sounds
- [ ] Win/lose sounds (slot machine)

#### Visual Effects (VFX)
- [ ] Better muzzle flash
- [ ] Bullet tracers
- [ ] Impact effects (per material)
- [ ] Blood/damage effects
- [ ] Extraction portal effect
- [ ] Slot machine effects
- [ ] UI animations

#### Level Design
- [ ] Proper game level(s)
- [ ] Loot placement
- [ ] Enemy spawn points
- [ ] Extraction zones
- [ ] Environmental storytelling
- [ ] Lighting and atmosphere

#### Game Economy
- [ ] Balance loot values
- [ ] Balance upgrade costs
- [ ] Balance enemy difficulty
- [ ] Balance extraction risk/reward
- [ ] Gambling odds tuning

#### Tutorial
- [ ] Tutorial level or overlay
- [ ] Control hints
- [ ] Objective guidance
- [ ] First-time player experience

#### Optimization
- [ ] Object pooling for projectiles/effects
- [ ] LOD for models
- [ ] Occlusion culling
- [ ] Audio optimization
- [ ] AI optimization (if needed)
- [ ] Profiling and performance fixes

#### Code Cleanup
- [ ] Remove legacy/unused code
- [ ] Consistent naming conventions
- [ ] Add XML documentation
- [ ] Refactor large methods
- [ ] Make systems more modular
- [ ] Unit tests (optional)

---

## 📁 PROJECT STRUCTURE

```
Assets/
├── Scripts/
│   ├── Extraction/
│   │   ├── ExtractManager.cs
│   │   └── ExtractionZone.cs
│   ├── Gambling/
│   │   └── SlotMachine.cs
│   ├── General Stuff/
│   │   ├── Debugging.cs
│   │   └── TargetBehavior.cs
│   ├── Inventory/
│   │   ├── InventoryItem.cs
│   │   ├── InventoryItemUI.cs
│   │   ├── ItemData.cs
│   │   ├── ItemDatabase.cs
│   │   ├── ItemPickup.cs
│   │   ├── PlayerInventoryManager.cs
│   │   ├── ReforgeStationMenuController.cs
│   │   └── VendorSellManager.cs
│   ├── PlayerScripts/
│   │   ├── CameraMovement.cs
│   │   ├── ItemPickupBehavior.cs
│   │   ├── PlayerMovement.cs
│   │   └── PlayerStats/
│   │       ├── BaseStat.cs
│   │       ├── HealthStat.cs
│   │       └── StatsManager.cs
│   ├── UI/
│   │   ├── ExtractionUI.cs
│   │   ├── GameUIManager.cs
│   │   ├── LevelSelector.cs
│   │   └── TempMouse.cs
│   └── WeaponScripts/
│       ├── Inspect.cs
│       ├── Weapon.cs
│       ├── WeaponSoundHandler.cs
│       └── StateMachine/
│           ├── IWeaponState.cs
│           ├── WeaponStateBase.cs
│           ├── WeaponStateMachine.cs
│           ├── WeaponStateType.cs
│           └── States/
│               ├── WeaponDrawingState.cs
│               ├── WeaponFiringState.cs
│               ├── WeaponHolsteringState.cs
│               ├── WeaponIdleState.cs
│               ├── WeaponInspectingState.cs
│               ├── WeaponReloadingState.cs
│               └── WeaponSwitchingState.cs
├── GameInput.cs (Input System generated)
└── ImportedAssets/
    └── JMO Assets/WarFX/ (particle effects)
```

---

## 🎮 CURRENT CONTROLS

| Key | Action |
|-----|--------|
| WASD | Move |
| Mouse | Look |
| Space | Jump |
| Shift | Sprint |
| Ctrl | Crouch (toggle) |
| LMB | Fire |
| RMB | Aim (ADS) |
| R | Reload |
| Tab | Toggle Inventory |
| E | Interact / Pickup |
| H | Holster Weapon |
| Middle Mouse | Inspect Weapon (hold) |
| Escape | Close menus |
| **Debug (X to enable)** | |
| X | Toggle Debug Mode |
| G | Give 1000 Credits |
| K | Kill Player (Clear Inv) |
| J | Damage Player (10 HP) |

---

## 🐛 KNOWN ISSUES

1. **Holster/Heal conflict** - H key is used for both holster and debug heal when debug mode is on
2. **Slot machine visual** - Symbols are text-based, no 3D reel spinning
3. **Stats not saved** - Player stat upgrades don't persist between sessions
4. **No enemy system** - Game has no threats currently
5. **Extraction basic** - Just loads main menu, no rewards screen
6. **Audio placeholders** - Many sounds are missing or generic

---

## 📝 NOTES FOR FUTURE DEVELOPMENT

- Consider using Addrsesables for asset loading
- Player stats should be saved alongside inventory
- Wave system could use ScriptableObjects for configuration
- Skill tree could extend the existing BaseStat system
- Consider adding difficulty settings
- Mobile support would require significant input rework
