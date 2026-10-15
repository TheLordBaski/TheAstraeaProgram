#!/usr/bin/env python3
"""
PRT-01 catalogue generator (2026-10-16). A ONE-OFF: it turned the table of product/plan/05-parts.md into data.

It reads Assets/TAP/Resources/Data/parts.json, tags the 36 parts of the slice (milestone M0, size, role, manufacturer, cost), adds
the 132 parts of the Early Access releases with template stats (hidden until their milestone), and writes parts.json again plus
Data/part_templates.json (milestones, size classes, manufacturers, role bands).

parts.json and part_templates.json are the source of truth from now on: balance them by hand (PRT-11). Run this only to
reproduce the first catalogue (`--out DIR` writes the two files there instead of over the project's) or to start from a spec.

    python tools/catalogue/build_catalogue.py [--out DIR]
"""
import json
import math
import os
import re
import sys
from collections import Counter, OrderedDict

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
DATA = os.path.join(ROOT, 'Assets', 'TAP', 'Resources', 'Data')
G0 = 9.80665

AW, CA, VP, LI, VH, QS = ('Astraea Works', 'Corvid Aerospace', 'Vance Propulsion', 'Lumen Instruments', 'Vantor Heavy Works', 'Quiet Star Labs')

SIZES = OrderedDict([
    ('S0', dict(diameter=0.625, nodeSize=0, breakingForce=[30000, 120000])),
    ('S1', dict(diameter=1.25, nodeSize=1, breakingForce=[100000, 800000])),
    ('S2', dict(diameter=2.5, nodeSize=2, breakingForce=[300000, 1200000])),
    ('S3', dict(diameter=3.75, nodeSize=3, breakingForce=[800000, 3000000])),
    ('radial', dict(diameter=0, nodeSize=-1, breakingForce=[20000, 800000])),
    ('none', dict(diameter=0, nodeSize=-1, breakingForce=[20000, 800000])),
])
BREAK = {'S0': 60000, 'S1': 180000, 'S2': 600000, 'S3': 1400000, 'radial': 30000, 'none': 30000}
NODE = {k: v['nodeSize'] for k, v in SIZES.items() if v['nodeSize'] >= 0}

MANUFACTURERS = [
    (AW, "The agency's own workshop. Plain, sturdy and cheap: tanks, couplings, ladders. Its descriptions say what the part does and nothing else."),
    (CA, "Pods, probes, fins, chutes. Light, clever, a little too optimistic: its parts are the first to be cut to save grams."),
    (VP, "Engines and boosters. Thrust first, manners later: loud, powerful, over-engineered, always a point or two of Isp ahead of the brochure."),
    (LI, "Instruments, panels, antennas, lights. Precise to a fault, fussy about calibration, and the only maker that gives error bars."),
    (VH, "Big tanks, heavy engines, structure. Slow, massive and conservative: nothing leaves the factory that could not survive a worse day."),
    (QS, "Nuclear, ion, power and mining equipment. Experimental and quietly brilliant; the manual is always one revision behind the part."),
]

# role id -> (category, id prefixes, cost per kg, note, limits)
ROLES = OrderedDict()


def role(rid, cat, prefixes, cost, note, **limits):
    lim = OrderedDict((k, v) for k, v in limits.items())
    lim['costPerKg'] = [round(cost * 0.5, 2), round(cost * 2.0, 2)]
    ROLES[rid] = dict(category=cat, idPrefixes=prefixes, cost=cost, note=note, limits=lim)


role('command.pod', 'Command', ['pod_'], 70, 'Crewed command pods and lander cans.', massPerSeat=[250, 1000], dryMass=[400, 4000])
role('command.probe', 'Command', ['probe_'], 300, 'Uncrewed probe cores.', dryMass=[15, 600])
role('command.seat', 'Command', ['seat_'], 120, 'An external seat.', dryMass=[30, 150])
role('command.cabin', 'Command', ['cabin_', 'cupola_', 'lab_'], 90, 'Crew modules, the cupola and the lab.', massPerSeat=[250, 1200])
role('tank.chemical', 'Propellant', ['tank_', 'adapter_', 'coupler_'], 5, 'Tanks of the chemical mix: about 1/9 of the full weight is the tank.', dryFraction=[0.09, 0.14])
role('tank.mono', 'Utility', ['mono_'], 12, 'Monopropellant tanks.', dryFraction=[0.1, 0.3])
role('tank.hydrogen', 'Propellant', ['hydro_'], 15, 'Hydrogen tanks: light fuel in a heavy, insulated shell.', dryFraction=[0.3, 0.8])
role('tank.xenon', 'Propellant', ['xenon_'], 40, 'Xenon tanks for ion engines.', dryFraction=[0.3, 0.7])
role('tank.ore', 'Surface', ['ore_'], 6, 'Ore storage: ore is dense, the tank is a small part of the weight.', dryFraction=[0.01, 0.1])
role('engine.lifter', 'Engines', ['eng_'], 40, 'Lifters that work from the pad up.', twrAsl=[8, 30], ispVac=[290, 335], ispRatio=[0.78, 0.93])
role('engine.vacuum', 'Engines', ['eng_'], 60, 'Upper-stage engines: high Isp in vacuum, poor in air.', twrVac=[8, 25], ispVac=[330, 380], ispRatio=[0.1, 0.5])
role('engine.small', 'Engines', ['eng_'], 60, 'Small restartable engines for probes and landers.', twrVac=[10, 25], ispVac=[300, 340], ispRatio=[0.25, 0.9])
role('engine.radial', 'Engines', ['eng_'], 45, 'Side-mounted engines.', twrVac=[8, 25], ispVac=[290, 330], ispRatio=[0.75, 0.93])
role('engine.aerospike', 'Engines', ['eng_'], 80, 'Altitude-compensating engines: nearly the same Isp at every altitude.', twrVac=[6, 20], ispVac=[320, 360], ispRatio=[0.85, 1.0])
role('engine.nuclear', 'Engines', ['eng_'], 150, 'Nuclear-thermal engines on Hydrogen: high Isp, low thrust.', twrVac=[1, 8], ispVac=[750, 950], ispRatio=[0.3, 1.0])
role('engine.ion', 'Engines', ['eng_'], 400, 'Ion engines on Xenon and electricity: tiny thrust, enormous Isp.', twrVac=[0.1, 3], ispVac=[3000, 5500])
role('engine.attitude', 'Engines', ['eng_'], 90, 'Small attitude and trim thrusters on the chemical mix.', twrVac=[5, 25], ispVac=[250, 320])
role('booster.solid', 'Boosters', ['srb_'], 6, 'Solid boosters.', ispVac=[180, 250], dryFraction=[0.08, 0.3], twrWet=[3, 12])
role('booster.separation', 'Boosters', ['srb_'], 80, 'Tiny solid motors that push stages apart.', dryFraction=[0.3, 0.8], twrWet=[20, 150])
role('control.wheel', 'Utility', ['rw_'], 150, 'Reaction wheels.', torquePerKg=[60, 200])
role('control.rcs', 'Utility', ['rcs_'], 100, 'RCS thruster blocks.', dryMass=[5, 80])
role('structure.decoupler', 'Coupling', ['dec_'], 15, 'Decouplers.', dryMass=[10, 1500])
role('structure.separator', 'Coupling', ['sep_'], 25, 'Separators: both halves separate.', dryMass=[10, 1500])
role('structure.docking', 'Coupling', ['dock_'], 80, 'Docking ports.', dryMass=[20, 400])
role('structure.nose', 'Aero', ['nose_'], 10, 'Nose cones.', dryMass=[5, 600])
role('structure.fairing', 'Aero', ['fairing_'], 12, 'Fairing bases (PRT-04).', dryMass=[40, 900])
role('structure.truss', 'Coupling', ['truss_', 'strut_cube'], 12, 'Trusses and nodes for stations and landers.', dryMass=[5, 200])
role('structure.plate', 'Coupling', ['plate_'], 10, 'Structural plates.', dryMass=[10, 400])
role('structure.adapter', 'Coupling', ['adapter_struct_'], 12, 'Hollow adapters, no tank.', dryMass=[30, 400])
role('structure.bay', 'Coupling', ['bay_'], 20, 'Service bays with doors.', dryMass=[60, 600])
role('structure.clamp', 'Ground', ['clamp'], 8, 'Launch clamps.', dryMass=[200, 1500])
role('structure.strut', 'Coupling', ['strut', 'fuel_line'], 20, 'Two-point parts (FLT-16).', dryMass=[1, 20])
role('power.battery', 'Utility', ['battery_'], 45, 'Batteries.', dryMass=[2, 150])
role('power.solar', 'Utility', ['solar_'], 120, 'Solar panels.', dryMass=[3, 250])
role('power.generator', 'Utility', ['rtg', 'fuelcell'], 200, 'RTGs and fuel cells.', dryMass=[20, 400])
role('power.radiator', 'Utility', ['radiator_'], 60, 'Radiators (FLT-04).', dryMass=[10, 120])
role('comms.antenna', 'Utility', ['antenna_'], 120, 'Antennas.', dryMass=[2, 150])
role('utility.light', 'Utility', ['light_'], 60, 'Lights.', dryMass=[2, 20])
role('utility.ladder', 'Utility', ['ladder_'], 20, 'Ladders for climbing.', dryMass=[4, 40])
role('recovery.chute', 'Recovery', ['chute_'], 35, 'Parachutes.', dryMass=[20, 500])
role('recovery.shield', 'Recovery', ['shield_'], 25, 'Heat shields: about 40% of the weight is the shield.', dryFraction=[0.2, 0.6])
role('recovery.leg', 'Ground', ['leg_'], 30, 'Landing legs.', dryMass=[10, 600])
role('aero.fin', 'Aero', ['fin_', 'canard_', 'airbrake', 'gridfin'], 25, 'Fins, control surfaces and brakes.', dryMass=[10, 250])
role('science.instrument', 'Science', ['sci_'], 400, 'Science instruments.', dryMass=[1, 60])
role('surface.wheel', 'Surface', ['wheel_'], 50, 'Rover wheels.', dryMass=[15, 300])
role('surface.chassis', 'Surface', ['rover_'], 30, 'Rover chassis.', dryMass=[50, 500])
role('surface.drill', 'Surface', ['drill_'], 90, 'Drills.', dryMass=[50, 500])
role('surface.converter', 'Surface', ['isru_'], 120, 'Converters: ore to propellants.', dryMass=[100, 2000])
role('surface.scanner', 'Surface', ['scanner_', 'sensor_'], 300, 'Resource scanners and sensors.', dryMass=[5, 300])
role('surface.grabber', 'Surface', ['grabber'], 100, 'The grabbing unit.', dryMass=[50, 400])
role('special.hidden', 'Hidden', ['eva_', 'flag'], 0, 'The rocketeer and the flag: never in the parts list.')
ROLES['special.hidden']['limits'] = OrderedDict()

