# Adrift

**Adrift** is a 3D open-world survival game developed in **Unity 6** (URP), in which a shipwrecked player drifts across an infinite ocean on a small raft. The goal is to survive: gather debris, fish, cook, purify water, expand and fortify the raft, and defend against a shark that hunts the player and attacks the raft.

> Made as a game project — an ocean survival experience with low-poly art and audio integrated via FMOD Studio.

## About the game

The player wakes up alone on an endless ocean, clinging to a small raft. From there, every decision matters:

- **Survive** — manage **health**, **hunger** and **thirst**. When hunger or thirst reach zero, health starts to drop. If health reaches zero, you die.
- **Gather** — everything you need comes from the sea. Debris (wood, plastic, metal and leaves) floats toward the raft over time.
- **Build** — expand the raft and build structures to keep yourself alive and protected.
- **Defend** — a shark roams around the raft. If you're in the water, it attacks you. From time to time, it charges the raft and damages the barricades.

There is no pre-defined end goal — it's an open-ended survival experience on an infinite ocean.

## Key features

### Survival
- **3 vital bars**: Health (100), Hunger (100, decays ~1/4s) and Thirst (100, decays ~1/3s).
- **Visual danger system**: when health, hunger or thirst are critical, a *vignette* effect (URP Volume) pulses in the corresponding color (red, yellow or green).
- **Death system**: on death, the player plays a death animation, sees a *game over* panel and can restart or return to the main menu.

### Movement & camera
- **Third-person** character control (CharacterController) with a mouse-following orbital camera.
- **Physical swimming**: when in contact with water, the player floats with *buoyancy* and swims (with animation and sound).
- **Leap out of the water**: near the raft, the player can jump back on board.
- The player **sticks to the raft**: while walking on it, they follow the raft's movement.

### Resource gathering
- **Hook**: charge (H or left-click) and throw it into the sea. The hook has a magnetic catch radius that grabs all nearby debris — press H again to reel everything into the inventory.
- **Collection net**: a placeable structure that automatically catches debris drifting past it.

### Fishing
- The **fishing rod** works like the hook, but catches fish (which swim around the raft).
- When a fish bites, a **mini-game** starts: press H when the indicator is in the green zone to reel it in. The zone shrinks after each success; miss 3 times and the fish escapes.
- Raw fish can be **cooked on the grill**.

### Crafting
- **Hand crafting**: 4 direct recipes in the inventory.
- **Crafting table**: a placeable structure that unlocks advanced recipes with ingredient details.

### Recipes

| Item | Ingredients |
|------|-------------|
| Raft (block) | 2 Wood + 1 Plastic |
| Barricade | 3 Wood + 2 Leaves |
| Net | 2 Wood + 2 Leaves |
| Knife | 1 Wood + 1 Plastic + 1 Metal |
| Water bottle | 2 Plastic |
| Crafting table | 2 Wood |
| Chest | 2 Wood + 1 Metal |
| Grill | 2 Wood + 2 Metal |
| Water purifier | 1 Wood + 1 Leaf + 1 Plastic |

### Raft building
- Press **L** to enter build mode.
- **Grid-based** placement with a ghost preview (green = valid, red = invalid).
- Two placement types: **expansion** (new raft blocks) and **on-top** (structures, rotatable in 90° steps).
- Pieces are parented to the raft root and float/drift together.

### Placeable structures
- **Barricade** — has HP and is damaged by shark attacks; with visual damage feedback and an optional health display.
- **Chest** — 15 storage slots.
- **Grill** — cooks raw fish (8 s, with smoke particles and a progress indicator).
- **Water purifier** — turns salt water into drinking water (empty bottle → sea water → purify → drink).
- **Net** — automatic collection.
- **Crafting table** — advanced crafting.

