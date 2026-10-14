"""Render the First Light audition pack with the locally installed instruments.

Python 3.12; pip install pedalboard==0.9.26 mido scipy soundfile pyloudnorm
Run: python tools/audio/render_flight_audio.py [music|sfx|verify|all]
"""
from pathlib import Path
import argparse
import hashlib
import json
import math
import struct
import xml.etree.ElementTree as ET

import mido
import numpy as np
import pedalboard as pb
import pyloudnorm as loudness
import soundfile as sf
from scipy import signal
from flight_scores import SCORES, compose

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Art/Audio/FirstLight"
LIBRARY = Path("D:/Audio")
BBC = LIBRARY / "BBCSO/Spitfire Audio - BBC Symphony Orchestra"
VST = Path("C:/Program Files/Common Files/VST3")
SR, BPM, BARS = 48000, 72, 32
BEAT = 60 / BPM
LENGTH = BARS * 4 * BEAT
FRAMES = round(LENGTH * SR)
RNG = np.random.default_rng(141026)
ALPHABET = ".ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+"
REPORT = {}
# All stems share this harmonic timeline, so changing layers never changes key or bar.
CHORDS = [(50, 57, 61, 64, 66), (47, 54, 57, 61, 64),
          (43, 50, 54, 57, 61), (45, 52, 57, 59, 64),
          (54, 57, 61, 64, 69), (43, 50, 54, 59, 64),
          (50, 57, 61, 64, 66), (45, 52, 57, 62, 64)]
PHASES = {
    "01_Launch":       [0.24, 0.60, 0.66, 0.72, 0.26, 0.74, 0.55],
    "02_HighAtmosphere": [0.64, 0.56, 0.30, 0.28, 0.50, 0.22, 0.12],
    "03_Orbit":        [0.70, 0.22, 0.15, 0.00, 0.60, 0.00, 0.00],
    "04_Transit":      [0.58, 0.10, 0.12, 0.00, 0.23, 0.00, 0.00],
    "05_Touchdown":    [0.38, 0.64, 0.36, 0.32, 0.46, 0.12, 0.00],
    "06_Reentry":      [0.24, 0.32, 0.62, 0.20, 0.10, 0.52, 0.72],
}
STEMS = ["Pad", "Strings", "Cello", "Horns", "Celeste", "Pizzicato", "Pulse"]


def juce_decode(text):
    size, chars = text.split(".", 1)
    data = bytearray(int(size))
    for i, char in enumerate(chars):
        value, bit = ALPHABET.index(char), i * 6
        j, shift = divmod(bit, 8)
        if j < len(data):
            data[j] |= (value << shift) & 255
        if j + 1 < len(data):
            data[j + 1] |= value >> (8 - shift)
    return bytes(data)


def juce_encode(data):
    chars = []
    for bit in range(0, len(data) * 8, 6):
        j, shift = divmod(bit, 8)
        value = data[j] >> shift
        if j + 1 < len(data):
            value |= data[j + 1] << (8 - shift)
        chars.append(ALPHABET[value & 63])
    result = str(len(data)) + "." + "".join(chars)
    assert juce_decode(result) == data
    return result