MILESTONES = ['M0', 'EA1', 'EA2', 'EA3', 'EA4']

# the 36 parts of the slice: id -> (size, role, manufacturer)
EXISTING = {
    'pod_kestrel': ('S1', 'command.pod', CA), 'pod_albatross': ('S2', 'command.pod', CA), 'probe_wren': ('S1', 'command.probe', CA),
    'tank_s1_short': ('S1', 'tank.chemical', AW), 'tank_s1_med': ('S1', 'tank.chemical', AW), 'tank_s1_long': ('S1', 'tank.chemical', AW),
    'tank_s2_short': ('S2', 'tank.chemical', VH), 'tank_s2_long': ('S2', 'tank.chemical', VH),
    'adapter_s2_s1': ('S1/S2', 'tank.chemical', VH), 'tank_s0_mini': ('S0', 'tank.chemical', CA),
    'eng_hornet': ('S1', 'engine.lifter', VP), 'eng_ember': ('S1', 'engine.vacuum', VP), 'eng_goliath': ('S2', 'engine.lifter', VP),
    'eng_spark': ('S0', 'engine.small', VP), 'srb_pogo': ('S1', 'booster.solid', VP), 'srb_kodiak': ('S1', 'booster.solid', VP),
    'dec_s1': ('S1', 'structure.decoupler', AW), 'dec_s2': ('S2', 'structure.decoupler', AW), 'dec_radial': ('radial', 'structure.decoupler', AW),
    'leg_ls2': ('radial', 'recovery.leg', AW), 'leg_ls5': ('radial', 'recovery.leg', AW),
    'chute_main': ('none', 'recovery.chute', CA), 'chute_drogue': ('none', 'recovery.chute', CA), 'chute_radial': ('radial', 'recovery.chute', CA),
    'shield_s1': ('S1', 'recovery.shield', AW), 'shield_s2': ('S2', 'recovery.shield', VH),
    'battery_radial': ('radial', 'power.battery', AW), 'battery_s1': ('S1', 'power.battery', AW),
    'rw_s1': ('S1', 'control.wheel', AW), 'rw_s2': ('S2', 'control.wheel', VH), 'rcs_quad': ('radial', 'control.rcs', AW),
    'mono_s1': ('S1', 'tank.mono', AW), 'mono_radial': ('radial', 'tank.mono', AW),
    'dock_s1': ('S1', 'structure.docking', AW), 'nose_s1': ('S1', 'structure.nose', CA), 'fin_fs1': ('radial', 'aero.fin', CA),
    'eva_crew': ('none', 'special.hidden', AW), 'flag': ('none', 'special.hidden', AW),
}

# ------------------------------------------------------------------------------------------------------ builders

NEW = []
CATEGORIES_ADDED = ['Science', 'Surface']

FAMILY = {  # maxTemp, skinMaxTemp, impactTolerance, drag (top, bottom, side)
    'tank': (1000, 1300, 6, (0.9, 0.9, 1.0)),
    'engine': (1800, 2000, 7, (0.9, 0.6, 0.9)),
    'srb': (1300, 1600, 7, (0.75, 0.6, 1.0)),
    'pod': (1100, 1600, 14, (0.35, 1.05, 0.8)),
    'probe': (1000, 1300, 12, (0.9, 0.9, 0.9)),
    'struct': (1300, 1600, 9, (0.9, 0.9, 0.9)),
    'radial': (1000, 1300, 8, (0.5, 0.5, 0.6)),
    'aero': (1400, 1700, 8, (0.1, 0.1, 0.05)),
    'nose': (1600, 2200, 8, (0.12, 0.9, 0.75)),
    'recovery': (1100, 1400, 9, (0.6, 0.9, 0.8)),
    'shield': (2600, 3300, 12, (0.9, 1.2, 0.8)),
}


def r3(x):
    return round(x, 3)


def node(nid, y, up, size):
    return OrderedDict(id=nid, pos=[0, r3(y), 0], dir=[0, up, 0], size=size)


def stack_nodes(h, top, bottom):
    return [node('top', h / 2, 1, top), node('bottom', -h / 2, -1, bottom)]


def surface(d, allowed=True, onto=True):
    return OrderedDict(allowed=allowed, onto=onto, pos=[0, 0, r3(-d / 2)], dir=[0, 0, 1])


def radial_surface(d):
    return OrderedDict(allowed=True, onto=False, pos=[0, 0, r3(-d * 0.15)], dir=[0, 0, 1])


def add(pid, title, cat, size, rl, manu, ms, desc, dry, d, h, model, nodes=None, surf=None, family='struct', brk=None, **extra):
    """One new part. `extra` carries modules (engine=..., crew=...), resources, thermal overrides."""
    maxT, skinT, imp, drag = FAMILY[family]
    p = OrderedDict()
    p['id'] = pid
    p['title'] = title
    p['manufacturer'] = manu
    p['category'] = cat
    p['size'] = size
    p['role'] = rl
    p['milestone'] = ms
    cost_rate = ROLES[rl]['cost']
    p['cost'] = int(round(dry * cost_rate))
    p['description'] = re.sub(r'\s*\([A-Z]{3}-\d+\)', '', desc)  # a task id in the notes is not for the player
    p['dryMass'] = dry
    p['diameter'] = d
    p['height'] = h
    p['model'] = model
    p['nodes'] = nodes or []
    if surf is not None:
        p['surfaceAttach'] = surf
    res = extra.pop('resources', None)
    if res:
        p['resources'] = [OrderedDict(id=k, amount=v) for k, v in res]
    p['maxTemp'] = extra.pop('maxTemp', maxT)
    p['skinMaxTemp'] = extra.pop('skinMaxTemp', skinT)
    for k in ('specificHeat', 'skinMassPerArea', 'emissivity'):
        if k in extra:
            p[k] = extra.pop(k)
    p['impactTolerance'] = extra.pop('impactTolerance', imp)
    b = brk or BREAK[size.split('/')[-1]]
    p['breakingForce'] = b
    p['breakingTorque'] = int(b * 0.83)
    if 'crossfeed' in extra:
        p['crossfeed'] = extra.pop('crossfeed')
    p['drag'] = OrderedDict(cdTop=drag[0], cdBottom=drag[1], cdSide=drag[2])
    drag_over = extra.pop('dragOver', None)
    if drag_over:
        p['drag'].update(drag_over)
    for k, v in extra.items():
        p[k] = v
    NEW.append((ms, p))
    return p


def mdl(t, d, h, style='white', **kw):
    m = OrderedDict(type=t, diameter=d, height=h)
    m.update(kw)
    m['style'] = style
    return m


def eng_model(d, h, style='engine', count=None):
    m = OrderedDict(type='engine', diameter=d, mountHeight=r3(h * 0.28), bellTop=r3(d * 0.34), bellBottom=r3(d * 0.82), bellLength=r3(h * 0.72))
    if count:
        m['count'] = count
    m['style'] = style
    return m


def engine(kind, thrust, ispv, ispa, h, gimbal=0.0, resp=2.5, prop='chemical', alt=0.0, color=(1.0, 0.7, 0.35), scale=1.0, heat=0.0, minthr=0.0):
    e = OrderedDict(type=kind, thrustVac=thrust, ispVac=ispv, ispAsl=ispa, gimbalRange=gimbal, throttleResponse=resp, propellant=prop)
    if minthr:
        e['minThrottle'] = minthr
    if alt:
        e['alternator'] = alt
    if heat:
        e['heatProduction'] = heat
    e['nozzlePos'] = [0, r3(-h / 2), 0]
    e['exhaustScale'] = scale
    e['exhaustColor'] = list(color)
    return e


