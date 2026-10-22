"""Author seven original TAP facilities, local-origin exports and a dressed campus.

Run in Blender: --background --python build_space_center.py
Every visible detail is geometry; the shared atlas is editable in ArmorPaint.
"""
import bpy
import math
import json
import sys
from mathutils import Vector, Matrix
from pathlib import Path
from collections import defaultdict

HERE=Path(__file__).resolve().parent
if str(HERE) not in sys.path:sys.path.insert(0,str(HERE))
ROOT=HERE.parent
PROJECT=ROOT.parents[1]
EXPORT=PROJECT/'Assets/TAP/Art/SpaceCenter/Models'
TEX=ROOT/'Textures'
PI=math.pi
scene=bpy.data.scenes.new('TAP Space Centre')
bpy.context.window.scene=scene
for old_scene in list(bpy.data.scenes):
    if old_scene!=scene:bpy.data.scenes.remove(old_scene)
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=1
scene.render.engine='CYCLES'
scene.cycles.samples=40
scene.cycles.use_denoising=True
scene.render.image_settings.file_format='PNG'
scene.view_settings.view_transform='AgX'
scene.view_settings.look='AgX - Medium High Contrast'
scene.view_settings.exposure=0
scene.render.film_transparent=False

mat=bpy.data.materials.new('SpaceCenter_PBR');mat.use_nodes=True
nodes=mat.node_tree.nodes;links=mat.node_tree.links
bsdf=nodes.get('Principled BSDF')
images={}
for channel in ['BaseColor','Roughness','Metallic','Normal']:
    image=bpy.data.images.load(str(TEX/f'SpaceCenter_{channel}.png'),check_existing=True)
    image.pack()
    if channel!='BaseColor':image.colorspace_settings.name='Non-Color'
    node=nodes.new('ShaderNodeTexImage');node.image=image;node.label=channel
    images[channel]=node
links.new(images['BaseColor'].outputs['Color'],bsdf.inputs['Base Color'])
links.new(images['Roughness'].outputs['Color'],bsdf.inputs['Roughness'])
links.new(images['Metallic'].outputs['Color'],bsdf.inputs['Metallic'])
normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.3
links.new(images['Normal'].outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],bsdf.inputs['Normal'])

class Builder:
    def __init__(self,name,offset):
        self.name=name;self.offset=offset
        self.col=bpy.data.collections.new(name);scene.collection.children.link(self.col)
        self.buffers=defaultdict(lambda:[[],[],[],[]])
        self.group='Structure';self.objects=[]
    def face(self,verts,surface,smooth=False):
        if self.group=='Signs' and surface==3:surface=2
        buf=self.buffers[self.group];n=len(buf[0]);buf[0].extend(verts)
        buf[1].append(tuple(range(n,n+len(verts))))
        # Atlas origin is lower left; leave eight pixels of tile padding.
        u=(surface%4)/4;v=1-(surface//4+1)/4;p=.004;w=.25-2*p
        if len(verts)==4:uv=[(0,0),(1,0),(1,1),(0,1)]
        else:uv=[(.5+.48*math.cos(2*PI*i/len(verts)),.5+.48*math.sin(2*PI*i/len(verts))) for i in range(len(verts))]
        buf[2].extend([(u+p+a*w,v+p+b*w) for a,b in uv]);buf[3].append(smooth)
    def finish(self):
        for name,(verts,faces,uvs,smooth) in self.buffers.items():
            mesh=bpy.data.meshes.new(self.name+' / '+name);mesh.from_pydata(verts,[],faces);mesh.update()
            uv=mesh.uv_layers.new(name='UVMap')
            uv.data.foreach_set('uv',[q for point in uvs for q in point])
            for p,s in zip(mesh.polygons,smooth):p.use_smooth=s
            obj=bpy.data.objects.new(name,mesh);self.col.objects.link(obj);obj.data.materials.append(mat)
            if name in ['Shells','Cladding','Foundation']:
                bevel=obj.modifiers.new('Soft manufactured edges','BEVEL');bevel.width=.005 if self.name=='01_VehicleAssembly' else .075;bevel.segments=2
                bevel.limit_method='ANGLE'
                normals=obj.modifiers.new('Face weighted normals','WEIGHTED_NORMAL')
            self.objects.append(obj)
        bpy.context.view_layer.update()
        return self

B=None
def group(name):B.group=name
def face(v,m,s=False):B.face(v,m,s)
def box(name,p,d,m=1,rot=0):
    x,y,z=p;dx,dy,dz=[n/2 for n in d];c=math.cos(rot);s=math.sin(rot)
    vs=[]
    for a,b,h in [(-dx,-dy,-dz),(dx,-dy,-dz),(dx,dy,-dz),(-dx,dy,-dz),(-dx,-dy,dz),(dx,-dy,dz),(dx,dy,dz),(-dx,dy,dz)]:
        vs.append((x+a*c-b*s,y+a*s+b*c,z+h))
    for ids in [(3,2,1,0),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)]:face([vs[i] for i in ids],m)

def pier(x,y,z,width,depth,height,lean=0,m=0):
    """A solid raked concrete pier, with a broad foot and an inset head."""
    vs=[(x+sx*width/2+(lean if top else 0),y+sy*depth/2,z+height*top)
        for top in (0,1) for sx,sy in ((-1,-1),(1,-1),(1,1),(-1,1))]
    for ids in ((3,2,1,0),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)):
        face([vs[i] for i in ids],m)

def facade_portal(x,y,width,height,depth=3.2):
    group('Load bearing facade')
    for side in (-1,1):
        pier(x+side*width/2,y,.35,1.8,depth,height,-side*1.2,0)
        box('Pier foot',(x+side*width/2,y,.75),(3,depth+1.2,.9),0)
    box('Deep roof fascia',(x,y,height+.25),(width+2.2,depth,1.6),1)
    box('Fascia shadow',(x,y-depth/2-.06,height-.38),(width-2,.08,.26),2)
    for side in (-1,1):
        box('Facade light',(x+side*(width/2-1.1),y-depth/2-.04,height/2),(.06,.04,height-2),9)

def beam(a,b,r=.12,m=5,sides=8):
    a=Vector(a);b=Vector(b);axis=(b-a).normalized()
    ref=Vector((0,0,1)) if abs(axis.z)<.98 else Vector((1,0,0))
    u=axis.cross(ref).normalized();v=axis.cross(u).normalized()
    rings=[[tuple(p+r*(math.cos(i*2*PI/sides)*u+math.sin(i*2*PI/sides)*v)) for i in range(sides)] for p in (a,b)]
    face(list(reversed(rings[0])),m);face(rings[1],m)
    for i in range(sides):j=(i+1)%sides;face([rings[0][i],rings[0][j],rings[1][j],rings[1][i]],m,True)

def cyl(p,r,h,m=5,n=32,r2=None):
    x,y,z=p;r2=r if r2 is None else r2
    lower=[(x+r*math.cos(i*2*PI/n),y+r*math.sin(i*2*PI/n),z-h/2) for i in range(n)]
    upper=[(x+r2*math.cos(i*2*PI/n),y+r2*math.sin(i*2*PI/n),z+h/2) for i in range(n)]
    face(list(reversed(lower)),m);face(upper,m)
    for i in range(n):j=(i+1)%n;face([lower[i],lower[j],upper[j],upper[i]],m,True)

