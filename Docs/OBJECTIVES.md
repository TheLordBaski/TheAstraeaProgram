# Game events and objectives

The mission guide in flight is data. A set of objectives is a JSON file in `Assets/TAP/Resources/Data/Objectives/`; the game
finds every file there. No code names a body: a set says `$home` or `$moon`, or the id of a body of the system it is for, and
texts say `{home}` or `{moon}`.

## Events

`GameEvents` (in `TAP.Core`) is a static bus. The simulation and the scenes raise events; anything may listen with
`GameEvents.Raised += handler` and must remove itself when its scene ends. A listener that throws does not stop the others.

| Event | Raised when |
|---|---|
| `Launched`, `Staged` | The first stage fires; any stage fires (detail: the stage number) |
| `ReachedSpace`, `Orbit`, `SoiChanged`, `Landed`, `Splashed`, `ReentrySurvived` | A vessel's state changes (`EventWatcher` looks at every vessel twice a second; the first sight of a vessel says nothing) |
| `EvaStarted`, `EvaEnded`, `FlagPlanted`, `Docked` | The thing happens |
| `CrewRecovered`, `VesselRecovered` | A vessel is recovered (crew only when it carried some) |
| `PartDestroyed`, `CrewLost` | A part breaks up or burns; a crew member dies |
| `ScienceCollected` | Defined for the science tasks; nothing raises it yet |

Every event has the time, the vessel (id and name), the body's id, and optionally a crew name and a detail.

## A set

```json
{
  "id": "orbit_moon",
  "title": "Mission: orbit {moon}",
  "order": 1,
  "objectives": [
    { "id": "orbit", "title": "Achieve a stable orbit of {home}", "hint": "Circularise at apoapsis.",
      "when": { "body": "$home", "situation": "Orbiting" } },
    { "id": "moon_orbit", "title": "Reach orbit of {moon}", "hint": "Target {moon} in the map.",
      "when": { "body": "$moon", "situation": "Orbiting", "done": [ "orbit" ] } }
  ]
}
```

Objectives are checked in order, twice a second and whenever an event is raised, until nothing more changes; each is done for
good once its condition has held. Every field of a condition that is set must hold:

| Field | Holds when |
|---|---|
| `kind` | The event of this kind is being raised now (an event counts when it is raised, not after) |
| `body` | The vessel is at this body, or the event happened there |
| `situation` | The vessel's situation is one of these (`Landed`, `Landed\|Splashed`) |
| `launched`, `crewed`, `eva`, `flag`, `groundContact` | The vessel is, or is not, so |
| `altitudeAbove` | Above `"space"` (the top of the air, 70 km without air) or a number of metres |
| `surfaceSpeedBelow` | Surface speed under this |
| `encounter` | The vessel's path next meets this body |
| `flagOn` | A flag stands on this body |
| `done`, `notDone` | Objectives of the set are, or are not, done |
| `any`, `not` | Any of these conditions holds; this one does not |

Bodies are `$home`, `$moon` (the home planet's first moon), `$star`, or an id (`home/tellus`, `debug/testrock`). Texts take
`{home}`, `{moon}`, `{body:REF}`, `{space}` (km) and `{space+10}`. A set with a mistake (an unknown field, event, situation,
body or objective) is left out and the reason is logged; `ObjectiveTests` checks every file against the home and debug systems.

`id` is kept in saves (`GameSave.milestones`). The first set (`lunar`, the default) keeps the plain ids older saves hold; the others
are stored as `set:id`. `GameSave.objectiveSet` is the set in use; the `»` button of the guide switches to the next.
