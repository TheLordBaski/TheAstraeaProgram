# Science mode: design

**Status:** agreed with you on 2026-10-16. It replaces the open choice in
[SCIENCE_AND_TECH_OPTIONS.md](SCIENCE_AND_TECH_OPTIONS.md) for the research half of the game. Plan files
[08-science](../plan/08-science.md) and [09-tech-tree](../plan/09-tech-tree.md) carry it as tasks (section 10).

## 1. In one paragraph

Instruments record science by themselves. Each finding pays **science points** once, the first time. To pay, the data
must reach home: either transmitted over a **comms network** you build (ground stations, antennas, relays, with planets
blocking line of sight), or carried home. Samples can only be carried, unless a **lab** analyses them into data that
can be transmitted. Science points buy tree nodes. Some nodes are **prototype groups**: buying the node gives one
unreliable prototype part, and a test mission unique to that group certifies it and unlocks the rest of the group.

## 2. Rules

1. **No bare timers.** Nothing unlocks by waiting. All time in this design depends on an input (data size against
   bandwidth, samples waiting in a lab), so warping cannot skip a step. (From 2026-10-10.)
2. **First findings only.** A subject pays its full value once. Repeats pay nothing, so there is no farming curve.
   (From 2026-10-10.)
3. **Full value, but only when it gets home.** Transmitted data pays 100%, the same as carried data, but only over
   a live link home.
4. **Every prototype group has its own test.** No generated "burn engine X for N seconds" lists. This is what stops
   the repetition.
5. **Everything is data.** Subjects, instruments, nodes, groups, tests and antennas are defined in JSON and validated
   at load (FND-04).

## 3. The loop

1. Research a node with science points. If it is a prototype group, you get its prototype part.
2. Build a craft for a target: instruments, antennas or a return capsule, and the prototype if it has a test that can
   be done on the way.
3. Fly. Instruments record findings automatically when their conditions match. The rocketeer collects samples on EVA.
4. Get the data home: transmit while a link exists, store it until one does, or carry it home. Samples ride home, or
   go to a lab.
5. Science arrives. The prototype's test is met during the flight, and the whole group unlocks.
6. Spend the points. Plan the next target, including relays you need to reach it.

## 4. Science

### 4.1 Subjects

- A subject is experiment × body × situation × biome (where biome matters). The situations are the six in
  08-science.md (landed, splashed, flying low/high, space low/high).
- A subject pays `base × body/situation multiplier` once. No diminishing returns, no caps.
- The recovery record (furthest situation reached: flew by, orbited, landed on X) pays on recovery at Tellus, once per
  record.
- Balance target: the points available in the home system add up to about 140% of the tree's cost. You cannot get
  stuck, and you cannot grind past the tree.

### 4.2 Automatic recording

- Every instrument has an auto toggle, on by default. When the vessel enters a matching situation and biome, it
  records the finding and shows a one-line toast ("Pressure profile · Rubra · Rift · Flying low"). No dialog.
- Instruments that need electricity do not record without it, and the toast says why.
- Surface samples need the rocketeer on EVA (a "Take sample" action). That is the one manual act.

### 4.3 Readings and samples

Every finding is one of two kinds:

| Kind | Can be transmitted | Examples |
|---|---|---|
| **Reading** | yes, over a live link | crew observation, EVA field notes, thermal probe, pressure gauge, seismic array, gravimeter, landmark survey |
| **Sample** | no; carry it home or analyse it in a lab | surface sample, bio-culture canister, materials exposure rack, air sampler |

- Readings have a size in Mbit. Transmission takes `size ÷ bandwidth`, and bandwidth falls with link strength
  (section 5). A big reading from Magnus takes days over a weak link.
- Samples have a mass and stay in the part or the pod that holds them. A rocketeer can move them between parts and
  between docked vessels. One container holds one copy of a subject.
- The bio-culture canister and the materials rack are single use until a lab resets them.
- Later instruments (magnetometer, radiation detector, spectrometer, telescope; options doc 3.4) are readings.

