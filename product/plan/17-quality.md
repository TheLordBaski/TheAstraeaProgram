# 17 · Quality, testing and performance (QA)

**Have:**
- 20 EditMode unit tests.
- A scripted pilot that flies full missions through the player's controls: `orbit`, `lunar`, `persistence`,
  `suborbital`, `failures`, `docking`, `ascent` and the Luma resume missions.
- The missions run in the editor and in the standalone build (batch mode, windowless), and write reports to
  `Docs/TestReports`.
- Evidence tables in [Docs/FEATURES_AND_LIMITATIONS.md](../../Docs/FEATURES_AND_LIMITATIONS.md).

That is the strongest asset of the project. The plan scales it to the whole game.

Performance baselines are FND-13 and CI is FND-11.

---

### QA-01 · Test strategy
**Milestone** M1 · **Claude** 1 d · **You** 0.5 d · **Needs** —

- Which layer covers what:

  | Layer | Covers |
  |---|---|
  | Unit tests | maths, data, rules |
  | Play-mode integration | UI flows, save/load |
  | Mission autotests | physics and the game loops end to end |
  | Save corpus | compatibility |
  | Benchmarks | performance |
  | Playtests | fun and clarity |

- Every task's "Done when" names its test.
- Rules:
  - any bug fixed gets a test;
  - nightly results are reviewed at the start of each work session.

**Done when**
- [ ] `Docs/TESTING.md` exists and the task template refers to it.

### QA-02 · Autotest framework v2
**Milestone** EA1 · **Claude** 3 d · **You** 0.5 d · **Needs** FND-11

- Missions become composable steps with parameters: launch, ascend to orbit, transfer to a body, capture, land, EVA,
  science, dock, return. Written against any craft and any body.
- Results as JSON, for CI summaries, and as markdown reports as today.
- Several missions run in parallel (separate batch-mode players), with timeouts and a retry for known flakiness,
  flagged in the report.
- The pilot keeps using only player controls, so the tests keep proving the game is playable.

**Done when**
- [ ] Today's missions are re-expressed in v2 and still pass.
- [ ] A new body mission is under 30 lines of script.
- [ ] Nightly runs 4 in parallel on your PC.

### QA-03 · Save compatibility suite
**Milestone** EA1 · **Claude** 1 d · **You** — · **Needs** FND-05

- Each release adds saves (a sandbox, a science game, and later a career) to the corpus. Every build loads them all
  and runs one minute of flight on the active vessel.

**Done when**
- [ ] The corpus loads in CI.
- [ ] A deliberate format change without a migration fails the test.

### QA-04 · Playtesting program
**Milestone** EA1 → R1 · **Claude** 1 d · **You** 12 d · **Needs** REL-05

- **Before Early Access**: closed playtests with Steam Playtest or keys, 10–30 players. A survey after key moments
  (first orbit, first landing, first hour).
- **During Early Access**: an opt-in feedback prompt, and optional telemetry (D-10) for anonymous progress events:
  time to orbit, abandoned tutorials, crashes.
- A triage routine: weekly, labels on GitHub Issues (bug, balance, clarity, crash), and a public known-issues list.

**Done when**
- [ ] Each milestone has a playtest report with the top 10 issues and what was done about them.

### QA-05 · Release candidate testing
**Milestone** EA1 → R1 · **Claude** 1 d · **You** 6 d · **Needs** FND-11

- A checklist per release:
  - fresh install and upgrade install on Windows 10/11 and a Linux distribution (plus Steam Deck, optional);
  - save migration;
  - resolutions and aspect ratios (16:9, 21:9, 16:10);
  - keyboard layouts (QWERTZ, AZERTY);
  - joystick;
  - offline start;
  - a low-spec machine;
  - every tutorial, all autotests.

**Done when**
- [ ] The checklist is in `Docs/RELEASE_CHECKLIST.md`, completed and attached to every release.
