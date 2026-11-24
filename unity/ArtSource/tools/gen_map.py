# 스테이지 한 개를 굽는다: 테마(data/stages/<id>.json) -> 지면 그림 + 아틀라스 + 맵 + 실행용 stage.json
#   python3 gen_map.py <stage.json> <출력 폴더> <미리보기 폴더>
# 색과 분위기는 전부 테마 파일에서 온다. 키 설명은 design/stages.md.
import sys, os, json, math, random, importlib
import numpy as np
from PIL import Image
import px
from px import *
import sprites
STAGE_FILE = sys.argv[1]; OUT = sys.argv[2]; PREV = sys.argv[3]
os.makedirs(OUT, exist_ok=True); os.makedirs(PREV, exist_ok=True)

def merge(base, over):
    out = dict(base)
    for k, v in over.items():
        out[k] = merge(base[k], v) if isinstance(v, dict) and isinstance(base.get(k), dict) else v
    return out
def load_stage(path):
    st = json.load(open(path, encoding='utf-8'))
    if st.get('base'):
        st = merge(load_stage(os.path.join(os.path.dirname(path), st['base'] + '.json')), st)
        st.pop('base')
    return st
ST = load_stage(STAGE_FILE); GC = ST['ground']

# 풍경 스프라이트만 테마 램프로 다시 그린다. 인물·스파크·오두막·UI는 스테이지가 바뀌어도 같은 색.
SCENERY = ('tree_', 'sapling_', 'bush_', 'hedge', 'flowers_', 'tulip_', 'fern', 'tallgrass', 'reeds', 'giantmush', 'bigmush',
           'mush', 'rock_', 'log', 'stump', 'leafpile', 'leafbit_', 'lily', 'wilt', 'bloom', 'sprout', 'shadevine')
BASE_SPR = dict(sprites.SPR)
if ST.get('ramps'):
    for k, v in ST['ramps'].items(): px.R[k] = ramp(*v)
    importlib.reload(sprites)
SPR = {n: (sprites.SPR[n] if n.startswith(SCENERY) else BASE_SPR[n]) for n in BASE_SPR}
T = 16; MW, MH = 60, 44; W, H = MW*T, MH*T
rng = random.Random(20261112); nrng = np.random.default_rng(7)

def pick(seq, slots):
    """테마 목록에서 하나 고른다. 난수는 늘 같은 횟수만 쓰므로 테마가 달라도 배치는 같다."""
    return seq[rng.randrange(slots) % len(seq)]