def instrument(preset_name, articulation=None):
    """Use a factory NKS preset inside the plugin's own VST3 state container."""
    plugin_path = VST / "BBC Symphony Orchestra (64 Bit).vst3/Contents/x86_64-win/BBC Symphony Orchestra (64 Bit).vst3"
    plugin = pb.load_plugin(str(plugin_path))
    preset = BBC / "NKS" / (preset_name + ".nksf")
    data = preset.read_bytes()
    pos = data.index(b"PCHK")
    size = struct.unpack_from("<I", data, pos + 4)[0]
    state = data[pos + 12:pos + 8 + size]
    assert state[:4] == b"VC2!"
    if articulation:
        xml = ET.fromstring(state[8:].rstrip(b"\x00").replace(b'version="0"', b'version="1.0"', 1))
        artics = xml.find("ARTICS").findall("ARTIC")
        selected = None
        for i, artic in enumerate(artics):
            settings = {s.get("id"): s for s in artic.findall("SETTING")}
            active = articulation.lower() in settings["a_name"].get("value").lower()
            settings["a_active"].set("value", "2" if active else "0")
            if active:
                selected = i
        assert selected is not None, (preset_name, articulation)
        for setting in xml.find("ARTICS").findall("SETTING"):
            if setting.get("id") == "p_lastSelectedPrimaryArtic":
                setting.set("value", str(selected))
        body = ET.tostring(xml, encoding="utf-8")
        state = b"VC2!" + struct.pack("<I", len(body)) + body + b"\x00"
    root = ET.fromstring(plugin.raw_state[8:].rstrip(b"\x00"))
    component = juce_decode(root.find("IComponent").text)
    start = component.index(b"VC2!")
    component = bytearray(component[:start] + state)
    struct.pack_into(">I", component, 20, len(component) - 24)
    struct.pack_into(">I", component, start - 4, len(state))
    root.find("IComponent").text = juce_encode(component)
    body = ET.tostring(root, encoding="utf-8")
    plugin.raw_state = b"VC2!" + struct.pack("<I", len(body)) + body + b"\x00"
    plugin.dynamics, plugin.expression, plugin.reverb = 52, 85, 12
    return plugin


def events(notes, cycles=1):
    messages = []
    for cycle in range(cycles):
        for beat, duration, pitch, velocity in notes:
            t = cycle * LENGTH + beat * BEAT
            messages.extend([mido.Message("note_on", note=pitch, velocity=velocity, time=t),
                             mido.Message("note_off", note=pitch, time=t + duration * BEAT)])
    return sorted(messages, key=lambda m: (m.time, m.type == "note_on"))


def midi_file(name, notes):
    mid = mido.MidiFile(ticks_per_beat=960)
    track = mido.MidiTrack()
    mid.tracks.append(track)
    track.append(mido.MetaMessage("set_tempo", tempo=mido.bpm2tempo(BPM)))
    last = 0
    for message in events(notes):
        tick = round(message.time / BEAT * 960)
        track.append(message.copy(time=tick - last))
        last = tick
    mid.save(OUT / "MIDI" / (name + ".mid"))


def filtered(x, cutoff, kind="lowpass"):
    return signal.sosfiltfilt(signal.butter(3, cutoff, btype=kind, fs=SR, output="sos"), x, axis=-1).astype(np.float32)


def write_audio(relative, audio, loop=False):
    path = OUT / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    audio = np.asarray(audio, dtype=np.float32)
    assert audio.ndim == 2 and audio.shape[0] in (1, 2)
    assert np.isfinite(audio).all()
    assert 0.0001 < np.max(np.abs(audio)) < 0.98, relative
    sf.write(path, audio.T, SR, subtype="PCM_24")
    REPORT[relative] = {"seconds": round(audio.shape[1] / SR, 6), "channels": audio.shape[0],
                        "peak_dbfs": round(20 * math.log10(float(np.max(np.abs(audio)))), 2),
                        "loop": loop, "frames": audio.shape[1]}


