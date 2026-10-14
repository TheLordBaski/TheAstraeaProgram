# Flight audio alternatives

Ten original 32-bar scores provide **ten alternatives for each flight phase: sixty arrangements total**.
Each score lasts 1:46.667 at 72 BPM and has a 2:40 preview demonstrating launch, high atmosphere, orbit,
transit, reentry and landing. The scores have different harmony, melodies, phrasing and instrumental roles.

| Score | Direction | Listen |
| --- | --- | --- |
| First Light | Optimistic brass and strings, celeste and a steady pulse | [Preview](FirstLight/FirstLight_Audition.wav) |
| Far Horizon | Brighter chamber orchestra, an oboe lead and broken pizzicato phrases | [Preview](FarHorizon/FarHorizon_Audition.wav) |
| Night Drift | Minor harmony, lower viola texture, sparse bells and a slower pulse | [Preview](NightDrift/NightDrift_Audition.wav) |
| Starward | A rising flute theme, open major chords and light, skipping strings. | [Preview](Starward/Starward_Audition.wav) |
| Blue Silence | Suspended minor harmony, low viola breaths and isolated harp notes. | [Preview](BlueSilence/BlueSilence_Audition.wav) |
| Gravity Well | Descending brass answers, dark minor colour and a restrained uneven pulse. | [Preview](GravityWell/GravityWell_Audition.wav) |
| Dawn Patrol | Dotted horn calls, a walking low-string line and a firm launch rhythm. | [Preview](DawnPatrol/DawnPatrol_Audition.wav) |
| Amber Skies | Warm F-major strings, harp responses and a gently swaying accompaniment. | [Preview](AmberSkies/AmberSkies_Audition.wav) |
| Clockwork Voyage | Interlocking pizzicato in groups of three, clarinet fragments and a quiet mechanical pulse. | [Preview](ClockworkVoyage/ClockworkVoyage_Audition.wav) |
| Homecoming | Broad resolving strings and horn phrases, with long rests and soft celeste. | [Preview](Homecoming/Homecoming_Audition.wav) |

The flight controller advances through all ten pieces during long flights, keeping the current phase's layer
mix. It starts at a varying position in the list and does not repeat a piece until it has visited the rest.
At a piece boundary, one bar fades out and the next piece's first bar fades in; different keys do not overlap.
Within a piece, flight-phase changes still begin on bar boundaries and blend over two bars, keeping every
stem synchronized. Music keeps real-time tempo through pause and time warp.

The seven parameter names remain the same. `Horns` is the lead role, performed by horns, oboes, flute or clarinet,
and `Celeste` is played by celeste or harp. Ten scores contain about 17:47 of distinct musical timelines;
the sixty tracks are their six phase arrangements, not sixty unrelated compositions.

Open each score's `*_Stems.rpp` in REAPER for its seven rendered stems. Editable MIDI and plugin state snapshots
are alongside them. The new REAPER projects include the measured master gain. `Music/` contains the six phase
arrangements; the game streams the stems instead. These remain draft mixes awaiting gameplay listening.

## Ten alternatives per phase

