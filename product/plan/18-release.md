# 18 · Release and Early Access (REL)

**Decided:**
- Early Access first.
- Windows and Linux on Steam; macOS and consoles later.
- Solo development with Claude.

**Have:** a Windows player build and a public GitHub repository.

**Everything else is to do:**
- legal checks;
- the store presence;
- a release pipeline;
- community channels;
- the launch.

---

### REL-01 · Early Access plan and roadmap
**Milestone** M1 · **Claude** 1 d · **You** 2 d · **Needs** —

- **What Early Access 0.1 contains** and what each later update adds (the milestones in [README.md](README.md)).
- **Commitments**:
  - pricing: an Early Access price that rises at 1.0 (D-09);
  - update cadence: about every 2–3 months;
  - the save compatibility promise (D-13);
  - how feedback is used.
- **Steam's Early Access questionnaire** answers:
  - why Early Access;
  - how long;
  - how the game changes;
  - the price at 1.0.
- A public roadmap page (a Steam announcement and the repo).

**Done when**
- [ ] `product/EARLY_ACCESS.md` is written, the questionnaire answers are drafted, and the roadmap page is ready.

### REL-02 · Legal, licences and name
**Milestone** M1 · **Claude** 1 d · **You** 2 d · **Needs** —

- A trademark and name search for "The Astraea Program" and "TAP" in the relevant classes (games, software; EU and
  US), and the Steam name. Do this before any marketing, to avoid a rename after launch.
- Domain and social handles.
- **Licences**:
  - a third-party licence list: Unity packages, fonts (Liberation Sans in TextMesh Pro), sound libraries, music,
    glTFast;
  - the EULA;
  - a privacy policy if there is telemetry or crash upload (D-10).
- **Originality check**: no KSP names, text or art (already a project rule). Mission names, parts and bodies stay
  original.
- Not legal advice: for the trademark, a short consultation with a lawyer is worth the cost.

**Done when**
- [ ] The search result is recorded.
- [ ] The licence list is in `Docs/THIRD_PARTY.md` and in the credits (UX-13).

### REL-03 · Steamworks
**Milestone** EA1 · **Claude** 3 d · **You** 2 d · **Needs** REL-02

- **The Steam app** (Steam Direct fee): depots for Windows x64 and Linux x64; branches (default, beta, experimental);
  launch options (`-safe-mode`).
- **Steam Cloud**: Auto-Cloud on the saves folder, excluding the backups.
- **Achievements**: about 30, mapped from the world firsts (CAR-07) and a few playful ones.
- **Screenshots**: Steam screenshot integration.
- **Steamworks SDK**: integration (Steamworks.NET or Facepunch.Steamworks), only for what it needs: achievements,
  screenshots and rich presence. The game must run without Steam for testing.

**Done when**
- [ ] A beta branch build installs from Steam on Windows and Linux.
- [ ] Cloud saves sync between the two.
- [ ] An achievement unlocks.

### REL-04 · Store page and trailer
**Milestone** EA1 · **Claude** 1 d · **You** 5 d · **Needs** ART-12, REL-03

- The store page:
  - short and long descriptions;
  - a features list;
  - 10+ screenshots;
  - GIFs;
  - tags;
  - system requirements (from FND-13);
  - the Early Access section (REL-01).
- A 60–90 s trailer: scripted camera shots from autotest scenes (repeatable captures), plus gameplay capture by you.
  Music from AUD-06.
- "Coming soon" page live 2–3 months before Early Access, to gather wishlists.

**Done when**
- [ ] The page passes Steam review.
- [ ] The trailer is published.
- [ ] Wishlists are counted weekly.

### REL-05 · Community and feedback channels
**Milestone** EA1 · **Claude** 1 d · **You** 2 d · **Needs** —

- A Discord server (announcements, feedback, bug reports, screenshots), the Steam forums, and a public roadmap.
- The in-game *Report a problem* (FND-12) links here.
- A devlog cadence: a short update every 2–4 weeks with screenshots, so there is momentum before launch.

**Done when**
- [ ] The channels are live and the report button leads to them.
- [ ] The first 3 devlogs are published before Early Access.

### REL-06 · Release pipeline
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** FND-11, REL-03

- A tag on `main` builds both platforms in CI (FND-11) and runs the release checklist tests.
- Uploads to the Steam beta branch with SteamCMD. Promotion to default is a manual step.
- Patch notes from a template, and a rollback: the previous build stays one click away.

**Done when**
- [ ] A dry run takes a tag to the beta branch without manual building.

### REL-07 · Linux and Steam Deck
**Milestone** EA1 · **Claude** 2 d · **You** 1 d · **Needs** FND-11

- Linux specifics: case-sensitive paths (check every `Resources` and file path), Vulkan and OpenGL fallbacks, and
  input devices.
- Steam Deck is optional. Verification needs full gamepad play, which is a big UI task. Aim for "Playable" with the
  Deck's keyboard/mouse layouts first (D-18).

**Done when**
- [ ] The Linux build passes the release checklist (QA-05) on a real machine.
- [ ] The Steam Deck status target is decided.

### REL-08 · Demo (optional)
**Milestone** EA1 · **Claude** 1 d · **You** 1 d · **Needs** REL-04

- A demo build (sandbox, Tellus system only, tutorials 1–5) for a Steam Next Fest before Early Access.

**Done when**
- [ ] The demo is built from the same code with a flag.
- [ ] It enters a Next Fest.

### REL-09 · 1.0 launch
**Milestone** R1 · **Claude** 1 d · **You** 5 d · **Needs** every R1 task

- Leave Early Access:
  - a new trailer;
  - press and creator outreach (a key list);
  - a launch discount plan;
  - the Steam launch visibility round;
  - day-one patch readiness;
  - price change per REL-01.
- A post-launch plan: patch cadence and the first post-1.0 update, which is multiple systems and trade routes
  ([20-multi-system-and-trade.md](20-multi-system-and-trade.md)).

**Done when**
- [ ] 1.0 is live on Windows and Linux.
- [ ] The day-one checklist is done.
- [ ] The post-launch plan is published.
