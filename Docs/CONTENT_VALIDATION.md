# Content validation and hot reload

The game's data is JSON in `Assets/TAP/Resources`:
- `Data/galaxy.json` lists the star systems;
- `Data/system.json` and `Data/system_debug.json` describe the bodies, with terrain presets in `Data/Terrain`
  ([TERRAIN_FORMAT.md](TERRAIN_FORMAT.md));
- `Data/parts.json` holds the resources and parts;
- `Craft/*.json` are the starter craft.

The game checks this data when it loads it, and a problem is reported as a sentence you can act on, instead of
showing up later as odd physics or an exception in flight.

## How a problem reads

```
Data/parts.json line 175: part eng_hornet, engine.ispVac: expected a number above 0 (seconds), found nothing.
```

Each problem names:
- the **file** and **line** (where the field is missing, the line of the object it is missing from);
- the **entry**, such as a part, a resource, a body, the launch site or a terrain preset;
- the **field**, as a path you can find in the file (`engine.ispVac`, `terrain.layers[3].craters.sizes[0].cell`);
- what was **expected** and what was **found**, sometimes with a hint (`did you mean thrustVac?`).

A value that a body takes from a terrain preset is reported in the preset's file, at its line.

**Errors** make an entry unusable. **Warnings** don't stop anything but are worth a look:
- a field the format doesn't have (it is ignored, so a typo silently loses its value);
- a field written with other capitals;
- a part in no tab of the parts list;
- an engine more efficient at sea level than in vacuum.

## What is checked

The C# classes the data is read into are its schema. Every field must be one they have, and every value must be of
the right kind: a number, text, a list, an object, or one of an enum's names. On top of that, each kind of content
has its rules:

- **Parts:**
  - ids are unique and resources exist;
  - masses, sizes, temperatures and strengths are above 0;
  - vectors have 3 numbers, and directions aren't all 0;
  - attach nodes have unique ids;
  - engines have a type, thrust, both Isps and a propellant that exists;
  - a decoupler's explosive node and a docking port's node are nodes of the part;
  - RCS nozzles have 6 numbers each;
  - parachutes, legs, heat shields, wheels and fins have sensible values.
- **Resources:** unique ids, a density of 0 or more, and a colour of 3 numbers from 0 to 1.
- **Systems:**
  - body ids are unique and there is one root;
  - parents exist, with no loops, and every body with a parent has an orbit;
  - radius, GM, orbits (eccentricity below 1), atmospheres (temperature altitudes rising) and colours are valid;
  - a star has its light;
  - every terrain builds: noise octaves, layer sources, flat areas and biome ids;
  - the launch site stands on a flat area of a body with a surface.
- **Galaxy:** system ids are unique, the system files exist, and the home system is one of them.
- **Starter craft:** parts exist, the parents form a tree, attach nodes are the parts' own, and rotations are unit
  quaternions.

The Celestial Body Lab checks its drafts with the same rules.

## When there are problems

- **Loading:** every problem is logged once, one console message per file.
- **Main menu:** a notice under the buttons opens the list. It opens by itself when there are errors.
- **A part or resource with errors** is left out, and the rest load:
  - a craft that uses it can't be opened in the assembly building;
  - a saved vessel that uses it isn't flown, and stays in the save as it is;
  - a launch with it is refused.

  Each of these says which part has which error. The vessel or craft comes back once the data is fixed.
- **A system file with errors** isn't loaded. The menu lists every error and the game buttons are off until the data
  is fixed.

## Checking everything

- In the editor, *TAP → Validate Content* checks every file the game ships:
  - the galaxy;
  - each system with its terrain presets;
  - every preset, including ones no body uses yet;
  - the parts;
  - the starter craft.

  It lists the problems in a window and the console.
- The EditMode test `ContentTests.AllContent_LoadsWithoutProblems` fails on any problem, warnings included, so data
  with problems can't be merged unnoticed.

## Hot reload (F8)

While playing in the editor, press **F8** after editing a data file. The game reads the files again, checks them, and
says what changed: on the screen in flight and in the assembly building, and in full in the console.

| Change | Takes effect |
|---|---|
| Part stats: thrust, Isp, masses, drag, temperatures, strengths, parachute, leg, wheel, RCS and fin values | Now, also on parts already flying: an engine uses a new thrust from its next step |
| Resource density and flow | Now |
| A body's atmosphere, name, description, map colour and warp limits; a star's light | Now |
| Part models, attach nodes, what a part carries, crew seats, RCS nozzles, leg length and splay, a module added or removed | Vessels built from now on |
| New parts | In the parts list when the assembly building opens again |
| A body's radius, mass, orbit, spin and terrain; the launch site; bodies added or removed | After a restart |

If the new data has errors, those entries keep their previous values, and the message says so.

Hot reload works in the editor only, where the files can change. A player build reads its data once.

## Adding a new kind of content

Tech, experiments, contracts and strategies will follow the same pattern:
1. Write the data classes. They are the schema.
2. Parse with `ContentJson.Parse` (it gives line numbers) and check the shape with `ContentJson.CheckShape`.
3. Write the rules with a `ContentEntry` per entry: `Positive`, `InRange`, `Vector`, `OneOf`, `Check`. It fills in
   the file, line, field and what was found.
4. Log the report with `ContentLog.Add`, and add the file to `ContentCheck.All`.
