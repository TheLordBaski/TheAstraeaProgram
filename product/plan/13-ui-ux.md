# 13 · Interface, onboarding and accessibility (UX)

**Have:**
- **Interface**:
  - a code-built uGUI toolkit (`UIKit`);
  - main menu, assembly building UI, flight HUD with navball, staging stack, resources, orbit info and the mission
    guide;
  - map UI, part menus, pause menu, tooltips, a control guide (F1);
  - the developer window (draggable).
- **Missing**:
  - no settings screen, save browser or new-game flow;
  - no tutorials, handbook, scenarios, flag choice or accessibility options.
- **Clarity**: you asked for better visual clarity, which is addressed here (UX-04) and in the art pass (ART-11).

---

### UX-01 · Interface design system v2
**Milestone** M1 · **Claude** 3 d · **You** 1 d · **Needs** —

- One style guide in code:
  - type scale (5 sizes), minimum text size, contrast rules;
  - spacing grid;
  - panel, button and field styles;
  - colours by meaning (good, warning, danger, info, science, funds, reputation);
  - an icon set (ART-13).
- UI scale (FND-08) applied everywhere.
- Audit the existing screens and convert them.

**Done when**
- [ ] A style page in the developer window shows every component.
- [ ] All screens use the styles.
- [ ] No text below the minimum size at 1080p with 100% scale.

### UX-02 · Main menu and new-game flow
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** FND-05, FND-06

- **Main menu**: Continue, New game, Load, Training, Scenarios, Settings, Handbook, Credits, Report a problem, Quit.
- **New game**: mode (sandbox, science, career from EA3), agency name, flag (UX-11) and difficulty (CAR-09).
- **Save browser**: thumbnails, mode, date, play time; delete, rename, open folder. Backups can be restored (FND-05).

**Done when**
- [ ] A player can start, continue and load every mode without the developer window.
- [ ] Corrupt saves show the restore option.

### UX-03 · Settings screen
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** FND-08, FND-09

- Tabs for graphics, audio, controls, gameplay, interface and accessibility (UX-10).
- Controls: key rebinding with conflict warnings, joystick axes with a live preview, and reset.
- Apply, revert and defaults. Changing resolution offers a 15-second confirmation.

**Done when**
- [ ] Every setting from FND-08 and FND-09 is reachable and persists.
- [ ] A rebinding conflict is explained.

### UX-04 · Flight HUD readability pass
**Milestone** EA1 · **Claude** 3 d · **You** 2 d · **Needs** UX-01

The "visual clarity" pass for the interface:
- Navball and speed display sized by the HUD scale.
- Altitude (sea level and terrain), vertical and horizontal speed readable at a glance.
- The staging stack gets bigger icons, fuel gauges per stage, and highlights the parts on hover; reordering by drag
  already exists.
- The resources panel defaults to the current stage.
- Electricity and propellant warnings with time left.
- Temperature gauges near hot parts (FLT-04). The objectives panel (FND-14).
- A notification feed that doesn't cover the navball.
- Before and after screenshots, and your sign-off.

**Done when**
- [ ] Five testers can read altitude, speed, stage fuel and electricity from a still screenshot in under 2 s each.
- [ ] You approve the before/after.

### UX-05 · Training (interactive tutorials)
**Milestone** EA1 · **Claude** 5 d · **You** 2 d · **Needs** FND-14, SCI-10

- Tutorials on the objectives system. Each one is a saved scenario plus guided steps with highlights, checks and
  hints:
  1. The assembly building: build a sounding rocket.
  2. First flight: launch, stage, parachute landing.
  3. Getting to orbit: gravity turn, circularise.
  4. Manoeuvres: plan and fly a Luma transfer with nodes.
  5. Landing on Luma: suicide burn basics, legs, EVA, flag.
  6. Rendezvous and docking.
  7. Science: experiments, transmit or recover, research.
  8. Career basics (EA3): contracts, funds, facilities.
- Each has a restart and a skip, and can be replayed.

**Done when**
- [ ] Tutorials 1–7 ship in EA1 and 8 in EA3.
- [ ] Each passes a scripted run.
- [ ] A new player finishes 1–3 without outside help (playtest).

### UX-06 · Scenarios
**Milestone** EA5 · **Claude** 3 d · **You** 2 d · **Needs** UX-05, CAR-05

- Six saved challenges with objectives and a success screen, for example:
  - "Stranded above Luma" (rescue);
  - "Station resupply";
  - "Aerobrake at Rubra";
  - "Catch that asteroid";
  - "Caligo return";
  - "Grand tour of the Magnus moons".

**Done when**
- [ ] Each scenario is solvable. An autotest or you completes each once.

### UX-07 · The handbook (in-game encyclopedia)
**Milestone** EA1 · **Claude** 3 d · **You** 2 d · **Needs** FND-10

- Pages:
  - concepts: orbits, Δv, staging, aerodynamics, heat, docking, science, research, career, resources;
  - every body (SYS-18);
  - every part category;
  - the Δv map;
  - controls.
- Search, links from tooltips (a "?" opens the right page), and pictures from in-game screenshots.
- Claude drafts, you review. Expanded each milestone.

**Done when**
- [ ] About 40 pages in EA1, all reviewed.
- [ ] Every body and part category has a page by 1.0.

### UX-08 · Notifications and the mission log
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** FND-14

- One notification system:
  - toasts for science, contracts, crew, warnings and firsts;
  - a mission log window with filters, saved.
- The existing notices (flight log lines, dev messages) go through it.

**Done when**
- [ ] Every event type posts one clear notice.
- [ ] The log survives saving.
- [ ] Nothing spams more than once per second.

### UX-09 · Screenshots
**Milestone** EA1 · **Claude** 1 d · **You** — · **Needs** —

- A key (rebindable) saves a screenshot to the user folder, optionally at 2× resolution and without the UI. Steam
  screenshot integration comes in REL-03.

**Done when**
- [ ] Screenshots save with and without the UI at the chosen resolution.

### UX-10 · Accessibility
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** UX-01

- UI and text scale.
- A colour-blind-safe palette option: navball markers differ by shape as well as colour; map and state colours.
- Reduce flashing (explosions, warnings).
- Hold-to-toggle options for held keys.
- Subtitles for any voice or text events, if audio carries information.

**Done when**
- [ ] A colour-blind simulation screenshot shows every marker distinguishable.
- [ ] All options persist.

### UX-11 · Flags
**Milestone** EA1 · **Claude** 1 d · **You** 1 d · **Needs** —

- A gallery of about 12 original flags (ART-13), and custom PNGs from a user folder (with size rules).
- Used for the agency, planted flags and vessel decals (HUB-07).

**Done when**
- [ ] A custom PNG appears in the gallery and on a planted flag.

### UX-12 · Loading screens and tips
**Milestone** EA1 · **Claude** 1 d · **You** 0.5 d · **Needs** FND-07

- Loading screens with art (ART-12) and a rotating tip from a list of about 60 tips (string tables). A progress bar
  where the load has steps.

**Done when**
- [ ] Every scene change shows a loading screen with a tip.

### UX-13 · Credits and about
**Milestone** EA1 · **Claude** 0.5 d · **You** 0.5 d · **Needs** REL-02

- Credits: you, contributors, tools, and third-party licences from REL-02 (fonts, sound libraries, packages).
- The version and build date.

**Done when**
- [ ] Every third-party asset in the build is credited as its licence requires.