def units(res, n):
    return [(res, n)]


# ---------------------------------------------------------------------------------------------- command and crew
CMD = 'Command'
add('pod_mayfly1', 'Mayfly-1 Lander Can', CMD, 'S1', 'command.pod', CA, 'EA1',
    'Light one-seat lander can for short hops and moon landings. No heat shield: not for reentry.', 520, 1.25, 1.1,
    mdl('capsule', 1.25, 1.1, 'pod', bottomDiameter=1.25, topDiameter=0.8), stack_nodes(1.1, 0, 1), family='pod', skinMaxTemp=1300,
    resources=[('Monoprop', 10), ('Electric', 60)], specificHeat=900, skinMassPerArea=9, emissivity=0.45,
    crew=OrderedDict(seats=1, hatchPos=[0, 0.02, -0.5], hatchNormal=[0, 0.22, -0.975]),
    command=OrderedDict(requiresCrew=True, sas=True, ecPerSecond=0), reactionWheel=OrderedDict(torque=1800, ecPerSecond=0.08))
add('probe_finch', 'Finch Micro Probe', CMD, 'S0', 'command.probe', CA, 'EA1',
    'Tiny probe core with a small battery. Keeps a small craft pointing where it is told, and little else.', 30, 0.625, 0.25,
    mdl('probe', 0.625, 0.25, 'dark'), stack_nodes(0.25, 0, 0), family='probe', resources=units('Electric', 15),
    command=OrderedDict(requiresCrew=False, sas=True, ecPerSecond=0.01), reactionWheel=OrderedDict(torque=150, ecPerSecond=0.02))
add('probe_sparrow', 'Sparrow Radial Probe', CMD, 'radial', 'command.probe', CA, 'EA1',
    'A very light probe core that mounts on the side of a craft or a stage. Stability assist only.', 20, 0.4, 0.3,
    mdl('probe', 0.4, 0.3, 'dark'), None, radial_surface(0.4), family='probe', resources=units('Electric', 10),
    command=OrderedDict(requiresCrew=False, sas=True, ecPerSecond=0.01), reactionWheel=OrderedDict(torque=100, ecPerSecond=0.015))
add('pod_mayfly2', 'Mayfly-2 Lander Can', CMD, 'S2', 'command.pod', CA, 'EA2',
    'Two-seat lander can with wide windows. Built for the Moon and short hops; no heat shield.', 1400, 2.5, 1.4,
    mdl('capsule', 2.5, 1.4, 'pod', bottomDiameter=2.5, topDiameter=1.6), stack_nodes(1.4, 1, 2), family='pod', skinMaxTemp=1300,
    resources=[('Monoprop', 30), ('Electric', 120)], specificHeat=900, skinMassPerArea=9, emissivity=0.45,
    crew=OrderedDict(seats=2, hatchPos=[0, 0.05, -0.93], hatchNormal=[0, 0.29, -0.957]),
    command=OrderedDict(requiresCrew=True, sas=True, ecPerSecond=0), reactionWheel=OrderedDict(torque=6000, ecPerSecond=0.2))
add('probe_starling', 'Starling Advanced Probe', CMD, 'S1', 'command.probe', CA, 'EA2',
    'Probe core with every attitude-hold mode, for craft that fly themselves.', 180, 1.25, 0.35,
    mdl('probe', 1.25, 0.35, 'dark'), stack_nodes(0.35, 1, 1), family='probe', resources=units('Electric', 120),
    command=OrderedDict(requiresCrew=False, sas=True, ecPerSecond=0.03), reactionWheel=OrderedDict(torque=2500, ecPerSecond=0.1))
add('probe_owl', 'Owl Heavy Probe', CMD, 'S2', 'command.probe', CA, 'EA2',
    'Heavy probe core with a strong reaction wheel and a large battery, for big uncrewed stages.', 520, 2.5, 0.5,
    mdl('probe', 2.5, 0.5, 'dark'), stack_nodes(0.5, 2, 2), family='probe', resources=units('Electric', 300),
    command=OrderedDict(requiresCrew=False, sas=True, ecPerSecond=0.05), reactionWheel=OrderedDict(torque=14000, ecPerSecond=0.3))
add('seat_perch', 'Perch External Seat', CMD, 'radial', 'command.seat', CA, 'EA2',
    'An open seat on the side of a craft. A rocketeer rides it in a suit, with no cabin around.', 60, 0.5, 0.6,
    mdl('probe', 0.5, 0.6, 'dark'), None, radial_surface(0.5), family='radial',
    crew=OrderedDict(seats=1, hatchPos=[0, 0.1, -0.3], hatchNormal=[0, 0, -1]))
add('cabin_bunk', 'Bunk Crew Cabin', CMD, 'S1', 'command.cabin', AW, 'EA4',
    'A two-seat cabin with bunks and no controls. Adds seats to a long mission.', 700, 1.25, 1.6,
    mdl('tank', 1.25, 1.6, 'white'), stack_nodes(1.6, 1, 1), surface(1.25, True, True), family='pod', resources=units('Electric', 60),
    crew=OrderedDict(seats=2, hatchPos=[0, 0.05, -0.63], hatchNormal=[0, 0, -1]))
add('cabin_hitch', 'Hitch Crew Module', CMD, 'S2', 'command.cabin', VH, 'EA4',
    'A four-seat crew module with no controls. Roomy, heavy and well insulated.', 1800, 2.5, 2.4,
    mdl('tank', 2.5, 2.4, 'white'), stack_nodes(2.4, 2, 2), surface(2.5, True, True), family='pod', resources=units('Electric', 120),
    crew=OrderedDict(seats=4, hatchPos=[0, 0.05, -1.25], hatchNormal=[0, 0, -1]))
add('cupola_lookout', 'Lookout Cupola', CMD, 'S2', 'command.cabin', VH, 'EA4',
    'One seat under a ring of windows, with control of the craft. The best view for docking and station keeping.', 900, 2.5, 1.2,
    mdl('capsule', 2.5, 1.2, 'pod', bottomDiameter=2.5, topDiameter=1.9), stack_nodes(1.2, 2, 2), family='pod', resources=units('Electric', 80),
    crew=OrderedDict(seats=1, hatchPos=[0, 0.05, -1.25], hatchNormal=[0, 0, -1]),
    command=OrderedDict(requiresCrew=True, sas=True, ecPerSecond=0.02), reactionWheel=OrderedDict(torque=4000, ecPerSecond=0.1))
add('lab_quill', 'Quill Research Lab', CMD, 'S2', 'command.cabin', LI, 'EA4',
    'A two-seat laboratory module. Crew inside turn stored science data into more of it (SCI-08).', 2200, 2.5, 2.4,
    mdl('tank', 2.5, 2.4, 'white'), stack_nodes(2.4, 2, 2), surface(2.5, True, True), family='pod', resources=units('Electric', 200),
    crew=OrderedDict(seats=2, hatchPos=[0, 0.05, -1.25], hatchNormal=[0, 0, -1]))

# ---------------------------------------------------------------------------------------------- tanks
PR = 'Propellant'


def tank(pid, title, size, ms, manu, desc, d, h, n, rl='tank.chemical', res='chemical', dry=None, kind='tank', surf=True, top=None, bottom=None, fam='tank', **kw):
    dry = dry if dry is not None else n * 0.125
    if size == 'radial':
        nodes, surf_def = None, radial_surface(d)
    else:
        top = NODE[size] if top is None else top
        bottom = NODE[size] if bottom is None else bottom
        nodes, surf_def = stack_nodes(h, top, bottom), (surface(d) if surf else None)
    m = mdl(kind, d, h, 'white') if kind != 'adapter' else mdl('adapter', d, h, 'white', topDiameter=kw.pop('topD'), bottomDiameter=d)
    add(pid, title, ROLES[rl]['category'], size, rl, manu, ms, desc, dry, d, h, m, nodes, surf_def, family=fam, resources=units(res, n), **kw)


tank('tank_s0_short', 'Pip-25 Propellant Tank', 'S0', 'EA1', CA, 'Short 0.625 m propellant tank for probes and small landers.', 0.625, 0.55, 125)
tank('tank_s0_long', 'Pip-100 Propellant Tank', 'S0', 'EA1', CA, 'Long 0.625 m propellant tank for probes and small landers.', 0.625, 2.2, 500)
tank('tank_s1_xl', 'LT-800 Propellant Tank', 'S1', 'EA1', AW, 'Extra-long 1.25 m propellant tank.', 1.25, 8.8, 4000)
tank('tank_s2_med', 'HT-1600 Propellant Tank', 'S2', 'EA1', VH, 'Medium 2.5 m propellant tank.', 2.5, 3.8, 8000)
tank('adapter_s1_s0', 'TA-10 Taper Adapter Tank', 'S0/S1', 'EA1', AW, 'Conical 1.25 m to 0.625 m adapter carrying propellant.', 1.25, 1.0, 250, topD=0.625,
     kind='adapter', surf=False, top=0, bottom=1)
