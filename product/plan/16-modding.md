# 16 · Modding (MOD)

**Have:**
- Parts, bodies and craft are JSON data.
- Part models can be replaced by an FBX placed inside the Unity project (not in a player build).

**KSP 1.0** was modded heavily from day one: config files for parts and planets, and C# plugins loaded at start.
Much of its longevity came from mods.

TAP should support the same two levels:
- **Content packs**: data plus models, without code.
- **Plugins**: C# assemblies.

This needs the Mono scripting backend, not IL2CPP (D-11).

---

### MOD-01 · Mods folder and content packs
**Milestone** EA5 · **Claude** 4 d · **You** 1 d · **Needs** FND-04, ART-02

- A `Mods/` folder in the user folder. Each mod has a manifest: id, version, game version range, dependencies.
- Content packs can add or patch:
  - parts;
  - research nodes;
  - contracts;
  - strategies;
  - experiments and texts;
  - craft;
  - flags;
  - later, whole star systems (FND-15).
  Patches say which entries they change (by id and field).
- **Runtime models**: Unity can't import FBX in a player, so mods use glTF, loaded with glTFast, and textures as PNG.
  The built-in FBX pipeline stays for the game's own parts (D-08).
- Load order, conflict reports, and validation with readable errors (FND-04).

**Done when**
- [ ] An example pack adds a new engine (glTF model, stats, research node) and patches a tank's capacity. It works in
      the Linux and Windows builds.

### MOD-02 · Plugin API
**Milestone** EA5 · **Claude** 5 d · **You** 1 d · **Needs** MOD-01, FND-14

- DLLs are loaded from mod folders at start, after a warning that plugins run code.
- A public, documented API:
  - part modules (the `PartModule` lifecycle);
  - game events (FND-14);
  - vessel, orbit and resource queries;
  - UI hooks (a window);
  - persistence per module.
- The API is versioned. Internal classes are marked, and breaking changes follow a policy.

**Done when**
- [ ] An example plugin adds a part module (a "hover altimeter" readout) and a window.
- [ ] It survives saving and reloading.

### MOD-03 · Modding documentation and examples
**Milestone** EA5 · **Claude** 2 d · **You** 1 d · **Needs** MOD-01, MOD-02

- A `Docs/MODDING.md` guide covering the data schemas, the glTF model rules (like PART_MODELS), the API reference
  (generated) and a template project.
- The example pack and plugin are published in the repo.

**Done when**
- [ ] Following the guide from scratch, you build the example mod in under an hour.

### MOD-04 · Mod list in the game
**Milestone** EA5 · **Claude** 1 d · **You** 0.5 d · **Needs** MOD-01

- The main menu lists mods (enabled/disabled, version, errors). Saves remember the mods they used and warn when one
  is missing.

**Done when**
- [ ] Disabling a mod that a save uses shows a clear warning, and loading still works with its parts removed.