def annulus(p,outer,inner,h,m=5,n=48):
    x,y,z=p
    for i in range(n):
        a=i*2*PI/n;b=(i+1)*2*PI/n
        def q(r,t,dz):return (x+r*math.cos(t),y+r*math.sin(t),z+dz)
        face([q(inner,a,h/2),q(outer,a,h/2),q(outer,b,h/2),q(inner,b,h/2)],m)
        face([q(inner,b,-h/2),q(outer,b,-h/2),q(outer,a,-h/2),q(inner,a,-h/2)],m)
        face([q(outer,a,-h/2),q(outer,b,-h/2),q(outer,b,h/2),q(outer,a,h/2)],m,True)
        face([q(inner,b,-h/2),q(inner,a,-h/2),q(inner,a,h/2),q(inner,b,h/2)],m,True)

def ring(p,r,t=.12,m=5,n=48,rotation=None):
    x,y,z=p
    for i in range(n):
        a=i*2*PI/n;b=(i+1)*2*PI/n
        pa=Vector((r*math.cos(a),r*math.sin(a),0));pb=Vector((r*math.cos(b),r*math.sin(b),0))
        if rotation:pa=rotation@pa;pb=rotation@pb
        beam(Vector(p)+pa,Vector(p)+pb,t,m,6)

def dome(p,r,m=1,rows=12,n=48):
    x,y,z=p
    for j in range(rows):
        a=j*PI/2/rows;b=(j+1)*PI/2/rows
        for i in range(n):
            c=i*2*PI/n;d=(i+1)*2*PI/n
            def point(t,u):return (x+r*math.cos(t)*math.cos(u),y+r*math.cos(t)*math.sin(u),z+r*math.sin(t))
            if j==rows-1:face([point(a,c),point(a,d),(x,y,z+r)],m,True)
            else:face([point(a,c),point(a,d),point(b,d),point(b,c)],m,True)

def text(label,p,size=1.1,m=9,rot=(PI/2,0,0)):
    curve=bpy.data.curves.new(label,'FONT');curve.body=label;curve.align_x='CENTER';curve.size=size;curve.extrude=.012
    obj=bpy.data.objects.new(label,curve);B.col.objects.link(obj);obj.location=p;obj.rotation_euler=rot
    # A dedicated constant color for signage avoids accidental tile sampling before conversion.
    name='Sign '+str(m);signmat=bpy.data.materials.get(name)
    if signmat is None:
        color=(.95,.95,.95,1)
        signmat=bpy.data.materials.new(name);signmat.diffuse_color=color;signmat.use_nodes=True
        signmat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=color
    curve.materials.append(signmat);B.objects.append(obj)

def railing(a,b,m=5):
    a=Vector(a);b=Vector(b);length=(b-a).length
    for dz in (.56,1.12):beam(a+Vector((0,0,dz)),b+Vector((0,0,dz)),.055,m,6)
    for i in range(max(1,math.ceil(length/2))+1):
        p=a.lerp(b,i/max(1,math.ceil(length/2)));beam(p,p+Vector((0,0,1.18)),.065,m,6)

def stairs(p,w,h,run=1,m=5,direction=1):
    x,y,z=p;steps=math.ceil(h/.24);depth=h*run/steps
    for i in range(steps):box('Tread',(x,y+direction*i*depth,z+(i+.5)*h/steps),(w,depth+.05,.12),m)
    for side in (-1,1):
        a=(x+side*w/2,y,z);b=(x+side*w/2,y+direction*(steps-1)*depth,z+h)
        beam(a,b,.15,2);railing(a,b)

def door(x,y,z,w=2.4,h=3.6):
    group('Doors and frames');box('Recess',(x,y,z+h/2),(w+.4,.18,h+.3),2)
    box('Door',(x,y-.12,z+h/2),(w,.12,h),12)
    box('Glazing',(x,y-.2,z+h*.67),(w*.72,.05,h*.28),4)
    beam((x+w*.32,y-.29,z+h*.35),(x+w*.32,y-.29,z+h*.51),.04,5)
    box('Canopy',(x,y-.8,z+h+.35),(w+1.2,2,.18),1)

def windows(x,y,z,nx,nz,spacing=3.3):
    group('Glazing and mullions')
    for k in range(nz):
        for j in range(nx):
            xx=x+j*spacing;zz=z+k*3.7
            box('Frame',(xx,y,zz),(2.65,.16,2.5),2)
            box('Pane',(xx,y-.1,zz),(2.38,.07,2.24),4)
            box('Transom',(xx,y-.16,zz),(2.4,.10,.07),5)
            box('Mullion',(xx,y-.16,zz),(.065,.10,2.25),5)
            box('Sill',(xx,y-.3,zz-1.3),(2.85,.65,.13),0)

def side_windows(x,y,z,count,floors,spacing=3.3):
    group('Side glazing and mullions')
    for k in range(floors):
        for j in range(count):
            yy=y+j*spacing;zz=z+k*3.7
            box('Side frame',(x,yy,zz),(.16,2.65,2.5),2)
            box('Side pane',(x+.1,yy,zz),(.07,2.38,2.24),4)
            box('Side transom',(x+.16,yy,zz),(.10,2.4,.07),5)
            box('Side mullion',(x+.16,yy,zz),(.10,.065,2.25),5)
            box('Side sill',(x+.3,yy,zz-1.3),(.65,2.85,.13),0)

def parapet(x,y,z,w,d):
    group('Roof edges and drainage')
    for xx in (x-w/2,x+w/2):box('Parapet',(xx,y,z+.35),(.22,d,.7),1)
    for yy in (y-d/2,y+d/2):box('Parapet',(x,yy,z+.35),(w,.22,.7),1)
    for xx in (x-w/2+.4,x+w/2-.4):
        beam((xx,y+d/2+.2,.5),(xx,y+d/2+.2,z),.075,5,8)
        for zz in range(2,int(z),3):box('Drain bracket',(xx,y+d/2+.1,zz),(.35,.35,.12),2)

def cabinet(x,y,z=0):
    group('Service cabinets')
    box('Cabinet plinth',(x,y,z+.2),(2.3,1.4,.4),0)
    box('Cabinet',(x,y,z+1.5),(2,1.1,2.2),12)
    box('Cabinet door seam',(x,y-.57,z+1.5),(.03,.03,2),2)
    for xx in (-.7,.7):
        box('Cabinet handle',(x+xx,y-.61,z+1.55),(.04,.07,.35),5)
        for zz in range(5):box('Cabinet vent',(x+xx,y-.59,z+.7+zz*.12),(.38,.025,.045),2)
    box('Warning plate',(x+.45,y-.62,z+2.1),(.35,.03,.3),6)
    for xx in (-.85,.85):
        for yy in (-.4,.4):cyl((x+xx,y+yy,z+.43),.06,.07,5,6)

def pipe_flange(p,axis=(1,0,0),r=.32):
    group('Valves and flanges');v=Vector(axis).normalized();p=Vector(p)
    beam(p-v*.08,p+v*.08,r,5,20)
    ref=Vector((0,0,1)) if abs(v.z)<.95 else Vector((1,0,0))
    u=v.cross(ref).normalized();w=v.cross(u)
    for i in range(8):
        a=i*PI/4;c=p+r*.8*(math.cos(a)*u+math.sin(a)*w)
        beam(c-v*.1,c+v*.1,.045,2,6)