def alternative_score(suite):
    if suite in SCORES:
        return compose(suite)
    # Separate harmony, phrasing and rhythmic density, while retaining the shared seven-layer contract.
    chords = {
        "FarHorizon": [(50, 57, 62, 66, 69), (45, 52, 59, 64, 69),
                       (49, 56, 59, 64, 68), (42, 54, 57, 61, 68),
                       (43, 55, 59, 62, 66), (40, 52, 55, 59, 66),
                       (50, 57, 62, 64, 69), (45, 57, 59, 64, 66)],
        "NightDrift": [(50, 57, 60, 64, 65), (46, 53, 57, 60, 65),
                       (41, 53, 57, 60, 67), (48, 55, 60, 62, 67),
                       (43, 50, 57, 58, 62), (45, 57, 60, 62, 65),
                       (46, 53, 55, 60, 65), (45, 52, 57, 62, 64)]
    }[suite]
    notes = {name: [] for name in STEMS}
    night = suite == "NightDrift"
    for c, chord in enumerate(chords):
        start = c * 16
        for pitch in chord[1:]:
            notes["Pad"].append((start, 15.7, pitch, 38 if night else 46))
        for pitch in chord[2:4]:
            notes["Strings"].append((start + .15, 11.5 if night else 15.2, pitch + 12, 42 if night else 53))
        notes["Cello"].append((start, 15 if night else 7.5, chord[0], 46))
        if not night:
            notes["Cello"].append((start + 8, 7.4, chord[1] - 12, 50))
        # A question and answer, with room between phrases rather than continuous ostinato.
        phrase = [(2, chord[2], 5.5), (10, chord[3], 3.2)] if night else [
            (0, chord[2], 2.7), (3, chord[3], 2.7), (7, chord[4], 4.5), (13, chord[3], 2.4)]
        if not night or c % 2 == 1:
            for offset, pitch, duration in phrase:
                notes["Horns"].append((start + offset, duration, pitch, 45 if night else 57))
        bells = [(3.5, chord[3] + 24, 3), (12, chord[2] + 24, 2)] if night else [
            (.5, chord[4] + 12, 1.8), (4.5, chord[2] + 24, 2), (9, chord[3] + 24, 2.5)]
        for offset, pitch, duration in bells:
            notes["Celeste"].append((start + offset, duration, pitch, 38 if night else 49))
        offsets = [0, 3, 8, 11] if night else [0, 1.5, 3, 4, 5.5, 7, 8, 9.5, 11, 12, 13.5, 15]
        for i, offset in enumerate(offsets):
            notes["Pizzicato"].append((start + offset, .42, chord[1 + (i % 4)] + 12, 40 if night else 52 + i % 3 * 4))
    assert all(notes[name] for name in STEMS if name != "Pulse")
    assert all(0 <= beat < BARS * 4 and 0 < duration and beat + duration <= BARS * 4
               and 0 <= pitch <= 127 and 0 < velocity <= 127
               for track in notes.values() for beat, duration, pitch, velocity in track)
    return notes


