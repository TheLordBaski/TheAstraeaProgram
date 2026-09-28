# The Astraea Program automated mission report: escape
Result: PASSED
Checks: 6/6 passed

- [x] return trajectory found by patched-conic search: excess speed 350 m/s (350 outward, -15 along track); leaves Tellus's sphere after 41.4 h, back after 428.9 Tellus days, periapsis 8912 km
- [x] probe on its departure hyperbola: e 1.025, 120 km periapsis
- [x] left Tellus's sphere of influence into Astraea's: frame Astraea, 87459 km from Tellus, 0.0 m from the prediction
- [x] solar orbit resonant with Tellus: period 439.75 vs 439.87 Tellus days, e 0.0478
- [x] back in Tellus's sphere of influence after a year: frame Tellus after 0.96 years at warp
- [x] arrival matches the prediction: periapsis 8912 km vs 8912 km planned

## Log
    [AutoTest        1.5] PHASE escape planning
    [AutoTest        1.5] CHECK PASS: return trajectory found by patched-conic search — excess speed 350 m/s (350 outward, -15 along track); leaves Tellus's sphere after 41.4 h, back after 428.9 Tellus days, periapsis 8912 km
    [AutoTest        2.5] CHECK PASS: probe on its departure hyperbola — e 1.025, 120 km periapsis
    [AutoTest        2.5] PHASE leaving the home planet
    [AutoTest   152760.3] CHECK PASS: left Tellus's sphere of influence into Astraea's — frame Astraea, 87459 km from Tellus, 0.0 m from the prediction
    [AutoTest   152760.3] CHECK PASS: solar orbit resonant with Tellus — period 439.75 vs 439.87 Tellus days, e 0.0478
    [AutoTest   152760.3] PHASE a year around the star
    [AutoTest  9268297.5] CHECK PASS: back in Tellus's sphere of influence after a year — frame Tellus after 0.96 years at warp
    [AutoTest  9268297.5] CHECK PASS: arrival matches the prediction — periapsis 8912 km vs 8912 km planned
