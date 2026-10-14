"""Verify ten alternatives per phase and write the playable music catalogue."""
import hashlib
import json
import re
import numpy as np
import soundfile as sf
import render_flight_audio as r
from flight_scores import SCORES

NAMES = ["FirstLight", "FarHorizon", "NightDrift", *SCORES]
TITLES = {"FirstLight": "First Light", "FarHorizon": "Far Horizon", "NightDrift": "Night Drift"}


def verify():
    assert len(NAMES) == 10
    catalogue = []
    phases = {phase: [] for phase in r.PHASES}
    fingerprints = []
    for name in NAMES:
        r.OUT = r.ROOT / "Art/Audio" / name
        r.verify()
        spec = json.loads((r.OUT / "music.json").read_text())
        checks = {key.replace("\\", "/"): value for key, value in json.loads((r.OUT / "verification.json").read_text()).items()}
        assert spec["bpm"] == r.BPM and spec["bars"] == r.BARS and spec["stems"] == r.STEMS
        assert spec["phase_gains"] == r.PHASES and spec["frames"] == r.FRAMES
        raw = []
        for layer in r.STEMS:
            stem, rate = sf.read(r.OUT / "Stems" / (layer + ".wav"), dtype="float32")
            raw.append(stem)
            filename = layer if name == "FirstLight" else name + "_" + layer
            fmod, fmod_rate = sf.read(r.ROOT / "Art/Audio/FMOD/Assets" / (filename + ".wav"), dtype="float32")
            gain = 1 if name == "FirstLight" else spec["master_gain"]
            assert rate == fmod_rate == r.SR and stem.shape == fmod.shape
            assert np.max(np.abs(stem * gain - fmod)) < 2e-6, (name, layer)
        for phase, weights in r.PHASES.items():
            mix, _ = sf.read(r.OUT / "Music" / (phase + ".wav"), dtype="float32")
            reconstructed = sum(stem * weight for stem, weight in zip(raw, weights)) * spec["master_gain"]
            assert np.max(np.abs(mix - reconstructed)) < 3e-6, (name, phase)
            phases[phase].append(checks["Music/" + phase + ".wav"]["sha256"])
        midi = b"".join(p.read_bytes() for p in sorted((r.OUT / "MIDI").glob("*.mid")))
        assert midi
        fingerprints.append(hashlib.sha256(midi).hexdigest())
        catalogue.append({"id": name, "title": TITLES.get(name, SCORES.get(name, {}).get("title")),
            "event": "event:/Music/" + ("Flight" if name == "FirstLight" else name),
            "preview": name + "/" + name + "_Audition.wav", "reaper": name + "/" + name + "_Stems.rpp",
            "arrangements": {phase: name + "/Music/" + phase + ".wav" for phase in r.PHASES}})
        assert (r.ROOT / "Art/Audio" / catalogue[-1]["preview"]).exists()
        assert (r.ROOT / "Art/Audio" / catalogue[-1]["reaper"]).exists()
    assert len(set(fingerprints)) == 10, "A score reuses another score's MIDI."
    assert all(len(hashes) == len(set(hashes)) == 10 for hashes in phases.values())
    source = (r.ROOT / "Assets/TAP/Scripts/Game/Flight/FlightAudio.cs").read_text()
    assert re.findall(r'"(event:/Music/[^"\n]+)"', source) == [s["event"] for s in catalogue]
    result = {"bpm": r.BPM, "bars": r.BARS, "seconds_per_score": r.LENGTH,
        "scores": catalogue, "alternatives_per_phase": {p: len(h) for p, h in phases.items()},
        "checks": {"distinct_midi_scores": 10, "distinct_phase_arrangements": 60,
                   "synchronized_stems": 70, "fmod_source_calibration": "passed", "phase_mix_reconstruction": "passed"}}
    (r.ROOT / "Art/Audio/music-catalogue.json").write_text(json.dumps(result, indent=2) + "\n")
    print("Verified ten distinct scores, sixty phase arrangements and seventy calibrated FMOD stem sources.", flush=True)


if __name__ == "__main__":
    verify()