def service_cart(x,y):
    group('Service vehicle')
    box('Chassis',(x,y,.75),(2.2,4.7,.35),2)
    box('Cab',(x,y-1.3,1.8),(2.1,1.7,1.8),3)
    box('Windscreen',(x,y-2.17,2),(1.85,.06,1.0),4)
    box('Roof',(x,y-1.3,2.78),(2.25,1.9,.12),1)
    box('Bed',(x,y+.9,1.2),(2.15,2.5,.6),1)
    for dx in (-1.1,1.1):
        for dy in (-1.45,1.45):
            beam((x+dx-.13,y+dy,.66),(x+dx+.13,y+dy,.66),.48,7,16)
            beam((x+dx-.17,y+dy,.66),(x+dx+.17,y+dy,.66),.22,5,12)
    for dx in (-.75,.75):box('Headlamp',(x+dx,y-2.24,1.3),(.4,.04,.25),14)
    cyl((x,y-1.3,2.95),.13,.24,6,12)

def hvac(x,y,z,w=3):
    group('Roof plant and vents');box('AC',(x,y,z+.8),(w,2.5,1.6),5)
    for i in range(8):box('Vent slit',(x-w*.4+i*w*.11,y-1.26,z+.8),(.10,.06,1.25),2)
    for xx in (-w*.24,w*.24):
        cyl((x+xx,y,z+1.64),.49,.09,2,20);ring((x+xx,y,z+1.72),.48,.04,5,20)
        for a in range(4):beam((x+xx,y,z+1.73),(x+xx+.44*math.cos(a*PI/2),y+.44*math.sin(a*PI/2),z+1.73),.04,5)

def foundation(w,d):
    group('Foundation');box('Apron',(0,0,.15),(w,d,.3),0)
    group('Site markings')
    for x in (-w/2+1.2,w/2-1.2):box('Apron edge',(x,0,.315),(.14,d-2,.025),9)
    for y in (-d/2+1.2,d/2-1.2):box('Apron edge',(0,y,.315),(w-2,.14,.025),9)

def hazard(x,y,z,w):
    group('Safety markings');box('Stripe base',(x,y,z),(w,.08,.65),6)
    for xx in range(int(w/.65)):
        left=x-w/2+.1+xx*.65
        face([(left,y-.05,z-.32),(left+.28,y-.05,z-.32),(left+.6,y-.05,z+.32),(left+.32,y-.05,z+.32)],2)

def bollards(y,xmin,xmax):
    group('Site furniture')
    for x in range(int(xmin),int(xmax)+1,3):
        cyl((x,y,.85),.14,1.1,6,10);cyl((x,y,.98),.145,.16,2,10)

def tank(x,y,z,r,h):
    group('Tanks and pipework');cyl((x,y,z+h/2),r,h,1,32);dome((x,y,z+h),r,1,6,32)
    for zz in (z+.4,z+h*.5,z+h-.25):ring((x,y,zz),r+.04,.08,5,32)
    for a in range(4):
        t=a*PI/2;cyl((x+math.cos(t)*r*.72,y+math.sin(t)*r*.72,z-.25),.16,.9,2,10)
    for i in range(int(h/.35)):
        beam((x-.35,y-r-.22,z+i*.35),(x+.35,y-r-.22,z+i*.35),.035,5,6)
    for xx in (-.38,.38):beam((x+xx,y-r-.22,z),(x+xx,y-r-.22,z+h+.7),.045,5,6)
    cyl((x,y,z+h+r+.17),r*.18,.3,5,20)
    group('Tank insulation seams')
    for i in range(12):
        a=i*PI/6
        beam((x+(r+.018)*math.cos(a),y+(r+.018)*math.sin(a),z+.3),
             (x+(r+.018)*math.cos(a),y+(r+.018)*math.sin(a),z+h-.2),.016,12,5)
    group('Tank fittings')
    beam((x,y-r,z+1.1),(x,y-r-1,z+1.1),.17,5,12)
    pipe_flange((x,y-r-.6,z+1.1),(0,1,0),.3)
    beam((x,y-r-.7,z+1.1),(x,y-r-.7,z+1.8),.06,3,8)
    ring((x,y-r-.7,z+1.85),.26,.04,10,16)
    ring((x,y,z+h+.1),r+.08,.045,5,32)
    for a in range(8):
        t=a*PI/4
        beam((x+(r+.08)*math.cos(t),y+(r+.08)*math.sin(t),z+h),
             (x+(r+.08)*math.cos(t),y+(r+.08)*math.sin(t),z+h+1),.035,5,6)
    ring((x,y,z+h+1),r+.08,.045,5,32)

def flood(x,y,h=10):
    group('Floodlights');cyl((x,y,h/2),.12,h,5,12)
    beam((x-1,y,h),(x+1,y,h),.08,5)
    for dx in (-.65,.65):
        box('Lamp housing',(x+dx,y-.15,h),(1,.5,.65),2);box('Lamp',(x+dx,y-.42,h),(.85,.03,.48),14)

