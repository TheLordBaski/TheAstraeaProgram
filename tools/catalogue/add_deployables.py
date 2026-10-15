#!/usr/bin/env python3
"""
PRT-03 one-off (2026-10-16): gives the placeholder parts of the catalogue that unfold, extend or open their `deployable` data
(Docs/DEPLOYABLES.md). The transform names are what PRT-02's models (and any hand-made model) must provide. parts.json is the source of truth
afterwards; this only reproduces the first pass.

    python tools/catalogue/add_deployables.py
"""
import json
import os
from collections import OrderedDict

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
PARTS = os.path.join(ROOT, 'Assets', 'TAP', 'Resources', 'Data', 'parts.json')


def rot(name, to, start=0.0, end=1.0, curve='smooth', frm=None):
    return motion(name, 'rotate', to, start, end, curve, frm)


def slide(name, to, start=0.0, end=1.0, curve='smooth'):
    return motion(name, 'translate', to, start, end, curve, None)


def grow(name, frm, to, start=0.0, end=1.0, curve='smooth'):
    return motion(name, 'scale', to, start, end, curve, frm)


def motion(name, kind, to, start, end, curve, frm):
    m = OrderedDict(transform=name, kind=kind)
    if frm is not None:
        m['from'] = frm
    m['to'] = to
    m['curve'] = curve
    m['start'] = start
    m['end'] = end
    return m


def dep(motions, extend=4.0, retract=3.0, ext_label='Deploy', ret_label='Retract', brk=0, imp=0, ec=0.0, start='retracted'):
    d = OrderedDict()
    d['extendLabel'] = ext_label
    d['retractLabel'] = ret_label
    d['extendTime'] = extend
    d['retractTime'] = retract
    if start != 'retracted':
        d['startState'] = start
    if brk:
        d['breakPressure'] = brk
    if imp:
        d['breakImpact'] = imp
    if ec:
        d['ecPerSecond'] = ec
    d['motions'] = motions
    return d