| Score | Launch | High atmosphere | Orbit | Transit | Touchdown | Reentry |
| --- | --- | --- | --- | --- | --- | --- |
| First Light | [Listen](FirstLight/Music/01_Launch.wav) | [Listen](FirstLight/Music/02_HighAtmosphere.wav) | [Listen](FirstLight/Music/03_Orbit.wav) | [Listen](FirstLight/Music/04_Transit.wav) | [Listen](FirstLight/Music/05_Touchdown.wav) | [Listen](FirstLight/Music/06_Reentry.wav) |
| Far Horizon | [Listen](FarHorizon/Music/01_Launch.wav) | [Listen](FarHorizon/Music/02_HighAtmosphere.wav) | [Listen](FarHorizon/Music/03_Orbit.wav) | [Listen](FarHorizon/Music/04_Transit.wav) | [Listen](FarHorizon/Music/05_Touchdown.wav) | [Listen](FarHorizon/Music/06_Reentry.wav) |
| Night Drift | [Listen](NightDrift/Music/01_Launch.wav) | [Listen](NightDrift/Music/02_HighAtmosphere.wav) | [Listen](NightDrift/Music/03_Orbit.wav) | [Listen](NightDrift/Music/04_Transit.wav) | [Listen](NightDrift/Music/05_Touchdown.wav) | [Listen](NightDrift/Music/06_Reentry.wav) |
| Starward | [Listen](Starward/Music/01_Launch.wav) | [Listen](Starward/Music/02_HighAtmosphere.wav) | [Listen](Starward/Music/03_Orbit.wav) | [Listen](Starward/Music/04_Transit.wav) | [Listen](Starward/Music/05_Touchdown.wav) | [Listen](Starward/Music/06_Reentry.wav) |
| Blue Silence | [Listen](BlueSilence/Music/01_Launch.wav) | [Listen](BlueSilence/Music/02_HighAtmosphere.wav) | [Listen](BlueSilence/Music/03_Orbit.wav) | [Listen](BlueSilence/Music/04_Transit.wav) | [Listen](BlueSilence/Music/05_Touchdown.wav) | [Listen](BlueSilence/Music/06_Reentry.wav) |
| Gravity Well | [Listen](GravityWell/Music/01_Launch.wav) | [Listen](GravityWell/Music/02_HighAtmosphere.wav) | [Listen](GravityWell/Music/03_Orbit.wav) | [Listen](GravityWell/Music/04_Transit.wav) | [Listen](GravityWell/Music/05_Touchdown.wav) | [Listen](GravityWell/Music/06_Reentry.wav) |
| Dawn Patrol | [Listen](DawnPatrol/Music/01_Launch.wav) | [Listen](DawnPatrol/Music/02_HighAtmosphere.wav) | [Listen](DawnPatrol/Music/03_Orbit.wav) | [Listen](DawnPatrol/Music/04_Transit.wav) | [Listen](DawnPatrol/Music/05_Touchdown.wav) | [Listen](DawnPatrol/Music/06_Reentry.wav) |
| Amber Skies | [Listen](AmberSkies/Music/01_Launch.wav) | [Listen](AmberSkies/Music/02_HighAtmosphere.wav) | [Listen](AmberSkies/Music/03_Orbit.wav) | [Listen](AmberSkies/Music/04_Transit.wav) | [Listen](AmberSkies/Music/05_Touchdown.wav) | [Listen](AmberSkies/Music/06_Reentry.wav) |
| Clockwork Voyage | [Listen](ClockworkVoyage/Music/01_Launch.wav) | [Listen](ClockworkVoyage/Music/02_HighAtmosphere.wav) | [Listen](ClockworkVoyage/Music/03_Orbit.wav) | [Listen](ClockworkVoyage/Music/04_Transit.wav) | [Listen](ClockworkVoyage/Music/05_Touchdown.wav) | [Listen](ClockworkVoyage/Music/06_Reentry.wav) |
| Homecoming | [Listen](Homecoming/Music/01_Launch.wav) | [Listen](Homecoming/Music/02_HighAtmosphere.wav) | [Listen](Homecoming/Music/03_Orbit.wav) | [Listen](Homecoming/Music/04_Transit.wav) | [Listen](Homecoming/Music/05_Touchdown.wav) | [Listen](Homecoming/Music/06_Reentry.wav) |

The [verified catalogue](music-catalogue.json) lists all sixty tracks and their FMOD event paths.

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
UI sounds, cockpit ambience, more engine families and longer, less regular musical forms remain future work.

## FMOD and reproduction

All ten scores and 27 vessel events live in [FirstLight.fspro](FMOD/FirstLight.fspro), with the source WAVs
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
# Render one of the seven additional scores (the other names are in flight_scores.py):
python tools/audio/render_flight_audio.py music --suite Starward
python tools/audio/verify_music_catalogue.py
```

The raw score stems are intentionally quiet. Every alternative score's named FMOD working WAVs include
its `music.json` master_gain; the first score applies its gain in the FMOD event instead. The renderer writes
calibrated working copies for all nine alternatives automatically. SFX working copies are direct copies.
Rebuild the Desktop banks after replacing sources. For a part edit, `music --suite AmberSkies --layer Horns`
re-renders only that layer, then recalculates the phase mixes, preview, gain and calibrated working copies.
The oboe leads stay within the installed instrument's sampled range; its notes below MIDI 58 are silent.
The seven additional compositions are defined in `tools/audio/flight_scores.py`; their renderer also writes
calibrated FMOD working copies. After all sources exist, `tools/audio/extend_fmod.js` adds missing events and
builds banks. It preserves existing authored events. Normal mixing changes only need a Desktop bank build.

All ten music packs passed format, finite-signal, headroom, matching-length and boundary-continuity checks.
The foley renderer checks its mono files and loop endpoint. Seventeen focused Editor tests pass, including native
FMOD bank loading, all ten looping scores with their seven shared controls, the 15 expansion foley event types,
and the musical edge fades. Catalogue verification checks sixty distinct phase mixes, ten distinct MIDI scores,
seventy calibrated FMOD stem copies, and reconstruction of each phase mix from its source stems.
These checks establish technical validity; they do not replace gameplay listening or validate the final mix.
Evidence is in `Logs/TenScoresAudioTests.json` and each pack's render/verification reports.

## Sources

The music is newly composed; no KSP recordings or melodies were sampled. Instrument licensing and FMOD
distribution requirements still apply as described in the original pack. Expansion foley sources are listed
in [sources.json](FlightExpansion/sources.json), with the matching Sonniss archive licences in
`FlightExpansion/Licenses`. They are game assets, not a standalone commercial sound library.