tank('mono_s0', 'MP-25 Monoprop Tank', 'S0', 'EA1', AW, 'Small 0.625 m monopropellant tank for RCS.', 0.625, 0.3, 25, rl='tank.mono', res='Monoprop', dry=10)
tank('tank_s2_xl', 'HT-6400 Propellant Tank', 'S2', 'EA2', VH, 'Very long 2.5 m propellant tank for heavy stages.', 2.5, 15.2, 32000)
tank('tank_s3_short', 'XT-3600 Propellant Tank', 'S3', 'EA2', VH, 'Short 3.75 m propellant tank.', 3.75, 3.7, 18000)
tank('tank_s3_med', 'XT-7200 Propellant Tank', 'S3', 'EA2', VH, 'Medium 3.75 m propellant tank.', 3.75, 7.4, 36000)
tank('tank_s3_long', 'XT-14400 Propellant Tank', 'S3', 'EA2', VH, 'Long 3.75 m propellant tank for first stages.', 3.75, 14.8, 72000)
tank('adapter_s3_s2', 'TA-32 Taper Adapter Tank', 'S2/S3', 'EA2', VH, 'Conical 3.75 m to 2.5 m adapter carrying propellant.', 3.75, 2.4, 5500, topD=2.5,
     kind='adapter', surf=False, top=2, bottom=3)
tank('mono_s2', 'MP-600 Monoprop Toroid', 'S2', 'EA2', AW, 'A ring-shaped 2.5 m monopropellant tank that leaves the middle free.', 2.5, 0.9, 600, rl='tank.mono', res='Monoprop', dry=150)
tank('tank_radial', 'Saddle Radial Tank', 'radial', 'EA2', AW, 'A side-mounted propellant tank for drop tanks and boosters.', 0.6, 1.6, 220, kind='radialtank', surf=False)
tank('xenon_s0', 'XG-40 Xenon Tank', 'S0', 'EA2', QS, 'A small 0.625 m tank of xenon for ion engines. It holds 40 kg.', 0.625, 0.5, 400, rl='tank.xenon', res='Xenon', dry=30)
tank('xenon_radial', 'XG-20 Xenon Tank', 'radial', 'EA2', QS, 'A side-mounted tank of xenon for ion engines. It holds 20 kg.', 0.3, 0.6, 200, rl='tank.xenon', res='Xenon', dry=15,
     kind='radialtank', surf=False)
tank('hydro_s1', 'HY-140 Hydrogen Tank', 'S1', 'EA2', QS, 'A long 1.25 m insulated hydrogen tank for nuclear engines. It holds 140 kg.', 1.25, 4.4, 2000, rl='tank.hydrogen', res='Hydrogen',
     dry=200)
tank('hydro_s2', 'HY-1100 Hydrogen Tank', 'S2', 'EA2', QS, 'A long 2.5 m insulated hydrogen tank for nuclear engines. It holds 1,100 kg.', 2.5, 7.5, 16000, rl='tank.hydrogen',
     res='Hydrogen', dry=1600)


def coupler(pid, title, size_top, ms, manu, desc, d, h, n, bottoms, bottom_size):
    nodes = [node('top', h / 2, 1, NODE[size_top])]
    for i, (x, z) in enumerate(bottoms):
        nid = 'bottom' if i == 0 else 'bottom_' + 'bcdef'[i - 1]
        nn = node(nid, -h / 2, -1, bottom_size)
        nn['pos'] = [x, r3(-h / 2), z]
        nodes.append(nn)
    add(pid, title, PR, f'S1/{size_top}', 'tank.chemical', manu, ms, desc, n * 0.125, d, h, mdl('adapter', d, h, 'white', topDiameter=d, bottomDiameter=d), nodes,
        family='tank', resources=units('chemical', n), brk=BREAK[size_top])


coupler('coupler_bi_s2', 'Twin Coupler', 'S2', 'EA2', VH, 'Splits a 2.5 m stack into two 1.25 m stacks. Holds propellant.', 2.5, 2.0, 1500, [(0.62, 0), (-0.62, 0)], 1)
coupler('coupler_tri_s2', 'Triple Coupler', 'S2', 'EA2', VH, 'Splits a 2.5 m stack into three 1.25 m stacks. Holds propellant.', 2.5, 2.0, 1600,
        [(0.8, 0), (-0.4, 0.69), (-0.4, -0.69)], 1)
coupler('coupler_quad_s3', 'Quad Coupler', 'S3', 'EA3', VH, 'Splits a 3.75 m stack into four 1.25 m stacks. Holds propellant.', 3.75, 2.4, 5000,
        [(1.0, 0), (-1.0, 0), (0, 1.0), (0, -1.0)], 1)

# ---------------------------------------------------------------------------------------------- engines and boosters
EN, BO = 'Engines', 'Boosters'


def eng(pid, title, size, rl, ms, manu, desc, d, h, dry, e, style='engine', count=None, radial=False, brk=None, **kw):
    nodes = None if radial else stack_nodes(h, NODE[size], NODE[size])
    surf = radial_surface(d) if radial else OrderedDict(allowed=False, onto=False)
    add(pid, title, EN, size, rl, manu, ms, desc, dry, d, h, eng_model(d, h, style, count), nodes, surf, family='engine', brk=brk, engine=e, **kw)


eng('eng_gnat', 'Gnat Lander Engine', 'S0', 'engine.small', 'EA1', VP, 'Light restartable engine for landers and probes. A lot of thrust for its weight.', 0.625, 0.55, 70,
    engine('liquid', 12000, 330, 110, 0.55, 2.0, 4.0, alt=0.5, scale=0.3, color=(1.0, 0.72, 0.4)))
eng('eng_cricket', 'Cricket Radial Engine', 'radial', 'engine.radial', 'EA1', VP, 'Mounts on the side of a stage. Fixed nozzle, strong at sea level.', 0.45, 1.0, 220,
    engine('liquid', 40000, 300, 250, 1.0, 0.0, 2.5, alt=0.8, scale=0.5), radial=True)
eng('eng_wasp', 'Wasp LV-300 Engine', 'S1', 'engine.lifter', 'EA1', VP, 'High-thrust 1.25 m lifter. More push than the Hornet for more weight.', 1.25, 1.7, 2200,
    engine('liquid', 330000, 300, 255, 1.7, 2.5, 2.5, alt=4.0, scale=1.3))
eng('eng_albacore', 'Albacore VX-400 Engine', 'S2', 'engine.vacuum', 'EA2', VP, 'Large vacuum engine with a wide bell for upper stages.', 2.5, 2.4, 2100,
    engine('liquid', 400000, 355, 125, 2.4, 3.0, 2.5, alt=2.0, scale=1.6, color=(0.75, 0.8, 1.0)), style='engine_gold')
eng('eng_centipede', 'Centipede Cluster', 'S2', 'engine.lifter', 'EA2', VP, 'Four fixed nozzles on one 2.5 m mount, for heavy lifts and big landers.', 2.5, 2.2, 4200,
    engine('liquid', 900000, 315, 270, 2.2, 1.5, 2.0, alt=6.0, scale=1.6), count=4)
eng('eng_titan', 'Titan LV-2000 Engine', 'S3', 'engine.lifter', 'EA2', VH, 'The heavy 3.75 m lifter for the biggest first stages.', 3.75, 3.6, 7500,
    engine('liquid', 1700000, 315, 285, 3.6, 2.0, 1.5, alt=10.0, scale=2.4))
eng('eng_lantern', 'Lantern NTR', 'S1', 'engine.nuclear', 'EA2', QS, 'A nuclear-thermal engine. It heats hydrogen in a reactor: very high Isp, low thrust, and it gets hot.', 1.25, 2.2, 3000,
    engine('liquid', 60000, 800, 380, 2.2, 1.0, 0.4, prop='Hydrogen', scale=0.8, heat=150000, color=(0.65, 0.75, 1.0)), style='engine_gold')
eng('eng_glimmer', 'Glimmer Ion Drive', 'S0', 'engine.ion', 'EA2', QS, 'An ion drive. It turns electricity and xenon into a tiny, endless thrust.', 0.625, 0.5, 250,
    engine('liquid', 2000, 4200, 100, 0.5, 0.0, 0.3, prop='Xenon', scale=0.25, color=(0.5, 0.6, 1.0)))
eng('eng_leviathan', 'Leviathan VX-1500 Engine', 'S3', 'engine.vacuum', 'EA3', VH, 'The heavy 3.75 m vacuum engine for big transfer stages.', 3.75, 4.2, 5400,
    engine('liquid', 1100000, 360, 140, 4.2, 2.0, 1.5, alt=8.0, scale=2.4, color=(0.75, 0.8, 1.0)), style='engine_gold')
eng('eng_wedge', 'Wedge Aerospike', 'S1', 'engine.aerospike', 'EA3', VP, 'An aerospike: nearly the same Isp from the pad to vacuum, with a flat plug in place of a bell.', 1.25, 1.2, 1800,
    engine('liquid', 220000, 340, 305, 1.2, 0.0, 2.0, alt=2.5, scale=1.1))
eng('eng_vernier', 'Vernier Thruster', 'radial', 'engine.attitude', 'EA3', VP, 'A small side thruster for trim and attitude on the chemical mix.', 0.25, 0.4, 25,
    engine('liquid', 4000, 290, 220, 0.4, 0.0, 6.0, scale=0.2), radial=True)


