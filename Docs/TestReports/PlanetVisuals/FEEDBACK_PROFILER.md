# Planet feedback diagnostic Profiler captures

Collected with user authorization on the busy PC, in the Unity Editor. These are sample snapshots, not controlled Ultra/Low comparisons or standalone-player benchmarks. Simulation was paused and cameras were placed for rendering checks. Their frame durations must not be used as a 60 FPS acceptance result.

The initial captured main thread contained about 16.69 ms of EditorLoop and 12.01 ms of PlayerLoop. FlightSim.LateUpdate included 1.94 ms while a Luma map view was active; hidden flight terrain LOD was still updating. MapUI.Update included 1.46 ms. The render thread included 10.60 ms waiting for the main thread. These inclusive samples overlap; they must not be added together.

The later diagnostic frame contains about 15.81 ms of EditorLoop, 2.60 ms of MapUI.Update, and 0.098 ms of FlightSim.LateUpdate. The render thread spends about 18.24 ms waiting for main-thread commands. No Planet integration/composition markers are present in the moon-facing map view with Tellus behind the camera. A separate rendered check confirms that the feature rejects this view before requesting its intermediate targets. These differing snapshots identify code paths; changing Editor load prevents a general performance-improvement claim from their numbers.

Changes verified independently of timings:

- Reject offscreen atmospheric bodies before queuing the renderer feature.
- Distance-based clouds use an analytic filtered texture layer, avoiding the former cloud march.
- Map/distant cloud views allocate no temporal history textures and issue no history copies.
- Hidden flight terrain LOD pauses in map mode and resumes on exit. Physics and surface-object updates retain their existing paths.
- Profiling samplers name integration/composition per body for the next graphical-player investigation.

The initial selected frame report is [profiler_feedback_before.json](D:/Dev/KSP/Docs/TestReports/PlanetVisuals/profiler_feedback_before.json); the later report is [profiler_feedback_after.json](D:/Dev/KSP/Docs/TestReports/PlanetVisuals/profiler_feedback_after.json). Full raw captures remain at [before](D:/Dev/KSP/Temp/PlanetFeedbackBefore.raw) and [after](D:/Dev/KSP/Temp/PlanetFeedbackAfter.raw). Profiler recording and GPU profiling were restored to disabled afterward.

When the PC is available, use the same graphical player, resolution, paused pose and warmup for both presets. Inspect CPU/GPU timelines with Tellus visible, Tellus offscreen while viewing Luma, inside clouds and during camera/map transitions. Editor/UI overhead, total graphics load and laptop thermals remain unresolved contributors to the supplied FPS screenshots.