### Shark (enemy)
The shark is driven by a state machine with 4 behaviors:
1. **Roaming** — swims randomly at the surface or in deep water, avoiding crossing through the raft.
2. **Hunting** — if the player is in the water within 35 m, it chases and attacks (20 damage), with a 12 s hunt timeout.
3. **Attacking player** — damaging bite.
4. **Attacking raft** — with a 15% chance (60 s cooldown), it rams the raft and damages a random barricade.

The player can **fight back** with the knife (left-click): attacks make the shark flee back to *Roaming*.

### Water cycle (thirst loop)
Empty bottle → fill in the ocean (salt water) → purify → drink (restores 30 thirst) → back to empty bottle.

### Food cycle
Raw fish → grill → cooked fish (restores 40 hunger).

### Weather
- Two states: **sunny** and **rainy**, changing randomly (30% every 60 s).
- Smooth 4 s transitions for fog, ambient light, sun light, rain particles and splashes on the water, plus rain audio (FMOD).

## Tech stack

| Component | Technology |
|-----------|------------|
| Engine | Unity 6000.0.38f1 (Unity 6) |
| Rendering | Universal Render Pipeline (URP) |
| Audio | FMOD Studio (dedicated project in `fmod-project-adrift/`) |
| Input | Legacy Input System (`Input.GetKey`/`GetAxis`) |
| UI | Unity UI (uGUI) + TextMeshPro |
| Physics | CharacterController, Rigidbody, OverlapSphere, SphereCast |
| Water | LowPolyWater (procedural waves) |
| Effects | VFX particles (rain, splashes, cooking), URP Volume (vignette) |

## Scenes

| Scene | Description |
|-------|-------------|
| `Assets/Scenes/MainMenuScene.unity` | Main menu with a video background, intro story sequence (typewriter text + crossfading images) |
| `Assets/Scenes/Adrift.unity` | Main scene — ocean survival |
| `Assets/Scenes/CenaAGUA.unity` | Secondary test/debug scene |

## Controls

| Key | Action |
|-----|--------|
| `H` | Throw/reel in hook or fishing rod; trigger fishing mini-game |
| `E` | Open inventory; interact (chests, tables) |
| `L` | Build mode |
| `F` | Pick up items |
| `←` / `→` | Cycle through construction objects |
| `1`–`9` / mouse wheel | Select hotbar item |
| Right-click | Consume items, interact with structures |
| Left-click | Attack with knife / release hook |
| `ESC` | Pause menu |

## How to run

1. Open the project in **Unity 6** (6000.0.38f1) — the project uses URP and FMOD Studio.
2. Set up the **FMOD Unity Integration** if needed (the FMOD project is in `fmod-project-adrift/`).
3. Open the `Assets/Scenes/MainMenuScene.unity` scene.
4. Press **Play**.

## Project structure

```
Assets/
├── Animations/          # Player animation clips (idle, walk, throw, die, swim)
├── Character/           # Character 3D model and textures
├── Fonts/               # Typography
├── InputSystem/         # Input mappings
├── Items/               # Items (ScriptableObjects) — wood, food, tools, structures
├── Low Poly Water/      # Low-poly water asset
├── Main Menu/           # Main menu content and intro story
├── Materials/           # Materials
├── Plugins/             # Plugins (FMOD, etc.)
├── Prefabs/             # Game prefabs
├── Recipes/             # Crafting recipes (ScriptableObjects)
├── Scenes/              # Game scenes
├── Scripts/             # C# code organized by feature
├── Settings/            # Project settings
├── Shark/               # Shark prefabs and AI
└── Sprites/             # UI sprites
```

The code is organized by feature inside `Assets/Scripts/` (Camera, Debris, Fish, FMOD, Hook, Inventory, Movement, Player, UI, Weather), and the design is data-driven: items and recipes are all **ScriptableObjects**, editable in the Inspector.

## Contributing / planned extensions

The game is at an open demo stage. Natural improvement ideas:

- **Save/load** system (no persistence currently).
- **More recipes** for items and structures.
- **More species** of wildlife/threats beyond the shark.
- A more defined win or lose condition.

---

**Adrift** — made with Unity 6, URP and FMOD Studio.