def srb(pid, title, size, rl, ms, manu, desc, d, h, dry, fuel, thrust, ispv, ispa, radial=False, brk=None, scale=1.1):
    nodes = None if radial else stack_nodes(h, NODE[size], NODE[size])
    surf = radial_surface(d) if radial else surface(d)
    e = OrderedDict(type='solid', thrustVac=thrust, ispVac=ispv, ispAsl=ispa, gimbalRange=0, throttleResponse=0, propellant='SolidFuel',
                    nozzlePos=[0, r3(-h / 2), 0], exhaustScale=scale, exhaustColor=[1.0, 0.55, 0.2])
    add(pid, title, BO, size, rl, manu, ms, desc, dry, d, h,
        mdl('srb', d, h, 'srb', bellTop=r3(d * 0.4), bellBottom=r3(d * 0.68), bellLength=r3(h * 0.12)), nodes, surf, family='srb', brk=brk,
        resources=units('SolidFuel', fuel), engine=e)


srb('srb_spit', 'Spit Separation Motor', 'radial', 'booster.separation', 'EA1', VP, 'A tiny solid motor that pushes a spent stage away. It burns for about a second.', 0.2, 0.45, 12, 10, 15000, 170, 150, radial=True, scale=0.3)
srb('srb_bison', 'Bison Long Booster', 'S1', 'booster.solid', 'EA2', VP, 'A very long 1.25 m solid booster for a second pair of boosters.', 1.25, 10.2, 2200, 9600, 380000, 215, 180, brk=700000)
srb('srb_behemoth', 'Behemoth Heavy Booster', 'S2', 'booster.solid', 'EA2', VP, 'A heavy 2.5 m solid booster. Cannot be throttled or shut down once lit.', 2.5, 9.0, 5200, 30000, 1200000, 215, 180, brk=1000000, scale=1.8)

# ---------------------------------------------------------------------------------------------- control
UT = 'Utility'
add('rw_s0', 'Gyro-XS Reaction Wheel', UT, 'S0', 'control.wheel', AW, 'EA1', 'Inline 0.625 m reaction wheel for small craft.', 20, 0.625, 0.15,
    mdl('disc', 0.625, 0.15, 'wheel'), stack_nodes(0.15, 0, 0), family='struct', reactionWheel=OrderedDict(torque=2400, ecPerSecond=0.15), maxTemp=1100, skinMaxTemp=1300)
add('rw_radial', 'Gyro-R Reaction Wheel', UT, 'radial', 'control.wheel', AW, 'EA1', 'A small reaction wheel that mounts on the side of a craft.', 14, 0.3, 0.2,
    mdl('disc', 0.3, 0.2, 'wheel'), None, radial_surface(0.3), family='radial', reactionWheel=OrderedDict(torque=1400, ecPerSecond=0.12), maxTemp=1100, skinMaxTemp=1300)
add('rcs_linear', 'Linear RCS Port', UT, 'radial', 'control.rcs', AW, 'EA1', 'A single RCS nozzle for translation or trim.', 12, 0.25, 0.25,
    mdl('rcs', 0.25, 0.25, 'rcs'), None, radial_surface(0.25), family='radial',
    rcs=OrderedDict(thrust=600, ispVac=240, ispAsl=100, propellant='Monoprop', nozzles=[[0, 0, 0.1, 0, 0, 1]]), maxTemp=1300, skinMaxTemp=1500, impactTolerance=10)
add('rw_s3', 'Gyro-XL Reaction Wheel', UT, 'S3', 'control.wheel', VH, 'EA2', 'Inline 3.75 m reaction wheel for the biggest craft.', 1500, 3.75, 0.5,
    mdl('disc', 3.75, 0.5, 'wheel'), stack_nodes(0.5, 3, 3), family='struct', reactionWheel=OrderedDict(torque=90000, ecPerSecond=1.6), maxTemp=1100, skinMaxTemp=1300)
add('rcs_duo', 'Duo RCS Block', UT, 'radial', 'control.rcs', AW, 'EA2', 'Two RCS nozzles on one block, for trim and small craft.', 25, 0.3, 0.3,
    mdl('rcs', 0.3, 0.3, 'rcs'), None, radial_surface(0.3), family='radial',
    rcs=OrderedDict(thrust=800, ispVac=240, ispAsl=100, propellant='Monoprop', nozzles=[[0, 0.12, 0.1, 0, 1, 0], [0, -0.12, 0.1, 0, -1, 0]]), maxTemp=1300, skinMaxTemp=1500, impactTolerance=10)

# ---------------------------------------------------------------------------------------------- structure and coupling
CP, AE = 'Coupling', 'Aero'


def stackpart(pid, title, cat, size, rl, ms, manu, desc, dry, d, h, model, fam='struct', surf=None, **kw):
    add(pid, title, cat, size, rl, manu, ms, desc, dry, d, h, model, stack_nodes(h, NODE[size], NODE[size]), surf, family=fam, **kw)


def deco(pid, title, size, ms, manu, desc, dry, d, h, impulse, rl='structure.decoupler'):
    stackpart(pid, title, CP, size, rl, ms, manu, desc, dry, d, h, mdl('decoupler', d, h, 'decoupler'), crossfeed=False,
              decoupler=OrderedDict(ejectionImpulse=impulse, kind='stack', explosiveNode='top'))


deco('dec_s0', 'SD-0 Stack Decoupler', 'S0', 'EA1', AW, 'Separates the stage below a 0.625 m stack. Blocks propellant crossfeed.', 20, 0.625, 0.15, 700)
deco('sep_s1', 'SS-1 Separator', 'S1', 'EA1', AW, 'Separates a 1.25 m stack so that both halves drift apart. Blocks crossfeed.', 60, 1.25, 0.25, 1800, rl='structure.separator')
deco('sep_s2', 'SS-2 Separator', 'S2', 'EA1', AW, 'Separates a 2.5 m stack so that both halves drift apart. Blocks crossfeed.', 450, 2.5, 0.35, 6000, rl='structure.separator')
deco('dec_s3', 'SD-3 Heavy Stack Decoupler', 'S3', 'EA2', VH, 'Separates the stage below a 3.75 m stack. Blocks propellant crossfeed.', 1100, 3.75, 0.4, 12000)
deco('sep_s0', 'SS-0 Separator', 'S0', 'EA2', AW, 'Separates a 0.625 m stack so that both halves drift apart. Blocks crossfeed.', 25, 0.625, 0.15, 800, rl='structure.separator')
deco('sep_s3', 'SS-3 Separator', 'S3', 'EA2', VH, 'Separates a 3.75 m stack so that both halves drift apart. Blocks crossfeed.', 1250, 3.75, 0.45, 14000, rl='structure.separator')
add('dec_radial_heavy', 'RD-2 Heavy Radial Decoupler', CP, 'radial', 'structure.decoupler', VH, 'EA2', 'Holds a big side-mounted booster and pushes it clear when fired.',
    120, 0.5, 0.9, mdl('radialdecoupler', 0.5, 0.9, 'decoupler'), None, surface(0.5), family='struct', brk=750000, crossfeed=False,
    decoupler=OrderedDict(ejectionImpulse=60000, kind='radial', explosiveNode='srf'))
# explosiveNode "srf" is the surface node of a radial decoupler, as RD-1's; its nodes list is empty


def dock(pid, title, size, ms, manu, desc, dry, d, h, rng, speed, nodeid='top', rl='structure.docking'):
    stackpart(pid, title, CP, size, rl, ms, manu, desc, dry, d, h, mdl('dockingport', d, h, 'dock'), maxTemp=1100, skinMaxTemp=1400,
              dockingPort=OrderedDict(nodeId=nodeid, captureRange=rng, captureSpeed=speed, captureAngleDeg=15, undockImpulse=150 if size != 'S0' else 80))


dock('dock_s0', 'Clasp-0 Docking Port', 'S0', 'EA2', AW, 'Androgynous docking port for 0.625 m craft. Captures within 0.4 m.', 25, 0.625, 0.25, 0.4, 0.8)
dock('dock_s2', 'Clasp-2 Docking Port', 'S2', 'EA2', AW, 'Androgynous docking port for 2.5 m craft. Captures within 1 m.', 180, 2.5, 0.45, 1.0, 1.2)
dock('dock_s1_inline', 'Clasp-1S Shielded Docking Port', 'S1', 'EA4', AW, 'An inline docking port with a cover that closes when not in use.', 95, 1.25, 0.5, 0.6, 1.0)

add('truss_short', 'TR-1 Truss', CP, 'none', 'structure.truss', AW, 'EA2', 'A one-metre truss for stations and landers.', 40, 0.4, 1.0,
    mdl('tank', 0.4, 1.0, 'dark'), stack_nodes(1.0, 0, 0), family='struct')
add('truss_long', 'TR-3 Truss', CP, 'none', 'structure.truss', AW, 'EA2', 'A three-metre truss for stations and landers.', 110, 0.4, 3.0,
    mdl('tank', 0.4, 3.0, 'dark'), stack_nodes(3.0, 0, 0), family='struct')
