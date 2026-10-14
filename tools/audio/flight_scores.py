"""Seven additional authored flight suites; the seven names are musical roles."""

SCORES = {
    "Starward": {
        "title": "Starward", "description": "A rising flute theme, open major chords and light, skipping strings.",
        "chords": [(48,55,60,64,67), (45,52,57,60,64), (41,53,57,60,67), (43,55,59,62,69),
                   (40,52,55,59,62), (41,53,57,62,65), (48,55,60,64,69), (43,55,60,62,67)],
        "melody": [(0,2,2.6), (3,3,2.5), (6,4,4.6), (12,3,2.8)], "lead_octave": 12,
        "bells": [(1,2,2), (6.5,4,2.5), (13,3,2)], "bell_octave": 12,
        "rhythm": [0,1.5,3,5,6.5,8,9.5,11,13,14.5], "pattern": [0,2,1,3,2],
        "bass": [(0,7.6,0), (8,7.4,7)], "pulse": [0,3,4,6],
        "lead": "Woodwinds_Flutes_a3", "pad_cutoff": .49, "pad_voices": "3v",
    },
    "BlueSilence": {
        "title": "Blue Silence", "description": "Suspended minor harmony, low viola breaths and isolated harp notes.",
        "chords": [(40,52,55,59,66), (48,55,59,62,67), (43,50,55,59,62), (38,50,57,62,64),
                   (45,52,55,59,64), (40,52,57,59,62), (48,55,59,64,67), (47,54,59,64,66)],
        "melody": [(3,2,6), (12,3,2.5)], "lead_bars": [1,3,5,7],
        "bells": [(2.5,3,4), (11,4,3)], "bell_octave": 12, "harp": True,
        "rhythm": [1,6,10,14], "pattern": [0,2,3,1],
        "bass": [(0,14.8,0)], "pulse": [0], "strings": "Violas", "strings_octave": 0,
        "string_gate": 10.5, "pad_gate": 15.3, "lead": "Woodwinds_Clarinets_a3", "pad_cutoff": .35,
    },
    "GravityWell": {
        "title": "Gravity Well", "description": "Descending brass answers, dark minor colour and a restrained uneven pulse.",
        "chords": [(38,50,53,57,60), (39,51,55,58,62), (46,53,57,60,65), (43,50,55,58,62),
                   (38,50,57,60,65), (48,55,58,62,67), (46,53,58,60,65), (45,52,57,58,64)],
        "melody": [(1,4,3.5), (5,3,2.5), (9,2,5.5)],
        "bells": [(3,3,2), (10.5,2,3)], "bell_octave": 24,
        "rhythm": [0,1,3,4,5,7,8,9,11,12,13,15], "pattern": [2,1,0,1],
        "bass": [(0,5.6,0), (6,3.5,7), (10,5.5,0)], "pulse": [0,1.5,3,4,6.5],
        "strings": "Violas", "strings_octave": 0, "pad_cutoff": .38,
    },
    "DawnPatrol": {
        "title": "Dawn Patrol", "description": "Dotted horn calls, a walking low-string line and a firm launch rhythm.",
        "chords": [(43,55,59,62,67), (38,50,57,62,66), (40,52,55,59,64), (48,55,60,64,67),
                   (45,52,57,60,64), (47,54,59,62,66), (48,55,60,62,67), (38,50,57,60,66)],
        "melody": [(0,2,1.3), (1.5,3,2), (4,4,3.4), (8,3,1.3), (9.5,2,2), (12,3,3)],
        "bells": [(.75,4,1.5), (7.5,3,2), (14,2,1.5)], "bell_octave": 12,
        "rhythm": [0,1,2.5,4,5,6.5,8,9,10.5,12,13,14.5], "pattern": [0,1,3,2],
        "bass": [(0,3.6,0), (4,3.6,7), (8,3.6,12), (12,3.6,7)], "pulse": [0,2,3.5,4,6],
        "pad_cutoff": .46,
    },
    "AmberSkies": {
        "title": "Amber Skies", "description": "Warm F-major strings, harp responses and a gently swaying accompaniment.",
        "chords": [(41,53,57,60,67), (38,50,57,60,65), (46,53,57,62,65), (48,55,60,64,67),
                   (43,50,55,58,65), (45,52,57,60,64), (46,53,58,60,65), (48,55,60,62,67)],
        "melody": [(.5,2,4.8), (6,3,3.4), (11,4,4.3)],
        "bells": [(0,2,2), (4,3,2), (8.5,4,2.5), (13.5,3,1.8)], "bell_octave": 12, "harp": True,
        "rhythm": [0,2,3.5,6,8,10,11.5,14], "pattern": [0,3,1,2],
        "bass": [(0,11.5,0), (12,3.5,7)], "pulse": [0,4.5],
        "lead": "Woodwinds_Oboes_a3", "lead_octave": 12, "pad_cutoff": .43,
    },
    "ClockworkVoyage": {
        "title": "Clockwork Voyage", "description": "Interlocking pizzicato in groups of three, clarinet fragments and a quiet mechanical pulse.",
        "chords": [(47,54,57,61,66), (43,55,59,62,66), (38,50,57,61,66), (45,52,57,59,64),
                   (40,52,55,59,66), (42,54,57,61,64), (43,55,59,61,66), (42,54,58,61,64)],
        "melody": [(2,3,1.7), (4,2,2.6), (9,4,1.7), (11,3,3.7)],
        "bells": [(1.5,2,2), (5.5,3,2), (10,4,2.5), (14,2,1.5)], "bell_octave": 12,
        "rhythm": [0,.75,1.5,3,3.75,4.5,6,6.75,8,8.75,9.5,11,11.75,12.5,14,14.75],
        "pattern": [0,2,1,3,1,2], "bass": [(0,7.2,0), (8,7.2,7)], "pulse": [0,1.5,3,5,6.5],
        "lead": "Woodwinds_Clarinets_a3", "string_gate": 12.5, "pad_cutoff": .40,
    },
    "Homecoming": {
        "title": "Homecoming", "description": "Broad resolving strings and horn phrases, with long rests and soft celeste.",
        "chords": [(42,54,57,62,66), (40,52,55,59,66), (43,55,59,62,69), (45,52,57,61,66),
                   (47,54,57,62,66), (43,55,59,62,64), (50,57,62,64,66), (45,57,62,64,69)],
        "melody": [(0,2,5.5), (6,3,3.5), (10,4,5.5)], "lead_bars": [0,1,3,4,6,7],
        "bells": [(2,3,3), (9,2,3)], "bell_octave": 24,
        "rhythm": [0,3.5,8,11.5], "pattern": [0,2,1,3],
        "bass": [(0,15.3,0)], "pulse": [0,4], "strings": "Violins_2", "pad_cutoff": .44,
    },
}