def vab():
    foundation(80,74)
    # Thin, closed wall sections leave a real high-bay interior and roll-out opening.
    group('VAB West wall');box('West skin',(-28.89,6,38),(.22,42,75.4),1)
    group('VAB East wall');box('East skin',(20.89,6,38),(.22,42,75.4),1)
    group('VAB Rear wall');box('Rear skin',(-4,26.89,38),(49.56,.22,75.4),1)
    group('VAB Front wall')
    for x in (-22.75,14.75):box('Door jamb wall',(x,-14.89,38),(12.5,.22,75.4),1)
    box('Door header',(-4,-14.89,67.9),(25,.22,15.6),1)
    group('VAB Roof');box('Roof sheet',(-4,6,75.59),(49.56,41.56,.22),1)
    group('Shells')
    box('Low bay',(25,6,11),(10,42,21.4),12)
    for x in (-24,16):box('Administration annex',(x,-23,8.1),(10,15,15.6),1)
    group('Cladding')
    box('Roof cap',(-4,6,75.75),(50.1,42.1,.1),2)
    for x in range(-27,22,3):
        box('Rear rib',(x,27.025,38),(.035,.05,74),5)
        if -16.5<x<8.5:box('Header rib',(x,-15.025,67.9),(.035,.05,15.6),5)
        else:box('Front rib',(x,-15.025,38),(.035,.05,74),5)
    for z in (15,35,55,73):
        if z>60.1:box('Front panel seam',(-4,-15.035,z),(49,.03,.035),2)
        else:
            for x in (-22.75,14.75):box('Front panel seam',(x,-15.035,z),(12.4,.03,.035),2)
        box('Side panel seam',(-29.035,6,z),(.03,42,.035),2)
        box('Side panel seam',(21.035,6,z),(.03,42,.035),2)
    for y in range(-14,28,3):
        for x in (-29.025,21.025):box('Side cladding ribs',(x,y,43),(.05,.035,64),5)
    box('Side service band',(21.055,6,54),(.035,42,.35),12)
    group('Side louvers')
    for y in (-9,0,9,18):
        box('Louver recess',(21.3,y,28),(.2,5.6,7),2)
        for z in range(25,32):box('Louver blade',(21.5,y,z),(.5,5.3,.22),5)
    group('Facade maintenance')
    for y in (23,25):beam((21.7,y,22),(21.7,y,76),.045,5,6)
    for z in range(22,77):beam((21.7,23,z),(21.7,25,z),.04,5,6)
    box('Maintenance platform',(23.2,24,45),(4.2,5,.3),5)
    railing((25.1,21.6,45.2),(25.1,26.4,45.2))
    railing((21.5,21.6,45.2),(25.1,21.6,45.2))
    box('Side service band',(-29.055,6,19),(.035,42,.35),12)
    # Vertical roller-door panels and a recessed central seam.
    group('Hangar door and tracks')
    for i in range(2):
        group('VAB Door Left' if i==0 else 'VAB Door Right')
        x=-10.25+i*12.5;box('Door leaf',(x,-15.23,30.2),(12.5,.09,59.8),5)
        for z in range(3,59,2):box('Door ribs',(x,-15.3,z),(11.8,.035,.05),2)
        for xx in (-5,5):box('Door guide',(x+xx,-15.35,31),(.055,.07,57),1)
    group('Hangar door and tracks')
    for x in (-17,9):box('Track',(x,-15.3,31),(.09,.15,61),3)
    group('Threshold paint');box('Threshold stripe',(-4,-15.9,.322),(25,.5,.012),6)
    group('Roof crane');box('Crane gantry',(-4,6,78),(35,2,1.8),6)
    for x in (-21,13):box('Crane end truck',(x,6,77.5),(2,13,.7),2)
    box('Crane cab',(3,4.5,76),(2.7,2.2,2.2),3)
    group('Roof rails');railing((-29,-15,77),(-29,27,77));railing((21,-15,77),(21,27,77));railing((-29,27,77),(21,27,77))
    for x in (-21,-10,1,12):hvac(x,19,77,4)
    windows(-27.3,-30.6,5,3,3);windows(12.7,-30.6,5,3,3)
    for x in (-21,16):door(x,-30.7,.4)
    group('Signs');box('VAB sign',(-4,-15.8,66),(20,.28,4.8),3);text('VEHICLE ASSEMBLY',(-4,-16,65.5),1.45)
    text('01',(-24,-15.5,60),4.5)
    group('Service ducts')
    for x in (23,27):
        beam((x,-10,1),(x,-10,24),.4,5,12);beam((x,-10,24),(x,0,24),.4,5,12)
        for z in range(3,23,4):box('Pipe support',(x,-9.4,z),(.9,1.2,.2),2)
    stairs((32,-12,.4),2,6,1.6)
    bollards(-33,-28,-19);bollards(-33,12,22);flood(-35,-29,13);flood(34,-29,13)
    group('Apron lanes')
    for x in (-9,1):box('Crawler lane',(x,-26,.34),(.18,20,.035),6)
    for y in (14,20):cabinet(32,y,.3)
    service_cart(-34,16)
    group('Flush cladding')
    for x in (-29.6,21.6):
        skin_x=-29.055 if x<0 else 21.055
        box('Service spine',(skin_x,24,38.1),(.045,5.5,75.4),12)
        for y in (-9,0,9,18):box('Upper cladding cassette',(skin_x,y,52.5),(.045,7.5,37),12)
    # The cladding meets at the corners; there is no external corner column or portal.
    group('Door edge seals')
    for x in (-16.6,8.6):box('Door seal',(x,-15.29,31),(.055,.04,59),7)
    box('Door head seal',(-4,-15.29,60.6),(25.2,.04,.055),7)
    for x in (-24,16):
        group('Office plinths');box('Pod base',(x,-23,.38),(10,15,.12),12)
        group('Office perimeter trim')
        for dx in (-4.95,4.95):box('Office mullion',(x+dx,-30.54,8.1),(.045,.065,15.4),5)
        box('Office fascia',(x,-23,15.94),(10.04,15.04,.06),1)
    # Roof equipment remains seated on the thinner roof coping.
    for label in ('Roof crane','Roof rails','Roof plant and vents'):
        vertices=B.buffers[label][0]
        B.buffers[label][0]=[(x,y,z-1.2) for x,y,z in vertices]
    from vab_interior import furnish
    furnish(globals())

def launch_pad():
    foundation(86,78)
    group('Shells')
    # Two banks leave a genuine open flame trench through the deck.
    for x in (-14.5,14.5):box('Raised pad',(x,1,2.25),(19,42,3.9),0)
    for y in (-17,19):box('Deck bridge',(0,y,3.8),(10,6,.8),0)
    box('Flame trench floor',(0,1,.43),(10,45,.25),15)
    box('Deflector',(0,8,1.3),(8,7,1.8),15,PI/12)
    # Broad sloping crawler approach with modeled side walls.
    group('Crawler ramp')
    for a,b in [(-24,-4.5),(4.5,24)]:
        face([(a,-36,.35),(b,-36,.35),(b,-20,4.2),(a,-20,4.2)],0)
        face([(a,-36,.35),(a,-20,4.2),(a,-20,.35)],0)
        face([(b,-20,.35),(b,-20,4.2),(b,-36,.35)],0)
        for x in (a+.8,b-.8):beam((x,-36,.42),(x,-20,4.3),.07,6,6)
    group('Launch table');annulus((0,1,6.5),8,4.4,2,2,64)
    for i in range(6):
        a=i*PI/3;x=6.3*math.cos(a);y=1+6.3*math.sin(a)
        cyl((x,y,4.8),.65,2.8,5,16)
        box('Mount anchor',(x,y,4.3),(2,2,.35),1)
    group('Tower foundations');tx,ty=-21,12
    box('Tower mat',(tx,ty,1.7),(17,18,2.7),0)
    for x in (-6,6):
        for y in (-6,6):
            box('Tower footing',(tx+x,ty+y,5.2),(4.1,4.1,5.4),0)
            box('Anchor shoe',(tx+x,ty+y,8),(2.5,2.5,.55),5)
            for dx in (-.95,.95):
                for dy in (-.95,.95):cyl((tx+x+dx,ty+y+dy,8.4),.11,.4,2,6)
    group('Tower megastructure')
    for x in (-6,6):
        for y in (-6,6):box('Box column',(tx+x,ty+y,45),(1.3,1.3,74),3)
    for level in range(8,80,9):
        for y in (-6,6):
            box('Ring beam',(tx,ty+y,level),(13.3,1.05,1.1),3)
            beam((tx-6,ty+y,level),(tx+6,ty+y,level+9),.28,5,8)
            beam((tx+6,ty+y,level),(tx-6,ty+y,level+9),.28,5,8)
        for x in (-6,6):
            box('Ring beam',(tx+x,ty,level),(1.05,13.3,1.1),3)
            beam((tx+x,ty-6,level),(tx+x,ty+6,level+9),.28,5,8)
        box('Service deck',(tx,ty,level),(12,12,.32),5)
    group('Lift core and hoist')
    box('Enclosed lift core',(tx-2,ty,44),(3.7,5,72),2)
    for z in range(12,81,9):box('Lift landing',(tx+.05,ty,z),(.12,4,2.8),12)
    for y in (ty-6.8,ty+6.8):
        box('Carriage rail',(tx+6.7,y,45),(.45,.48,72),5)
        for z in range(9,81,3):box('Rail bracket',(tx+6.2,y,z),(1.4,.6,.22),2)
    box('Hoist cap',(tx,ty,82),(15,15,2.2),3)
    for y in (ty-5,ty+5):
        beam((tx+4,y,82),(tx+4,y,86),.18,5,12)
        beam((tx+4,y-1.5,84),(tx+4,y+1.5,84),1,2,24)
        for yy in (-.5,.5):beam((tx+4,y+yy,84),(tx+6.9,y+yy,57),.035,5,6)
    group('Catch carriage')
    box('Moving carriage',(tx+7.2,ty,56),(3,17.4,7),2)
    for y in (ty-7,ty+7):
        cyl((tx+8,y,58.1),1.35,2.4,5,32)
        for z in (54,58):box('Roller housing',(tx+6.8,y,z),(2.1,2.6,1.4),5)
    group('Paired catch arms')
    for side in (-1,1):
        start=Vector((tx+8,ty+side*7,57));end=Vector((10,1+side*8,57))
        axis=end-start;length=axis.length;angle=math.atan2(axis.y,axis.x)
        u=Vector((-math.sin(angle),math.cos(angle),0))
        middle=(start+end)/2
        for h in (-1.7,1.7):
            for edge in (-1,1):
                center=middle+u*edge*1.15+Vector((0,0,h))
                box('Catch box chord',center,(length,.5,.65),2,angle)
        for j in range(7):
            a=start.lerp(end,j/7);b=start.lerp(end,(j+1)/7)
            for edge in (-1,1):
                beam(a+u*edge*1.15+Vector((0,0,-1.7)),b+u*edge*1.15+Vector((0,0,1.7)),.16,5,8)
                beam(a+u*edge*1.15+Vector((0,0,1.7)),b+u*edge*1.15+Vector((0,0,-1.7)),.16,5,8)
            beam(a-u*1.15+Vector((0,0,1.7)),a+u*1.15+Vector((0,0,1.7)),.18,2,8)
        box('Capture rail',middle+Vector((0,0,2.13)),(length-1.5,1.5,.35),7,angle)
        box('Catch tip',end+Vector((0,0,1.75)),(1.2,2.8,.55),6,angle)
        lower=Vector((tx+8,ty+side*7,51.3));upper=start.lerp(end,.48)+Vector((0,0,-1.7))
        beam(lower,lower.lerp(upper,.6),.36,2,16);beam(lower.lerp(upper,.5),upper,.22,5,16)
    group('Upper umbilical arm')
    box('QD support',(-7,ty,73),(16,3.3,1.4),3)
    beam((-15,ty,68),(0,ty,72.5),.35,5,10)
    box('QD head',(.6,ty,72.3),(2.8,3.9,3.2),12)
    railing((-15,ty-1.8,74),(0,ty-1.8,74))
    group('Tower service risers')
    for y in (ty-3,ty,ty+3):
        beam((tx-6.9,y,8),(tx-6.9,y,81),.24,5,14)
        for z in range(12,80,6):pipe_flange((tx-6.9,y,z),(0,0,1),.37)
    beam((tx,ty,83),(tx,ty,91),.10,5,10)
    group('Deck rails');railing((-24,-17,4.3),(-24,22,4.3));railing((24,-17,4.3),(24,22,4.3));railing((-24,22,4.3),(24,22,4.3))
    stairs((28,-14,.4),2.2,4,1.7)
    for x in (29,36):tank(x,14,1,2.7,8)
    group('Deluge pipework')
    for x in (-8,8):
        beam((x,22,1),(x,0,1),.32,5,14);beam((x,0,1),(x,0,5),.32,5,14)
        beam((x,0,5),(x*.65,0,5),.22,5,12)
    for x,y in [(-36,-27),(36,-27),(36,30)]:flood(x,y,19)
    hazard(0,-20.4,3.65,47);text('LAUNCH COMPLEX 02',(14,-20.6,2.2),.85)
    for y in (-1,4):cabinet(35,y,.3)
    for x in (-8,8):pipe_flange((x,10,1),(0,1,0),.5)
    group('Deluge pump skid');box('Pump base',(32,-9,.8),(9,5,.9),0)
    for x in (29,33,36):
        beam((x,-10,1.8),(x,-7,1.8),.5,12,20)
        pipe_flange((x,-7,1.8),(0,1,0),.7)
        box('Motor',(x,-10.5,1.6),(1,1.5,1),2)
    group('Safety perimeter')
    for y in (-29,30):
        for x in (-27,27):cyl((x,y,1.2),.15,1.8,6,10)
    service_cart(-34,18)

