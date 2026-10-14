# Flight audio alternatives

Three original 32-bar scores now share the same flight-phase controls. Each lasts 1:46.667 at 72 BPM;
each has six looping arrangements and a 2:40 preview demonstrating launch, high atmosphere, orbit,
transit, reentry and landing. These are new compositions, with different harmony, melodies and rhythm.

| Score | Direction | Listen |
| --- | --- | --- |
| First Light | Optimistic brass and strings, celeste and a steady pulse | [Preview](FirstLight/FirstLight_Audition.wav) |
| Far Horizon | Brighter chamber orchestra, an oboe lead and broken pizzicato phrases | [Preview](FarHorizon/FarHorizon_Audition.wav) |
| Night Drift | Minor harmony, lower viola texture, sparse bells and a slower pulse | [Preview](NightDrift/NightDrift_Audition.wav) |

The flight controller cycles through the three scores on successive flight scenes within a running session.
Its initial choice varies with application start time. It keeps one score and a shared musical clock through
that flight; atmospheric changes blend its layers on bar boundaries. There is no mid-phrase track shuffle.
The seven parameter names stay the same; in Far Horizon the `Horns` musical role is performed by oboes.

Open each score's `*_Stems.rpp` in REAPER for its seven rendered stems. Editable MIDI and plugin state snapshots
are alongside them. `Music/` contains the six finished phase arrangements; the game streams the stems instead.
These remain draft mixes awaiting a listening pass in the game.

## Added sound effects

[Listen to the expansion reel](FlightExpansion/SFX_Expansion_Audition.wav). It plays the files in alphabetical
order, separated by half a second. The 15 mono effects are in `FlightExpansion/SFX`:

| Files / FMOD event names under `event:/Vessel/` | Trigger |
| --- | --- |
| `Dock_Latch_01`, `Dock_Latch_02` | Successful active-vessel docking; two different latch recordings |
| `Undock_Release_01`, `Undock_Release_02` | A previously docked port enters release cooldown |
| `Leg_Servo_01`, `Leg_Servo_02` | A working landing leg deploys or retracts; grouped deployments share one sound |
| `Hatch_Open`, `Hatch_Close` | EVA exit and boarding events |
| `Step_Surface_01` through `03` | Moving, grounded EVA crew on terrain |
| `Step_Metal_01` through `03` | Moving, grounded EVA crew on vessel parts |
| `Jetpack_Loop` | EVA translation input, with jetpack enabled and propellant available |

EVA steps use three alternating transients and walking/running cadence, gated by actual surface speed.
The terrain step is a general rocky contact, not a biome-specific footstep set. The jetpack is a 2D suit-style
sound; the finite foley events are spatial. Effects follow the existing volume sliders, pause and rails-warp mute.
Changing vessels initializes mechanism states silently, avoiding false deployment sounds on loading a craft.
UI sounds, cockpit ambience, more engine families and a larger long-form score library remain future work.

## FMOD and reproduction

All three scores and 27 vessel events live in [FirstLight.fspro](FMOD/FirstLight.fspro), with the source WAVs
under `FMOD/Assets`. The existing three Desktop banks remain the game's only bank set. Normal edits need only
a Desktop build in Studio; Unity imports them to `Assets/StreamingAssets/FMOD`.

The Python renderer uses the same environment and installed instruments described in
[the original pack](FirstLight/README.md). From the repository root:

```powershell
python tools/audio/render_flight_audio.py music --suite FarHorizon
python tools/audio/render_flight_audio.py music --suite NightDrift
python tools/audio/render_flight_audio.py verify --suite FarHorizon
python tools/audio/render_flight_audio.py verify --suite NightDrift
python tools/audio/render_expansion_sfx.py
```

The raw score stems are intentionally quiet. FMOD's `FarHorizon_*` and `NightDrift_*` working WAVs include
the corresponding `music.json` master_gain; the first score applies its gain in its FMOD event instead.
After re-rendering a score, multiply each source stem by that gain when replacing its named FMOD working copy.
SFX working copies are direct copies. Rebuild the Desktop banks after replacing sources.
`tools/audio/extend_fmod.js` is a one-time authoring script and refuses to overwrite the existing alternatives.

The two music packs passed format, finite-signal, headroom, matching-length and boundary-continuity checks.
The foley renderer checks its mono files and loop endpoint. Sixteen focused Editor tests pass, including native
FMOD bank loading, all three looping scores with their seven shared controls, and the 15 new foley event types.
These checks establish technical validity; they do not replace gameplay listening or validate the final mix.
Evidence is in `Logs/FlightAudioExpansionTests.json` and each pack's render/verification reports.

## Sources

The music is newly composed; no KSP recordings or melodies were sampled. Instrument licensing and FMOD
distribution requirements still apply as described in the original pack. Expansion foley sources are listed
in [sources.json](FlightExpansion/sources.json), with the matching Sonniss archive licences in
`FlightExpansion/Licenses`. They are game assets, not a standalone commercial sound library.
