# Editing part models (FBX / Blender)

Every part is generated in code from `Resources/Data/parts.json` when the game starts. You can replace the look of any
part with a model of your own: export the generated one, edit it in Blender (or any tool that reads and writes FBX), and
save it under the part's id. The game then uses your model everywhere the part appears (assembly building, part icons,
flight). Physics does not depend on the art: the collider, mass, attach nodes and anchors (nozzle, hatch, parachute)
still come from the part data, so a new model cannot make a rocket fly differently.

## 1. Export the generated models

In the Unity editor: **TAP → Part Models → Export All (FBX + OBJ)**. It writes to `Art/PartModels/Export/`:

| File | Use |
|---|---|
| `<part id>.fbx` | The model to edit: named hierarchy with pivots, material names |
| `obj/<part id>.obj` + `obj/part_materials.mtl` | The same model as one OBJ per part, for tools that don't read FBX (no hierarchy, so no animated pieces) |
| `PARTS.md` | Reference sheet: each part's size, attach nodes, animated transforms, anchors and materials |

The export folder is recreated on every export and is not in git. Keep your own work elsewhere, e.g. `.blend` files in
`Art/PartModels/Source/`.

## 2. Edit in Blender

Tested with Blender 5.2: import, edit, export with the default FBX settings, then load in the game (see *Verified* below).

1. **File → Import → FBX** with the default settings. Leave *Apply Transform* off: it looks tidier in Blender, but the
   pieces under `Model` come back into the game at 1/100 size and rotated 90° (tested).
2. What you get: an empty named after the part (scale 0.01, because the file is in centimetres; the viewport and
   measurements are in metres), its child **`Model`** (the body), animated children such as **`Bell`**, and anchor empties
   such as `Nozzle`. Blender's +Z is the part's up axis (the game's +Y). The part origin is at 0,0,0.
3. Model freely, keeping to these rules:
   - **Size and attach points.** Keep the overall size close to the original. Stack parts connect at the attach nodes
     listed in `PARTS.md` (for example `top` at 0.8 m, `bottom` at −0.8 m), and those do not move with your model.
   - **Animated pieces.** Keep their names and origins (pivots) exactly:
     - `Bell` (engines): gimbals about its origin.
     - `LegPivot` (landing legs): rotates about its local X axis, from −172° (stowed) to minus the splay angle
       (deployed). In the exported rest pose the strut points straight down.
     - `LegPivot/Piston`: slides along its local Y axis as the suspension compresses.
     - `Foot`: optional.
   - **New pieces** go under `Model`. Select the piece, then `Model`, and press **Ctrl+P → Object**; the piece stays
     where you placed it.
   - **Materials.** Name a material after one of the game's shared materials and it will look and light like the rest
     of the craft. The names are `Part_White`, `Part_Dark`, `Part_Orange`, `Part_Metal`, `Part_MetalDark`, `Part_Gold`,
     `Part_Glass`, `Part_Shield`, `Part_Yellow`, `Part_Red`, `Part_Black`, `Part_Suit`, `Part_Visor`, `Part_Canopy`,
     `Part_Concrete`, `Part_Blue` and `Part_Grey`. Blender's copies such as `Part_Metal.001` count too. Any other
     material is imported as a URP Lit material, so textured materials work as well.
   - **Anchors** (`Nozzle`, `CanopyAnchor`, `Hatch`, `RcsNozzle0…`) are placed from `parts.json`, so copies in your file
     are ignored. To move an anchor or an attach node, edit the part in `parts.json`.
4. **File → Export → FBX** with the default settings (Forward −Z, Up Y, Apply Unit). Save it as
   **`Assets/TAP/Resources/PartModels/<part id>.fbx`**, for example `eng_hornet.fbx` for the Hornet LV-215 engine.
   The part ids are in `PARTS.md`.

## 3. See it in the game

Switch back to Unity. Asset auto-refresh is off in this project, so press **Ctrl+R** (Assets → Refresh) to import the
file, then press Play. The first import sets the model to import meshes and materials only (no colliders, cameras,
lights or animation). Standalone builds pick it up at the next build (**TAP → Build Windows Player**). To go back to the
generated model, delete the FBX. **TAP → Part Models → Open Hand-made Models Folder** opens the folder.

## Verified

The Hornet LV-215 engine went through the whole round trip:

1. Exported from the game.
2. Imported into Blender 5.2 (run headless), where the nozzle bell was widened by 30% and a gold ring (`Part_Gold`) was
   added, placed in world space and parented to `Model`.
3. Exported with Blender's default settings to `Resources/PartModels/eng_hornet.fbx`.

Back in the game:

- The unchanged body is identical to the generated one (1.25 × 0.45 × 1.25 m, same position).
- The bell is 1.30 m wide, with its gimbal pivot still at (0, 0.35, 0).
- The ring sits 0.5 m up the axis, 1.1 m across.
- All materials resolve to the shared ones, and there is exactly one nozzle anchor, the one placed from the part data.

Side by side, generated and edited: `Screenshots/part_model_roundtrip.png`. The test model was removed afterwards, so
the Hornet keeps its original look.

## OBJ and STL

The OBJ files are there for tools that don't read FBX. They carry no hierarchy or pivots, so they only suit parts with
no animated pieces: import the OBJ into Blender and export an FBX as above. Blender can also export STL (for example for
3D printing), but STL has no materials or hierarchy and the game doesn't read it.