def science():
    foundation(82,68)
    group('Shells');box('West laboratories',(-20,1,7),(19,39,13.4),1)
    box('East laboratories',(20,1,9),(19,39,17.4),1)
    box('Atrium',(0,9,10),(22,23,19.4),12)
    box('Connecting gallery',(0,-10,5),(23,7,9.4),4)
    cyl((0,12,21),10,2.4,2,64);dome((0,12,22.2),10,1,16,64)
    group('Observatory slit');box('Shutter ridge',(0,12,30.7),(2.3,13.8,1),2)
    box('Sliding shutter',(0,6.5,28.5),(2.5,.2,5),5)
    for a in range(8):
        t=a*PI/4;beam((10*math.cos(t),12+10*math.sin(t),22.3),(6*math.cos(t),12+6*math.sin(t),30),.075,5)
    group('Cladding')
    for x,z in [(-20,14),(20,18)]:box('Roof',(x,1,z),(20,40,.45),2)
    for x in (-30,-10,10,30):box('Concrete pier',(x,-18.7,7.3),(.8,1,14),0)
    windows(-26,-18.65,4,5,3);windows(13.5,-18.65,4,5,4)
    for x in (-20,20):door(x,-18.8,.4,2.8,3.7)
    for x in (-24,-16,16,24):hvac(x,12,14.3 if x<0 else 18.3)
    group('Courtyard');cyl((0,-24,.7),7,.6,0,48);cyl((0,-24,1.05),5.7,.1,4,48)
    for a in range(8):
        t=a*PI/4;box('Bench',(10*math.cos(t),-24+6*math.sin(t),.95),(3,.6,.25),12,t+PI/2)
    group('Signs');box('Science sign',(0,-13.65,8),(19,.22,2.5),3);text('SCIENCE & RESEARCH',(0,-13.85,7.5),1)
    group('Roof apparatus')
    for x in (-25,-16):
        cyl((x,5,16.2),.6,3.8,5,16);cyl((x,5,18.3),.9,.25,2,20)
    tank(33,15,1,2.5,6);bollards(-22,-28,-13);bollards(-22,12,29)
    side_windows(29.65,-14.5,4,10,4)
    parapet(-20,1,14.2,19,39);parapet(20,1,18.2,19,39)
    cabinet(33,-7,.3);cabinet(33,-11,.3)
    group('Laboratory service manifolds')
    for y in (-6,0,6):
        beam((-32,y,1),(-32,y,11),.18,5,12)
        beam((-32,y,11),(-28,y,11),.18,5,12)
        pipe_flange((-32,y,4),(0,0,1),.29)
    service_cart(-35,-23)

