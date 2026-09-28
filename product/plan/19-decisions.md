# 19 · Decisions

Decisions that shape the plan. **Decided** ones came from you; **open** ones have a recommendation and the task that
needs them. Tell me your choice for any open one, or say "take the recommendation", and I'll update the plan.

## Decided

| # | Decision | Your answer | Consequence in the plan |
|---|---|---|---|
| D-15 | Aircraft and spaceplanes | **No hangar and runway for now; TAP is a space game. Some aero parts are needed.** | No wings, jets, intakes, landing gear or runway. Rocket aero parts are in: nose cones, fairings, fins, control fins and canards, airbrakes, heat shields, grid fins (optional). The hub keeps room for a runway and hangar later (HUB-01). |
| D-19 | Platforms | **PC: Windows and Linux first** | Linux builds and CI in M1 (FND-11); Linux in every release checklist; macOS and consoles after 1.0 ([21-out-of-scope.md](21-out-of-scope.md)). |
| D-20 | Team | **Solo + Claude** | Two lanes: Claude builds and tests, you decide, review, playtest and make art. Estimates are split that way; the schedule is in the README. |
| D-21 | Release model | **Early Access first** | Milestones EA1–EA5 are public updates; R1 is 1.0. |
| D-17 | Scope beyond one system | **Multiple planetary systems and trade routes (details later)** | FND-15 makes everything multi-system from the start. Expansions X1 and X2 are in [20-multi-system-and-trade.md](20-multi-system-and-trade.md), with the design questions to answer there. |

## Open

| # | Question | Options | Recommendation | Needed by |
|---|---|---|---|---|
| D-01 | Propellant model | **A**: one chemical Propellant plus Hydrogen (nuclear) and Xenon (ion). **B**: fuel + oxidiser like most real rockets and KSP. **C**: keep one "LiquidFuel" for everything | **A**. Already how the slice works; simple for players; nuclear and ion still feel different. | FLT-01, PRT-01 (M1) |
| D-02 | Names of the star and the new worlds | Working names in [02-solar-system.md](02-solar-system.md) (Astraea, Ignis, Caligo, Mica, Pruina, Rubra, Vigil, Rupes, Magnus, Maris, Gelida, Gravis, Lapis, Granum, Remota) or your own | Keep the Latin-flavoured set: it matches Tellus and Luma, and naming the star Astraea ties it to the program's name. | SYS-01 (M1) |
| D-03 | Scale | Keep the slice's small scale (home radius 600 km, KSP-like) or go larger | Keep it. Missions stay short; every system is tuned for it. | Decided implicitly unless you object |
| D-04 | Interior (IVA) views | **A**: portraits only. **B**: portraits plus interiors for the 3 main pods. **C**: interiors for every crewed part | **B** in EA5, and more later only if players ask for it. | CRW-11 (EA5) |
| D-05 | Character art | Model it yourself, commission a character artist, or adapt a licensed base model | Commission the model and rig, and license motion-capture animations. The rocketeers carry the game's charm, and they are the hardest art to get right alone. | ART-05 (EA5; start sourcing in EA3) |
| D-06 | Sound and music | Library sounds processed by you; music commissioned vs licensed | Sound effects from royalty-free libraries (processed to TAP's taste). Commission about 10 music tracks for 1.0, and license 4–5 placeholders for EA1. | AUD-02, AUD-06 (EA1) |
| D-07 | Destructible facilities | Include (like KSP) or skip | Include late (EA5) with an "indestructible" difficulty option. It's charming but not core. | HUB-05 |
| D-08 | Model format for mods | glTF at runtime (glTFast) vs something else | glTF. Unity can't import FBX at runtime; glTF is the open standard and Blender exports it. | MOD-01 (EA5) |
| D-09 | Early Access price | For example Early Access €14.99–19.99 rising to €24.99–29.99 at 1.0 | Decide after the first playtests (REL-01), from comparable titles. | REL-01, REL-03 |
| D-10 | Telemetry and crash reports | None; opt-in anonymous events and crash upload; opt-out | Opt-in, anonymous, self-hosted or a privacy-friendly service, with a privacy policy (REL-02). | QA-04, REL-02 (EA1) |
| D-11 | Scripting backend | Mono (current) vs IL2CPP | Mono. Plugin mods need it (MOD-02), and it builds faster on Windows and Linux. Revisit if performance needs IL2CPP (it would end code mods). | FND-11 (M1) |
| D-12 | Languages | English only through 1.0, the system ready for more; or ship more languages at 1.0 | English through 1.0 with FND-10 in place; translations after 1.0, maybe community ones. | FND-10 |
| D-13 | Save compatibility during Early Access | Promise saves survive updates; or allow breaking changes | Promise it, with migrations (FND-05) and the save corpus (QA-03). Early adopters' 20-hour careers are precious. | FND-05 |
| D-14 | Physics model | Keep one rigid body per vessel (no wobble); or joint-based physics like KSP | Keep it. It's stable, fast and already verified; struts reinforce instead of stiffen (FLT-16). It's also what makes 300-part rockets feasible. | FLT-16 |
| D-16 | Clouds | None (as KSP 1.0); simple cloud layers on Tellus, Caligo and Rubra dust | A simple cloud layer on Tellus, and haze on Caligo, in EA5 if there is time. They add a lot of beauty, but they're not needed for play. | ART-08 |
| D-18 | Steam Deck | No target; "Playable" with keyboard/mouse layouts; "Verified" with full gamepad UI | "Playable" first. Full gamepad UI after 1.0 unless demand is strong. | REL-07 |
| D-22 | Comms network with relays and signal delay (KSP added it after 1.0) | Not planned; after 1.0; before 1.0 | After 1.0, together with the multi-system work (relays make sense across systems). | [21-out-of-scope.md](21-out-of-scope.md) |
| D-23 | The name after the trademark search ([TRADEMARK_SEARCH.md](../TRADEMARK_SEARCH.md)) | Keep "The Astraea Program" and file trade marks; or rename before any marketing | Keep it: no conflict for the full name in the US or EU registers or on Steam. File an EU trade mark (classes 9 and 41, about €900) in M1, after a short lawyer check, and extend it to the US within 6 months. "TAP" stays an abbreviation, never the brand. | REL-02 (M1), before REL-04 |
