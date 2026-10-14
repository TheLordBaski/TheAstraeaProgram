# First Light â€” flight audio audition

Original musical composition for The Astraea Program; a first direction to audition, not a final mastered soundtrack.
Rendered with the installed **Vital**, **Valhalla Supermassive** and **BBC Symphony Orchestra Discover** instruments.
Effects are processed and layered recordings from the owner's local Sonniss GDC collections; source credits are in `sources.json`.

See [the expanded audio collection](../README.md) for all ten scores and the added flight foley.

## Listen

Start with **FirstLight_Audition.wav**: a 2:40 continuous musical flight, with two-bar blends between phases.
It follows launch â†’ high atmosphere â†’ orbit â†’ transit â†’ re-entry â†’ touchdown. New phases begin at 0:00, 0:26.667,
0:53.333, 1:20, 1:46.667 and 2:13.333. This preview demonstrates music transitions; it is not a gameplay recording.

`Music/` contains six 1:46.667 looping arrangements. `Stems/` contains seven synchronized layers at 72 BPM, 4/4,
32 bars, 48 kHz stereo, 24-bit PCM. All start at the same musical position and use the same harmonic progression.
`FirstLight_Stems.rpp` opens the rendered stems in REAPER, initially mixed for launch. `MIDI/` holds the editable
instrument parts. `Presets/` holds Pedalboard VST3 state snapshots for reproducible rendering; these are not REAPER
or FMOD presets. The rendered stems need no plugins at playback time.

**SFX_Audition.wav** plays each effect in filename order, with a one-second gap and up to four seconds per file.

| Phase | Direction |
| --- | --- |
| Launch | Optimistic horns, strings, moving pizzicato and a soft pulse |
| High atmosphere | Strings and pad open out as brass and rhythm recede |
| Orbit | Sparse celeste, wide pad, quiet strings; no percussion |
| Transit | Mostly pad, fewer prominent melodic elements, lower level |
| Touchdown | Warm string-led variation; also suitable for landing approach |
| Re-entry | Lower strings, restrained pulse and reduced sparkling detail |

These are arrangements of one shared suite. They are not six separately composed songs. A future music pass should
add alternate harmony, less regular phrases, rests and additional long-form material after this direction is heard.

## Sound effects

`SFX/` contains mono 48 kHz / 24-bit files:

- Four 12-second loops: low/high engine layers, atmospheric wind, re-entry plasma.
- Ignition, shutdown, RCS burst, three staging variants, touchdown, explosion and parachute deployment.

For a thruster held open, use a sustained pressure-noise event; the RCS burst supplied here is a one-shot.
The engine layers are a designed combination of machinery, a jet recording, rumble and synthesis, not a field
recording of a real rocket. The staging variants use the same underlying recording and need a listening pass.

## In-game integration

**FMOD Studio 2.03.15 and its matching Unity integration are installed and configured.** The Studio installation
is user-local, with a Start menu shortcut. The system-wide installation request was canceled at the Windows approval
prompt. The official Studio installer was checked against Firelight Technologies' Authenticode signature.

Open `../FMOD/FirstLight.fspro` in Studio to edit the sound design. The project contains its source WAVs, the editable
events and mixer, plus Desktop banks. Unity's saved FMOD settings point to this project and load `Master`,
`Master.strings` and `Flight`. Runtime banks are copied to `Assets/StreamingAssets/FMOD`.

The flight scene creates `FlightAudio` on its scene controller and adds an FMOD listener to the flight camera.
The game assembly references `FMODUnity`. Existing simulation and save data are unchanged.

| Event | Current behavior |
| --- | --- |
| `event:/Music/Flight` | Seven synchronized streaming stems, one continuous 32-bar loop, separate layer gain parameters |
| `event:/Vessel/Engine` | Actual running-engine output drives volume; air pressure blends external roar against a quiet filtered structural layer |
| `event:/Vessel/Wind` | Volume follows dynamic pressure |
| `event:/Vessel/Reentry` | Plasma volume follows part convective heating per estimated area |
| `event:/Vessel/Stage01` through `Stage03` | Random staging variant, once per active-vessel staging event |
| `event:/Vessel/RCS` | Strength follows nozzle activity; repeated bursts are limited to one per 0.24 seconds |
| `event:/Vessel/Touchdown` | Ground-contact edge, with a two-second retrigger guard |
| `event:/Vessel/Parachute` | One transient when a chute opens, including symmetric deployments |
| `event:/Vessel/Ignition`, `Shutdown` | Actual engine-running transitions |
| `event:/Vessel/Explosion` | Active-vessel part destruction, rate-limited during cascades |