add('strut_cube', 'Cube Node', CP, 'none', 'structure.truss', AW, 'EA2', 'A small cube with a node on every face, for stations and landers.', 25, 0.5, 0.5,
    mdl('tank', 0.5, 0.5, 'dark'),
    [node('top', 0.25, 1, 0), node('bottom', -0.25, -1, 0),
     OrderedDict(id='left', pos=[-0.25, 0, 0], dir=[-1, 0, 0], size=0), OrderedDict(id='right', pos=[0.25, 0, 0], dir=[1, 0, 0], size=0),
     OrderedDict(id='front', pos=[0, 0, 0.25], dir=[0, 0, 1], size=0), OrderedDict(id='back', pos=[0, 0, -0.25], dir=[0, 0, -1], size=0)], family='struct')
for s, dry, h in (('s1', 25, 0.1), ('s2', 120, 0.15), ('s3', 300, 0.2)):
    size = s.upper()
    stackpart('plate_' + s, 'Structural Plate ' + size, CP, size, 'structure.plate', 'EA2', AW if s != 's3' else VH,
              f'A flat {SIZES[size]["diameter"]} m plate to mount parts on.', dry, SIZES[size]['diameter'], h,
              mdl('disc', SIZES[size]['diameter'], h, 'dark'), surf=surface(SIZES[size]['diameter']))
add('adapter_struct_s2_s1', 'Hollow Adapter', CP, 'S1/S2', 'structure.adapter', VH, 'EA2', 'A light 2.5 m to 1.25 m adapter with no tank.', 90, 2.5, 1.2,
    mdl('adapter', 2.5, 1.2, 'white', topDiameter=1.25, bottomDiameter=2.5), stack_nodes(1.2, 1, 2), family='struct', brk=400000)
for sz, dry, h in (('S0', 8, 0.5), ('S2', 180, 2.6)):
    add('nose_' + sz.lower(), f'NC-{sz[1]} Nose Cone', AE, sz, 'structure.nose', CA, 'EA1', f'Streamlined {SIZES[sz]["diameter"]} m nose cone. Greatly reduces drag on top of a stack.', dry,
        SIZES[sz]['diameter'], h, mdl('nosecone', SIZES[sz]['diameter'], h, 'white'), [node('bottom', -h / 2, -1, NODE[sz])], OrderedDict(allowed=False, onto=True), family='nose',
        dragOver=OrderedDict(bodyLift=0.25))
add('nose_s3', 'NC-3 Nose Cone', AE, 'S3', 'structure.nose', CA, 'EA2', 'Streamlined 3.75 m nose cone. Greatly reduces drag on top of a stack.', 420, 3.75, 3.4,
    mdl('nosecone', 3.75, 3.4, 'white'), [node('bottom', -1.7, -1, 3)], OrderedDict(allowed=False, onto=True), family='nose', dragOver=OrderedDict(cdTop=0.12, cdBottom=0.9, cdSide=0.75, bodyLift=0.25))
for sz, dry, ms in (('S1', 80, 'EA1'), ('S2', 300, 'EA1'), ('S3', 700, 'EA2')):
    d = SIZES[sz]['diameter']
    stackpart('fairing_' + sz.lower(), f'PF-{sz[1]} Fairing Base', AE, sz, 'structure.fairing', ms, CA if sz == 'S1' else VH,
              f'The base ring of a {d} m fairing. The shell grows from it (PRT-04) and drops away in two halves.', dry, d, 0.3, mdl('disc', d, 0.3, 'white'))
add('clamp', 'Anchor Launch Clamp', 'Ground', 'radial', 'structure.clamp', VH, 'EA1', 'Holds the rocket on the pad until the engines are at full thrust, then lets go (FLT-09).',
    600, 0.8, 1.2, mdl('disc', 0.8, 1.2, 'dark'), None, radial_surface(0.8), family='struct', brk=800000)
for sz, dry, d, h in (('S1', 120, 1.25, 1.2), ('S2', 400, 2.5, 2.4)):
    stackpart('bay_' + sz.lower(), f'SB-{sz[1]} Service Bay', CP, sz, 'structure.bay', 'EA2', AW if sz == 'S1' else VH,
              f'An enclosed {d} m bay with doors. Cargo inside gets no drag and no heating (FLT-05).', dry, d, h, mdl('tank', d, h, 'white'), surf=surface(d))
add('strut', 'Strut Connector', CP, 'none', 'structure.strut', AW, 'EA4', 'A rigid link between two parts, to stiffen a long craft (FLT-16).', 5, 0.1, 1.0,
    mdl('tank', 0.1, 1.0, 'dark'), None, radial_surface(0.1), family='radial')
add('fuel_line', 'Fuel Line', CP, 'none', 'structure.strut', AW, 'EA4', 'A pipe that carries propellant from one tank to another part, past the decouplers.', 8, 0.1, 1.0,
    mdl('tank', 0.1, 1.0, 'dark'), None, radial_surface(0.1), family='radial')

# ---------------------------------------------------------------------------------------------- power, comms, lights, ladders
add('battery_s0', 'Cell-50 Battery', UT, 'S0', 'power.battery', AW, 'EA1', 'A 0.625 m battery disc storing 50 EC.', 3, 0.625, 0.15, mdl('battery', 0.625, 0.15, 'dark'),
    stack_nodes(0.15, 0, 0), family='radial', resources=units('Electric', 50), skinMaxTemp=1200, brk=BREAK['S0'])
add('solar_fixed', 'SP-F Fixed Panel', UT, 'radial', 'power.solar', LI, 'EA1', 'A small fixed solar panel that is always on. It delivers power in sunlight.', 6, 0.5, 0.05,
    mdl('disc', 0.5, 0.05, 'dark'), None, radial_surface(0.5), family='radial')
add('solar_fold', 'SP-2 Folding Panel', UT, 'radial', 'power.solar', LI, 'EA1', 'A deployable solar panel. It folds flat for launch and opens in orbit.', 12, 0.3, 0.3,
    mdl('battery', 0.3, 0.3, 'dark'), None, radial_surface(0.3), family='radial')
add('antenna_whip', 'Whip Antenna', UT, 'radial', 'comms.antenna', LI, 'EA1', 'A short whip antenna: fast and cheap on power, short range.', 4, 0.1, 0.7,
    mdl('battery', 0.1, 0.7, 'dark'), None, radial_surface(0.1), family='radial')
add('antenna_dish', 'DS-1 Dish', UT, 'radial', 'comms.antenna', LI, 'EA1', 'A medium dish antenna for the inner system.', 35, 0.5, 0.5,
    mdl('disc', 0.5, 0.5, 'white'), None, radial_surface(0.5), family='radial')
add('light_spot', 'Spotlight', UT, 'radial', 'utility.light', LI, 'EA1', 'A narrow lamp for night landings and work.', 4, 0.15, 0.2,
    mdl('battery', 0.15, 0.2, 'dark'), None, radial_surface(0.15), family='radial')
add('ladder_fixed', 'Fixed Ladder', UT, 'radial', 'utility.ladder', AW, 'EA1', 'A fixed ladder for a rocketeer to climb.', 8, 0.15, 1.5,
    mdl('tank', 0.15, 1.5, 'dark'), None, radial_surface(0.15), family='radial')
add('battery_s2', 'Cell-1600 Battery Bank', UT, 'S2', 'power.battery', AW, 'EA2', 'A 2.5 m battery bank storing 1,600 EC.', 80, 2.5, 0.3, mdl('battery', 2.5, 0.3, 'dark'),
    stack_nodes(0.3, 2, 2), family='radial', resources=units('Electric', 1600), skinMaxTemp=1200, brk=BREAK['S2'])
add('solar_wing', 'SP-8 Solar Wing', UT, 'radial', 'power.solar', LI, 'EA2', 'A large deployable solar wing for big craft and stations.', 45, 0.4, 0.4,
    mdl('battery', 0.4, 0.4, 'dark'), None, radial_surface(0.4), family='radial')
add('radiator_panel', 'RP-1 Radiator', UT, 'radial', 'power.radiator', QS, 'EA2', 'A fixed radiator that sheds heat from the parts beside it (FLT-04).', 20, 0.5, 0.05,
    mdl('disc', 0.5, 0.05, 'white'), None, radial_surface(0.5), family='radial')
add('radiator_fold', 'RF-3 Folding Radiator', UT, 'radial', 'power.radiator', QS, 'EA2', 'A folding radiator that opens in space for three times the area (FLT-04).', 35, 0.3, 0.3,
    mdl('battery', 0.3, 0.3, 'white'), None, radial_surface(0.3), family='radial')
add('antenna_highgain', 'HG-5 High-Gain Dish', UT, 'radial', 'comms.antenna', LI, 'EA2', 'A large dish for the outer system. Long range, slow to deploy.', 90, 0.8, 0.8,
    mdl('disc', 0.8, 0.8, 'white'), None, radial_surface(0.8), family='radial')
add('light_flood', 'Floodlight', UT, 'radial', 'utility.light', LI, 'EA2', 'A wide lamp that lights a landing site or a work area.', 10, 0.25, 0.3,
    mdl('battery', 0.25, 0.3, 'dark'), None, radial_surface(0.25), family='radial')
