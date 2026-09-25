# The Tellus system

All values come from `Assets/TAP/Resources/Data/system.json` (data-driven: edit the file to change the
world). Derived values below are computed from those numbers with G = 6.674×10⁻¹¹ m³ kg⁻¹ s⁻².

## Tellus (home world)

| Property | Value |
|---|---|
| Radius | 600 km |
| Gravitational parameter μ | 3.5316×10¹² m³/s² |
| Mass | 5.29×10²² kg |
| Surface gravity | 9.81 m/s² (1.00 g) |
| Sidereal rotation | 6 h (21,600 s), prograde; equatorial surface speed 174.5 m/s |
| Atmosphere | 70 km thick, 101.325 kPa at sea level, scale height 5.6 km, N₂/O₂ (M = 28.96 g/mol, γ = 1.4) |
| Temperature profile | 288 K at sea level → 217 K (11–20 km) → 271 K (47–51 km) → 215 K at 70 km |
| Ocean | Yes (sea level = radius); splashdowns float |
| Terrain | Procedural (seed 4242), −3.6 km to +6.5 km, flattened launch site |
| Escape velocity (surface) | 3.43 km/s |
| Circular orbit at 80 km | 2,279 m/s, period 31.2 min |
| Sphere of influence | Root body (infinite) |
| Time-warp altitude limits | 5×/10×/50× above 70 km, 100× above 120 km, 1,000× above 240 km, 10,000× above 480 km, 100,000× above 960 km |

**Launch site**: Astraea Launch Complex at 0° N, 0° E, pad deck 71.2 m above sea level, 700 m flattened radius.
Launching east gains the 174.5 m/s rotation speed. The assembly building stands 420 m west of the pad.

## Luma (moon)

| Property | Value |
|---|---|
| Radius | 220 km |
| Gravitational parameter μ | 7.744×10¹⁰ m³/s² |
| Mass | 1.16×10²¹ kg |
| Surface gravity | 1.60 m/s² (0.163 g) |
| Rotation | Tidally locked (one turn per orbit) |
| Atmosphere | None |
| Terrain | Procedural cratered highlands and maria (seed 777), ±4.5 km |
| Orbit around Tellus | Circular, semi-major axis 13,000 km, inclination 0°, mean anomaly 60° at UT 0 |
| Orbital period | 156,700 s ≈ 43.5 h (7.3 Tellus days) |
| Orbital speed | 521 m/s |
| Sphere of influence radius | 2,821 km (Laplace: a·(m/M)^0.4) |
| Escape velocity (surface) | 839 m/s |
| Circular orbit at 20 km | 568 m/s, period 44 min |
| Time-warp altitude limits | 5×/10× above 6 km, 50× above 8 km, 100× above 12 km, 1,000× above 30 km, 10,000× above 60 km, 100,000× above 120 km |

## Distances and travel

| Leg | Value |
|---|---|
| Tellus surface → Luma surface (centre distance) | 13,000 km (12,180 km surface to surface) |
| Hohmann transfer from an 80 km orbit | ≈ 860 m/s prograde, ≈ 8.3 h to reach Luma's orbit |

## Approximate Δv budget (vacuum Δv; ascent includes drag and gravity losses)

| Manoeuvre | Δv |
|---|---|
| Launch to 80 km orbit | ≈ 3,400 m/s |
| Trans-Luma injection | ≈ 860 m/s |
| Luma orbit insertion (to ~30 km) | ≈ 250–320 m/s |
| Descent and landing | ≈ 600–700 m/s |
| Ascent to low Luma orbit | ≈ 600–650 m/s |
| Trans-Tellus injection | ≈ 250–300 m/s |
| Reentry and landing | 0 (aerobraking, heat shield, parachute) |
| **Total** | **≈ 6,000–6,300 m/s + margin** |

The **Luma Pathfinder** starter rocket carries about 6,690 m/s of vacuum Δv (lift-off TWR 1.95).
