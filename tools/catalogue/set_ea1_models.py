#!/usr/bin/env python3
"""
PRT-02 one-off (2026-10-16): points the EA1 parts of the catalogue (and the parts of the same families) at the new shapes of
PartModelFactory.Shapes.cs, and gives the deployable pieces of the later releases the hierarchy their models will need (a dish is a child of
its boom). parts.json is the source of truth afterwards; this only reproduces that pass.

    python tools/catalogue/set_ea1_models.py
"""
import json
import os
import sys
from collections import OrderedDict

sys.path.insert(0, os.path.dirname(__file__))
from add_deployables import PARTS, fmt, fmt_part  # noqa: E402


def model(type_, d, h, style=None, **kw):
    m = OrderedDict(type=type_, diameter=d, height=h)
    m.update(kw)
    if style:
        m['style'] = style
    return m


MODELS = {
    'pod_mayfly1': model('landercan', 1.25, 1.1, 'pod', bottomDiameter=1.25, topDiameter=1.05),
    'probe_sparrow': model('radialprobe', 0.4, 0.3, 'dark'),
    'mono_s0': model('tank', 0.625, 0.3, 'mono'),
    'mono_s2': model('tank', 2.5, 0.9, 'mono'),
    'srb_spit': model('sepmotor', 0.2, 0.45, 'srb'),
    'sep_s0': model('separator', 0.625, 0.15, 'decoupler'),
    'sep_s1': model('separator', 1.25, 0.25, 'decoupler'),
    'sep_s2': model('separator', 2.5, 0.35, 'decoupler'),
    'sep_s3': model('separator', 3.75, 0.45, 'decoupler'),
    'clamp': model('clamp', 0.8, 1.2, 'dark'),
    'battery_s0': model('disc', 0.625, 0.15, 'battery'),
    'solar_fixed': model('solarpanel', 0.5, 0.5, 'fixed'),
    'solar_fold': model('solarpanel', 0.3, 0.5, 'fold'),
    'antenna_whip': model('antenna', 0.1, 0.7, 'whip'),
    'antenna_dish': model('antenna', 0.5, 0.5, 'dish'),
    'light_spot': model('spotlight', 0.15, 0.2, 'dark'),
    'ladder_fixed': model('ladder', 0.15, 1.5, 'dark'),
    'fairing_s1': model('fairingbase', 1.25, 0.3, 'white'),
    'fairing_s2': model('fairingbase', 2.5, 0.3, 'white'),
    'fairing_s3': model('fairingbase', 3.75, 0.3, 'white'),
    'sci_thermo': model('instrument', 0.25, 0.3, 'boom'),
    'sci_baro': model('instrument', 0.25, 0.3, 'box'),
    'sci_bio': model('instrument', 0.25, 0.3, 'canister'),
    'sci_matrack': model('instrument', 0.25, 0.3, 'rack'),
}

# the bounding cylinder follows the shape: (diameter, height)
SIZES = {'solar_fixed': (0.5, 0.5), 'solar_fold': (0.3, 0.5)}

# a piece that must follow another is its child: "Boom/Dish" turns with the boom it sits on
PATHS = {
    'antenna_dish': {'Dish': 'Boom/Dish'},
    'antenna_highgain': {'Dish': 'Mast/Dish'},
    'solar_wing': {'WingA': 'Arm/WingA', 'WingB': 'Arm/WingB'},
    'solar_tracker': {'ArrayA': 'Mast/ArrayA', 'ArrayB': 'Mast/ArrayB'},
    'drill_s': {'Bit': 'Arm/Bit'},
    'drill_l': {'Bit': 'Arm/Bit'},
    'scanner_orbital': {'Head': 'Boom/Head'},
}

# the dish's boom is a short telescopic mast
SLIDES = {('antenna_dish', 'Boom'): [0, 0.12, 0]}


def main():
    cat = json.load(open(PARTS, encoding='utf8'), object_pairs_hook=OrderedDict)
    seen = set()
    for p in cat['parts']:
        pid = p['id']
        if pid in MODELS:
            p['model'] = MODELS[pid]
            seen.add(pid)
        if pid in SIZES:
            p['diameter'], p['height'] = SIZES[pid]
        for m in (p.get('deployable') or {}).get('motions', []):
            m['transform'] = PATHS.get(pid, {}).get(m['transform'], m['transform'])
            if (pid, m['transform']) in SLIDES:
                m['to'] = SLIDES[(pid, m['transform'])]
    assert seen == set(MODELS), set(MODELS) - seen
    text = ['{', '  "resources": [']
    text += ['    ' + fmt(r) + (',' if i < len(cat['resources']) - 1 else '') for i, r in enumerate(cat['resources'])]
    text += ['  ],', '  "mixes": [']
    text += ['    ' + fmt(m) + (',' if i < len(cat['mixes']) - 1 else '') for i, m in enumerate(cat['mixes'])]
    text += ['  ],', '  "categories": ' + fmt(cat['categories']) + ',', '  "parts": [']
    text += [fmt_part(p) + (',' if i < len(cat['parts']) - 1 else '') for i, p in enumerate(cat['parts'])]
    text += ['  ]', '}']
    with open(PARTS, 'w', encoding='utf8', newline='\n') as f:
        f.write('\n'.join(text) + '\n')
    print(f'{len(seen)} parts got a new shape')


if __name__ == '__main__':
    main()