def music(suite="FirstLight"):
    for folder in ["MIDI", "Presets", "Stems", "Music"]:
        (OUT / folder).mkdir(parents=True, exist_ok=True)
    notes = {name: [] for name in STEMS}
    for c, chord in enumerate(CHORDS):
        start = c * 16
        for pitch in chord[1:]:
            notes["Pad"].append((start, 15.8, pitch, 45))
        for pitch in chord[2:]:
            notes["Strings"].append((start + 0.06, 15.6, pitch + 12, 52))
        notes["Cello"].append((start, 7.7, chord[0], 55))
        notes["Cello"].append((start + 8, 7.7, chord[0] + 7, 49))
        for offset, pitch, duration in [(0, chord[2], 3.5), (4, chord[4], 1.5),
                                       (6, chord[1] + 12, 5.4), (12, chord[3], 3.2)]:
            notes["Horns"].append((start + offset + 0.04, duration, pitch, 62 if offset == 6 else 54))
        bell = [chord[1] + 24, chord[3] + 24, chord[2] + 24, chord[4] + 12]
        for i, offset in enumerate([0.25, 3.5, 7, 11.5]):
            notes["Celeste"].append((start + offset, 1.8 + (i % 2), bell[i], 47 + i * 3))
        for step in range(32):
            pitch = chord[1 + [0, 2, 1, 3, 2, 1, 3, 1][step % 8]] + 12
            notes["Pizzicato"].append((start + step * 0.5 + (0.018 if step % 2 else 0), 0.30, pitch, 49 + step % 4 * 5))
    if suite != "FirstLight":
        notes = alternative_score(suite)
    presets = {
        "Strings": ("BBCSO_Discover_Strings_Violins_1", "Long"),
        "Cello": ("BBCSO_Discover_Strings_Celli", "Long"),
        "Horns": ("BBCSO_Discover_Brass_Horns_a4", "Long"),
        "Celeste": ("BBCSO_Discover_Percussion_Harp_and_Celeste", "Celeste"),
        "Pizzicato": ("BBCSO_Discover_Strings_Violins_1", "Pizzicato"),
    }
    if suite == "FarHorizon":
        presets["Horns"] = ("BBCSO_Discover_Woodwinds_Oboes_a3", "Long")
    elif suite == "NightDrift":
        presets["Strings"] = ("BBCSO_Discover_Strings_Violas", "Long")
    config = SCORES.get(suite, {})
    if config.get("lead"):
        presets["Horns"] = ("BBCSO_Discover_" + config["lead"], "Long")
    if config.get("strings"):
        presets["Strings"] = ("BBCSO_Discover_Strings_" + config["strings"], "Long")
    if config.get("harp"):
        presets["Celeste"] = ("BBCSO_Discover_Percussion_Harp_and_Celeste", "Harp")
    gains = [0.15, 0.14, 0.13, 0.15, 0.09, 0.11, 0.09]
    for index, name in enumerate(STEMS):
        print("Rendering", name, flush=True)
        if name == "Pulse":
            x = np.zeros((2, FRAMES), dtype=np.float32)
            pulse_beats = range(BARS * 4) if suite == "FirstLight" else range(0, BARS * 4, 4 if suite == "NightDrift" else 2)
            if config:
                pulse_beats = [bar + offset for bar in range(0, BARS * 4, 8) for offset in config["pulse"]]
            for beat in pulse_beats:
                t = np.arange(round(SR * 0.7)) / SR
                hit = (np.sin(2 * np.pi * (48 * t + 7 * (1 - np.exp(-12 * t)))) * np.exp(-8 * t)
                       + RNG.normal(0, .06, len(t)) * np.exp(-35 * t))
                start = round(beat * BEAT * SR)
                count = min(len(hit), FRAMES - start)
                x[:, start:start + count] += hit[:count] * (1 if beat % 4 == 0 else .48)
        else:
            if name == "Pad":
                plugin = pb.load_plugin(str(VST / "Vital.vst3"))
                # Vital exposes envelope times as normalized, nonlinear parameters.
                plugin.parameters["envelope_1_attack"].raw_value = 0.56
                plugin.parameters["envelope_1_release"].raw_value = 0.62
                plugin.oscillator_1_unison_voices = config.get("pad_voices", "5v")
                plugin.oscillator_1_unison_detune = 5
                plugin.filter_1_switch = True
                plugin.parameters["filter_1_cutoff"].raw_value = config.get("pad_cutoff", .47)
                plugin.filter_1_resonance = 12
                plugin.volume = -14
            else:
                plugin = instrument(*presets[name])
            (OUT / "Presets" / (name + ".vst3state")).write_bytes(plugin.raw_state)
            midi_file(name, notes[name])
            # A warm-up cycle supplies the release/reverb tails at the loop start.
            audio = plugin(events(notes[name], 2), duration=LENGTH * 2 + 12, sample_rate=SR, buffer_size=4096)
            if name in ("Pad", "Celeste"):
                reverb = pb.load_plugin(str(VST / "ValhallaSupermassive.vst3"))
                reverb.mix, reverb.feedback, reverb.density = (44 if name == "Pad" else 32), 68, 80
                reverb.delay_ms, reverb.delaywarp = 540, 58
                reverb.highcut, reverb.lowcut = 5800, 180
                (OUT / "Presets" / (name + "_Supermassive.vst3state")).write_bytes(reverb.raw_state)
                audio = reverb(audio, SR)
            x = audio[:, FRAMES:FRAMES * 2].copy()
            del audio, plugin
        x = filtered(x, 30, "highpass")
        rms = float(np.sqrt(np.mean(x ** 2)))
        assert rms > 1e-6, name + " instrument produced silence"
        x *= min(gains[index] / max(float(np.max(np.abs(x))), 1e-6), 0.047 / rms)
        # Very short circular splice removes sample-level discontinuities, retaining all 32 bars.
        x = circular_splice(x, 0.012)
        write_audio("Stems/" + name + ".wav", x, True)
    stems = [sf.read(OUT / "Stems" / (n + ".wav"), dtype="float32", always_2d=True)[0].T for n in STEMS]
    meter = loudness.Meter(SR)
    mixes = [sum(x * gain for x, gain in zip(stems, weights)) for weights in PHASES.values()]
    # One shared master gain preserves the relative dynamics and exact stem blend relationships.
    gain = min(10 ** ((-18 - meter.integrated_loudness(mixes[0].T)) / 20),
               0.79 / max(float(np.max(np.abs(x))) for x in mixes))
    for name, mix in zip(PHASES, mixes):
        mix *= gain
        write_audio("Music/" + name + ".wav", mix, True)
        REPORT["Music/" + name + ".wav"]["lufs_i"] = round(meter.integrated_loudness(mix.T), 2)
    (OUT / "music.json").write_text(json.dumps({"bpm": BPM, "bars": BARS, "beats_per_bar": 4,
        "frames": FRAMES, "sample_rate": SR, "master_gain": gain, "stems": STEMS,
        "phase_gains": PHASES, "suite": suite}, indent=2) + "\n")
    audition(mixes)
    reaper_project(gain if config else .8)
    if config:
        # FMOD working copies include the measured gain, as in the earlier alternative scores.
        for name, stem in zip(STEMS, stems):
            calibrated = stem * gain
            assert np.isfinite(calibrated).all() and np.max(np.abs(calibrated)) < .98
            sf.write(ROOT / "Art/Audio/FMOD/Assets" / (suite + "_" + name + ".wav"), calibrated.T, SR, subtype="PCM_24")