### 4.4 Labs

There are two labs: lab_nib (S1, one seat, one sample at a time, tier 5) and lab_quill (S2, two seats, two at
once, tier 8). A lab does two things, and only these two:

- **Analyses samples.** A sample put into the lab becomes a reading of the same subject at full value, which can then
  be transmitted. Each sample takes lab time (hours, faster with more crew aboard) and electricity. The lab works on
  rails during warp, but only on samples it holds, so warp cannot create science.
- **Resets single-use instruments** on its own vessel or a docked one (bio canister, materials rack), so one craft can
  use them at several sites.

There are no points over time and no value bonus. A lab saves a return trip; it does not make science.

## 5. Comms network

This is full CommNet. It moves decision D-22 from "after 1.0" to before 1.0, because the network is now part of
science.

### 5.1 Links

- **Ground stations** on Tellus (three, spread around the planet) turn with it, and Tellus blocks them.
- **Antennas** on vessels have:
  - a power, which sets their range;
  - a bandwidth;
  - electricity per Mbit;
  - a type, either *direct* (talks only to the ground or a relay) or *relay* (also forwards other vessels' signals).
- **Range** between two ends is `√(powerA × powerB)`, as in KSP. Signal strength falls from 100% at short range to
  0% at maximum range. Bandwidth scales with strength.
- **Line of sight.** A link exists only if no body (as a sphere) blocks the straight line. Atmospheres do not block.
- **Path home.** A vessel is connected if a chain of links reaches a ground station. The weakest link in the chain
  sets its strength.
- **No signal delay** inside the home system. Light-lag comes with interstellar flight (X1), where it becomes part of
  the gameplay.

### 5.2 What a connection does

| | Connected | Not connected |
|---|---|---|
| Transmit readings | yes, at the link's bandwidth | queue waits; nothing is lost |
| Probe control | full | **partial**: SAS hold modes and staging work; throttle can only go down; no manual steering |
| Crewed vessel | full | full |

- A transmission pauses when the link is lost (a moon gets in the way) and resumes on its own.
- The link is evaluated during warp on rails too, so a queue finishes during warp only while the link exists.

### 5.3 Growth

- The ground stations' power grows with tree nodes in the Comms branch (**Ground Network I–IV**), paid with science
  like any node. Science mode has no funds and no facility upgrades.
- Rough reach, with the matching vessel antenna: I covers Tellus and Luma, II the inner planets out to Rubra, III
  Magnus, IV the whole system. TEC balance sets the numbers.
- Beyond that, you build relays: a satellite with a relay antenna at Luma, a relay ring around Rubra, a long-range
  relay at Magnus. Placing relays so they keep line of sight is the new logistics puzzle.

### 5.4 Map view

- A comms overlay draws active links and colours them by strength. The vessel panel shows "Connected · 74% · via
  Luma Relay 2" or "No signal".

## 6. Tech tree and prototypes

### 6.1 Nodes

- The 48-node draft tree in 09-tech-tree.md stays the base. Science points buy nodes when their prerequisites are met.
- The Comms branch gains the four Ground Network nodes and the relay antennas.

### 6.2 Prototype groups

- Some nodes are **prototype groups**. Each names one **lead part**, for example the Lantern in Nuclear Propulsion.
- Researching the node makes the lead part available as a **prototype**, with a PROTOTYPE badge in the assembly
  building. The other parts of the node (variants, sizes, matching tanks) stay locked.
- The group has a **test mission**: 1–3 objectives, written by hand for that group only. The prototype part itself must
  do them, on any flight, at any time. There is no contract to accept. A toast confirms each objective.
- When all objectives are done, the group is **certified**: the prototype becomes a normal part, and every part in the
  node unlocks.
- Tests can be bundled. One flight can certify an engine on the way out and a heat shield on the way back.

### 6.3 Unreliable prototypes

Until certified, a prototype has a small chance to fail each time it is used. Failures can be recovered from. They
should cost a retry or a margin, rarely a mission:

| Part kind | Failure | Recovery |
|---|---|---|
| Engine | does not ignite, or shuts down early in a burn | ignite again |
| Heat shield | ablator burns faster | larger margin, or a shallower entry |
| Landing leg | does not lock on deploy | retract and deploy again |
| Docking port | latch does not catch | back off and dock again |
| Wheel | motor stalls | restart |
| Drill / converter | jams | restart |

- The chance is per use (ignition, deploy, entry), about 5%, set per group in data.
- A failure never counts against the test: if the conditions still hold, try again.
- Certified and normal parts never fail. This is the only failure system in the game.

### 6.4 Test missions (drafts)

Examples to set the tone. The full draft list of 15 groups is in [09-tech-tree.md](../plan/09-tech-tree.md); TEC-06
writes the final tests:

| Group (lead part) | Test mission |
|---|---|
| Vacuum Propulsion (eng_ember) | Burn for 30 s above the atmosphere of Tellus |
| Recovery (shield_s1) | Re-enter Tellus from orbit and land |
| Landing I (leg_ls2) | Land on Luma |
| Docking I (dock_s1) | Dock two vessels in orbit |
| Nuclear Propulsion (eng_lantern) | Burn for 90 s in Luma orbit, with 2 restarts |
| Rovers (wheel_m) | Drive 2 km on another body |
| Ion Propulsion (eng_glimmer) | Thrust for 10 days in total beyond Rubra's orbit |
| Resource Utilisation (isru_s) | Make fuel from ore on another body |
| Field Lab (lab_nib) | Analyse a sample in orbit of another body |
| Experimental Propulsion (eng_wedge) | Fire while on an escape trajectory from Astraea |

The draft tree has 15 prototype groups (engines, shields, legs, docking, wheels, ISRU, the field lab and the relay
antennas). Struts, tanks, fins and structures never have tests.

## 7. Science mode itself

- New game type **Science**: research and science only. No funds, contracts, facility upgrades or crew wages.
  (`GameMode.Science` already exists in code.)
- Start: tier 0 parts, Ground Network I, zero science.
- Time warp is free. Nothing in this design gets cheaper by waiting.
- **Sandbox** uses the same science (instruments fill the archive), but every part is available and nothing fails.
  CommNet is a new-game switch there, on by default.
- **Career** later uses all of this and adds funds, contracts and facility levels. That is designed separately.

## 8. Decided after the first pass (2026-10-16)

1. **Two labs.** A small lab, **lab_nib** (Nib Field Lab, S1, one seat, one sample at a time, EA2), sits in tier 5
   so a station at Rubra can analyse samples. The big **lab_quill** (two seats, two samples at once) stays in tier 8.
2. **Two relay parts.** **antenna_relay** (RA-2 Relay Dish, inner system, EA1, tier 4) and **antenna_relay_deep**
   (RA-15 Deep Relay, outer system, EA2, tier 7). The whip, dish and high-gain stay direct.

Both are in `parts.json` (hidden until their milestone) and in [05-parts.md](../plan/05-parts.md).

## 9. Still open

1. **Which nodes are prototype groups.** The 15 in [09-tech-tree.md](../plan/09-tech-tree.md) are a draft. You choose
   the final list (TEC-06).
2. **Does a test pay science too?** Default: no, the unlock is the reward.
3. **Depth levels** (better instruments reopening old subjects, options doc option 1) are not part of this design. A
   later addition if the mid-game runs dry.

## 10. In the plan

The plan was rewritten on 2026-10-16: [08-science.md](../plan/08-science.md) (SCI-01…16, with the comms network in
SCI-13…16) and [09-tech-tree.md](../plan/09-tech-tree.md) (TEC-01…07, with test missions in TEC-05/06 and failures in
TEC-07). D-22 in [19-decisions.md](../plan/19-decisions.md) moved the comms network before 1.0.