Music starts quietly on the pad. Low atmospheric ascent uses the launch arrangement; the upper atmosphere removes
rhythm and brass. A stable planetary orbit selects Orbit; escaping, solar orbit and a suborbital coast in vacuum use
Transit. Low descending approaches and stable landed/splashed states select Touchdown. Hot atmospheric descent
selects Reentry, including when close to the ground. Airless launches also use Launch.

Atmospheric and heating thresholds have hysteresis. A candidate must remain stable for 1.5 seconds; ordinary phase
changes begin on a bar boundary and blend over two bars (6.667 seconds). The stems keep their common playhead. Vessel
switches reset transient detection and blend toward the new context without restarting the music.

Master, Music and Effects use the existing settings sliders, each applied once. Important notifications and staging
briefly duck the music; running engines duck it more gently. A master limiter has a -1 dB ceiling. This does not
replace a listening and loudness pass. Physical effects pause with the simulation and mute during rails warp; music
keeps real-time tempo. Scene teardown stops loops and outstanding vessel effects.

This is the original flight pack. The [expansion](../README.md) adds nine more scores, docking, leg and EVA foley.
Non-flight scenes, UI sounds and separate engine-family recordings remain outside the current integration.
Additional long-form material and quiet intervals are still needed for a finished soundtrack.

## Edit and rebuild

In FMOD Studio, open `../FMOD/FirstLight.fspro`, adjust events/mixing, then build the Desktop banks. Unity watches
`Art/Audio/FMOD/Build` and refreshes the runtime copies. You can also build from PowerShell at the repository root:

```powershell
& "$env:LOCALAPPDATA/Programs/FMOD Studio 2.03.15/fmodstudiocl.exe" -build -export-guids -platforms Desktop Art/Audio/FMOD/FirstLight.fspro
```

The original source pack is `FirstLight/Stems` and `FirstLight/SFX`; FMOD's imported working copies are in
`FMOD/Assets`. If the renderer is rerun, replace the corresponding FMOD source WAVs and rebuild. The one-time
`tools/audio/build_fmod.js` seed script deliberately refuses to overwrite a project that already contains events.

## Reproduce / checks

Renderer: `tools/audio/render_flight_audio.py`. Tool-only Python environment used on this machine:
`C:\Users\jakub\AppData\Local\Temp\tap-audio-venv\Scripts\python.exe`.
Dependencies are listed at the top of the renderer; nothing is added to the Unity player's Python dependencies.

Run the renderer with `music`, `sfx`, `all`, or `verify`. The script checks non-silent finite samples, headroom,
matching stem frames and waveform continuity at loop boundaries. `render-report.json` records measured levels;
`verification.json` records file hashes and technical checks. These checks do not establish musical quality,
perceived loop transparency, final true-peak loudness, or an acceptable mix with gameplay.

The Editor compiled the integration and flight controller. Native FMOD verification loaded all three runtime banks,
checked the looping music event and its seven gain parameters, and resolved all twelve vessel events. Fifteen focused
EditMode tests passed, including orbital/suborbital separation, atmosphere and heating hysteresis, airless approach
and launch, and re-entry priority. Evidence: `Logs/FmodSetup.log` and `Logs/FlightAudioTests.xml`.

A complete gameplay listening pass and REAPER-session playback have not been verified. The short loop splice checks
establish sample continuity, not perceived transparency or musical approval.

## Sources and rights

The MIDI composition is newly written for this project; no KSP recordings or melodies were sampled. Instrument
recordings remain subject to the BBC Discover instrument licence. The supplied Sonniss licence permits use and
editing of its recordings incorporated in games, including commercial projects; the recording authors retain their
copyright. Do not market the processed recordings as original field recordings or distribute the collection as a
standalone commercial sound library. `sources.json` records exact input filenames. The four source archive licences
have been copied into `Licenses/` for the project's eventual third-party notices.

FMOD's commercial distribution licence remains a separate project requirement. Official references:
[FMOD licensing](https://www.fmod.com/licensing),
[Unity setup](https://www.fmod.com/docs/2.04/unity/user-guide.html),
[music transitions](https://www.fmod.com/docs/2.04/studio/authoring-events.html),
[integration source and binary availability](https://github.com/fmod/fmod-for-unity).