def dish(x,y,z,r,tilt=PI/6,azimuth=0):
    group('Parabolic reflectors');R=Matrix.Rotation(azimuth,3,'Z')@Matrix.Rotation(tilt,3,'X')
    origin=Vector((x,y,z))
    def p(rr,t,back=0):return tuple(origin+R@Vector((rr*math.cos(t),rr*math.sin(t),rr*rr/(r*2.4)+back)))
    for j in range(12):
        lo=j*r/12;hi=(j+1)*r/12
        for i in range(64):
            a=i*2*PI/64;b=(i+1)*2*PI/64
            if j==0:face([p(0,0),p(hi,a),p(hi,b)],1,True)
            else:face([p(lo,a),p(hi,a),p(hi,b),p(lo,b)],1,True)
            # Back skin makes the reflector a closed, visibly thick shell.
            if j==0:face([p(hi,b,-.17),p(hi,a,-.17),p(0,0,-.17)],5,True)
            else:face([p(lo,b,-.17),p(hi,b,-.17),p(hi,a,-.17),p(lo,a,-.17)],5,True)
    for i in range(64):
        a=i*2*PI/64;b=(i+1)*2*PI/64
        face([p(r,a),p(r,a,-.17),p(r,b,-.17),p(r,b)],5)
    group('Reflector ribs and feed')
    focus=origin+R@Vector((0,0,r*.85))
    for i in range(12):
        t=i*PI/6
        for j in range(8):beam(p(j*r/8,t,-.27),p((j+1)*r/8,t,-.27),.06 if r<6 else .10,5,6)
        for j in range(1,8):beam(p(j*r/8,t,.025),p((j+1)*r/8,t,.025),.018 if r<6 else .028,12,5)
        if i%4==0:beam(p(r*.86,t),focus,.09,2,8)
    beam(focus-R@Vector((0,0,.55)),focus+R@Vector((0,0,.55)),.3,3,16)
    ring(origin+R@Vector((0,0,r/2.4)),r,.12,5,64,R)
    group('Dish mount');cyl((x,y,2),r*.38,3.4,0,32)
    cyl((x,y,4),r*.30,.65,2,32)
    for dx in (-r*.25,r*.25):beam((x+dx,y,4),(x+dx,y,z-.6),.42,5,12)
    beam((x-r*.34,y,z-.65),(x+r*.34,y,z-.65),.65,2,24)
    box('Azimuth cabinet',(x+r*.30,y-1.2,4),(1.1,1.5,2),3)
    group('Dish base bolts')
    for i in range(12):
        t=i*PI/6;cyl((x+r*.32*math.cos(t),y+r*.32*math.sin(t),3.85),.09,.15,5,6)

def tracking():
    foundation(86,73)
    group('Shells');box('Control block',(-8,-18,5.6),(39,18,10.6),1)
    box('Roof',(-8,-18,11.2),(40,19,.5),2)
    windows(-24,-27.15,5,10,2)
    door(-8,-27.3,.4,3)
    dish(-11,7,11,12,PI/5,-.2)
    dish(25,-6,6.6,6.3,PI/3,.8)
    dish(24,22,6.1,5.4,PI/7,-.7)
    hvac(-20,-16,11.5);hvac(1,-16,11.5)
    group('Cable trenches')
    for x in (-11,24):
        box('Covered cable tray',(x,-3,.5),(1,30,.25),2)
        for y in range(-15,12,2):box('Tray seam',(x,y,.64),(1,.09,.06),5)
    group('Signs');box('Tracking sign',(-8,-27.3,9.2),(27,.2,2),3);text('DEEP SPACE NETWORK',(-8,-27.5,8.9),1)
    bollards(-30,-25,11);flood(-37,-28)
    side_windows(11.65,-23.5,5,4,2)
    parapet(-8,-18,11.5,39,18)
    for x in (16,20):cabinet(x,-24,.3)
    group('Antenna base access');stairs((-22,3,.4),1.8,3.2,1.2)
    railing((-25,8,.4),(-25,19,.4))
    service_cart(-32,-18)

def training():
    foundation(80,73)
    group('Shells');box('Crew school',(-15,-12,8),(33,23,15.4),1)
    box('Simulation wing',(18,-7,6),(27,33,11.4),12)
    # Exposed annular centrifuge with a pair of training gondolas.
    cyl((-10,18,1),14,1.3,0,64)
    cyl((-10,18,3),2.5,4,2,32);cyl((-10,18,5.3),3,.6,3,32)
    group('Centrifuge');ring((-10,18,1.8),12.7,.24,5,64)
    angle=.35
    for side in (-1,1):
        end=(-10+side*11*math.cos(angle),18+side*11*math.sin(angle),5.4)
        beam((-10,18,5.4),end,.65,5,12)
        box('Crew gondola',(end[0],end[1],5.5),(4,3,3.2),1,angle)
        box('Gondola glass',(end[0],end[1]-.9,6),(2.8,1.4,1.4),4,angle)
        cyl((end[0],end[1],7.2),.4,.15,3,16)
    group('Pool enclosure');box('Neutral buoyancy building',(24,18,5),(23,25,9.4),1)
    box('Pool',(24,18,9.8),(20,22,.18),4)
    for x in (13,35):
        for y in (7,29):pier(x,y,10,.65,.65,7 if x==13 else 10,0,1)
    for y in (7,12.5,18,23.5,29):
        beam((13,y,17),(35,y,20),.25,1)
    for x in (13,24,35):beam((x,7,17+(x-13)*3/22),(x,29,17+(x-13)*3/22),.2,1)
    group('Pool glass roof')
    face([(13,7,17),(35,7,20),(35,29,20),(13,29,17)],4)
    windows(-28,-23.7,5,8,3);windows(9,-23.7,5,7,2)
    door(-15,-23.9,.4,3);door(18,-23.9,.4)
    for x in (-24,-8):hvac(x,-7,16)
    group('Signs');box('Crew sign',(-15,-23.9,13.6),(27,.22,2.5),3);text('CREW TRAINING',(-15,-24.1,13.2),1.25)
    text('ASTRAEA ACADEMY',(18,-24,9.6),.8)
    group('Training capsule');cyl((-32,17,2.1),2.3,3,1,32,r2=1.5)
    cyl((-32,17,4.2),1.5,1.3,1,32,r2=.5);cyl((-32,17,.6),2.6,.5,2,32)
    box('Hatch',(-32,15,2.2),(1.2,.3,1.7),2);stairs((-32,11,.4),1.5,1.5,1.2)
    bollards(-27,-28,30)
    side_windows(31.65,-18.5,4,7,2)
    parapet(-15,-12,15.9,33,23);parapet(18,-7,11.9,27,33)
    cabinet(35,-12,.3)
    group('EVA training yard')
    box('EVA platform',(-29,26,1),(12,8,1.3),5)
    for x in (-34,-24):beam((x,26,1.7),(x,26,7),.12,3)
    beam((-34,26,7),(-24,26,7),.15,3)
    for x in (-32,-29,-26):
        beam((x,26,6.8),(x,26,4),.035,2,6)
        ring((x,26,3.8),.6,.055,5,20,Matrix.Rotation(PI/2,3,'X'))
    stairs((-29,20,.4),2,1,1.2)
    service_cart(-34,-12)