def compose(name):
    config = SCORES[name]
    notes = {n: [] for n in ("Pad", "Strings", "Cello", "Horns", "Celeste", "Pizzicato", "Pulse")}
    for c, chord in enumerate(config["chords"]):
        start = c * 16
        # Four-bar harmony, with small breaths and staggered bow entries.
        for i, pitch in enumerate(chord[1:]):
            notes["Pad"].append((start + i * .025, config.get("pad_gate", 15.5), pitch, 42 + c % 3 * 3))
        for i, pitch in enumerate(chord[2:]):
            notes["Strings"].append((start + .12 + i * .035, config.get("string_gate", 15.1),
                                     pitch + config.get("strings_octave", 12), 45 + c % 4 * 3))
        for offset, duration, interval in config["bass"]:
            notes["Cello"].append((start + offset, duration, chord[0] + interval, 48 + c % 3 * 3))
        if c in config.get("lead_bars", range(8)):
            for i, (offset, degree, duration) in enumerate(config["melody"]):
                notes["Horns"].append((start + offset + .03, duration, chord[degree] + config.get("lead_octave", 0), 50 + i % 3 * 4))
        for i, (offset, degree, duration) in enumerate(config["bells"]):
            notes["Celeste"].append((start + offset, duration, chord[degree] + config["bell_octave"], 38 + i % 3 * 5))
        for i, offset in enumerate(config["rhythm"]):
            degree = 1 + config["pattern"][(i + c % 2) % len(config["pattern"])]
            notes["Pizzicato"].append((start + offset, .30 if name == "ClockworkVoyage" else .42,
                                       chord[degree] + 12, 42 + i % 4 * 4))
    assert all(notes[n] for n in notes if n != "Pulse")
    assert all(0 <= beat < 128 and 0 < duration and beat + duration <= 128
               and 0 <= pitch <= 127 and 0 < velocity <= 127
               for track in notes.values() for beat, duration, pitch, velocity in track), name
    if config.get("lead") == "Woodwinds_Oboes_a3":
        assert all(58 <= pitch <= 91 for _, _, pitch, _ in notes["Horns"]), "Oboe sample range"
    return notes