add('ladder_tele', 'Telescopic Ladder', UT, 'radial', 'utility.ladder', AW, 'EA2', 'A ladder that extends from a short stowed length to four metres.', 20, 0.15, 0.8,
    mdl('tank', 0.15, 0.8, 'dark'), None, radial_surface(0.15), family='radial')
add('solar_tracker', 'Sunfield Array', UT, 'radial', 'power.solar', LI, 'EA3', 'A huge solar array on a tracking mount that follows the star.', 160, 0.5, 0.5,
    mdl('battery', 0.5, 0.5, 'dark'), None, radial_surface(0.5), family='radial')
add('rtg', 'Hearth RTG', UT, 'radial', 'power.generator', QS, 'EA3', 'A radioisotope generator: constant power anywhere, heavy, and warm to the touch.', 40, 0.4, 0.8,
    mdl('battery', 0.4, 0.8, 'dark'), None, radial_surface(0.4), family='radial')
add('fuelcell', 'FC-1 Fuel Cell', UT, 'radial', 'power.generator', QS, 'EA3', 'Turns the chemical mix into electricity. It runs only while the tanks last.', 45, 0.4, 0.6,
    mdl('battery', 0.4, 0.6, 'dark'), None, radial_surface(0.4), family='radial')
add('fuelcell_array', 'FC-8 Fuel Cell Array', UT, 'S1', 'power.generator', QS, 'EA4', 'Eight fuel cells in a 1.25 m ring, for a station that runs on its tanks.', 280, 1.25, 0.8,
    mdl('disc', 1.25, 0.8, 'dark'), stack_nodes(0.8, 1, 1), family='struct')
add('radiator_large', 'RL-10 Active Radiator', UT, 'radial', 'power.radiator', QS, 'EA4', 'A large active radiator: it uses electricity to move heat away faster (FLT-04).', 60, 0.6, 0.1,
    mdl('disc', 0.6, 0.1, 'white'), None, radial_surface(0.6), family='radial')

# ---------------------------------------------------------------------------------------------- recovery, descent, aero
RC, GR = 'Recovery', 'Ground'


def chute(pid, title, ms, manu, desc, d, h, dry, semi, area, minp, alt, load, canopy, color, radial=False, nodes=None, **kw):
    pr = OrderedDict(semiDeployedArea=semi, deployedArea=area, minPressure=minp, deployAltitude=alt, semiDeployTime=1.0 if area > 100 else 0.6,
                     deployTime=4.0 if area > 100 else 2.0, maxLoad=load, maxCanopyTemp=700 if area > 100 else 900, canopyDiameter=canopy, canopyColor=list(color))
    add(pid, title, RC, 'none', 'recovery.chute', manu, ms, desc, dry, d, h, mdl('radialchute' if radial else 'chute', d, h, 'chute'), nodes if not radial else None,
        radial_surface(d) if radial else OrderedDict(allowed=False, onto=False), family='recovery', parachute=pr, **kw)


chute('chute_s0', 'Canopy-6 Parachute', 'EA1', CA, 'Small main parachute for 0.625 m probes and light capsules.', 0.4, 0.25, 35, 3, 120, 4000, 800, 40000, 7, (0.95, 0.45, 0.12),
      nodes=[node('bottom', -0.125, -1, 0)])
chute('chute_s2', 'Canopy-32 Parachute', 'EA1', CA, 'Big main parachute for heavy capsules and thin air.', 1.2, 0.6, 240, 14, 1900, 3000, 1200, 400000, 28, (0.95, 0.45, 0.12),
      nodes=[node('bottom', -0.3, -1, 2)])
chute('chute_drogue_s2', 'Drogue-24 Parachute', 'EA2', CA, 'A large drogue that slows a heavy craft before the main canopy opens.', 0.8, 0.5, 160, 8, 140, 5000, 3000, 400000, 10, (0.9, 0.9, 0.9),
      nodes=[node('bottom', -0.25, -1, 2)])


def shield(pid, title, size, ms, manu, desc, dry, d, h, ablator, ablation, brk=None, rl='recovery.shield'):
    add(pid, title, RC, size, rl, manu, ms, desc, dry, d, h, mdl('heatshield', d, h, 'shield'), stack_nodes(h, NODE[size], NODE[size]), None, family='shield',
        resources=units('Ablator', ablator), specificHeat=1300, skinMassPerArea=14, emissivity=0.85, brk=brk,
        heatShield=OrderedDict(ablationStartTemp=550, ablationHeat=16000000.0, maxAblationRate=ablation, insulation=3.0))


shield('shield_s0', 'HS-60 Ablative Heat Shield', 'S0', 'EA1', AW, 'Ablative shield for 0.625 m probes and capsules.', 25, 0.7, 0.2, 40, 0.3)
shield('shield_s3', 'HS-375 Ablative Heat Shield', 'S3', 'EA2', VH, 'Ablative shield for 3.75 m craft. Point it into the airflow during reentry.', 800, 3.9, 0.45, 1100, 2.5)
shield('shield_inflatable', 'Bloom Inflatable Shield', 'S1', 'EA4', QS, 'A 1.25 m stack that inflates into a wide shield, for heavy landers that need a gentler entry.', 110, 1.25, 0.4, 250, 1.8)


def leg(pid, title, ms, desc, dry, length, spring, damper, maxload, foot, radius, imp=12):
    add(pid, title, GR, 'radial', 'recovery.leg', AW, ms, desc, dry, radius, length * 0.62, mdl('leg', radius, length, 'leg'), None,
        OrderedDict(allowed=True, onto=False, pos=[0, 0, -0.1], dir=[0, 0, 1]), family='radial', brk=int(maxload * 1.2), impactTolerance=imp,
        landingLeg=OrderedDict(length=length, splayDeg=28, travel=round(length * 0.22, 2), spring=spring, damper=damper, impactTolerance=imp, maxLoad=maxload,
                               footRadius=foot, friction=0.9))


leg('leg_ls1', 'LS-1 Mini Strut', 'EA1', 'A small landing leg for probes and light landers. Breaks above 10 m/s.', 20, 1.0, 20000, 3500, 40000, 0.12, 0.2, 10)
leg('leg_ls9', 'LS-9 Heavy Strut', 'EA2', 'A heavy landing leg for big landers and surface bases.', 380, 3.2, 220000, 30000, 450000, 0.35, 0.5)


def fin(pid, title, ms, desc, dry, d, h, area, slope=3.5, stall=22, dragover=None, **kw):
    add(pid, title, AE, 'radial', 'aero.fin', CA, ms, desc, dry, d, h, mdl('fin', max(d, 0.5), h, 'fin'), None,
        OrderedDict(allowed=True, onto=False, pos=[0, 0, 0], dir=[0, 0, 1]), family='aero', brk=int(dry * 1300), dragOver=dragover,
        fin=OrderedDict(area=area, liftSlope=slope, normal=[1, 0, 0], stallDeg=stall), **kw)


fin('fin_fs2', 'FS-2 Swept Fin', 'EA1', 'A bigger swept tail fin. Place three or four near the bottom of a rocket.', 55, 0.1, 1.3, 1.1)
fin('fin_tail_s2', 'FT-2 Tail Fin', 'EA2', 'A large tail fin for 2.5 m rockets.', 120, 0.15, 2.0, 2.6)
fin('canard_ctl', 'CF-1 Canard', 'EA2', 'A small actuated control surface that steers by itself (FLT-17).', 20, 0.08, 0.7, 0.4)
fin('fin_ctl', 'CF-4 Control Fin', 'EA2', 'A large actuated control surface for steering in the air (FLT-17).', 45, 0.1, 1.2, 0.9)
fin('airbrake', 'AB-1 Airbrake', 'EA2', 'A panel that opens to add drag on the way down (FLT-17).', 60, 0.1, 1.0, 0.8, slope=1.0)
fin('gridfin', 'GF-1 Grid Fin', 'EA3', 'A lattice fin that steers a booster back to the pad.', 70, 0.1, 1.0, 0.7, slope=2.5, stall=30)

# ---------------------------------------------------------------------------------------------- science
SC = 'Science'
for pid, title, ms, desc, dry in (
        ('sci_thermo', 'Thermal Probe', 'EA1', 'Measures temperature. Works anywhere, and says what it found.', 3),
        ('sci_baro', 'Pressure Gauge', 'EA1', 'Measures air pressure. A reading in vacuum is a reading too.', 3),
        ('sci_bio', 'Bio-Culture Canister', 'EA1', 'A canister of cultures to expose to a new place. Single use.', 12),
        ('sci_matrack', 'Materials Exposure Rack', 'EA1', 'A rack of samples to expose to a new place. Single use.', 14),
        ('sci_seismic', 'Seismic Array', 'EA2', 'Listens to the ground. Works on the surface only.', 30),
        ('sci_airsampler', 'Air Sampler', 'EA2', 'Samples the air and says what it is made of.', 18),
        ('sci_gravimeter', 'Gravimeter', 'EA3', 'Maps the pull of gravity from orbit or the ground.', 15)):
    add(pid, title, SC, 'radial', 'science.instrument', LI, ms, desc, dry, 0.25, 0.3, mdl('battery', 0.25, 0.3, 'white'), None, radial_surface(0.25), family='radial')