def testing():
    foundation(90,82)
    group('Foundation');box('Test pit',(5,14,.5),(26,39,.4),15)
    group('Shells');box('Instrumentation bunker',(-24,-18,4),(25,26,7.4),0)
    box('Workshop',(16,-24,7),(40,17,13.4),1)
    group('Test stand')
    for x in (-6,16):
        for y in (6,24):
            box('Pylon',(x,y,16),(2.3,2.3,31),0)
            box('Steel column',(x,y,27),(.8,.8,16),3)
    for z in (10,21,33):
        for y in (6,24):beam((-6,y,z),(16,y,z),.6,3,8)
        for x in (-6,16):beam((x,6,z),(x,24,z),.6,3,8)
    for y in (6,24):
        for x in (-6,5):
            beam((x,y,10),(x+11,y,21),.24,5);beam((x+11,y,10),(x,y,21),.24,5)
            beam((x,y,21),(x+11,y,33),.24,5)
    box('Service platform',(5,15,22),(23,20,.4),5)
    group('Engine test article');cyl((5,15,20),1.5,3,5,32,r2=1)
    cyl((5,15,17),2.8,3.2,2,40,r2=1.5)
    ring((5,15,15.4),2.8,.12,5,40)
    for i in range(24):
        a=i*2*PI/24;beam((5+2.75*math.cos(a),15+2.75*math.sin(a),15.5),(5+1.45*math.cos(a),15+1.45*math.sin(a),18.6),.055,5,6)
    group('Test stand access')
    for z in (10.5,22.3,33.3):
        railing((-6,5,z),(16,5,z));railing((16,5,z),(16,24,z));railing((-6,24,z),(16,24,z))
    for i in range(5):
        x=-11 if i%2==0 else -15
        direction=1 if i%2==0 else -1
        start=4 if direction==1 else 4+17*4.2*1.25/18
        stairs((x,start,1+i*4.2),2,4.2,1.25,direction=direction)
        landing=9.4 if direction==1 else 3.6
        box('Stair landing',(-13,landing,5.2+i*4.2),(6,2,.2),5)
    box('Top access bridge',(-9.3,9.4,22),(7.5,2,.25),5)
    railing((-13,10.5,22.2),(-5.5,10.5,22.2))
    group('Flame deflector')
    for i in range(12):box('Refractory block',(5,4+i*2.3,1+i*.19),(15,2.25,1.3),15)
    for x in (29,37):tank(x,15,1.2,3.1,12)
    group('Fuel lines')
    for y in (7,10,13):
        beam((36,y,3),(20,y,3),.18,5,12);beam((20,y,3),(20,y,24),.18,5,12)
        beam((20,y,24),(7,y,24),.18,5,12)
    windows(-31,-31.2,4,5,1)
    for x in (5,25):
        group('Workshop doors');box('Rollup',(x,-32.65,5),(12,.2,9),2)
        for z in range(1,10):box('Door slat',(x,-32.8,z),(11.8,.12,.1),5)
        hazard(x,-32.9,.9,12)
    door(-24,-31.3,.4)
    group('Signs');text('PROPULSION TEST FACILITY',(16,-32.85,12),1.05)
    for x in (-36,40):flood(x,-35,13)
    group('Blast walls')
    for x in (-34,-29):box('Blast baffle',(x,20,4.3),(1.8,31,8),0)
    side_windows(36.15,-28,5,3,2)
    parapet(16,-24,13.9,40,17)
    for x in (-29,-20):hvac(x,-18,7.9)
    for y in (5,10,15):cabinet(-21,y,.3)
    for y in (7,10,13):pipe_flange((25,y,3),(1,0,0),.32)
    group('Test platform floor grating')
    for x in range(-4,16):box('Grating line',(x,15,22.23),(.045,18,.035),2)
    for y in range(7,25):box('Grating line',(5,y,22.24),(20,.045,.035),2)
    service_cart(-38,-20)

def mission_control():
    foundation(78,65)
    group('Shells');box('Operations hall',(0,5,6.6),(55,29,12.6),1)
    box('Control tower',(13,8,17),(19,21,32.7),1)
    box('Tower band',(13,8,25),(19.6,21.6,1),3)
    box('Roof',(-8,5,13.2),(41,30,.6),2)
    group('Control tower glazing')
    for z in (28,31):
        box('Front glass',(13,-2.65,z),(19.2,.18,2.3),4)
        box('Side glass',(22.65,8,z),(.18,21.2,2.3),4)
        for x in range(5,23,3):box('Mullion',(x,-2.85,z),(.12,.15,2.4),5)
        for y in range(-1,18,3):box('Mullion',(22.85,y,z),(.15,.12,2.4),5)
    group('Cladding');box('Tower crown',(13,8,33.7),(21,23,1),2)
    box('Entry canopy',(-9,-16,4.5),(30,12,.35),1)
    for x in (-22,4):beam((x,-20,.4),(x,-20,4.5),.14,5,12)
    windows(-23,-9.65,5,9,2)
    door(-9,-9.8,.4,4,4.2)
    group('Entry stairs')
    for i in range(3):box('Step',(-9,-21+i*.5,.37+i*.14),(30-i*.2,1,.18),0)
    group('Tower aerials');beam((13,8,34),(13,8,43),.13,5,12)
    for z in (37,39,41):beam((10,8,z),(16,8,z),.065,5)
    cyl((13,8,43.4),.25,.6,10,12)
    for x in (-23,-13,-3):hvac(x,11,13.5)
    group('Signs');box('Mission sign',(-9,-9.85,10),(31,.18,2),3);text('ASTRAEA MISSION CONTROL',(-9,-10,9.6),1.05)
    group('Flag court')
    for x in (-27,-22,-17):
        beam((x,-27,.4),(x,-27,10),.07,5);box('Flag',(x+1.1,-27,8.5),(2.2,.06,1.25),3 if x==-22 else 12)
    cyl((22,-21,1),5,1.2,0,40)
    group('Monument');beam((22,-21,1.6),(22,-21,8),.6,5,24)
    cyl((22,-21,8.6),.6,1.3,1,24,r2=.03)
    for a in range(3):
        t=a*2*PI/3
        face([(22+.6*math.cos(t),-21+.6*math.sin(t),2),(22+2*math.cos(t),-21+2*math.sin(t),1.8),(22+.6*math.cos(t),-21+.6*math.sin(t),5)],3)
    bollards(-24,-10,10)
    side_windows(27.65,-5,5,6,2)
    parapet(-8,5,13.5,41,29)
    for y in (2,8):cabinet(31,y,.3)
    group('Roof cable routes')
    for x in (-23,-13,-3):
        beam((x,11,13.6),(x,16,13.6),.07,5,8)
    beam((-23,16,13.6),(-3,16,13.6),.07,5,8)
    group('Tower facade joints')
    for z in (4,8,12,16,20):box('Panel seam',(22.56,8,z),(.03,20,.035),5)
    service_cart(-33,12)

def modern_envelope(name):
    """Deep structural frames and recessed envelopes replace thin decorative trim."""
    if name=='03_ScienceCentre':
        facade_portal(-20,-19.4,20.5,15.5,3.4)
        facade_portal(20,-19.4,20.5,19.5,3.4)
        group('Research plinths')
        for x in (-20,20):box('Solid lab base',(x,1,1.2),(19.4,39.4,1.7),12)
        box('Atrium cap',(0,9,20),(24,25,1.8),1)
        box('Atrium shadow',(0,-3.6,18.8),(20,.2,.45),2)
        group('Roof photovoltaics')
        for x in (-25,-18):
            for y in (-3,2,7):box('PV module',(x,y,14.9),(5.3,4.4,.18),11)
    elif name=='04_TrackingStation':
        facade_portal(-8,-27.8,40,12.7,3.4)
        group('Network building envelope')
        box('Recessed base',(-8,-18,1.15),(39.5,18.5,1.5),12)
        for y in (-23,-15):pier(13,y,.35,1.8,2.4,12.6,-1.2,0)
        box('Side fascia',(12.3,-18,12.95),(3,20,1.2),1)
        for x,y,r in [(-11,7,4.6),(25,-6,2.5),(24,22,2.2)]:
            group('Antenna foundations');cyl((x,y,.7),r,1.1,12,48)
    elif name=='05_TrainingFacility':
        facade_portal(-15,-24.2,34,17.4,3.4)
        facade_portal(18,-24.2,28,13.6,3.4)
        group('Academy plinths')
        box('School base',(-15,-12,1.2),(33.5,23.5,1.7),12)
        box('Simulation base',(18,-7,1.2),(27.5,33.5,1.7),12)
        for y in (-17,-5,7):pier(32.3,y,.35,2,2.4,13,-.7,0)
        group('Buoyancy hall fascia')
        box('Upper canopy edge',(35,18,20.1),(1.3,24,1.2),1)
        box('Lower canopy edge',(13,18,17.1),(1.3,24,1.2),1)
    elif name=='06_TestingFacility':
        facade_portal(16,-33.3,42,15.4,3.8)
        group('Test stand anchor blocks')
        for x in (-6,16):
            for y in (6,24):
                box('Pylon socket',(x,y,1.7),(5,5,2.7),0)
                box('Steel shoe',(x,y,3.25),(3.5,3.5,.5),2)
        group('Blast baffle caps')
        for x in (-34,-29):box('Baffle cap',(x,20,8.55),(2,31.3,.7),12)
        box('Workshop base',(16,-24,1.2),(40.5,17.5,1.7),12)
    elif name=='07_MissionControl':
        facade_portal(-8,-10.9,42,14.8,4)
        group('Control centre monolith')
        box('Operations plinth',(0,5,1.2),(55.8,29.8,1.7),12)
        for x in (3,23):
            pier(x,18.7,.35,2.7,3.5,34,0,0)
            box('Tower edge',(x,-3.1,18),(1.2,1.3,22),1)
        box('Observation roof',(13,8,34.5),(24,26,1.3),1)
        box('Upper shadow line',(13,-5.1,33.65),(21,.2,.3),2)
        group('Sun control fins')
        for x in (-23,-19,-15,-11,-7,-3):
            box('Fixed fin',(x,-11.7,6.7),(.12,2,9.8),5)