def audition(mixes):
    order = [0, 1, 2, 3, 5, 4]
    section = round(8 * 4 * BEAT * SR)
    fade = round(2 * 4 * BEAT * SR)
    demo = np.zeros((2, section * len(order)), dtype=np.float32)
    timeline = np.arange(demo.shape[1]) % FRAMES
    for i, phase in enumerate(order):
        start, end = i * section, (i + 1) * section
        demo[:, start:end] = mixes[phase][:, timeline[start:end]]
        if i:
            blend = np.linspace(0, 1, fade, dtype=np.float32)[None, :]
            old = mixes[order[i - 1]][:, timeline[start:start + fade]]
            demo[:, start:start + fade] = old * (1 - blend) + demo[:, start:start + fade] * blend
    demo[:, :SR * 2] *= np.linspace(0, 1, SR * 2)
    demo[:, -SR * 5:] *= np.linspace(1, 0, SR * 5)
    write_audio(OUT.name + "_Audition.wav", demo)


def circular_splice(x, seconds):
    x = x.copy()
    n = round(seconds * SR)
    # Match boundary samples without adding silence or changing duration.
    delta = x[:, -1:] - x[:, :1]
    x[:, :n] += delta * np.linspace(1, 0, n, dtype=np.float32)[None, :]
    return x