# ---------------------------------------------------------------------------------------------- rover, resources, asteroids
SF = 'Surface'
for pid, title, dry, d, h in (('wheel_s', 'Rover Wheel S', 30, 0.35, 0.2), ('wheel_m', 'Rover Wheel M', 90, 0.6, 0.3), ('wheel_l', 'Rover Wheel L', 220, 0.9, 0.45)):
    add(pid, title, SF, 'radial', 'surface.wheel', VH, 'EA2', f'A powered rover wheel with suspension and steering ({"S" if dry < 50 else "M" if dry < 150 else "L"} size).', dry, d, h,
        mdl('disc', d, h, 'wheel'), None, radial_surface(d), family='radial')
add('rover_chassis', 'Rover Chassis', SF, 'none', 'surface.chassis', VH, 'EA2', 'A flat body with mounting points for wheels, a pod and a battery.', 120, 1.2, 0.3,
    mdl('disc', 1.2, 0.3, 'dark'), [node('top', 0.15, 1, 1), node('bottom', -0.15, -1, 1)], surface(1.2), family='struct')
for pid, title, ms, dry, desc in (('drill_s', 'Drill S', 'EA4', 90, 'A drill for the ground or a grabbed asteroid (RES-03).'),
                                  ('drill_l', 'Drill L', 'EA4', 280, 'A large drill for the ground or a grabbed asteroid (RES-03).')):
    add(pid, title, SF, 'radial', 'surface.drill', QS, ms, desc, dry, 0.5 if dry < 200 else 0.8, 1.2, mdl('disc', 0.5, 1.2, 'dark'), None, radial_surface(0.5), family='radial')
add('isru_s', 'Converter S', SF, 'S1', 'surface.converter', QS, 'EA4', 'Turns ore into propellant, with electricity (RES-04). It makes heat.', 280, 1.25, 1.2,
    mdl('tank', 1.25, 1.2, 'dark'), stack_nodes(1.2, 1, 1), surface(1.25), family='struct')
add('isru_l', 'Converter L', SF, 'S2', 'surface.converter', QS, 'EA4', 'A large converter from ore to propellant (RES-04). It needs radiators.', 1400, 2.5, 2.0,
    mdl('tank', 2.5, 2.0, 'dark'), stack_nodes(2.0, 2, 2), surface(2.5), family='struct')
add('scanner_orbital', 'Survey Scanner', SF, 'radial', 'surface.scanner', LI, 'EA4', 'Maps ore from a low polar orbit (RES-02).', 150, 0.5, 0.6,
    mdl('disc', 0.5, 0.6, 'white'), None, radial_surface(0.5), family='radial')
add('scanner_narrow', 'Narrow-band Scanner', SF, 'radial', 'surface.scanner', LI, 'EA4', 'Shows the ore under the craft in detail, from a low orbit (RES-02).', 110, 0.4, 0.5,
    mdl('disc', 0.4, 0.5, 'white'), None, radial_surface(0.4), family='radial')
add('sensor_surface', 'Surface Sensor', SF, 'radial', 'surface.scanner', LI, 'EA4', 'Measures the ore at the landing spot (RES-02).', 12, 0.2, 0.3,
    mdl('battery', 0.2, 0.3, 'white'), None, radial_surface(0.2), family='radial')
tank('ore_s1', 'Ore Tank S1', 'S1', 'EA4', QS, 'A 1.25 m tank for ore.', 1.25, 1.1, 500, rl='tank.ore', res='Ore', dry=62.5)
tank('ore_s2', 'Ore Tank S2', 'S2', 'EA4', VH, 'A 2.5 m tank for ore.', 2.5, 2.0, 4000, rl='tank.ore', res='Ore', dry=500)
add('grabber', 'Grabbing Unit', SF, 'radial', 'surface.grabber', QS, 'EA4', 'Latches onto debris or an asteroid and holds it (FLT-11).', 140, 0.6, 0.8,
    mdl('disc', 0.6, 0.8, 'dark'), None, radial_surface(0.6), family='radial')

# ------------------------------------------------------------------------------------------------------ writing


def fmt_value(v):
    return json.dumps(v, separators=(', ', ': '))


def fmt_part(p, indent='    '):
    lines = [indent + '{']
    keys = list(p.keys())
    for i, k in enumerate(keys):
        comma = ',' if i < len(keys) - 1 else ''
        lines.append(f'{indent}  "{k}": {fmt_value(p[k])}{comma}')
    lines.append(indent + '}')
    return '\n'.join(lines)


def order_existing(p, size, rl, manu):
    q = OrderedDict()
    q['id'] = p['id']
    q['title'] = p['title']
    q['manufacturer'] = manu
    q['category'] = p['category']
    q['size'] = size
    q['role'] = rl
    q['milestone'] = 'M0'
    cost = ROLES[rl]['cost']
    q['cost'] = int(round(p['dryMass'] * cost))
    for k, v in p.items():
        if k not in q and k != 'manufacturer':
            q[k] = v
    return q


def main():
    out = DATA
    if '--out' in sys.argv:
        out = os.path.abspath(sys.argv[sys.argv.index('--out') + 1])
        os.makedirs(out, exist_ok=True)
    src = os.path.join(DATA, 'parts.json')
    cat = json.load(open(src, encoding='utf8'), object_pairs_hook=OrderedDict)
    slice_parts = [p for p in cat['parts'] if p['id'] in EXISTING]  # a rerun starts again from the slice's 38
    assert {p['id'] for p in slice_parts} == set(EXISTING), 'every part of the slice needs a mapping'
    existing = [order_existing(p, *EXISTING[p['id']]) for p in slice_parts]
    assert not any(p['id'] in {e['id'] for e in existing} for _, p in NEW), 'a new part reuses an id'
    ids = [p['id'] for _, p in NEW]
    assert len(ids) == len(set(ids)), 'duplicate new id: ' + str([i for i, c in Counter(ids).items() if c > 1])

    counts = Counter(p['milestone'] for p in existing if p['category'] != 'Hidden')
    counts.update(ms for ms, _ in NEW)
    want = {'M0': 36, 'EA1': 40, 'EA2': 63, 'EA3': 9, 'EA4': 20}
    assert dict(counts) == want, f'milestone counts {dict(counts)} != plan {want}'

    cats = list(cat['categories'])
    for c in CATEGORIES_ADDED:
        if c not in cats:
            cats.append(c)
    order = {c: i for i, c in enumerate(cats + ['Hidden'])}
    parts = existing + [p for _, p in NEW]
    # keep the slice's order; new parts follow, grouped by category in the order of the categories
    parts = existing + sorted([p for _, p in NEW], key=lambda p: (order[p['category']], MILESTONES.index(p['milestone'])))

    text = ['{', '  "resources": [']
    res = cat['resources']
    for i, r in enumerate(res):
        text.append('    ' + fmt_value(r) + (',' if i < len(res) - 1 else ''))
    text.append('  ],')
    text.append('  "mixes": [')
    for i, m in enumerate(cat['mixes']):
        text.append('    ' + fmt_value(m) + (',' if i < len(cat['mixes']) - 1 else ''))
    text.append('  ],')
    text.append('  "categories": ' + fmt_value(cats) + ',')
    text.append('  "parts": [')
    for i, p in enumerate(parts):
        text.append(fmt_part(p) + (',' if i < len(parts) - 1 else ''))
    text.append('  ]')
    text.append('}')
    with open(os.path.join(out, 'parts.json'), 'w', encoding='utf8', newline='\n') as f:
        f.write('\n'.join(text) + '\n')

    tpl = OrderedDict()
    tpl['milestones'] = MILESTONES
    tpl['release'] = 'M0'
    tpl['sizes'] = [OrderedDict(id=k, **v) for k, v in SIZES.items()]
    tpl['manufacturers'] = [OrderedDict(id=n, personality=t) for n, t in MANUFACTURERS]
    tpl['roles'] = [OrderedDict(id=k, category=v['category'], idPrefixes=v['idPrefixes'], note=v['note'], limits=v['limits']) for k, v in ROLES.items()]
    tl = ['{', '  "milestones": ' + fmt_value(tpl['milestones']) + ',', '  "release": "M0",', '  "sizes": [']
    for i, s in enumerate(tpl['sizes']):
        tl.append('    ' + fmt_value(s) + (',' if i < len(tpl['sizes']) - 1 else ''))
    tl += ['  ],', '  "manufacturers": [']
    for i, s in enumerate(tpl['manufacturers']):
        tl.append('    ' + fmt_value(s) + (',' if i < len(tpl['manufacturers']) - 1 else ''))
    tl += ['  ],', '  "roles": [']
    for i, s in enumerate(tpl['roles']):
        tl.append('    ' + fmt_value(s) + (',' if i < len(tpl['roles']) - 1 else ''))
    tl += ['  ]', '}']
    with open(os.path.join(out, 'part_templates.json'), 'w', encoding='utf8', newline='\n') as f:
        f.write('\n'.join(tl) + '\n')
    print(f'{len(parts)} parts ({dict(counts)}), {len(ROLES)} roles -> {out}')


if __name__ == '__main__':
    main()