facilities=[]
definitions=[('01_VehicleAssembly',(-95,65,0),vab),('02_LaunchComplex',(95,65,0),launch_pad),
 ('03_ScienceCentre',(-125,-55,0),science),('04_TrackingStation',(80,-60,0),tracking),
 ('05_TrainingFacility',(-55,-145,0),training),('06_TestingFacility',(100,-160,0),testing),
 ('07_MissionControl',(-25,-50,0),mission_control)]

def select(objects):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]

def evaluated_stats(objects):
    triangles=0;verts=0
    deps=bpy.context.evaluated_depsgraph_get()
    for o in objects:
        if o.type!='MESH':continue
        ev=o.evaluated_get(deps);me=ev.to_mesh();me.calc_loop_triangles()
        triangles+=len(me.loop_triangles);verts+=len(me.vertices);ev.to_mesh_clear()
    return triangles,verts

report=[]
for name,offset,fn in definitions:
    B=Builder(name,offset);fn();modern_envelope(name);B.finish()
    select(B.objects)
    # Convert font objects to real exportable meshes in the source too.
    for o in list(B.objects):
        if o.type=='FONT':
            select([o]);bpy.ops.object.convert(target='MESH')
            o.data.materials.clear();o.data.materials.append(mat)
            uv=o.data.uv_layers.new(name='UVMap')
            for loop in uv.data:loop.uv=(.375,.375)
    select(B.objects)
    triangles,vertices=evaluated_stats(B.objects)
    deps=bpy.context.evaluated_depsgraph_get()
    corners=[o.matrix_world@Vector(v) for o in B.objects for v in o.evaluated_get(deps).bound_box]
    bounds=[max(p[i] for p in corners)-min(p[i] for p in corners) for i in range(3)]
    assert triangles>4000,(name,triangles)
    assert all(len(o.data.uv_layers)>0 for o in B.objects if o.type=='MESH' and o.data.materials[0]==mat)
    # Write each asset while it is centered at its own ground origin.
    bpy.ops.export_scene.fbx(filepath=str(EXPORT/(name+'.fbx')),use_selection=True,object_types={'MESH'},
        apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False,use_mesh_modifiers=True,
        mesh_smooth_type='FACE',path_mode='COPY',embed_textures=True)
    bpy.ops.export_scene.gltf(filepath=str(HERE/(name+'.glb')),use_selection=True,export_format='GLB',export_yup=True)
    # OBJ export for a shared ArmorPaint workspace with the original atlas UVs.
    bpy.ops.wm.obj_export(filepath=str(HERE/(name+'_Paint.obj')),export_selected_objects=True,
        forward_axis='NEGATIVE_Z',up_axis='Y',export_materials=False)
    for o in B.objects:o.location+=Vector(offset)
    facilities.append(B)
    report.append(dict(id=name,triangles=triangles,vertices=vertices,meshes=len(B.objects),
        bounds_blender_xyz=bounds,campus_offset=offset,units='metres',origin='ground centre',uv='16 padded atlas tiles'))
    print('FACILITY_EXPORTED '+name+' '+str(triangles),flush=True)

# Campus dressing is separate from all exported facilities.
B=Builder('00_CampusLandscape',(0,0,0));group('Landscape')
box('Ground',(0,-45,-.45),(430,400,.8),13)
group('Roads')
for y in (9,-103):
    box('Road',(0,y,.015),(358,11,.08),8)
    for x in range(-171,176,9):box('Centre dash',(x,y,.07),(4,.16,.025),9)
for x in (-173,37,156):
    box('Road',(x,-49,.02),(10,300,.08),8)
    for y in range(-187,100,9):box('Centre dash',(x,y,.075),(.16,4,.025),9)
group('Sidewalks')
for y in (2,-110):box('Footpath',(0,y,.08),(345,2,.15),0)
for x in (-166,30,149):box('Footpath',(x,-49,.09),(2,290,.16),0)
group('Trees and planting')
for x,y in [(-155,-11),(-145,-11),(-135,-11),(-60,-11),(-45,-11),(-30,-11),(55,-110),(65,-110),(75,-110),(85,-110),(-12,-120),(0,-120),(-183,-110),(-183,-130)]:
    cyl((x,y,2),.25,4,2,10)
    for z,r in [(4,2.8),(6,2.3),(7.7,1.6)]:cyl((x,y,z),r,3.2,13,10,r2=.3)
    box('Planter',(x,y,.2),(4.6,4.6,.4),0)
B.finish()

world=bpy.data.worlds.new('Neutral daylight');world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.72,.72,.72,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.55
scene.world=world
sun_data=bpy.data.lights.new('Afternoon sun','SUN');sun_data.energy=2.2;sun_data.angle=.06
sun=bpy.data.objects.new('Afternoon sun',sun_data);scene.collection.objects.link(sun)
sun.rotation_euler=(math.radians(27),math.radians(-22),math.radians(-32))
cam_data=bpy.data.cameras.new('Campus camera');cam=bpy.data.objects.new('Campus camera',cam_data);scene.collection.objects.link(cam)
scene.camera=cam;cam.location=(355,-480,330);target=Vector((0,-40,13))
cam.rotation_euler=(target-Vector(cam.location)).to_track_quat('-Z','Y').to_euler();cam_data.type='ORTHO';cam_data.ortho_scale=480
scene.render.resolution_x=2000;scene.render.resolution_y=1500;scene.render.resolution_percentage=100
scene.render.filepath=str(ROOT/'Previews/00_SpaceCentre_Campus.png')
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
        area.spaces.active.clip_end=2000
scene['Asset package']='Seven modern industrial exterior facilities. Original TAP designs with a catch-style launch tower.'
scene['Texture workflow']='Shared 2K PBR trim atlas, per-face padded UVs; paint projects supplied separately.'
scene['Limitations']='Exterior assets. No walkable interiors, LOD chain, collision authoring or gameplay hookup.'
assert len(facilities)==7 and len(report)==7
(ROOT/'asset-manifest.json').write_text(json.dumps({'facilities':report,'total_triangles':sum(r['triangles'] for r in report),'texture_resolution':2048},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'SpaceCentre_Campus.blend'))
print('SPACE_CENTRE_BUILD_PASS',flush=True)