DEPLOYABLES = {
    # power and heat: panels fold out in two stages, and are the first thing the air tears off
    'solar_fold': dep([rot('WingA', [0, 0, -90], 0.0, 0.6), rot('WingB', [0, 0, 90], 0.4, 1.0)], 6, 5, 'Deploy panel', 'Retract panel', 15000, 6),
    'solar_wing': dep([rot('Arm', [0, 0, -90], 0.0, 0.4), rot('WingA', [0, 0, -180], 0.3, 0.7), rot('WingB', [0, 0, 180], 0.6, 1.0)], 10, 8, 'Deploy wing', 'Retract wing', 10000, 5),
    'solar_tracker': dep([slide('Mast', [0, 1.2, 0], 0.0, 0.4), rot('ArrayA', [0, 0, -90], 0.3, 1.0), rot('ArrayB', [0, 0, 90], 0.3, 1.0)], 14, 12, 'Deploy array', 'Retract array', 6000, 4),
    'radiator_fold': dep([rot('PanelA', [0, 0, -90], 0.0, 0.7), rot('PanelB', [0, 0, 90], 0.3, 1.0)], 6, 5, 'Deploy radiator', 'Retract radiator', 12000, 6),
    # communications
    'antenna_dish': dep([slide('Boom', [0, 0.35, 0], 0.0, 0.5), rot('Dish', [-90, 0, 0], 0.4, 1.0)], 5, 4, 'Extend dish', 'Retract dish', 20000, 6),
    'antenna_highgain': dep([slide('Mast', [0, 0.6, 0], 0.0, 0.5), grow('Dish', [0.1, 0.1, 0.1], [1, 1, 1], 0.4, 1.0)], 12, 10, 'Extend dish', 'Retract dish', 8000, 5),
    # structure and ground
    'ladder_tele': dep([grow('Ladder', [1, 0.2, 1], [1, 1, 1], 0.0, 1.0, 'linear')], 3, 3, 'Extend ladder', 'Retract ladder', 25000, 8),
    'bay_s1': dep([rot('DoorL', [0, 0, -110], 0.0, 1.0, 'easeOut'), rot('DoorR', [0, 0, 110], 0.0, 1.0, 'easeOut')], 2.5, 2.5, 'Open bay', 'Close bay', 30000, 8),
    'bay_s2': dep([rot('DoorL', [0, 0, -110], 0.0, 1.0, 'easeOut'), rot('DoorR', [0, 0, 110], 0.0, 1.0, 'easeOut')], 3.5, 3.5, 'Open bay', 'Close bay', 30000, 8),
    'airbrake': dep([rot('Flap', [0, 0, -80], 0.0, 1.0, 'easeOut')], 1.5, 1.5, 'Open brake', 'Close brake', 80000, 8),
    'dock_s1_inline': dep([rot('Cover', [0, 0, -120], 0.0, 1.0, 'smooth')], 2, 2, 'Open cover', 'Close cover', 40000, 0),
    'shield_inflatable': dep([grow('Bloom', [0.35, 0.2, 0.35], [1, 1, 1], 0.0, 1.0, 'easeOut')], 4, 0, 'Inflate', 'Deflate', 40000, 0),
    # surface and survey
    'drill_s': dep([rot('Arm', [0, 0, -100], 0.0, 0.5), slide('Bit', [0, -0.4, 0], 0.5, 1.0)], 6, 5, 'Lower drill', 'Raise drill', 15000, 6, 0.3),
    'drill_l': dep([rot('Arm', [0, 0, -100], 0.0, 0.5), slide('Bit', [0, -0.7, 0], 0.5, 1.0)], 9, 8, 'Lower drill', 'Raise drill', 15000, 6, 0.6),
    'scanner_orbital': dep([slide('Boom', [0, 0.6, 0], 0.0, 0.6), rot('Head', [-90, 0, 0], 0.5, 1.0)], 6, 5, 'Extend scanner', 'Retract scanner', 20000, 6),
    'scanner_narrow': dep([rot('Cover', [0, 0, -100], 0.0, 1.0)], 2, 2, 'Open cover', 'Close cover', 20000, 6),
}


def fmt(v):
    return json.dumps(v, separators=(', ', ': '))


def fmt_part(p, indent='    '):
    keys = list(p.keys())
    lines = [indent + '{']
    for i, k in enumerate(keys):
        lines.append(f'{indent}  "{k}": {fmt(p[k])}' + (',' if i < len(keys) - 1 else ''))
    lines.append(indent + '}')
    return '\n'.join(lines)


def main():
    cat = json.load(open(PARTS, encoding='utf8'), object_pairs_hook=OrderedDict)
    done = set()
    for p in cat['parts']:
        if p['id'] in DEPLOYABLES:
            p['deployable'] = DEPLOYABLES[p['id']]
            done.add(p['id'])
    assert done == set(DEPLOYABLES), f'unknown parts: {set(DEPLOYABLES) - done}'
    text = ['{', '  "resources": [']
    text += ['    ' + fmt(r) + (',' if i < len(cat['resources']) - 1 else '') for i, r in enumerate(cat['resources'])]
    text += ['  ],', '  "mixes": [']
    text += ['    ' + fmt(m) + (',' if i < len(cat['mixes']) - 1 else '') for i, m in enumerate(cat['mixes'])]
    text += ['  ],', '  "categories": ' + fmt(cat['categories']) + ',', '  "parts": [']
    text += [fmt_part(p) + (',' if i < len(cat['parts']) - 1 else '') for i, p in enumerate(cat['parts'])]
    text += ['  ]', '}']
    with open(PARTS, 'w', encoding='utf8', newline='\n') as f:
        f.write('\n'.join(text) + '\n')
    print(f'{len(done)} parts got a deployable')


if __name__ == '__main__':
    main()
