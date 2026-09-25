# The Astraea Program automated mission report: failures
Result: PASSED
Checks: 6/6 passed

- [x] overloaded joint fails from simulated loads: structural failure at SD-1 Stack Decoupler joint (244 kN tension > 180 kN; vessel 1.3 g, α 0.0 rad/s², ω 0.00 rad/s)
- [x] broken-off parts continue as a separate vessel: 2 loaded vessels (was 1)
- [x] capsule without heat shield is destroyed by reentry heating: Canopy-16 Main Parachute destroyed: overheated (skin 1402 K > 1400 K) (max skin 1599 K on Kestrel-1 Command Pod)
- [x] parachute deployed too fast is torn by the canopy load: Failed: torn by aerodynamic load (103 kN > 100 kN at 330 m/s) (armed at 4.8 km, 389 m/s, q 44 kPa; peak canopy load 100 kN)
- [x] high-speed impact destroys the capsule (collision damage): Kestrel-1 Command Pod destroyed: splashdown at 111.6 m/s
- [x] crew loss is reported: Bram Holloway was lost (splashdown at 111.6 m/s)

## Log
    [AutoTest        1.5] PHASE structural overload
    [AutoTest        5.5] CHECK PASS: overloaded joint fails from simulated loads — structural failure at SD-1 Stack Decoupler joint (244 kN tension > 180 kN; vessel 1.3 g, α 0.0 rad/s², ω 0.00 rad/s)
    [AutoTest        5.5] CHECK PASS: broken-off parts continue as a separate vessel — 2 loaded vessels (was 1)
    [AutoTest        7.6] PHASE overheating
    [AutoTest        7.6] spawned Heat Test Capsule at 85 km, 4,500 m/s, flight path -26° (no heat shield, flying nose first), periapsis 2 km
    [AutoTest       52.5] CHECK PASS: capsule without heat shield is destroyed by reentry heating — Canopy-16 Main Parachute destroyed: overheated (skin 1402 K > 1400 K) (max skin 1599 K on Kestrel-1 Command Pod)
    [AutoTest       52.5] PHASE unsafe parachute
    [AutoTest       52.5] spawned Chute Test Capsule at 5 km, 450 m/s air speed, diving 45°; arming the parachute immediately
    [AutoTest       53.4] CHECK PASS: parachute deployed too fast is torn by the canopy load — Failed: torn by aerodynamic load (103 kN > 100 kN at 330 m/s) (armed at 4.8 km, 389 m/s, q 44 kPa; peak canopy load 100 kN)
    [AutoTest       87.6] CHECK PASS: high-speed impact destroys the capsule (collision damage) — Kestrel-1 Command Pod destroyed: splashdown at 111.6 m/s
    [AutoTest       87.6] CHECK PASS: crew loss is reported — Bram Holloway was lost (splashdown at 111.6 m/s)
