"""Render missing flight foley and alternatives from the owner's Sonniss library."""
import hashlib
import json
import shutil
import numpy as np
import soundfile as sf
from scipy.signal import find_peaks
import render_flight_audio as r

r.OUT = r.ROOT / "Art/Audio/FlightExpansion"
SR = r.SR


def shot(name, x, seconds, level=.53):
    x = r.filtered(x[:, :round(seconds * SR)].copy(), 45, "highpass")
    x[:, :round(.006 * SR)] *= np.linspace(0, 1, round(.006 * SR))
    n = min(round(.08 * SR), x.shape[1])
    x[:, -n:] *= np.linspace(1, 0, n)
    x *= level / max(float(np.max(np.abs(x))), 1e-6)
    r.write_audio("SFX/" + name + ".wav", x)


def steps(fragment, length, surface):
    x = r.source(fragment, length)
    hop = 480
    energy = np.sqrt(np.mean(x[0, :x.shape[1] // hop * hop].reshape(-1, hop) ** 2, axis=1))
    peaks, _ = find_peaks(energy, distance=60, prominence=float(energy.max()) * .12)
    peaks = sorted(sorted(peaks, key=lambda p: energy[p], reverse=True)[:3])
    assert len(peaks) == 3, (fragment, peaks)
    for i, peak in enumerate(peaks):
        start = max(0, peak * hop - round(.06 * SR))
        clip = x[:, start:start + round(.45 * SR)]
        if clip.shape[1] < round(.45 * SR):
            clip = np.pad(clip, ((0, 0), (0, round(.45 * SR) - clip.shape[1])))
        shot(f"Step_{surface}_{i+1:02}", r.filtered(clip, 4500), .45, .38)


def render():
    latch = r.source("Cabin,frontdoor,deadbolt,lock,latch,thick,open,hyper.wav", 1)
    alternate = r.source("Latchlocker,steel,lever,small,close,release,squeak,snap,ring.wav", 1.35, start=.03)
    servo = r.source("ST Servo Motor metal spin speed variations.wav", 5, start=12)
    pressure = r.source("MECHHydr_Hydraulic Pressure Releasing_JSE_IR_01_Stereo.wav", 4)
    n = round(.9 * SR)
    t = np.arange(n) / SR
    thump = (np.sin(2*np.pi*(75*t + 2*(1-np.exp(-20*t)))) * np.exp(-13*t))[None, :]
    for i in range(2):
        click = r.filtered((latch if i == 0 else alternate)[:, :n], 5500)
        shot(f"Dock_Latch_{i+1:02}", click * .65 + thump * .15, .9)
        release = pressure[:, :round(.7*SR)] * np.exp(-np.arange(round(.7*SR))[None, :] / SR * 5)
        shot(f"Undock_Release_{i+1:02}", release * .45 + click[:, :release.shape[1]] * .3, .7)
        motor = servo[:, i*SR:i*SR+round(1.6*SR)].copy()
        motor *= np.sin(np.linspace(0, np.pi, motor.shape[1]))[None, :] ** .6
        motor[:, -round(.2*SR):] += click[:, :round(.2*SR)] * .3
        shot(f"Leg_Servo_{i+1:02}", r.filtered(motor, 3200), 1.6, .43)
    shot("Hatch_Open", latch * .6 + pressure[:, :SR] * .2, 1)
    shot("Hatch_Close", r.filtered(latch, 1500) * .6 + np.pad(thump, ((0, 0), (0, SR-n))), 1)
    steps("237_Foley_Footsteps_Metal_Boot_Walk_Normal_Close.wav", 10, "Metal")
    steps("289_Foley_Footsteps_Rocks_Sneaker_Jump_Land_On_Two_Feet_Close.wav", 9.7, "Surface")
    jet = r.filtered(pressure, 650, "highpass")
    jet = r.circular_splice(r.filtered(jet, 6500), .06)
    jet *= .38 / np.max(np.abs(jet))
    r.write_audio("SFX/Jetpack_Loop.wav", jet, True)
    files = sorted((r.OUT / "SFX").glob("*.wav"))
    assert len(files) == 15, len(files)
    reel = []
    for path in files:
        x, rate = sf.read(path, dtype="float32", always_2d=True)
        assert rate == SR and x.shape[1] == 1 and np.isfinite(x).all() and .001 < np.max(np.abs(x)) < .8
        if "Loop" in path.stem:
            assert abs(x[0, 0] - x[-1, 0]) < .00001
        reel.extend([np.repeat(x.T, 2, axis=0) * .7, np.zeros((2, SR//2), np.float32)])
    r.write_audio("SFX_Expansion_Audition.wav", np.concatenate(reel, axis=1))
    (r.OUT / "render-report.json").write_text(json.dumps(r.REPORT, indent=2) + "\n")
    (r.OUT / "verification.json").write_text(json.dumps({p.name: hashlib.sha256(p.read_bytes()).hexdigest()
        for p in files}, indent=2) + "\n")
    (r.OUT / "sources.json").write_text(json.dumps({"library_root": str(r.LIBRARY),
        "processed_recordings": sorted(r.SOURCE_FILES),
        "notice": "Sonniss recordings for incorporation in the game; not a standalone commercial sound library."}, indent=2) + "\n")
    licenses = r.OUT / "Licenses"
    licenses.mkdir(exist_ok=True)
    for year in (2017, 2018, 2023):
        shutil.copy2(r.ROOT / f"Art/Audio/FirstLight/Licenses/Sonniss-GDC-{year}.pdf", licenses)
    print("Verified 15 mono effects and the stereo audition reel.", flush=True)


if __name__ == "__main__":
    render()
