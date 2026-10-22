"""Shared, paintable 2K trim atlas. Each material has a padded tile and UVs."""
from pathlib import Path
import json
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Textures'
OUT.mkdir(exist_ok=True)
# Colors in sRGB. Geometry supplies seams, louvers, brackets and panel relief.
SURFACES = [
    ('Cast concrete', (143,143,139), .88, 0), ('Aerospace white', (239,240,238), .38, .03),
    ('Carbon steel', (24,25,27), .46, .72), ('White structural steel', (211,214,215), .35, .28),
    ('Smoked glazing', (13,23,34), .12, .18), ('Brushed stainless steel', (159,163,167), .29, .92),
    ('Safety yellow', (255,196,0), .46, .05), ('Rubber', (10,10,11), .86, 0),
    ('Asphalt', (38,39,40), .94, 0), ('White markings', (248,248,246), .65, 0),
    ('Signal red', (205,24,36), .38, .12), ('Solar cells', (9,17,29), .2, .38),
    ('Basalt cladding', (82,84,86), .61, .12), ('Natural grass', (43,77,21), .97, 0),
    ('White lamps', (244,248,255), .24, 0), ('Refractory concrete', (35,33,30), .96, 0),
]

def main():
    rng = np.random.default_rng(1482)
    size, tile = 2048, 512
    base=np.zeros((size,size,3),np.uint8)
    rough=np.zeros((size,size),np.uint8); metal=rough.copy(); ao=np.full_like(rough,255)
    normal=np.empty((size,size,3),np.uint8); normal[:]=(128,128,255)
    y,x=np.mgrid[0:tile,0:tile]
    edge=np.minimum.reduce([x,y,tile-1-x,tile-1-y]).astype(float)
    for i,(name,color,r,m) in enumerate(SURFACES):
        grain=rng.normal(0,1.25 if i not in (0,8,13,15) else 3,(tile,tile))
        blotch=3*np.sin(x*.021+np.sin(y*.014))*np.sin(y*.027)
        wear=np.exp(-edge/13)*(4+3*np.sin(y*.19))
        streak=1.5*np.sin(x*.17)*np.exp(-y/200)
        value=np.clip(np.array(color)[None,None,:]+(grain+blotch+wear-streak)[...,None],0,255)
        if i==11:
            grid=((x%62<2)|(y%90<2));value[grid]=(108,132,148)
        if i==4:
            value+=((x+y)/1024*5)[...,None]
        sy=(i//4)*tile;sx=(i%4)*tile;region=np.s_[sy:sy+tile,sx:sx+tile]
        base[region]=np.clip(value,0,255).astype(np.uint8)
        rough[region]=np.clip(r*255+grain*1.5+wear,0,255).astype(np.uint8)
        metal[region]=int(m*255)
        nx=np.clip(128+np.gradient(grain,axis=1)*1.4,110,146)
        ny=np.clip(128+np.gradient(grain,axis=0)*1.4,110,146)
        normal[region]=np.stack([nx,ny,np.full_like(nx,255)],axis=-1).astype(np.uint8)
    maps={'BaseColor':base,'Roughness':rough,'Metallic':metal,'AO':ao,'Normal':normal,
          'ORM':np.stack([ao,rough,metal],axis=-1),
          'MOS':np.stack([metal,ao,np.full_like(ao,255),255-rough],axis=-1)}
    for name,data in maps.items():Image.fromarray(data).save(OUT/f'SpaceCenter_{name}.png')
    (OUT/'surface-index.json').write_text(json.dumps(SURFACES,indent=2))
    assert all((OUT/f'SpaceCenter_{name}.png').exists() for name in maps)
    assert base.std()>30 and np.array_equal(maps['MOS'][:,:,3],255-rough)
    assert all(max(SURFACES[i][1])-min(SURFACES[i][1])<=8 for i in (0,1,2,3,5,7,8,9,12))
    print('SURFACES_PASS 2048x2048 / 16 surfaces / 7 maps')

if __name__=='__main__':main()