def noise(scale, amp=1.0):
    small = nrng.random((H//scale+3, W//scale+3)).astype(np.float32)
    im = Image.fromarray((small*255).astype(np.uint8)).resize((W+scale*2, H+scale*2), Image.BICUBIC)
    return (np.asarray(im, np.float32)[scale:scale+H, scale:scale+W]/255-.5)*2*amp
X, Y = np.meshgrid(np.arange(W, dtype=np.float32), np.arange(H, dtype=np.float32))
def ell_d(cx, cy, rx, ry):
    return (np.sqrt(((X-cx*T)/(rx*T))**2 + ((Y-cy*T)/(ry*T))**2)-1)*min(rx, ry)*T
def seg_d(p, q):
    ax, ay, bx, by = p[0]*T, p[1]*T, q[0]*T, q[1]*T
    vx, vy = bx-ax, by-ay; t = np.clip(((X-ax)*vx+(Y-ay)*vy)/(vx*vx+vy*vy+1e-6), 0, 1)
    return np.hypot(X-(ax+vx*t), Y-(ay+vy*t))
def poly_d(pts):
    d = np.full((H, W), 1e9, np.float32)
    for a, b in zip(pts, pts[1:]): d = np.minimum(d, seg_d(a, b))
    return d

AREAS = dict(A=(13, 33, 10, 7), B=(14, 12, 10, 7), C=(31, 27, 8.5, 8), D=(51, 30, 7, 7.5), E=(51, 11, 7.5, 6))
CORR = [((13, 27), (13, 19)), ((22, 33), (27, 31)), ((43.5, 29.3), (46, 29.5)), ((51, 23), (51, 16.5)), ((37, 29), (39.3, 29.3))]
n1 = noise(48); n2 = noise(20); n3 = noise(9); n4 = noise(70)
open_d = np.full((H, W), 1e9, np.float32)
for a in AREAS.values(): open_d = np.minimum(open_d, ell_d(*a))
for p, q in CORR: open_d = np.minimum(open_d, seg_d(p, q)-21)
open_d += n2*6
RIVER = [(40, -2), (39.2, 9), (41, 19), (41.3, 30), (42.2, 46)]
water_d = poly_d(RIVER) - (19 + n1*4)
water_d = np.minimum(water_d, ell_d(38.6, 21.5, 3.6, 2.8) + n2*4)
water = water_d < 0
PATHS = [[(9.7, 32.7), (10.3, 34.2), (16, 34.2), (22, 33), (27, 31), (33, 28.6), (37, 29.2), (39.2, 29.3)], [(13.4, 34.3), (13, 27), (13, 19), (14.5, 13)],
         [(43.6, 29.3), (47, 29.6), (50.5, 28.5), (51, 22), (51, 16.4), (51, 13.4)], [(33, 28.6), (30.2, 24.2)], [(14.5, 13), (16, 11.2)]]
path_d = np.full((H, W), 1e9, np.float32)
for pl in PATHS: path_d = np.minimum(path_d, poly_d(pl))
path_d = path_d - 8.5 + n3*3.5
opn = open_d < 0
walk = opn & ~water
path = (path_d < 0) & walk

# ------------------------------------------------------------------ ground painting
def C(h): return np.array(hx(h)[:3], np.float32)
g = np.zeros((H, W, 3), np.float32)
hn = nrng.random((H, W)).astype(np.float32)
v = n1*0.9 + n3*0.35 + (hn-.5)*0.25
GR = GC['grass']
g[:] = C(GR[2])
g[v > .28] = C(GR[3]); g[v > .62] = C(GR[4]); g[v < -.3] = C(GR[1]); g[v < -.66] = C(GR[0])
edge = (open_d > -11) & opn                      # canopy shade hugging the tree line
g[edge & (v < .3)] = C(GC['edge'][1]); g[edge & (v >= .3)] = C(GC['edge'][2])
edge2 = (open_d > -4) & opn; g[edge2] = C(GC['edge'][0])
ff = ~opn                                        # deep forest floor
g[ff] = C(GC['forest'][1]); g[ff & (v > .2)] = C(GC['forest'][2]); g[ff & (v < -.3)] = C(GC['forest'][0])
# clover / moss patches
cl = (n4 + n2*.5 > .55) & walk & ~path & ~edge
g[cl & (hn > .45)] = C(GC['clover'][0]); g[cl & (hn > .9)] = C(GC['clover'][1])
# dirt path with colour-matched rim
g[path] = C(GC['path'][1]); g[path & (n3 + (hn-.5)*.6 > .35)] = C(GC['path'][2]); g[path & (n3 < -.45)] = C(GC['path'][0])
rim = path & (path_d > -1.6); g[rim] = C(GC['path_rim'])
g[path & (hn > .988)] = C(GC['path_specks'][0]); g[path & (hn < .008)] = C(GC['path_specks'][1])
# shore + water
shore = walk & (water_d < 4.5); g[shore] = C(GC['shore'][1]); g[walk & (water_d < 1.5)] = C(GC['shore'][0])
WC = GC['water']
g[water] = C(WC[2])
g[water & (water_d < -11)] = C(WC[1]); g[water & (water_d < -20)] = C(WC[0])
g[water & (water_d > -5)] = C(WC[3]); g[water & (water_d > -2.2)] = C(GC['water_foam']); g[water & (water_d > -1)] = C(GC['water_edge'])
rip = water & (water_d < -5) & ((Y.astype(int) % 7) == (X.astype(int)//11 % 7)) & (((X.astype(int) + (Y.astype(int)//7)*5) % 11) < 5) & (n3 > -.1)
g[rip] = C(GC['water_ripple'])
wf = water & ~opn                                # river running under the forest canopy: cooler
g[wf] = g[wf]*np.array(GC['water_shade'], np.float32)
# dappled sunlight filtering through the canopy
dn = noise(13); dn2 = noise(30)
dap = (dn + dn2*.6 > .52) & opn & (open_d < -6)
DP = GC['dapple']
g[dap] = g[dap]*(1-DP['strength']) + C(DP['color'])*DP['strength']
dap2 = (dn + dn2*.6 > .78) & opn & (open_d < -6)
g[dap2] = g[dap2]*(1-DP['strength2']) + C(DP['color2'])*DP['strength2']
# ---- biome tints: every glade gets its own light and ground colour
AN = list(AREAS); nd = np.stack([np.sqrt(((X-AREAS[a][0]*T)/(AREAS[a][2]*T))**2 + ((Y-AREAS[a][1]*T)/(AREAS[a][3]*T))**2) for a in AN]) 
biome = np.argmin(nd + n1[None]*0.12, 0)
TINT = {a: (None if not ST['areas'][a].get('tint') else (hx(ST['areas'][a]['tint']['color'])[:3], ST['areas'][a]['tint']['strength'])) for a in AREAS}
for i, a in enumerate(AN):
    if TINT[a] is None: continue
    m = (biome == i) & ~water; col, k = TINT[a]
    g[m] = g[m]*(1-k) + np.array(col, np.float32)*k
ground = np.clip(g, 0, 255).astype(np.uint8)

def gp(x, y, col):
    if 0 <= x < W and 0 <= y < H: ground[int(y), int(x)] = col[:3]
grassy = walk & ~path & ~shore
for _ in range(5200):                            # grass tufts
    x, y = rng.randrange(2, W-3), rng.randrange(3, H-2)
    if not grassy[y, x]: continue
    k = rng.random(); lt, dk = hx(GC['tuft'][1]), hx(GC['tuft'][0])
    if k < .5: gp(x, y, dk); gp(x+1, y-1, dk); gp(x-1, y-1, lt); gp(x-1, y-2, lt)
    elif k < .8: gp(x, y, lt); gp(x, y-1, lt); gp(x+2, y, dk); gp(x+2, y-1, lt); gp(x+1, y+1, dk)
    else: gp(x, y, dk); gp(x+1, y, dk); gp(x+1, y-1, lt)
for _ in range(1500):                            # tiny meadow flowers
    x, y = rng.randrange(2, W-3), rng.randrange(3, H-2)
    if not grassy[y, x]: continue
    rp = R[pick(GC['flower_ramps'], 7)]
    gp(x, y, hx(GC['flower_center'])); gp(x-1, y, rp[3]); gp(x+1, y, rp[2]); gp(x, y-1, rp[3]); gp(x, y+1, rp[2]); gp(x, y+2, hx(GC['tuft'][0]))
for _ in range(500):                             # pebbles on paths / shore
    x, y = rng.randrange(2, W-3), rng.randrange(3, H-2)
    if (path[y, x] and path_d[y, x] < -3) or shore[y, x]:
        gp(x, y, hx(GC['pebble'][1])); gp(x+1, y, hx(GC['pebble'][0])); gp(x, y-1, hx(GC['pebble'][2]))
for _ in range(900):                             # forest-floor ferns & fallen petals
    x, y = rng.randrange(2, W-3), rng.randrange(3, H-2)
    if ff[y, x] and not water[y, x]: gp(x, y, hx(GC['floor_fern'][1])); gp(x+1, y-1, hx(GC['floor_fern'][1])); gp(x-1, y-1, hx(GC['floor_fern'][0]))

# ------------------------------------------------------------------ objects
OBJ = []; cell = 8; GW, GH = W//cell, H//cell
solid = np.zeros((GH, GW), bool)
for j in range(GH):
    for i in range(GW):
        solid[j, i] = walk[j*cell:(j+1)*cell, i*cell:(i+1)*cell].mean() < 0.62
dyn_open = np.zeros((GH, GW), bool)              # cells that puzzles will open later
def mark(px0, py0, px1, py1, val=True, arr=None):
    arr = solid if arr is None else arr
    arr[max(0, int(py0//cell)):int(math.ceil(py1/cell)), max(0, int(px0//cell)):int(math.ceil(px1/cell))] = val
placed = []
def add(k, s, tx, ty, fp=None, **kw):
    """tx,ty in tiles (y down) = sprite base point. fp=(w,h) solid footprint in px."""
    x, y = tx*T, ty*T
    o = dict(k=k, s=s, px=x, py=y); o.update(kw); OBJ.append(o); placed.append((x, y, max(10, (fp[0] if fp else 8)*.7)))
    if fp: mark(x-fp[0]/2, y-fp[1], x+fp[0]/2, y+1)
    return o
def ok_spot(x, y, r, pathclear=5):
    xi, yi = int(x), int(y)
    if not (r+2 < xi < W-r-2 and r+10 < yi < H-4): return False
    if not walk[yi-int(r*.6):yi+3, xi-int(r):xi+int(r)+1].all(): return False
    if open_d[yi, xi] > -9 or water_d[yi, xi] < 9: return False
    if pathclear is not None and path_d[yi-4:yi+3, xi-int(r):xi+int(r)+1].min() < pathclear: return False
    return all(math.hypot(x-px_, (y-py_)*1.4) > r+pr for px_, py_, pr in placed)
def scatter(area, items):
    cx, cy, rx, ry = AREAS[area]
    for it in items:
        for _ in range(it.get('n', 1)):
            for _try in range(400):
                a = rng.random()*math.tau; d = math.sqrt(rng.random())
                x, y = (cx+math.cos(a)*rx*d)*T, (cy+math.sin(a)*ry*d)*T
                r = it.get('r', 9)
                if ok_spot(x, y, r, it.get('pc', 5)):
                    s = rng.choice(it['s']) if isinstance(it['s'], (list, tuple)) else it['s']
                    kw = {q: it[q] for q in ('n_sp', 't', 'f') if q in it}
                    add(it.get('k', 'deco'), s, x/T, y/T, it.get('fp'), **kw); break

# --- A: the witch's cottage clearing
add('deco', 'cottage', 9.6, 32.2, (62, 26))
add('npc', 'cat', 12.9, 33.0, (8, 5), id='cat'); add('sign', 'sign', 15.4, 33.2, (8, 4), id='sign_home')
add('deco', 'cauldron', 6.0, 34.2, (14, 7)); add('deco', 'woodpile', 5.2, 32.0, (16, 6)); add('deco', 'well', 18.6, 30.6, (18, 9))
add('deco', 'laundry', 17.2, 37.2, None); mark(17.2*T-18, 37.2*T-3, 17.2*T-15, 37.2*T+1); mark(17.2*T+15, 37.2*T-3, 17.2*T+18, 37.2*T+1)
add('shake', 'barrel', 14.6, 31.9, (10, 6), n_sp=2, t='leaf'); add('deco', 'pot_red', 6.9, 32.3, (8, 5)); add('shake', 'pot_blue', 7.8, 32.5, (8, 5), n_sp=1, t='leaf'); add('deco', 'pot_cream', 13.9, 32.1, (8, 5))
for i, (px_, py_) in enumerate(((8.2, 37.0), (9.3, 37.4), (10.1, 36.8))): add('shake' if i == 1 else 'deco', 'pumpkin', px_, py_, (10, 5), **(dict(n_sp=1, t='moon') if i == 1 else {}))
for i in range(6): add('deco', 'fence', 6.5+i, 38.3, (16, 4))
add('deco', 'lantern_on', 16.6, 33.4, (4, 3)); add('deco', 'lantern_on', 13.9, 28.6, (4, 3)); add('deco', 'bench', 20.2, 35.0, (20, 6))
add('rustle', 'tree_apple', 19.0, 37.6, (8, 6), n_sp=3, t='leaf'); add('rustle', 'tree_mint_0', 5.6, 36.6, (8, 6), n_sp=3, t='leaf')
for tx, ty, s in ((11.4, 35.3, 'tulip_pink'), (12.0, 35.4, 'tulip_gold'), (8.3, 34.2, 'tulip_lilac'), (7.6, 34.4, 'tulip_red'), (14.2, 33.6, 'flowers_pink'), (6.2, 30.6, 'flowers_white')): add('deco', s, tx, ty)
scatter('A', [dict(k='glowcap', s='glowcap', n=2, fp=(8, 5), r=10, n_sp=2, t='leaf'),
              dict(k='shake', s=['bush_mint', 'bush_berry', 'bush_sage'], n=5, fp=(16, 6), r=13, n_sp=2, t='leaf'),
              dict(k='shake', s='rock_mid', n=1, fp=(12, 6), r=10, n_sp=2, t='leaf'),
              dict(s=['flowers_pink', 'flowers_gold', 'flowers_white', 'flowers_lilac'], n=9, r=8), dict(s=['tallgrass', 'tallgrass2', 'fern'], n=12, r=6),
              dict(s=['mush_red', 'mush_pink', 'rock_small', 'tulip_pink', 'tulip_gold'], n=9, r=5), dict(s='stump', n=1, fp=(12, 6), r=10)])
for p in ((16.5, 36.2), (9, 35.6), (20.5, 32.2), (11.5, 29.3)): OBJ.append(dict(k='spark', s='', px=p[0]*T, py=p[1]*T, t='leaf'))

# --- gate 1 (A -> B)
add('gate', 'gate', 13, 22.8, None, need=10, id='gate1', bw=5.0, bh=0.9)
mark(13*T-40, 22.8*T-12, 13*T+40, 22.8*T+2, True, dyn_open)
add('deco', 'lantern_off', 11.2, 24.3, (4, 3)); add('sign', 'sign', 14.9, 24.6, (8, 4), id='sign_gate1')

# --- B: mushroom grove
add('comp', 'comp_glow', 16.4, 10.4, None, id='glow', need=6, t='mush'); add('npc', 'elder', 12.2, 13.4, (12, 6), id='elder')
for tx, ty, s in ((8.2, 9.6, 'giantmush'), (20.6, 10.6, 'giantmush2'), (18.6, 15.6, 'giantmush'), (7.4, 14.8, 'giantmush2'), (13.2, 7.4, 'giantmush2')): add('deco', s, tx, ty, (10, 7))
add('rustle', 'tree_lilac_0', 21.0, 14.2, (8, 6), n_sp=3, t='mush'); add('rustle', 'tree_pink_0', 9.6, 16.6, (8, 6), n_sp=3, t='mush')
add('shake', 'log', 16.0, 16.4, (24, 7), n_sp=1, t='moon')
scatter('B', [dict(k='glowcap', s='glowcap', n=2, fp=(8, 5), r=10, n_sp=2, t='mush'),
              dict(k='shake', s=['bigmush_red', 'bigmush_pink', 'bigmush_lilac', 'bigmush_gold'], n=6, fp=(8, 5), r=12, n_sp=2, t='mush'),
              dict(k='shake', s='log', n=1, fp=(24, 7), r=15, n_sp=2, t='mush'), dict(k='shake', s='rock_mossy', n=1, fp=(14, 6), r=11, n_sp=2, t='mush'),
              dict(s=['mushcluster', 'mush_red', 'mush_pink', 'mush_lilac', 'mush_gold'], n=22, r=6), dict(s=['fern', 'tallgrass'], n=14, r=6),
              dict(s=['flowers_lilac', 'flowers_white', 'bush_teal'], n=6, r=9), dict(s='stump', n=2, fp=(12, 6), r=10)])
for p in ((11, 10.5), (17.5, 13), (14, 15.5), (19, 9)): OBJ.append(dict(k='spark', s='', px=p[0]*T, py=p[1]*T, t='mush'))

# --- leaf piles (A -> C), need wind
for i, ty in enumerate((30.7, 32.0, 33.3)): add('blocker', 'shadevine', 24.6 + (i % 2)*.25, ty + .5, None, bw=1.9, bh=1.5, id='vine', t='glow')
mark(24.6*T-18, 29.6*T, 24.6*T+22, 34.6*T, True, dyn_open)
add('sign', 'sign', 22.4, 31.7, (8, 4), id='sign_leaf')

# --- C: pond meadow
add('comp', 'comp_water', 30.0, 23.2, None, id='water', need=6, t='dew'); add('npc', 'frog', 36.2, 24.6, (8, 4), id='frog')
add('sprout', 'sprout', 39.35, 29.75, None, id='bridge'); mark(39.3*T, 28.4*T, 43.9*T, 30.3*T, True, dyn_open)
add('sign', 'sign', 38.1, 28.3, (8, 4), id='sign_river')
add('rustle', 'tree_sage_0', 26.6, 24.0, (8, 6), n_sp=3, t='dew'); add('rustle', 'tree_mint_1', 33.6, 33.2, (8, 6), n_sp=3, t='dew')
for tx, ty in ((35.4, 21.6), (36.4, 23.3), (37.9, 25.3), (38.4, 26.6), (38.8, 32.2), (38.2, 33.4)): add('deco', 'reeds', tx, ty)
scatter('C', [dict(k='glowcap', s='glowcap', n=2, fp=(8, 5), r=10, n_sp=2, t='dew'),
              dict(k='wilt', s='wilt', n=6, r=9, n_sp=2, t='dew'),
              dict(k='shake', s=['bush_sage', 'bush_blue', 'bush_mint'], n=4, fp=(16, 6), r=13, n_sp=2, t='dew'),
              dict(k='shake', s=['rock_big', 'rock_mossy'], n=3, fp=(16, 7), r=13, n_sp=2, t='dew'), dict(k='shake', s='flowers_blue', n=2, r=9, n_sp=1, t='dew'),
              dict(k='shake', s='pot_blue', n=1, fp=(8, 5), r=9, n_sp=1, t='moon'),
              dict(s=['flowers_blue', 'flowers_white', 'flowers_lilac'], n=10, r=8), dict(s=['tallgrass', 'tallgrass2', 'fern'], n=16, r=6),
              dict(s=['rock_small', 'mush_gold', 'tulip_lilac'], n=8, r=5), dict(s=['sapling_mint'], n=2, fp=(6, 5), r=12)])
for p in ((29, 28), (33, 24.5), (35, 30), (30.5, 31.5)): OBJ.append(dict(k='spark', s='', px=p[0]*T, py=p[1]*T, t='dew'))

# --- D: flower meadow
add('rustle', 'tree_pink_1', 47.2, 33.4, (8, 6), n_sp=3, t='petal'); add('rustle', 'tree_gold_0', 55.2, 27.2, (8, 6), n_sp=3, t='petal')
add('deco', 'bench', 54.2, 32.6, (20, 6)); add('deco', 'lantern_on', 49.6, 26.2, (4, 3)); add('comp', 'comp_wind', 53.4, 31.0, None, id='wind', need=6, t='petal')
for i, tx in enumerate((49.9, 51.1, 52.3)): add('blocker', 'leafpile', tx, 21.9 + (i % 2)*.2, None, bw=1.9, bh=1.5, id='leaf', t='wind')
mark(48.8*T, 20.4*T, 53.4*T, 22.3*T, True, dyn_open)
scatter('D', [dict(k='glowcap', s='glowcap', n=2, fp=(8, 5), r=10, n_sp=2, t='petal'),
              dict(k='wilt', s='wilt', n=4, r=9, n_sp=2, t='petal'),
              dict(k='shake', s=['flowers_pink', 'flowers_gold', 'flowers_red', 'flowers_lilac'], n=5, r=9, n_sp=2, t='petal'),
              dict(k='shake', s=['bush_pink', 'bush_berry'], n=4, fp=(16, 6), r=13, n_sp=2, t='petal'), dict(k='shake', s='stump', n=1, fp=(12, 6), r=10, n_sp=2, t='petal'),
              dict(k='shake', s='rock_mid', n=1, fp=(12, 6), r=10, n_sp=1, t='moon'),
              dict(s=['flowers_pink', 'flowers_gold', 'flowers_red', 'flowers_lilac', 'flowers_white'], n=26, r=7),
              dict(s=['tulip_pink', 'tulip_gold', 'tulip_lilac', 'tulip_red'], n=22, r=4), dict(s=['tallgrass', 'tallgrass2'], n=10, r=6),
              dict(s=['sapling_pink', 'sapling_gold'], n=3, fp=(6, 5), r=12)])
for p in ((48.5, 30), (53, 28), (51, 34), (54.5, 30.5), (49, 26.5)): OBJ.append(dict(k='spark', s='', px=p[0]*T, py=p[1]*T, t='petal'))

# --- gate 2 (D -> E) and the Great Tree glade
add('gate', 'gate', 51, 19.6, None, need=45, id='gate2', bw=5.0, bh=0.9)
mark(51*T-40, 19.6*T-12, 51*T+40, 19.6*T+2, True, dyn_open)
add('sign', 'sign', 52.8, 23.6, (8, 4), id='sign_gate2'); add('deco', 'lantern_off', 49.3, 23.4, (4, 3))
add('greattree', 'greattree', 51, 11.4, (30, 12), need=80, id='tree')
for tx, ty in ((47.6, 13.6), (54.4, 13.6), (51.0, 15.4)): add('lamp', 'lamp', tx, ty, (6, 4), id='lamp')
scatter('E', [dict(s=['flowers_white', 'flowers_pink', 'flowers_lilac'], n=14, r=7), dict(s=['fern', 'tallgrass2', 'mush_lilac', 'tulip_lilac'], n=14, r=5),
              dict(s=['rock_mossy'], n=2, fp=(14, 6), r=11)])
for p in ((47.5, 14.5), (55, 14)): OBJ.append(dict(k='spark', s='', px=p[0]*T, py=p[1]*T, t='moon'))

# lilies drifting on the water
for _ in range(400):
    x, y = rng.randrange(8, W-8), rng.randrange(8, H-8)
    if water_d[y, x] < -9 and opn[y, x] | (rng.random() < .25) and not dyn_open[y//cell, x//cell] and all(math.hypot(x-a, y-b) > 15 for a, b, _ in placed[-60:]):
        OBJ.append(dict(k='deco', s=rng.choice(('lily', 'lily', 'lily_flower')), px=x, py=y+3, f=1)); placed.append((x, y, 8))

# sun shafts in the clearings
for a, n in (('A', 5), ('B', 5), ('C', 5), ('D', 5), ('E', 4)):
    cx, cy, rx, ry = AREAS[a]
    for i in range(n):
        OBJ.append(dict(k='shaft', s='shaft', px=(cx + rng.uniform(-.75, .75)*rx)*T, py=(cy + rng.uniform(-.5, .8)*ry)*T))

# --- forest trees (jittered grid); deep ones are baked into the ground texture
ii = np.pad(np.cumsum(np.cumsum(walk.astype(np.int32), 0), 1), ((1, 0), (1, 0)))
def any_walk(x0, y0, x1, y1):
    x0, y0, x1, y1 = max(0, int(x0)), max(0, int(y0)), min(W, int(x1)), min(H, int(y1))
    if x1 <= x0 or y1 <= y0: return False
    return ii[y1, x1]-ii[y0, x1]-ii[y1, x0]+ii[y0, x0] > 0
ACC_N = dict(A=2, B=3, C=2, D=3, E=3)
def tree_kind(x, y):
    best = min(AREAS, key=lambda a: math.hypot((x/T-AREAS[a][0])/AREAS[a][2], (y/T-AREAS[a][1])/AREAS[a][3]))
    ar = ST['areas'][best]; acc = ar['trees']
    if best == 'B' and rng.random() < .22:
        i = rng.randrange(2); sp = ar.get('special')
        if sp: return sp[i % len(sp)]
        return f'tree_{acc[i % len(acc)]}_{i}'
    nm = pick(acc, ACC_N[best]) if rng.random() < (.34 if best in 'AC' else .72) else pick(ST['trees_base'], 5)
    return f'tree_{nm}_{rng.randint(0, 1)}'
forest = []
for row, gy in enumerate(range(10, H+46, 15)):
    for gx in range(-8 + (row % 2)*13, W+20, 26):
        x, y = gx + rng.randint(-6, 6), gy + rng.randint(-4, 4)
        xi, yi = min(max(x, 0), W-1), min(max(y, 0), H-1)
        if opn[yi, xi] and open_d[yi, xi] < 3: continue
        if water_d[yi, xi] < 7: continue
        if any_walk(x-13, y-42, x+13, y+1): continue   # keep crowns from swallowing the clearings
        forest.append((x, y, tree_kind(x, y)))
forest.sort(key=lambda t: t[1])
baked = 0
def comp(dst, cv, x0, y0):
    h, w = cv.a.shape[:2]; xa, ya = max(0, x0), max(0, y0); xb, yb = min(dst.shape[1], x0+w), min(dst.shape[0], y0+h)
    if xb <= xa or yb <= ya: return
    src = cv.a[ya-y0:yb-y0, xa-x0:xb-x0].astype(np.float32); al = src[..., 3:4]/255
    dst[ya:yb, xa:xb, :3] = (src[..., :3]*al + dst[ya:yb, xa:xb, :3]*(1-al)).astype(np.uint8)
def shadow(dst, x, y, rx, ry):
    x0, x1, y0, y1 = max(0, int(x-rx)), min(W, int(x+rx)+1), max(0, int(y-ry)), min(H, int(y+ry)+1)
    if x1 <= x0 or y1 <= y0: return
    yy, xx = np.mgrid[y0:y1, x0:x1]; m = ((xx-x)/rx)**2 + ((yy-y)/ry)**2 <= 1
    reg = dst[y0:y1, x0:x1].astype(np.float32); reg[m] *= np.array(GC['shadow'], np.float32); dst[y0:y1, x0:x1] = reg.astype(np.uint8)
def sprite_of(o):
    s = o['s']
    return {'gate': 'gate_closed', 'cat': 'cat_0', 'elder': 'elder_0', 'frog': 'frog_0', 'glowcap': 'glowcap_off', 'lamp': 'leafpile', 'greattree': 'greattree_asleep'}.get(s, s)
# soft ground shadows under everything that stands up
for o in OBJ:
    if o['k'] in ('spark', 'shaft') or o.get('f'): continue
    cv = SPR[sprite_of(o)][0]
    big = cv.w > 30
    shadow(ground, o['px']+ (3 if big else 1), o['py']-1, cv.w*(.42 if big else .46), max(2.5, cv.w*(.16 if big else .2)))
live = []
for x, y, nm in forest:
    cv, pvx, pvy = SPR[nm]
    if any_walk(x-cv.w/2-10, y-cv.h-4, x+cv.w/2+10, y+30): live.append((x, y, nm))
    else: baked += 1
for x, y, nm in forest:
    if (x, y, nm) in live: shadow(ground, x+3, y-1, 19, 7)
for x, y, nm in forest:
    if (x, y, nm) not in live:
        cv, pvx, pvy = SPR[nm]; comp(ground, cv, int(x-pvx), int(y-cv.h+pvy))
for x, y, nm in live: OBJ.append(dict(k='tree', s=nm, px=x, py=y))
print('forest trees', len(forest), 'baked', baked, 'live', len(live))

# ------------------------------------------------------------------ reachability check
from collections import deque
free = ~solid | dyn_open
free2 = free.copy(); free2[:-1, :] &= free[1:, :]; free2[:, :-1] &= free[:, 1:]; free2[:-1, :-1] &= free[1:, 1:]
START = (10.6, 34.5)
seen = np.zeros_like(free2); q = deque([(int(START[1]*T//cell), int(START[0]*T//cell))]); seen[q[0]] = True
assert free2[q[0]], 'start blocked'
while q:
    j, i = q.popleft()
    for dj, di in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        a, b = j+dj, i+di
        if 0 <= a < GH and 0 <= b < GW and free2[a, b] and not seen[a, b]: seen[a, b] = True; q.append((a, b))
def near_reach(px_, py_, r=3):
    j, i = int(py_//cell), int(px_//cell)
    return seen[max(0, j-r):j+r+1, max(0, i-r):i+r+1].any()
bad = [o for o in OBJ if o['k'] in ('shake', 'wilt', 'comp', 'npc', 'gate', 'sign', 'greattree', 'rustle', 'sprout', 'blocker', 'spark', 'glowcap', 'lamp') and not near_reach(o['px'], o['py'], 4 if o['k'] != 'greattree' else 6)]
print('unreachable:', [(o['k'], o['s']) for o in bad]); assert not bad
tot = {}
for o in OBJ:
    n = o.get('n_sp', 1 if o['k'] == 'spark' else 0); val = n*(3 if o.get('t') == 'moon' else 1)
    if val: tot[o['k']] = tot.get(o['k'], 0) + val
print('light available:', tot, sum(tot.values()))
# what can be had before each gate (no spells for gate1)
def area_of(o): return min(AREAS, key=lambda a: math.hypot((o['px']/T-AREAS[a][0])/AREAS[a][2], (o['py']/T-AREAS[a][1])/AREAS[a][3]))
def val(o): return o.get('n_sp', 1 if o['k'] == 'spark' else 0)*(3 if o.get('t') == 'moon' else 1)
g1 = sum(val(o) for o in OBJ if o['k'] in ('shake', 'spark') and area_of(o) == 'A')
g2 = sum(val(o) for o in OBJ if area_of(o) in 'ABC')
print('before gate1 (need 10):', g1, ' before gate2 (need 45):', g2)

# ------------------------------------------------------------------ export
Image.fromarray(ground).save(OUT + '/ground_png.bytes', format='PNG')
names = sorted(SPR)
AW = 512; x = y = 1; rh = 0; pos = {}
for n in sorted(names, key=lambda n: -SPR[n][0].h):
    cv = SPR[n][0]
    if x + cv.w + 1 > AW: x = 1; y += rh + 1; rh = 0
    pos[n] = (x, y); x += cv.w + 1; rh = max(rh, cv.h)
AH = 1
while AH < y + rh + 1: AH *= 2
atlas = np.zeros((AH, AW, 4), np.uint8)
meta = []
for n in names:
    cv, pvx, pvy = SPR[n]; ax, ay = pos[n]
    atlas[ay:ay+cv.h, ax:ax+cv.w] = cv.a
    meta.append(dict(n=n, x=ax, y=AH-ay-cv.h, w=cv.w, h=cv.h, px=round(pvx, 2), py=round(pvy, 2)))
Image.fromarray(atlas).save(OUT + '/atlas_png.bytes', format='PNG')
json.dump(dict(sprites=meta), open(OUT + '/atlas_meta.json', 'w'), separators=(',', ':'))
objs = []
for o in OBJ:
    d = dict(k=o['k'], s=o['s'], x=round(o['px']/T, 3), y=round((H-o['py'])/T, 3))
    for q in ('id', 't', 'n_sp', 'need', 'bw', 'bh', 'f'):
        if q in o: d[{'n_sp': 'n'}.get(q, q)] = o[q]
    objs.append(d)
rows = [''.join('#' if solid[GH-1-j, i] else '.' for i in range(GW)) for j in range(GH)]   # row 0 = bottom
wcell = np.zeros((GH, GW), bool)
for j in range(GH):
    for i in range(GW): wcell[j, i] = (water_d[j*cell:(j+1)*cell, i*cell:(i+1)*cell] < (-6 if opn[j*cell+4, i*cell+4] else -13)).all()
wrows = [''.join('~' if wcell[GH-1-j, i] else '.' for i in range(GW)) for j in range(GH)]
json.dump(dict(w=MW, h=MH, cell=cell/T, gw=GW, gh=GH, startX=START[0], startY=MH-START[1], solid=rows, water=wrows, objects=objs,
               areas=[dict(id=a, name=ST['areas'][a]['name'], x=AREAS[a][0], y=MH-AREAS[a][1], rx=AREAS[a][2], ry=AREAS[a][3]) for a in AREAS]),
          open(OUT + '/map.json', 'w', encoding='utf-8'), separators=(',', ':'), ensure_ascii=False)
# 실행용 분위기: 구역마다 스테이지 분위기 위에 덮어쓴 값을 미리 다 풀어서 적는다 (엔진은 합치지 않고 읽기만 한다)
json.dump(dict(id=ST['id'], name=ST['name'], mood=ST['mood'],
               areas=[dict(id=a, mood=merge(ST['mood'], ST['areas'][a].get('mood', {}))) for a in AREAS]),
          open(OUT + '/stage.json', 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print('atlas', AW, AH, 'objects', len(objs))

# ------------------------------------------------------------------ preview
prev = ground.copy()
draw = [o for o in OBJ if o['k'] not in ('spark', 'shaft')]
draw.sort(key=lambda o: (0 if o.get('f') else 1, o['py']))
for o in draw:
    cv, pvx, pvy = SPR[sprite_of(o)]; comp(prev, cv, int(round(o['px']-pvx)), int(round(o['py']-cv.h+pvy)))
for o in OBJ:
    if o['k'] == 'spark':
        cv, pvx, pvy = SPR['spark_%s_0' % o['t']]; comp(prev, cv, int(o['px']-pvx), int(o['py']-cv.h+pvy))
    if o['k'] == 'shaft':
        cv, pvx, pvy = SPR['shaft']; comp(prev, cv, int(o['px']-pvx), int(o['py']-cv.h+20))
cv, pvx, pvy = SPR['witch_down_0']; comp(prev, cv, int(START[0]*T-pvx), int(START[1]*T-cv.h+pvy))
def graded(img, mood):
    t = np.array(hx(mood['tint'])[:3], np.float32)/255; hz = mood['haze'].lstrip('#')
    hc = np.array([int(hz[i:i+2], 16) for i in (0, 2, 4)], np.float32); ha = int(hz[6:8], 16)/255 if len(hz) == 8 else 1.0
    return np.clip(img.astype(np.float32)*t*(1-ha) + hc*ha, 0, 255).astype(np.uint8)
raw_prev = prev
prev = graded(raw_prev, ST['mood'])
Image.fromarray(prev).save(PREV + '/map_full.png')
def shot(name, cx, cy):
    x0 = int(min(max(cx*T-160, 0), W-320)); y0 = int(min(max(cy*T-90, 0), H-180))
    Image.fromarray(prev[y0:y0+180, x0:x0+320]).resize((1280, 720), Image.NEAREST).save(PREV + f'/shot_{name}.png')
shot('A', 12.5, 33.5); shot('B', 14, 12); shot('C', 34, 27); shot('D', 51, 30); shot('E', 51, 11); shot('gate', 13, 23)
dbg = raw_prev.copy(); sm = np.kron(solid & ~dyn_open, np.ones((cell, cell), bool)); dbg[sm] = (dbg[sm]*.5 + np.array([255, 80, 80])*.5).astype(np.uint8)
dm = np.kron(dyn_open, np.ones((cell, cell), bool)); dbg[dm] = (dbg[dm]*.5 + np.array([80, 80, 255])*.5).astype(np.uint8)
Image.fromarray(dbg).save(PREV + '/map_collision.png')
