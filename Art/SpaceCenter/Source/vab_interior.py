"""VAB perimeter equipment in metres. The assembly envelope is deliberately vacant."""
import math

# Blender Z up; the floor at the assembly origin is 0.30 m above the apron origin.
CLEAR_MIN = (-16.0, -9.0, .40)
CLEAR_MAX = (8.0, 21.0, 68.30)
ASSEMBLY_ORIGIN = (-4.0, 6.0, .31)


def furnish(api):
    box, beam, cyl, ring, annulus, face, group, rail, text = [
        api[n] for n in ('box', 'beam', 'cyl', 'ring', 'annulus', 'face', 'group', 'railing', 'text')]
    B = api['B']
    pi = math.pi
    api['vab_colliders'] = colliders = []

    def solid(name, p, size, material=2):
        box(name, p, size, material)
        colliders.append(dict(name=name, center=p, size=size))

    def ibeam(x, y, bottom, height, width=.38):
        box('Web', (x,y,bottom+height/2), (.09,width,height), 2)
        for yy in (-width/2,width/2):
            box('Flange', (x,y+yy,bottom+height/2), (width,.07,height), 2)
        box('Anchor shoe', (x,y,.43), (width+.35,width+.35,.26), 5)
        for dx in (-.23,.23):
            for dy in (-.23,.23):cyl((x+dx,y+dy,.59),.038,.08,5,6)

    def deck(name,p,size):
        solid(name,p,size,5)
        x,y,z=p;w,d,h=size
        for xx in (x-w/2+.10,x+w/2-.10):
            box('Deck edge beam',(xx,y,z-.17),(.13,d,.34),2)
        # Drainage/grating joints supply real relief without thousands of open slats.
        for i in range(math.ceil(d/.6)):
            box('Deck seam',(x,y-d/2+i*.6,z+h/2+.006),(w,.018,.009),2)

    def toe(a,b):
        x1,y1,z=a;x2,y2,_=b
        rail(a,b,6)
        if abs(x2-x1)<.01:box('Kick plate',(x1,(y1+y2)/2,z+.08),(.035,abs(y2-y1),.16),6)
        else:box('Kick plate',((x1+x2)/2,y1,z+.08),(abs(x2-x1),.035,.16),6)

    group('Interior Floor')
    # A flush slab, not a raised launch-prep platform.
    solid('High bay floor',(-4,6,.18),(49.56,41.56,.26),0)
    group('Interior Floor markings')
    for x in (-17,9):box('Clear assembly boundary',(x,6,.315),(.09,30,.012),6)
    for y in (-9,21):
        for x in (-15,7):box('Corner guide',(x,y,.315),(3.9,.09,.012),6)
    # Expansion cuts are almost flush and never create a collider in the assembly space.
    for x in range(-28,21,4):box('Slab joint',(x,6,.312),(.012,41,.002),12)
    for y in range(-14,27,4):box('Slab joint',(-4,y,.312),(49,.012,.002),12)
    for x in (-19,11):
        for y in range(-10,23,3):box('Pedestrian route',(x,y,.318),(.08,1.5,.008),9)
    for x in (-24,16):text('SERVICE', (x,-10,.324), .70, rot=(0,0,0))
    text('ASSEMBLY 01',(-4,17,.324),1.15,rot=(0,0,0))

    group('Interior Wall lining')
    for x in (-28.73,20.73):
        box('Impact resistant lower wall',(x,6,2.4),(.08,41,4.2),12)
        box('Wall skirting',(x,6,.48),(.12,41,.28),2)
        for y in range(-12,27,3):box('Interior sheet seam',(x,y,38),(.09,.025,73.5),5)
    box('Rear impact wall',(-4,26.72,2.4),(49,.08,4.2),12)
    for x in range(-26,21,3):box('Rear sheet seam',(x,26.72,38),(.025,.08,73.5),5)

    group('Interior Primary steel')
    for x in (-28.1,20.1):
        for y in (-12,-3,6,15,24):
            ibeam(x,y,.5,73.8,.48)
            for z in (10.3,20.3,30.3,40.3,50.3,60.3,69.8):
                box('Bolted beam seat',(x,y,z),(.8,.9,.22),5)
    for z in (10.3,20.3,30.3,40.3,50.3,60.3,69.8):
        for x in (-28.05,20.05):box('Wall longitudinal beam',(x,6,z),(.35,38,.48),2)
        box('Rear girder',(-4,26.1,z),(48,.4,.48),2)
    for x in (-28,20):
        for y in (-12,15):
            for z in (1,21,41):
                beam((x,y,z),(x,y+9,z+19),.085,5,8)
                beam((x,y+9,z),(x,y,z+19),.085,5,8)

    # Each gallery is a separate mesh for culling and independent editing in Unity.
    for level in range(1,7):
        z=.3+10*level
        for side,x in (('West',-25),('East',17)):
            group(f'Interior {side} gallery {level:02}')
            if side=='West':
                deck('West gallery',(x,1.25,z-.09),(5.6,26.5,.18))
                deck('Stair bypass',(-22.8,19.25,z-.09),(1.2,9.5,.18))
                deck('Rear stair access',(x,23.4,z-.09),(5.6,1.2,.18))
            else:deck('East gallery',(x,6,z-.09),(5.6,36,.18))
            toe((x+(2.8 if side=='West' else -2.8),-12,z),
                (x+(2.8 if side=='West' else -2.8),24,z))
            toe((x-2.8,-12,z),(x+2.8,-12,z))
            # Exposed triangulated brackets support the galleries from the wall steel.
            for y in (-10,-2,6,13,23):
                outer=-28 if side=='West' else 20
                inner=-22.3 if side=='West' else 14.3
                beam((outer,y,z-2.8),(inner,y,z-.3),.09,2,8)
                beam((outer,y,z-.3),(inner,y,z-.3),.11,2,8)
            box('Level sign',(x, -11.92,z+1.8),(2.8,.06,.65),2)
            text(f'{side.upper()[0]} / {level:02}',(x,-11.98,z+1.55),.45)
        group(f'Interior Rear gallery {level:02}')
        deck('Rear gallery',(-4,24.5,z-.09),(44.4,3,.18))
        toe((-22.2,23,z),(14.2,23,z))
        for x in (-18,-10,-2,6,12):beam((x,26.2,z-2),(x,23.2,z-.2),.09,2,8)

    # Four flights per 10 m level: 178.6 mm risers, 371 mm going, 1.65 m clear width.
    for flight in range(24):
        level=flight//4
        group(f'Interior Stair tower {level+1:02}')
        x=-26.7 if flight%2==0 else -24.5
        y=16 if flight%2==0 else 21.2
        direction=1 if flight%2==0 else -1
        z=.3+flight*2.5
        steps=14
        for i in range(steps):
            box('Stair tread',(x,y+direction*(i+.5)*5.2/steps,z+(i+1)*2.5/steps-.045),(1.65,5.2/steps+.025,.09),5)
            box('Yellow nosing',(x,y+direction*(i+.05)*5.2/steps,z+(i+1)*2.5/steps+.003),(1.64,.035,.008),6)
        for dx in (-.86,.86):
            beam((x+dx,y,z-.08),(x+dx,y+direction*5.2,z+2.42),.10,2,6)
            rail((x+dx,y,z),(x+dx,y+direction*5.2,z+2.5),6)
        # Walkable slope collider avoids foot snagging on individual treads.
        run=(5.2**2+2.5**2)**.5
        colliders.append(dict(name='Stair ramp',center=(x,y+direction*2.6,z+1.16),size=(1.7,run,.16),rotation_x=direction*math.degrees(math.atan2(2.5,5.2))))
        end=y+direction*5.2
        ly=end+direction*.6
        deck('Stair landing',(-25.6,ly,z+2.41),(4.5,1.3,.18))
        toe((-27.85,ly+direction*.65,z+2.5),(-23.35,ly+direction*.65,z+2.5))
    group('Interior Stair tower posts')
    for x in (-27.9,-23.3):
        for y in (14.7,22.5):ibeam(x,y,.3,61.5,.22)

    group('Interior Freight lift')
    # Mesh cage against the east wall, outside the assembly and the pedestrian aisle.
    for x in (15,19.6):
        for y in (17.5,22.5):ibeam(x,y,.3,62,.22)
    for z in range(1,63,2):
        for x in (15,19.6):
            beam((x,17.5,z),(x,22.5,z),.028,5,4)
            beam((x,17.5,z),(x,22.5,z+1.8),.018,5,4)
        beam((15,22.5,z),(19.6,22.5,z),.028,5,4)
    for level in range(7):
        z=.3+level*10
        box('Lift landing gate',(17.3,17.48,z+1.1),(4.3,.1,2.2),2)
        box('Lift gate glazing',(17.3,17.4,z+1.25),(3.95,.05,1.7),4)
        for x in (15.8,17.3,18.8):beam((x,17.35,z+.4),(x,17.35,z+2),.025,6,6)
        box('Call panel',(14.55,17.4,z+1.15),(.25,.18,.4),5)
        cyl((14.55,17.27,z+1.2),.035,.02,10,8)
    deck('Freight lift floor',(17.3,20,.58),(4.2,4.5,.3))
    box('Hoist motor',(17.3,20,62.4),(2.2,1.7,1.4),5)
    for x in (16,18.6):beam((x,20,1),(x,20,62),.025,5,6)

    group('Interior Roof trusses')
    for y in (-11,0,11,23):
        for z in (72,74.5):box('Truss chord',(-4,y,z),(48,.22,.24),2)
        for i in range(12):
            x=-28+i*4
            beam((x,y,72),(x+2,y,74.5),.08,5,8)
            beam((x+2,y,74.5),(x+4,y,72),.08,5,8)
    group('Interior Crane rails')
    for x in (-26.5,18.5):
        box('Runway beam',(x,6,70.1),(.45,37,.8),2)
        box('Rail',(x,6,70.57),(.12,37,.15),5)
    group('Interior Bridge crane')
    for y in (9.2,12.8):
        box('Crane box girder',(-4,y,71.2),(45.8,.75,1.0),6)
        box('Crane lower flange',(-4,y,70.65),(46,.95,.12),2)
        for x in (-26.5,18.5):beam((x,y-.35,70.65),(x,y+.35,70.65),.35,5,16)
    # Park the trolley, wire ropes and hook over the east service zone.
    box('Hoist trolley',(15.8,11,71.9),(3.8,5.1,.65),2)
    beam((14.8,9.5,72.3),(17,9.5,72.3),.42,5,24)
    box('Hoist motor',(16.2,12,72.5),(1.6,1.5,.9),5)
    for x in (15.3,16.3):
        for y in (10.7,11.3):beam((x,y,61.7),(x,y,71.8),.018,5,6)
    box('Hook block',(15.8,11,61.5),(1.7,.8,1),6)
    # Curved open hook, not an intersecting ring in the working envelope.
    last=None
    for i in range(14):
        a=-pi*.95+i*pi*1.45/13
        p=(15.8+.5*math.cos(a),11,60.7+.5*math.sin(a))
        if last:beam(last,p,.10,5,8)
        last=p
    box('Crane capacity plate',(-4,8.80,71.2),(7,.04,.65),2)
    text('120 t  /  BRIDGE 01',(-4,8.76,70.98),.42)

    group('Interior Services')
    for x in (-28.3,20.3):
        for zz in (7,17,27,37,47,57,67):
            box('Cable tray',(x,5,zz),(.38,38,.16),5)
            for y in range(-12,25,3):box('Tray bracket',(x,y,zz-.2),(.6,.08,.4),2)
        for y in (-8,12):
            beam((x,y,.6),(x,y,70),.13,5,12)
            for z in range(2,69,4):annulus((x,y,z),.18,.125,.08,5,16)
    group('Interior Lighting fixtures')
    for z in (8.7,28.7,48.7,68.7):
        for x in (-21.8,13.8):
            for y in (-7,6,18):
                box('LED housing',(x,y,z),(.35,3,.15),2)
                box('LED diffuser',(x,y,z-.085),(.28,2.85,.03),14)
    for y in (-6,6,18):
        box('High bay light housing',(-4,y,74.8),(4,1.5,.24),2)
        box('High bay diffuser',(-4,y,74.66),(3.8,1.3,.035),14)

    def engine(x,y,scale=1):
        # Open nozzle with a throat, inner surface, injector, feed lines and turbopump.
        group('Interior Stored engines')
        z=.8
        box('Engine dolly',(x,y,.62),(2.7*scale,2.7*scale,.45),2)
        for dx in (-1,1):
            for dy in (-1,1):
                beam((x+dx*1.1*scale-.1,y+dy*1.1*scale,.48),(x+dx*1.1*scale+.1,y+dy*1.1*scale,.48),.20,7,12)
        profile=[(1.05,.0),(.94,.2),(.72,.65),(.45,1.3),(.31,1.9),(.29,2.2),(.24,2.2),(.26,1.9),(.4,1.3),(.67,.65),(.9,.2),(1.0,.0)]
        for i in range(48):
            a=i*2*pi/48;b=(i+1)*2*pi/48
            for k in range(len(profile)-1):
                r1,h1=profile[k];r2,h2=profile[k+1]
                face([(x+r1*scale*math.cos(a),y+r1*scale*math.sin(a),z+h1*scale),
                      (x+r1*scale*math.cos(b),y+r1*scale*math.sin(b),z+h1*scale),
                      (x+r2*scale*math.cos(b),y+r2*scale*math.sin(b),z+h2*scale),
                      (x+r2*scale*math.cos(a),y+r2*scale*math.sin(a),z+h2*scale)],5 if k<5 else 2,True)
        for i in range(32):
            a=i*pi/16
            beam((x+1.055*scale*math.cos(a),y+1.055*scale*math.sin(a),z),
                 (x+.315*scale*math.cos(a),y+.315*scale*math.sin(a),z+1.9*scale),.016,5,5)
        cyl((x,y,z+2.45*scale),.44*scale,.55*scale,2,32)
        annulus((x,y,z+2.77*scale),.6*scale,.16*scale,.15,5,32)
        for i in range(12):
            a=i*pi/6;cyl((x+.5*scale*math.cos(a),y+.5*scale*math.sin(a),z+2.89*scale),.035,.09,5,6)
        for dx in (-.65,.65):
            beam((x+dx*scale,y,z+1.3*scale),(x+dx*scale,y,z+3.3*scale),.09,5,10)
            beam((x+dx*scale,y,z+3.3*scale),(x,y,z+3.3*scale),.09,5,10)
        cyl((x+.8*scale,y,z+2.7*scale),.28*scale,.7*scale,5,24)
        annulus((x,y,z+.03),1.10*scale,1.0*scale,.1,5,48)
        colliders.append(dict(name='Engine on maintenance dolly',center=(x,y,2),size=(2.8*scale,2.8*scale,3.4*scale)))

    for x,y,s in ((16,-7,1.25),(16,-1,1.0),(16,5,.85),(-24,-7,1.05)):
        engine(x,y,s)

    group('Interior Tank transport cradles')
    for y in (1,10):
        x=-24
        solid('Transport stand',(x,y,.65),(5.0,6.7,.65),2)
        # Horizontal tank barrel aligned with Y, strapped to its transport cradle.
        beam((x,y-2.5,3.0),(x,y+2.5,3.0),1.9,1,48)
        for yy in (-2.5,2.5):
            beam((x,y+yy-.06,3),(x,y+yy+.06,3),1.96,5,48)
            beam((x,y+yy-.08,3),(x,y+yy+.08,3),1.25,12,48)
            for a in range(16):
                angle=a*pi/8
                beam((x+1.7*math.cos(angle),y+yy-.1,3+1.7*math.sin(angle)),
                     (x+1.7*math.cos(angle),y+yy+.1,3+1.7*math.sin(angle)),.04,5,6)
        for yy in (-1.9,1.9):
            for dx in (-1.65,1.65):box('Saddle support',(x+dx,y+yy,1.4),(.32,.45,1.4),5)
            # Rolled straps over each barrel, rotated into the XZ plane.
            rotation=api['Matrix'].Rotation(pi/2,3,'X')
            ring((x,y+yy,3),1.96,.065,2,48,rotation)
        for xx in (-2.2,2.2):
            for yy in (-2.7,2.7):beam((x+xx-.12,y+yy,.52),(x+xx+.12,y+yy,.52),.28,7,16)
        colliders.append(dict(name='Tank section on cradle',center=(x,y,2.7),size=(4.1,5.3,4.1)))

    group('Interior Fairings and adapters')
    for x in (-12,-5,2,9):
        y=24.4
        solid('Parts pallet',(x,y,.48),(4.8,3.8,.36),2)
        if x in (-12,2):
            # A split half-shell fairing exposes a separate inner skin and cut rims.
            for i in range(24):
                a=i*pi/24;b=(i+1)*pi/24
                for r,h,rr,hh in ((1.7,.7,1.7,3.6),(1.7,3.6,1.1,5.0),(1.1,5.0,.04,6.2)):
                    for inset in (0,.07):
                        shell=[(x+(r-inset)*math.cos(a),y+(r-inset)*math.sin(a)-.8,h),
                              (x+(r-inset)*math.cos(b),y+(r-inset)*math.sin(b)-.8,h),
                              (x+max(.008,rr-inset)*math.cos(b),y+max(.008,rr-inset)*math.sin(b)-.8,hh),
                              (x+max(.008,rr-inset)*math.cos(a),y+max(.008,rr-inset)*math.sin(a)-.8,hh)]
                        face(shell if inset==0 else shell[::-1],1 if inset==0 else 12,True)
            for dx in (-1.7,1.7):beam((x+dx,y-.8,.7),(x+dx,y-.8,3.6),.045,5,6)
        else:
            annulus((x,y,1.8),1.6,1.47,2.2,1,48)
            for z in (.72,2.87):annulus((x,y,z),1.73,1.42,.14,5,48)
            for i in range(16):
                a=i*pi/8;beam((x+1.6*math.cos(a),y+1.6*math.sin(a),.85),(x+1.6*math.cos(a),y+1.6*math.sin(a),2.7),.025,5,6)
        colliders.append(dict(name='Stored rocket shell',center=(x,y,3.5),size=(4.2,3.7,6.4)))

    group('Interior Tool bays')
    for x,y in ((-24,-11),(16,-11),(-20.3,15),(12,12)):
        solid('Workbench',(x,y,1),(2.8,1.15,.14),5)
        for dx in (-1.2,1.2):box('Bench leg',(x+dx,y,.68),(.12,1.0,.64),2)
        box('Drawer unit',(x-.6,y,.66),(1.1,1.0,.6),12)
        for z in (.46,.64,.82):
            box('Drawer reveal',(x-.6,y-.51,z),(1,.025,.025),2)
            box('Drawer handle',(x-.6,y-.56,z+.05),(.36,.08,.035),5)
        box('Monitor housing',(x+.7,y+.3,1.5),(.7,.12,.48),2)
        box('Monitor glass',(x+.7,y+.23,1.5),(.62,.025,.4),4)
        beam((x+.7,y+.3,1.1),(x+.7,y+.3,1.4),.04,5,6)
        box('Keyboard',(x+.7,y-.18,1.09),(.65,.28,.025),2)
        for dx in (-.75,-.4,0):box('Tool case',(x+dx,y-.2,1.15),(.22,.35,.16),7)
    group('Interior Storage racks')
    for x in (-27,19):
        for y in (-2,7):
            for dx in (-.6,.6):
                for dy in (-2.2,2.2):ibeam(x+dx,y+dy,.3,5,.09)
            for z in (1,2.5,4):
                box('Shelf',(x,y,z),(1.4,4.8,.1),5)
                for yy in (-1.5,0,1.5):
                    box('Transit case',(x,y+yy,z+.45),(1.1,1.25,.8),12)
                    box('Case latch',(x-.57,y+yy,z+.5),(.05,.3,.12),5)
                    box('Case identifier',(x-.59,y+yy-.3,z+.65),(.025,.22,.16),9)

    group('Interior Safety equipment')
    for x in (-21,13):
        for y in (-11,21):
            box('Fire station',(x,y,1.4),(.7,.35,1.3),10)
            cyl((x,y-.25,1.3),.14,.65,10,16)
            beam((x,y-.25,1.65),(x+.15,y-.25,1.85),.04,5,6)
            box('Safety placard',(x,y,2.4),(1,.08,.4),2)
            text('FIRE',(x,y-.06,2.27),.22)
    for x in (-18,10):
        for y in (-12,-4,4,12,21):
            cyl((x,y,.85),.09,1.1,6,10)
            cyl((x,y,.98),.095,.15,2,10)

    # Standalone markers are exported as empties, never as visible keep-out boxes.
    for name,position in (('RocketAssemblyOrigin',ASSEMBLY_ORIGIN),('ClearVolumeMin',CLEAR_MIN),('ClearVolumeMax',CLEAR_MAX)):
        obj=api['bpy'].data.objects.new(name,None);B.col.objects.link(obj);obj.location=position
        obj.empty_display_type='PLAIN_AXES';obj.empty_display_size=1
        api.setdefault('vab_markers',[]).append(obj)