def source(fragment, seconds, speed=1, start=0):
    matches = list(LIBRARY.rglob(fragment))
    assert len(matches) == 1, (fragment, len(matches))
    path = matches[0]
    with sf.SoundFile(path) as f:
        f.seek(min(round(start * f.samplerate), max(0, len(f) - 1)))
        x = f.read(round(seconds * speed * f.samplerate) + 4096, dtype="float32", always_2d=True).T
        rate = f.samplerate
    target = round(SR / speed)
    divisor = math.gcd(target, rate)
    x = signal.resample_poly(x, target // divisor, rate // divisor, axis=-1)
    x = np.mean(x, axis=0, keepdims=True)
    n = round(seconds * SR)
    x = np.tile(x, (1, math.ceil(n / x.shape[1])))[:, :n]
    x -= np.mean(x)
    x /= max(float(np.max(np.abs(x))), 1e-6)
    SOURCE_FILES.add(str(path.relative_to(LIBRARY)))
    return x.astype(np.float32)


SOURCE_FILES = set()


def sfx():
    (OUT / "SFX").mkdir(parents=True, exist_ok=True)
    n = 12
    boiler = source("23 Industrial Gas*near_PAN C.wav", n)
    jet = source("Jet,Fighter,F-22,Raptor,By,Slow,Crackle.wav", n, .70, 3)
    rumble = source("Spaces - Deep Space Rumble_01.wav", n, .82)
    air = source("MECHHydr_Hydraulic Pressure Releasing_JSE_IR_01_Stereo.wav", n)
    metal = source("METLMvmt_Metal Rattle 11_JSE_MM_Mono.wav", 4, .77)
    blast = source("Bluezone_BC0277_explosion_mortar_002_01.wav", 5, .82)
    t = np.arange(SR * n) / SR
    sub = (np.sin(2 * np.pi * 42 * t) + .42 * np.sin(2 * np.pi * 63 * t))[None, :] * .18
    low = filtered(boiler, 450) * .65 + filtered(rumble, 160) * .45 + sub
    high = filtered(jet, 6500) * .62 + filtered(boiler, 1800) * .30 + sub
    wind = filtered(RNG.normal(0, .25, (1, SR * n)), 3600)
    wind *= .65 + .18 * np.sin(2 * np.pi * .31 * t)[None, :]
    heat = filtered(jet, 4400) * .5 + filtered(boiler, 2200) * .2 + wind * .45
    loops = {"Engine_Low_Loop": low, "Engine_High_Loop": high,
             "Atmosphere_Wind_Loop": wind, "Reentry_Plasma_Loop": heat}
    for name, x in loops.items():
        x = circular_splice(filtered(x, 28, "highpass"), .080)
        x *= .53 / np.max(np.abs(x))
        write_audio("SFX/" + name + ".wav", x, True)

    def shot(name, x, seconds, attack=.006, release=.16):
        x = x[:, :round(seconds * SR)].copy()
        a, r = min(round(attack * SR), x.shape[1]), min(round(release * SR), x.shape[1])
        x[:, :a] *= np.linspace(0, 1, a)
        x[:, -r:] *= np.linspace(1, 0, r)
        x = filtered(x, 28, "highpass")
        x *= .67 / max(np.max(np.abs(x)), 1e-6)
        write_audio("SFX/" + name + ".wav", x)

    shot("Engine_Ignition", high[:, :SR * 4] * np.linspace(0, 1, SR * 4) + blast[:, :SR * 4] * .35, 4, .018, .55)
    shot("Engine_Shutdown", low[:, :SR * 2] * np.linspace(1, 0, SR * 2) ** 1.5 + air[:, :SR * 2] * .16, 2)
    burst = air[:, :round(SR * .4)] + wind[:, :round(SR * .4)] * .15
    shot("RCS_Burst", filtered(burst, 650, "highpass"), .4, .003, .12)
    for i in range(3):
        shifted = np.roll(metal, i * 1201, axis=-1)
        x = filtered(blast[:, :SR * 3], 1700) * .4 + shifted[:, :SR * 3] * .42
        x *= np.exp(-np.arange(SR * 3)[None, :] / SR * 1.8)
        shot("Stage_Separation_0" + str(i + 1), x, 3)
    landing = metal[:, :SR * 2] * .25 + filtered(blast[:, :SR * 2], 380) * .5
    shot("Touchdown", landing * np.exp(-np.arange(SR * 2)[None, :] / SR * 3), 2)
    shot("Explosion", blast + np.pad(metal * .2, ((0, 0), (0, SR))), 5, .002, .8)
    chute = source("Parachutist - EXT - PARACHUTE DEPLOY - Distant 3 - RSM191.wav", 3)
    shot("Parachute_Deploy", chute, 3)
    (OUT / "sources.json").write_text(json.dumps({"library_root": str(LIBRARY),
        "processed_recordings": sorted(SOURCE_FILES),
        "notice": "Sonniss recordings remain copyrighted by their respective owners. For use incorporated in the game, not as a standalone sound library."}, indent=2) + "\n")
    reel = []
    for path in sorted((OUT / "SFX").glob("*.wav")):
        x, _ = sf.read(path, dtype="float32", always_2d=True)
        x = x[:SR * 4].T * .6
        x[:, -round(.08 * SR):] *= np.linspace(1, 0, round(.08 * SR))
        reel.extend([np.repeat(x, 2, axis=0), np.zeros((2, SR), dtype=np.float32)])
    write_audio("SFX_Audition.wav", np.concatenate(reel, axis=-1))


def reaper_project(master_gain=.8):
    lines = ['<REAPER_PROJECT 0.1 7.0 1', f'  TEMPO {BPM}', f'  SAMPLERATE {SR}', f'  MASTER_VOLUME {master_gain}']
    for name, gain in zip(STEMS, PHASES["01_Launch"]):
        lines += ['  <TRACK', f'    NAME "{name}"', f'    VOLPAN {gain} 0',
                  '    <ITEM', '      POSITION 0', f'      LENGTH {LENGTH}', f'      NAME "{name}.wav"',
                  '      <SOURCE WAVE', f'        FILE "Stems/{name}.wav"', '      >', '    >', '  >']
    lines += ['>']
    (OUT / (OUT.name + "_Stems.rpp")).write_text("\n".join(lines) + "\n")


def verify():
    results = {}
    for path in sorted(OUT.rglob("*.wav")):
        x, rate = sf.read(path, dtype="float32", always_2d=True)
        assert rate == SR and np.isfinite(x).all(), path
        peak = float(np.max(np.abs(x)))
        assert .0001 < peak < .98, (path, peak)
        is_loop = path.parent.name in ("Music", "Stems") or "Loop" in path.stem
        if is_loop:
            assert np.max(np.abs(x[0] - x[-1])) < .00001, path
        if path.parent.name in ("Music", "Stems"):
            assert len(x) == FRAMES and x.shape[1] == 2, path
        results[str(path.relative_to(OUT))] = {"frames": len(x), "peak": round(peak, 5),
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest(), "loop": is_loop}
    sfx_count = len(list((OUT / "SFX").glob("*.wav")))
    assert len(results) == (28 if OUT.name == "FirstLight" else 14), len(results)
    assert {p.stem for p in (OUT / "Stems").glob("*.wav")} == set(STEMS)
    assert {p.stem for p in (OUT / "Music").glob("*.wav")} == set(PHASES)
    assert sfx_count == (13 if OUT.name == "FirstLight" else 0)
    (OUT / "verification.json").write_text(json.dumps(results, indent=2) + "\n")
    print("Verified", len(results), "WAV files: format, signal, headroom, loop boundaries and matching stem lengths.", flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", nargs="?", default="all", choices=("music", "sfx", "verify", "all"))
    parser.add_argument("--suite", choices=("FirstLight", "FarHorizon", "NightDrift", *SCORES), default="FirstLight")
    args = parser.parse_args()
    action = args.action
    if args.suite != "FirstLight" and action not in ("music", "verify"):
        parser.error("Alternative suites support music and verify; shared SFX stay in FirstLight.")
    OUT = ROOT / "Art/Audio" / args.suite
    suite_names = ["FirstLight", "FarHorizon", "NightDrift", *SCORES]
    RNG = np.random.default_rng(141026 + suite_names.index(args.suite))
    OUT.mkdir(parents=True, exist_ok=True)
    if action in ("all", "music"):
        music(args.suite)
    if action in ("all", "sfx"):
        sfx()
    if REPORT:
        report_path = OUT / "render-report.json"
        old = json.loads(report_path.read_text()) if report_path.exists() else {}
        old.update(REPORT)
        report_path.write_text(json.dumps(old, indent=2) + "\n")
    if action in ("all", "verify"):
        verify()
