import random, math
from px import *

SPR = {}   # name -> (Cv, pivot_x, pivot_y_from_bottom)
def reg(name, cv, px=None, py=1, outline=True):
    if outline: cv = cv.outlined()
    SPR[name] = (cv, cv.w/2 if px is None else px, py)

# ---------------------------------------------------------------- trees
def make_tree(rp, seed, w=44, h=58, trunk=6, fruit=None):
    rnd = random.Random(seed); c = Cv(w, h); cx = w//2; b = R['bark']
    ch = h - 15                      # crown height
    for y in range(ch-8, h):         # trunk, flaring at the roots
        t = (y-(ch-8))/(h-(ch-8)); half = trunk/2 + (2.2*t*t*t if t > .6 else 0)
        for x in range(int(cx-half), int(cx+half+1)):
            f = (x-(cx-half))/(2*half+.01)
            c.p(x, y, b[2] if f < .3 else (b[1] if f < .72 else b[0]))
    c.p(cx-1, h-9, b[0]); c.p(cx-1, h-8, b[0]); c.p(cx+1, h-14, b[3])
    blobs = [(cx, ch*0.52, w*0.40, ch*0.42)]
    n = 7 if w >= 40 else 5
    for i in range(n):
        a = i/n*math.tau + rnd.uniform(-.3, .3)
        r = rnd.uniform(.20, .27)*w
        blobs.append((cx+math.cos(a)*w*0.27, ch*0.52+math.sin(a)*ch*0.27, r, r*rnd.uniform(.8, .95)))
    blobs.append((cx-w*0.1, ch*0.25, w*.24, w*.2)); blobs.append((cx+w*0.14, ch*0.72, w*.25, w*.2))
    for i, (bx, by, rx, ry) in enumerate(sorted(blobs, key=lambda q: q[1])):
        bx = min(max(bx, rx), w-1-rx); by = min(max(by, ry), ch-ry)
        c.blob(bx, by, rx, ry, rp, seed*13+i)
    if fruit:
        for _ in range(7):
            fx, fy = rnd.randint(6, w-7), rnd.randint(8, ch-6)
            if c.get(fx, fy)[3] and c.get(fx+1, fy+1)[3]:
                c.p(fx, fy, fruit[2]); c.p(fx+1, fy, fruit[1]); c.p(fx, fy+1, fruit[1]); c.p(fx+1, fy+1, fruit[0])
    return c

for nm, fr in (('mint', None), ('sage', None), ('teal', None), ('pink', None), ('gold', None), ('lilac', None)):
    for v in range(2):
        reg(f'tree_{nm}_{v}', make_tree(R[nm], hash(nm) % 97 + v*5 + 1 if False else (sum(map(ord, nm))+v*5)))
reg('tree_apple', make_tree(R['sage'], 77, fruit=R['red']))
for i, nm in enumerate(('mint', 'pink', 'gold')):
    reg(f'sapling_{nm}', make_tree(R[nm], 200+i, w=26, h=36, trunk=4))

def great_tree(rp, rp2, seed, awake):
    w, h = 108, 122; c = Cv(w, h); rnd = random.Random(seed); cx = w//2; b = R['bark']; ch = h-30
    for y in range(ch-22, h):
        t = (y-(ch-22))/(h-(ch-22)); half = 9 + 9*t**3
        for x in range(int(cx-half), int(cx+half+1)):
            f = (x-(cx-half))/(2*half)
            c.p(x, y, b[3] if f < .12 else b[2] if f < .38 else (b[1] if f < .74 else b[0]))
    for i in range(5):  # bark grooves
        gx = cx-6+i*3; 
        for y in range(ch-10+i*2, h-6, 3): c.p(gx+(y//6) % 2, y, b[0])
    # hollow / door of light
    for y in range(h-22, h-4):
        for x in range(cx-5, cx+6):
            if ((x-cx)/5.5)**2 + ((y-(h-12))/9.5)**2 <= 1:
                c.p(x, y, hx('#fff3c4') if awake else tone(b[0], .9))
    if awake:
        for y in range(h-18, h-5): c.p(cx-1, y, hx('#fffbe6')); c.p(cx, y, hx('#fffbe6'))
    blobs = [(cx, ch*.5, w*.38, ch*.40)]
    for i in range(13):
        a = i/13*math.tau + rnd.uniform(-.2, .2); r = rnd.uniform(.15, .21)*w
        blobs.append((cx+math.cos(a)*w*.30, ch*.5+math.sin(a)*ch*.30, r, r*.88))
    for i in range(6):
        blobs.append((cx+rnd.uniform(-.25, .25)*w, ch*rnd.uniform(.25, .75), w*.16, w*.13))
    for i, (bx, by, rx, ry) in enumerate(sorted(blobs, key=lambda q: q[1])):
        bx = min(max(bx, rx), w-1-rx); by = min(max(by, ry), ch-ry)
        c.blob(bx, by, rx, ry, rp2 if (awake and i % 3 == 1) else rp, seed+i)
    if awake:
        for _ in range(46):
            fx, fy = rnd.randint(6, w-7), rnd.randint(6, ch-6)
            if c.get(fx, fy)[3]:
                col = hx('#fff6d0'); c.p(fx, fy, col); c.p(fx-1, fy, R['pink'][3]); c.p(fx+1, fy, R['pink'][3]); c.p(fx, fy-1, R['pink'][3]); c.p(fx, fy+1, R['pink'][2])
    return c
GREY = ramp('#7f9ea6', '#9bb8b8', '#b7cfc8', '#d3e4dc')
reg('greattree_asleep', great_tree(GREY, GREY, 5, False))
reg('greattree_awake', great_tree(R['pink'], R['gold'], 5, True))

# ---------------------------------------------------------------- shrubs & plants
def bush(rp, seed, berries=None, w=22, h=15):
    c = Cv(w, h); rnd = random.Random(seed)
    for i, (bx, by, rx, ry) in enumerate([(w*.3, h*.62, w*.28, h*.36), (w*.7, h*.62, w*.28, h*.36), (w*.5, h*.42, w*.32, h*.40), (w*.5, h*.7, w*.3, h*.28)]):
        c.blob(bx, by, rx, ry, rp, seed+i)
    if berries:
        for _ in range(6):
            x, y = rnd.randint(3, w-4), rnd.randint(3, h-4)
            if c.get(x, y)[3]: c.p(x, y, berries[1]); c.p(x, y-1, berries[3])
    return c
reg('bush_mint', bush(R['mint'], 1)); reg('bush_sage', bush(R['sage'], 2)); reg('bush_teal', bush(R['teal'], 3))
reg('bush_berry', bush(R['mint'], 4, R['red'])); reg('bush_blue', bush(R['sage'], 5, R['plum']))
reg('bush_pink', bush(R['pink'], 6)); reg('hedge', bush(R['teal'], 8, w=30, h=17))

def flower_head(c, x, y, rp, center=hx('#fff0a8')):
    c.p(x, y, center)
    for dx, dy, k in ((-1, 0, 2), (1, 0, 1), (0, -1, 3), (0, 1, 1)): c.p(x+dx, y+dy, rp[k])
def flowers(rp, seed, w=16, h=15, n=5):
    c = Cv(w, h); rnd = random.Random(seed); g = R['moss']
    pts = [(rnd.randint(2, w-3), rnd.randint(2, h-7)) for _ in range(n)]
    for x, y in pts:
        for yy in range(y+1, h): c.p(x + (1 if (yy % 4 == 0) else 0), yy, g[0] if yy > h-4 else g[1])
        c.p(x-1, min(h-2, y+4), g[2]); c.p(x+2, min(h-1, y+6), g[1])
    for i in range(0, w, 2): c.p(i, h-1-(i % 3 == 0), g[1]); c.p(i+1, h-1, g[0])
    for x, y in sorted(pts, key=lambda q: q[1]): flower_head(c, x, y, rp)
    return c
for nm in ('pink', 'gold', 'lilac', 'blue', 'red'): reg(f'flowers_{nm}', flowers(R[nm], sum(map(ord, nm))))
WHITE = ramp('#d8d4e8', '#f0eef6', '#ffffff', '#ffffff'); reg('flowers_white', flowers(WHITE, 9))

def tulip(rp, seed):
    c = Cv(7, 11); g = R['moss']
    for y in range(4, 11): c.p(3, y, g[1])
    c.p(2, 8, g[2]); c.p(1, 7, g[2]); c.p(4, 9, g[0]); c.p(5, 8, g[0])
    c.art(1, 0, ["a.b.a", "abbba", "abbbc", ".bbc."], {'a': rp[3], 'b': rp[2], 'c': rp[1]})
    return c
for nm in ('pink', 'gold', 'lilac', 'red'): reg(f'tulip_{nm}', tulip(R[nm], 1))

def fern(seed):
    c = Cv(15, 11); g = R['teal']
    for k, (dx, ln) in enumerate(((-1, 7), (-.5, 9), (0, 10), (.5, 9), (1, 7))):
        for i in range(ln):
            x = 7 + dx*i*.8; y = 10 - i + (abs(dx)*i*i*.06)
            c.p(x, y, g[2] if k < 2 else g[1] if k < 4 else g[0])
            if i % 2 == 0 and i > 1: c.p(x-1, y, g[3] if k < 2 else g[1]); c.p(x+1, y+1, g[0])
    return c
reg('fern', fern(1))

def tallgrass(rp):
    c = Cv(12, 10)
    for i, (x, hgt, k) in enumerate(((1, 5, 1), (3, 8, 2), (5, 6, 1), (6, 9, 2), (8, 7, 1), (10, 5, 0), (4, 4, 0), (9, 4, 2))):
        for j in range(hgt): c.p(x + (1 if j > hgt-3 and i % 2 else 0), 9-j, rp[k] if j < hgt-1 else rp[3])
    return c
reg('tallgrass', tallgrass(R['sage'])); reg('tallgrass2', tallgrass(R['mint']))

def reeds():
    c = Cv(12, 20); g = R['sage']; br = R['wood']
    for x, top in ((2, 4), (5, 1), (8, 5), (10, 8)):
        for y in range(top, 20): c.p(x, y, g[1] if y % 5 else g[0])
        for y in range(top, top+4): c.p(x, y, br[0] if x % 2 else br[1])
        c.p(x, top-1, g[2])
    c.p(3, 12, g[2]); c.p(4, 11, g[2]); c.p(7, 13, g[2]); c.p(6, 14, g[0])
    return c
reg('reeds', reeds())

def mushroom(rp, w=9, h=9, spots=True):
    c = Cv(w, h); st = R['cream']; cw = w/2
    for y in range(int(h*.45), h):
        for x in range(int(cw-w*.18), int(cw+w*.18)+1): c.p(x, y, st[2] if x < cw else st[1])
    c.blob(cw-.5, h*.36, w*.5, h*.36, rp, 3, hi=False)
    for x in range(1, w-1): c.p(x, int(h*.36+h*.3), tone(rp[0], .97)) if c.get(x, int(h*.36+h*.3))[3] else None
    if spots:
        for sx, sy in ((.3, .2), (.62, .3), (.45, .42), (.8, .5), (.18, .5)):
            x, y = int(sx*w), int(sy*h)
            if c.get(x, y)[3]: c.p(x, y, st[3])
    return c
for nm in ('red', 'pink', 'lilac', 'gold'):
    reg(f'mush_{nm}', mushroom(R[nm])); reg(f'bigmush_{nm}', mushroom(R[nm], 21, 22))
reg('giantmush', mushroom(R['red'], 40, 44)); reg('giantmush2', mushroom(R['lilac'], 34, 38))
def mushcluster(seed):
    c = Cv(16, 11); rnd = random.Random(seed)
    for i, (x, y, s, nm) in enumerate(((0, 4, 7, 'pink'), (9, 5, 6, 'lilac'), (5, 1, 8, 'red'))):
        c.paste(mushroom(R[nm], s, s+2, s > 6), x, y)
    return c
reg('mushcluster', mushcluster(1))

# ---------------------------------------------------------------- rocks & wood
def rock(w, h, seed, moss=True):
    c = Cv(w, h); c.blob(w/2-.5, h*.56, w*.5, h*.44, R['stone'], seed, hi=False)
    c.blob(w*.36, h*.42, w*.3, h*.34, R['stone'], seed+1, hi=False)
    for x in range(w):
        if c.get(x, h-1)[3] == 0 and c.get(x, h-2)[3]: pass
    if moss:
        m = R['moss']; rnd = random.Random(seed)
        for x in range(w):
            for y in range(h):
                if c.get(x, y)[3] and not c.get(x, y-1)[3] and rnd.random() < .7 and x < w*.75:
                    c.p(x, y, m[2]); 
                    if rnd.random() < .5: c.p(x, y+1, m[1])
    return c
reg('rock_big', rock(22, 16, 1)); reg('rock_mid', rock(14, 11, 2)); reg('rock_small', rock(8, 6, 3, False)); reg('rock_mossy', rock(18, 13, 7))

def log():
    c = Cv(28, 12); b = R['bark']; wd = R['wood']
    for y in range(2, 12):
        for x in range(3, 25):
            f = (y-2)/10; c.p(x, y, b[2] if f < .25 else b[1] if f < .7 else b[0])
    for x in range(6, 24, 5): c.p(x, 6, b[0]); c.p(x+1, 6, b[0]); c.p(x+2, 9, tone(b[0], .95))
    c.ell(24, 6.5, 3.4, 5, wd[2]); c.ell(24, 6.5, 2, 3.2, wd[1]); c.p(24, 6, wd[0]); c.p(24, 7, wd[0])
    m = R['moss']
    for x in range(4, 16): c.p(x, 2, m[2]); c.p(x, 1, m[3]) if x % 3 else None
    for x in range(5, 12, 2): c.p(x, 3, m[1])
    c.paste(mushroom(R['pink'], 5, 5, False), 14, -1 + 0)
    return c
reg('log', log())
def stump():
    c = Cv(14, 12); b = R['bark']; wd = R['wood']
    for y in range(4, 12):
        for x in range(1, 13):
            half = 5.5 + (1 if y > 9 else 0)
            if abs(x-6.5) <= half: c.p(x, y, b[2] if x < 4 else b[1] if x < 10 else b[0])
    c.ell(6.5, 4, 5.6, 3, wd[2]); c.ell(6.5, 4, 3.6, 1.8, wd[1]); c.p(6, 4, wd[0]); c.p(7, 4, wd[0])
    c.p(1, 10, R['moss'][2]); c.p(2, 11, R['moss'][1])
    return c
reg('stump', stump())
def woodpile():
    c = Cv(20, 13); wd = R['wood']; b = R['bark']
    for i, (x, y) in enumerate(((1, 8), (7, 8), (13, 8), (4, 3), (10, 3))):
        c.rect(x, y, 6, 5, b[1]); c.ell(x+2.5, y+2, 2.6, 2.2, wd[2]); c.p(x+2, y+2, wd[0]); c.rect(x, y+4, 6, 1, b[0])
    return c
reg('woodpile', woodpile())

# ---------------------------------------------------------------- props
def lantern(lit):
    c = Cv(9, 24); wd = R['wood']; g = R['gold']
    for y in range(8, 24): c.p(4, y, wd[1]); c.p(5, y, wd[0])
    c.rect(2, 22, 6, 2, wd[0]); c.rect(3, 1, 4, 1, R['roof'][1]); c.rect(2, 2, 6, 1, R['roof'][0])
    for y in range(3, 8):
        for x in range(2, 8): c.p(x, y, (hx('#fff6c8') if 3 <= x <= 6 and y < 7 else g[1]) if lit else (R['stone'][2] if 3 <= x <= 6 and y < 7 else R['stone'][0]))
    c.rect(2, 7, 6, 1, wd[0])
    if lit: c.p(4, 4, hx('#ffffff')); c.p(5, 5, g[2])
    return c
reg('lantern_on', lantern(True)); reg('lantern_off', lantern(False))
def sign():
    c = Cv(16, 16); wd = R['wood']
    for y in range(8, 16): c.p(7, y, wd[1]); c.p(8, y, wd[0])
    c.rect(1, 2, 14, 8, wd[2]); c.rect(1, 9, 14, 1, wd[1]); c.rect(1, 2, 14, 1, wd[3])
    for x in range(3, 13): c.p(x, 4, wd[0]) if x % 5 else None
    for x in range(3, 11): c.p(x, 6, wd[0]) if x % 4 else None
    c.p(14, 2, wd[1]); c.p(1, 9, wd[0])
    return c
reg('sign', sign())
def fence():
    c = Cv(16, 12); wd = R['wood']
    for x in (2, 12):
        for y in range(1, 12): c.p(x, y, wd[2]); c.p(x+1, y, wd[0])
        c.p(x, 0, wd[3]); c.p(x+1, 0, wd[1])
    c.rect(0, 3, 16, 2, wd[2]); c.rect(0, 5, 16, 1, wd[1]); c.rect(0, 8, 16, 1, wd[2]); c.rect(0, 9, 16, 1, wd[1])
    return c
reg('fence', fence())
def pot(rp, plant=None):
    c = Cv(11, 13)
    for y in range(4, 13):
        half = 4.6 - abs(y-8)*.28
        for x in range(11):
            if abs(x-5) <= half: c.p(x, y, rp[2] if x < 4 else rp[1] if x < 8 else rp[0])
    c.rect(2, 3, 7, 2, rp[3]); c.rect(3, 4, 5, 1, tone(rp[0], .85)); c.p(3, 7, rp[3])
    return c
reg('pot_red', pot(R['red'])); reg('pot_blue', pot(R['blue'])); reg('pot_cream', pot(R['cream']))
def barrel():
    c = Cv(12, 15); wd = R['wood']
    for y in range(1, 15):
        half = 5.2 - abs(y-8)*.12
        for x in range(12):
            if abs(x-5.5) <= half: c.p(x, y, wd[3] if x < 3 else wd[2] if x < 7 else wd[1])
    for y in (3, 11):
        for x in range(12):
            if c.get(x, y)[3]: c.p(x, y, R['stone'][1] if x < 8 else R['stone'][0])
    c.ell(5.5, 2, 4.4, 1.6, wd[1])
    return c
reg('barrel', barrel())
def pumpkin():
    c = Cv(13, 11); o = ramp('#e89a6a', '#f4b47c', '#fad09a', '#fde6c0')
    c.blob(3.5, 6.5, 3.5, 4, o, 1, hi=False); c.blob(9, 6.5, 3.5, 4, o, 2, hi=False); c.blob(6, 6.5, 3.6, 4.4, o, 3, hi=False)
    c.p(6, 1, R['moss'][0]); c.p(6, 0, R['moss'][1]); c.p(7, 0, R['moss'][2]); c.p(4, 4, o[3])
    return c
reg('pumpkin', pumpkin())
def cauldron():
    c = Cv(18, 17); k = ramp('#7a78a0', '#9694b8', '#b4b2cc', '#d6d4e4')
    c.blob(8.5, 10, 8, 6.5, k, 1, hi=False); c.rect(2, 5, 14, 2, k[2]); c.rect(3, 6, 12, 1, hx('#b6ecc8'))
    c.rect(4, 5, 10, 1, hx('#d8f8dc')); c.p(6, 3, hx('#d8f8dc')); c.p(10, 2, hx('#b6ecc8')); c.p(9, 4, hx('#ffffff'))
    c.rect(3, 15, 2, 2, k[0]); c.rect(13, 15, 2, 2, k[0]); c.p(5, 9, k[3]); c.p(5, 10, k[3])
    return c
reg('cauldron', cauldron())
def well():
    c = Cv(22, 28); st = R['stone']; wd = R['wood']; rf = R['roof']
    for y in range(17, 28):
        for x in range(2, 20): c.p(x, y, st[2] if x < 8 else st[1] if x < 15 else st[0])
    for y in range(18, 28, 3):
        for x in range(2, 20): c.p(x, y, st[0]) if (x + y) % 6 == 0 else None
    c.ell(10.5, 17, 8.6, 2.6, st[3]); c.ell(10.5, 17, 6, 1.5, hx('#8ccde6'))
    for y in range(6, 17): c.p(3, y, wd[1]); c.p(4, y, wd[0]); c.p(17, y, wd[1]); c.p(18, y, wd[0])
    for y in range(0, 8):
        for x in range(22):
            if abs(x-10.5) <= 2 + y*1.3: c.p(x, y, rf[3] if y == 0 else rf[2] if x < 9 else rf[1] if (x+y) % 4 else rf[0])
    c.rect(5, 9, 12, 1, wd[2]); c.p(10, 10, wd[0]); c.p(10, 11, wd[0]); c.rect(9, 12, 3, 3, wd[2])
    return c
reg('well', well())
def bench():
    c = Cv(22, 11); wd = R['wood']
    c.rect(0, 3, 22, 3, wd[2]); c.rect(0, 3, 22, 1, wd[3]); c.rect(0, 6, 22, 1, wd[0])
    c.rect(2, 7, 2, 4, wd[1]); c.rect(18, 7, 2, 4, wd[1]); c.rect(1, 0, 20, 2, wd[2]); c.rect(1, 2, 20, 1, wd[1])
    return c
reg('bench', bench())
def laundry():
    c = Cv(38, 22); wd = R['wood']
    for y in range(2, 22): c.p(1, y, wd[1]); c.p(2, y, wd[0]); c.p(35, y, wd[1]); c.p(36, y, wd[0])
    for x in range(3, 35): c.p(x, 3 + int(1.6*math.sin((x-3)/32*math.pi)), R['cream'][0])
    for i, (x, rp, w, h) in enumerate(((6, R['pink'], 6, 9), (14, R['cream'], 7, 7), (23, R['lilac'], 5, 10), (29, R['blue'], 4, 6))):
        for yy in range(h):
            for xx in range(w): c.p(x+xx, 5+yy, rp[3] if xx == 0 else rp[2] if xx < w-1 else rp[1])
        c.rect(x, 5+h-1, w, 1, rp[1]); c.p(x+1, 4, wd[0]); c.p(x+w-2, 4, wd[0])
    return c
reg('laundry', laundry())

def cottage():
    w, h = 84, 86; c = Cv(w, h); cr = R['cream']; rf = R['roof']; wd = R['wood']; st = R['stone']
    # walls
    for y in range(44, 82):
        for x in range(10, 74): c.p(x, y, cr[2] if x < 60 else cr[1])
    for y in range(44, 48):
        for x in range(10, 74): c.p(x, y, cr[0] if y < 46 else cr[1])     # roof shadow
    for y in range(78, 82):
        for x in range(9, 75): c.p(x, y, st[2] if (x//5 + y//2) % 2 else st[1])
    for x in (10, 11, 72, 73): 
        for y in range(44, 78): c.p(x, y, wd[1] if x in (10, 72) else wd[0])
    for x in range(10, 74): c.p(x, 60, wd[1]); c.p(x, 61, wd[0])
    for i in range(14): c.p(13+i, 62+i, wd[1]); c.p(14+i, 62+i, wd[0]); c.p(70-i, 62+i, wd[1]); c.p(69-i, 62+i, wd[0])
    # roof: big curved witch-hat roof with shingle rows
    for y in range(4, 50):
        t = (y-4)/46; half = 6 + 40*(t**1.5); lean = (1-t)*9
        for x in range(w):
            if abs(x-(42+lean)) <= half:
                row = y//4; k = (x + (row % 2)*3) % 6
                col = rf[2] if x < 42+lean-half*.3 else rf[1] if x < 42+lean+half*.45 else rf[0]
                if y % 4 == 3: col = tone(col, .93, 1.05)
                elif k == 0: col = tone(col, .96, 1.05)
                if y % 4 == 0 and k in (2, 3) and x < 42+lean: col = rf[3]
                c.p(x, y, col)
    for x in range(1, 83): 
        if c.get(x, 49)[3]: c.p(x, 49, rf[0]); c.p(x, 48, tone(rf[0], 1.0, 1.0))
    c.rect(49, 0, 5, 6, rf[1]); c.p(50, 0, rf[3]); c.p(55, 3, rf[0]); c.p(56, 4, rf[0])   # bent tip
    # chimney
    c.rect(20, 14, 8, 16, st[2]); c.rect(26, 14, 2, 16, st[0]); c.rect(19, 12, 10, 3, st[3]); c.rect(19, 14, 10, 1, st[1])
    for y in range(17, 28, 3): c.p(22, y, st[0]); c.p(24, y+1, st[1])
    # attic round window
    c.ell(44, 32, 5.5, 5.5, wd[1]); c.ell(44, 32, 4, 4, hx('#fff0b4')); c.rect(44, 28, 1, 9, wd[1]); c.rect(40, 32, 9, 1, wd[1]); c.p(42, 30, hx('#ffffff'))
    # door (arched)
    for y in range(58, 80):
        for x in range(36, 49):
            if y > 63 or ((x-42)/6.5)**2 + ((y-64)/6.5)**2 <= 1:
                c.p(x, y, wd[2] if x < 40 else wd[1] if x < 46 else wd[0])
    for y in range(60, 80, 1): c.p(40, y, wd[1]); c.p(44, y, wd[0])
    c.p(46, 70, R['gold'][2]); c.p(46, 71, R['gold'][0]); c.ell(42, 63, 2, 2, hx('#fff0b4'))
    c.rect(34, 80, 17, 2, st[3]); c.rect(33, 82, 19, 2, st[2]); c.rect(33, 84, 19, 1, st[0])
    # windows + flower boxes
    for wx in (16, 56):
        c.rect(wx, 62, 12, 11, wd[1]); c.rect(wx+1, 63, 10, 9, hx('#fff0b4')); c.rect(wx+6, 63, 1, 9, wd[1]); c.rect(wx+1, 67, 10, 1, wd[1])
        c.p(wx+2, 64, hx('#ffffff')); c.p(wx+3, 64, hx('#ffffff')); c.p(wx+2, 65, hx('#ffffff'))
        c.rect(wx-1, 73, 14, 3, wd[2]); c.rect(wx-1, 75, 14, 1, wd[0])
        for i, nm in enumerate(('pink', 'gold', 'lilac', 'pink', 'red')):
            flower_head(c, wx+1+i*3, 71, R[nm]); c.p(wx+i*3, 72, R['moss'][1]); c.p(wx+2+i*3, 72, R['moss'][0])
    # ivy on the wall
    m = R['mint']; rnd = random.Random(4)
    for i in range(40):
        x = 62 + rnd.randint(0, 11); y = 46 + rnd.randint(0, 30)
        if rnd.random() < (80-y)/34: c.p(x, y, m[rnd.randint(0, 2)]); c.p(x+1, y, m[rnd.randint(1, 3)])
    return c
reg('cottage', cottage())

# ---------------------------------------------------------------- puzzle objects
def pillar(c, x, glow):
    st = R['stone']
    for y in range(6, 30):
        for xx in range(8): c.p(x+xx, y, st[3] if xx == 0 else st[2] if xx < 4 else st[1] if xx < 7 else st[0])
    c.rect(x-1, 27, 10, 3, st[1]); c.rect(x-1, 29, 10, 1, st[0]); c.rect(x-1, 5, 10, 2, st[3]); c.rect(x-1, 7, 10, 1, st[1])
    for y in range(11, 24, 4): c.p(x+2, y, st[0]); c.p(x+5, y+2, st[0])
    col = R['gold'] if glow else R['plum']
    c.art(x+1, 0, ["..aa..", ".abbc.", ".abbc.", "..cc.."], {'a': col[3], 'b': col[2], 'c': col[1]})
    m = R['moss']; c.p(x, 26, m[2]); c.p(x+1, 27, m[1]); c.p(x+6, 9, m[2]); c.p(x+7, 10, m[1])
def gate(open_):
    c = Cv(56, 30); pillar(c, 2, open_); pillar(c, 46, open_)
    if not open_:
        v = R['teal']; rnd = random.Random(3)
        for k in range(4):
            for x in range(10, 46):
                y = 14 + k*4 + int(2.4*math.sin(x*.45 + k*1.7))
                c.p(x, y, v[1]); c.p(x, y+1, v[0])
                if (x + k*3) % 7 == 0: c.p(x, y-1, v[3]); c.p(x+1, y-2, v[2])
                if (x + k*5) % 11 == 0: flower_head(c, x, y-1, R['lilac'])
    return c
reg('gate_closed', gate(False)); reg('gate_open', gate(True))

def rune(kind, awake):
    c = Cv(18, 24); st = R['stone']
    for y in range(1, 24):
        half = 7.5 - (max(0, 7-y)**2)*.09
        for x in range(18):
            if abs(x-8.5) <= half: c.p(x, y, st[3] if x < 4 else st[2] if x < 9 else st[1] if x < 14 else st[0])
    g = (R['mint'] if kind == 'wind' else R['blue'])
    col = g[3] if awake else st[0]; col2 = g[1] if awake else st[0]
    if kind == 'wind':
        rows = ["..aaaa..", ".a....a.", "....aa..", "aaaaa...", "........", "aaaaaaa.", "......a.", "....aa.."]
    else:
        rows = ["...a....", "...aa...", "..abba..", ".abbbba.", ".abbbba.", ".abbbba.", "..aaaa..", "........"]
    c.art(5, 7, rows, {'a': col, 'b': col2})
    m = R['moss']
    for x in range(2, 16): 
        if x % 3: c.p(x, 23, m[1]); c.p(x, 22, m[2]) if x % 2 else None
    c.p(4, 3, m[2]); c.p(5, 2, m[3]); c.p(3, 4, m[1])
    return c
for k in ('wind', 'water'):
    reg(f'rune_{k}_off', rune(k, False)); reg(f'rune_{k}_on', rune(k, True))

def leafpile():
    c = Cv(30, 18); rnd = random.Random(8)
    c.blob(15, 11, 14, 7, R['gold'], 1, hi=False); c.blob(10, 8, 8, 6, R['pink'], 2, hi=False); c.blob(20, 8, 8, 6, R['gold'], 3, hi=False)
    for _ in range(40):
        x, y = rnd.randint(2, 27), rnd.randint(3, 15)
        if c.get(x, y)[3]:
            rp = R[rnd.choice(('gold', 'pink', 'red'))]; c.p(x, y, rp[3]); c.p(x+1, y, rp[2]); c.p(x, y+1, rp[1])
    return c
reg('leafpile', leafpile())
def sprout():
    c = Cv(11, 10); g = R['mint']
    c.art(0, 0, ["..aa...bb..", ".aab...bbc.", ".abb..bbc..", "..bb.bbc...", "....cc.....", "....bc.....", "....bc.....", "...dbcd....", "..ddddd....", "..........."],
          {'a': g[3], 'b': g[2], 'c': g[0], 'd': R['wood'][1]})
    return c
reg('sprout', sprout())
def lily(big, flower):
    w, h = (24, 15) if big else (11, 7); c = Cv(w, h); g = R['mint']
    for y in range(h):
        for x in range(w):
            dx, dy = (x-w/2+.5)/(w/2), (y-h/2+.5)/(h/2)
            if dx*dx+dy*dy <= 1 and not (dx > .25 and abs(dy) < dx*.3):
                c.p(x, y, g[3] if dx+dy*1.2 < -.7 else g[2] if dx+dy < .35 else g[1])
    for i in range(3): c.p(w*.3+i*w*.1, h*.5+i*.4, g[1])
    if flower:
        fx, fy = int(w*.4), int(h*.4); p = R['pink']
        c.art(fx-2, fy-2, [".a.a.", "aabaa", ".bcb.", "aabaa", ".a.a."], {'a': p[3], 'b': p[2], 'c': hx('#fff0a8')})
    return c
reg('lily', lily(False, False)); reg('lily_flower', lily(False, True)); reg('lily_big', lily(True, False)); reg('lily_big_flower', lily(True, True))
def wilt(bloom):
    c = Cv(11, 14); g = R['moss'] if bloom else ramp('#9aa89c', '#afbcae', '#c4cfc2', '#d8e0d6')
    if bloom:
        for y in range(6, 14): c.p(5, y, g[1])
        c.p(4, 10, g[2]); c.p(3, 9, g[2]); c.p(6, 11, g[0]); c.p(7, 10, g[0])
        b = R['blue']
        c.art(1, 0, ["..aa.aa..", ".abbabba.", ".abbcbba.", "aabcycbaa", ".abbcbba.", ".abbabba.", "..aa.aa.."], {'a': b[3], 'b': b[2], 'c': b[1], 'y': hx('#fff0a8')})
    else:
        for y in range(5, 14): c.p(5 + (1 if y < 8 else 0), y, g[1])
        c.p(4, 11, g[0]); c.p(3, 12, g[0]); c.p(7, 5, g[1]); c.p(8, 6, g[1])
        l = ramp('#b0a4c4', '#c4bad4', '#d8d0e2', '#e8e2ee')
        c.art(6, 6, [".aa.", "abba", "abca", ".cc."], {'a': l[2], 'b': l[1], 'c': l[0]})
    return c
reg('wilt', wilt(False)); reg('bloom', wilt(True))

# ---------------------------------------------------------------- characters
WP = {'H': hx('#b69ad8'), 'h': hx('#9a7fc4'), 'L': hx('#d2c0ee'), 'B': hx('#f8de9c'), 'S': hx('#fee6d4'), 's': hx('#f4c8b4'),
      'C': hx('#f8aeb0'), 'E': hx('#6e5c94'), 'R': hx('#fbe3a4'), 'r': hx('#eec47a'), 'D': hx('#ee9fb8'), 'd': hx('#d5809f'),
      'W': hx('#fffaf0'), 'K': hx('#a97f6c'), 'k': hx('#8f6a62')}
HAT = ["........HH......", ".......HHh......", "......LHHh......", "......LHHHh.....", ".....LHHHHh.....",
       "....BBBBBBBB....", ".LHHHHHHHHHHHHh.", "..hhhhhhhhhhhh.."]
W_DOWN = HAT + ["...RRRSSSSRRR...", "...RRSSSSSSRR...", "...RSESSSSESR...", "...RSESSSSESR...", "...RSCSSSSCSR...",
                "...Rr.sSSs.rR...", "...rRDWWWWDRr...", "....DDDDDDDD....", "...SDDDDDDDdS...", "....DDDDDDDd....",
                "...DDDDDDDDdd...", "...dDDDDDDddd..."]
W_UP = HAT + ["...RRRRRRRRRR...", "...RRRRRRRRRR...", "...RRRRRRRRRr...", "...RRRRRRRRRr...", "...RRRRRRRRrr...",
              "...rRRRRRRRrr...", "....rRRRRRrr....", "....DrRRRRrD....", "...SDDrrrrDdS...", "....DDDDDDDd....",
              "...DDDDDDDDdd...", "...dDDDDDDddd..."]
HAT_S = ["......HH........", "......HHh.......", "......LHHh......", ".....LHHHh......", ".....LHHHHh.....",
         "....BBBBBBBB....", ".LHHHHHHHHHHHHh.", "..hhhhhhhhhhhh.."]
W_SIDE = HAT_S + ["....RRRRRSSS....", "...RRRRRSSSSS...", "...RRRRRSSSES...", "...RRRRRSSSES...", "...RRRRrSSCSS...",
                  "...rRRRrsSSS....", "....rRRDWWD.....", "....rRDDDDD.....", ".....DDDSSDD....", ".....DDDDDDd....",
                  "....DDDDDDDd....", "....dDDDDDdd...."]
LEGS = {'down': [[".....KK..KK.....", ".....Kk..Kk....."], [".....KK..KK.....", ".........Kk....."], [".....KK..KK.....", ".....Kk..Kk....."], [".....KK..KK.....", ".....Kk........."]],
        'side': [[".......KK.......", ".......KKk......"], [".....KK..KK.....", "....KK....KKk..."], [".......KK.......", ".......KKk......"], ["......KK.KK.....", ".....KK...Kk...."]]}
LEGS['up'] = LEGS['down']
for d, body in (('down', W_DOWN), ('up', W_UP), ('side', W_SIDE)):
    for f in range(4):
        c = Cv(16, 23); bob = 1 if f in (1, 3) else 0
        c.art(0, 1 - bob, body, WP); c.art(0, 21, LEGS[d][f], WP)
        if bob: c.art(0, 20, [LEGS[d][f][0]], WP)
        reg(f'witch_{d}_{f}', c)

CP = {'a': hx('#f4ece0'), 'b': hx('#e0d2c4'), 'c': hx('#c8b6ac'), 'e': hx('#7a6a8c'), 'n': hx('#f8aeb0'), 's': hx('#b69ad8'), 't': hx('#9a7fc4'), 'y': hx('#f8de9c')}
CAT = ["..a.....a.....", ".aba...aba....", ".aaaaaaaaa....", ".aaaaaaaab....", ".aeaaaeaab....", ".aaanaaaab....", "..aaaaaab.....", "..sssssst.....",
       ".aaayaaabb....", ".aaaaaaabb..b.", ".aaaaaaabb.bb.", ".aaaaaaabbbb..", ".aabaabaabb...", ".bb.bb.bbb...."]
CAT2 = CAT[:9] + [".aaaaaaabb....", ".aaaaaaabb..bb", ".aaaaaaabbbbb.", ".aabaabaabb...", ".bb.bb.bbb...."]
reg('cat_0', (lambda c: (c.art(0, 0, CAT, CP), c)[1])(Cv(14, 14))); reg('cat_1', (lambda c: (c.art(0, 0, CAT2, CP), c)[1])(Cv(14, 14)))

def elder(f):
    c = Cv(20, 22); st = R['cream']; rp = R['red']
    for y in range(11, 22):
        for x in range(5, 15): c.p(x, y, st[2] if x < 11 else st[1])
    c.rect(6, 21, 8, 1, st[0])
    c.blob(9.5, 7 + f, 9.5, 6.5, rp, 2, hi=False)
    for sx, sy in ((4, 5), (9, 3), (14, 6), (11, 8), (6, 9)): c.p(sx, sy+f, st[3]); c.p(sx+1, sy+f, st[3])
    e = hx('#7a6a8c'); c.p(7, 14, e); c.p(12, 14, e)
    c.art(6, 16, ["wwwwwwww", ".wwwwww.", "..wwww..", "...ww..."], {'w': hx('#ffffff')})
    c.p(6, 13, st[0]); c.p(7, 13, st[0]); c.p(12, 13, st[0]); c.p(13, 13, st[0])
    return c
reg('elder_0', elder(0)); reg('elder_1', elder(1))
FP = {'a': hx('#a8dcae'), 'b': hx('#8fcea3'), 'c': hx('#6fb596'), 'w': hx('#fffaf0'), 'e': hx('#6e5c94'), 'y': hx('#f6f0c0'), 'n': hx('#f8aeb0')}
FROG = [".aa....aa.", "awea..awea", "aaaaaaaaab", "aaaaaaaaab", "anaaaaanab", ".abyyyybb.", "aabyyyybbc", "aab.yy.bbc", "bb......cc"]
FROG2 = ["..........", ".aa....aa.", "awea..awea", "aaaaaaaaab", "anaaaaanab", ".abyyyybb.", "aabyyyybbc", "aab.yy.bbc", "bb......cc"]
reg('frog_0', (lambda c: (c.art(0, 0, FROG, FP), c)[1])(Cv(10, 9))); reg('frog_1', (lambda c: (c.art(0, 0, FROG2, FP), c)[1])(Cv(10, 9)))

# ---------------------------------------------------------------- sparks (the little light spirits)
def spark(kind, f):
    c = Cv(12, 13); rp = {'leaf': R['mint'], 'mush': R['pink'], 'dew': R['blue'], 'petal': R['gold'], 'moon': R['lilac']}[kind]
    ry = 3.6 if f == 0 else 3.1; cy = 8.6 if f == 0 else 9.1
    for y in range(13):
        for x in range(12):
            dx, dy = (x-5.5)/(4.6 if f == 0 else 5), (y-cy)/ry
            if dx*dx+dy*dy <= 1: c.p(x, y, rp[3] if dx+dy*1.3 < -.55 else rp[2] if dx+dy < .5 else rp[1])
    e = tone(rp[0], .62); ey = int(cy)
    c.p(3, ey, e); c.p(8, ey, e); c.p(2, ey+1, hx('#f8aeb0')); c.p(9, ey+1, hx('#f8aeb0')); c.p(4, int(cy-2), hx('#ffffff'))
    top = int(cy-ry)
    if kind == 'leaf':
        g = R['moss']; c.p(5, top-1, g[0]); c.p(5, top-2, g[1]); c.p(6, top-3, g[2]); c.p(7, top-3, g[3]); c.p(4, top-3, g[2]); c.p(3, top-3, g[3])
    elif kind == 'mush':
        r = R['red']; c.rect(3, top-2, 6, 2, r[1]); c.rect(4, top-3, 4, 1, r[2]); c.p(4, top-2, hx('#ffffff')); c.p(7, top-1, hx('#ffffff')); c.p(2, top-1, r[1]); c.p(9, top-1, r[0])
    elif kind == 'dew':
        c.p(5, top-1, rp[2]); c.p(6, top-1, rp[2]); c.p(5, top-2, rp[3]); c.p(6, top-3, rp[3])
    elif kind == 'petal':
        p = R['pink']
        for x in (2, 5, 8): c.p(x, top-1, p[2]); c.p(x+1, top-1, p[1]); c.p(x, top-2, p[3])
    else:
        y = hx('#fff0a8'); c.p(4, top-3, y); c.p(3, top-2, y); c.p(3, top-1, y); c.p(4, top, y) if False else None; c.p(5, top-1, y); c.p(8, top-3, hx('#ffffff'))
    return c
for k in ('leaf', 'mush', 'dew', 'petal', 'moon'):
    for f in (0, 1): reg(f'spark_{k}_{f}', spark(k, f))

# ---------------------------------------------------------------- fx (no outline)
def raw(name, rows, pal, px=None, py=None):
    c = Cv(len(rows[0]), len(rows)); c.art(0, 0, rows, pal); SPR[name] = (c, c.w/2 if px is None else px, c.h/2 if py is None else py)
Wt = {'w': (255, 255, 255, 255), 'y': hx('#fff6c8'), 'g': (255, 250, 220, 150)}
raw('sparkle_0', ["..w..", "..y..", "wyyyw", "..y..", "..w.."], Wt); raw('sparkle_1', [".w.", "wyw", ".w."], Wt); raw('sparkle_2', ["w"], Wt)
raw('mote', ["yy", "yy"], {'y': (255, 250, 214, 200)})
for nm in ('mint', 'pink', 'gold'):
    raw(f'leafbit_{nm}_0', [".ab", "abc", "bc."], {'a': R[nm][3], 'b': R[nm][2], 'c': R[nm][1]})
    raw(f'leafbit_{nm}_1', ["ab.", "bbc", ".cc"], {'a': R[nm][3], 'b': R[nm][2], 'c': R[nm][1]})
PF = {'a': (255, 252, 244, 235), 'b': (240, 232, 226, 210)}
raw('puff_0', [".aa.", "aaab", "aabb", ".bb."], PF); raw('puff_1', ["..aa..", ".a..a.", "a....b", "a....b", ".a..b.", "..bb.."], PF); raw('puff_2', ["a...a", ".....", "..a..", ".....", "b...b"], PF)
raw('drop', [".a.", "aba", "abb", ".b."], {'a': R['blue'][3], 'b': R['blue'][2]})
raw('gust', ["...aaaa...", ".aa....aa.", "........aa", "aaaaaaaaa.", "..........", ".aaaaaa...", "......aa..", ".....aa..."], {'a': (255, 255, 255, 215)})
raw('ripple', ["aaaa..aaa"], {'a': (236, 250, 252, 230)})
raw('bang', [".aa.", ".aa.", ".aa.", ".aa.", "....", ".aa."], {'a': hx('#f8de9c')}, py=0)
raw('dots', ["a.a.a"], {'a': (255, 255, 255, 230)}, py=0)
raw('white', ["a"], {'a': (255, 255, 255, 255)})
for nm, rp in (('pink', R['pink']), ('blue', R['blue']), ('gold', R['gold'])):
    raw(f'bfly_{nm}_0', ["aa...aa", "abb.bba", ".abebb.", ".ab.bc.", "..c.c.."], {'a': rp[3], 'b': rp[2], 'c': rp[1], 'e': hx('#7a6a8c')})
    raw(f'bfly_{nm}_1', ["..a.a..", "..beb..", "..beb..", "..c.c..", "......."], {'a': rp[3], 'b': rp[2], 'c': rp[1], 'e': hx('#7a6a8c')})
raw('bird_0', ["..aa..", ".aaeo.", "aaab..", ".bb..."], {'a': R['blue'][2], 'b': R['blue'][1], 'e': hx('#6e5c94'), 'o': hx('#f8de9c')}, py=0)
raw('bird_1', ["..aa..", "aaaeo.", ".aab..", ".bb..."], {'a': R['blue'][2], 'b': R['blue'][1], 'e': hx('#6e5c94'), 'o': hx('#f8de9c')}, py=0)

def soft(name, w, h, fn, col):
    c = Cv(w, h)
    for y in range(h):
        for x in range(w):
            a = fn((x+.5)/w, (y+.5)/h)
            a = int(a*5)/5.0          # stepped alpha keeps the pixel-art feel
            if a > 0: c.a[y, x] = (col[0], col[1], col[2], int(255*a))
    SPR[name] = (c, w/2, h/2)
soft('glow', 32, 32, lambda u, v: max(0, 1-math.hypot(u-.5, v-.5)*2)*.75, (255, 248, 200))
soft('shadow', 14, 6, lambda u, v: .34 if (u-.5)**2*4+(v-.5)**2*4 <= 1 else 0, (88, 110, 150))
soft('shaft', 40, 150, lambda u, v: max(0, (1-abs(u-.5)*2.0)) * (.55 if v < .75 else .55*(1-v)/.25) * (.5+.5*min(1, v*5)), (255, 247, 205))
soft('ring', 20, 20, lambda u, v: .8 if abs(math.hypot(u-.5, v-.5)-.42) < .06 else 0, (255, 250, 220))

# ---------------------------------------------------------------- ui
def panel():
    c = Cv(24, 24); cr = R['cream']; bd = hx('#d9a9b4'); bd2 = hx('#c58fa6')
    for y in range(24):
        for x in range(24):
            e = min(x, y, 23-x, 23-y); corner = (min(x, 23-x) + min(y, 23-y))
            if corner < 3: continue
            c.p(x, y, bd2 if e == 0 or corner == 3 else bd if e == 1 else cr[3] if e == 2 else cr[2])
    for x in range(3, 21): c.p(x, 21, cr[1])
    SPR['ui_panel'] = (c, 12, 12)
panel()
def keycap():
    c = Cv(14, 14)
    for y in range(14):
        for x in range(14):
            if min(x, 13-x) + min(y, 13-y) < 2: continue
            c.p(x, y, hx('#fffaf0') if y < 10 else hx('#e6d6c8'))
    for x in range(2, 12): c.p(x, 0, hx('#c9b3bd')); c.p(x, 13, hx('#b89cab'))
    for y in range(2, 12): c.p(0, y, hx('#c9b3bd')); c.p(13, y, hx('#b89cab'))
    SPR['ui_key'] = (c, 7, 7)
keycap()
raw('ui_wind', ["...aaaa...", ".aa....aa.", "........aa", "aaaaaaaaa.", "..........", ".aaaaaa...", "......aa..", ".....aa..."], {'a': hx('#74b996')})
raw('ui_water', ["...a....", "...aa...", "..abba..", ".abbbba.", ".awbbba.", ".abbbba.", "..aaaa.."], {'a': hx('#7fb2d6'), 'b': hx('#bfe0f0'), 'w': hx('#ffffff')})
raw('ui_light', ["...a...", "a..a..a", ".a.b.a.", "..bwb..", "aabwbaa", "..bwb..", ".a.b.a.", "a..a..a", "...a..."], {'a': hx('#f0c67e'), 'b': hx('#f8de9c'), 'w': hx('#fffbe6')})

# ---------------------------------------------------------------- companions (one per biome) & swap-puzzle props
def comp_glow():
    c = Cv(14, 15); st = R['cream']; cap = ramp('#4f9c9a', '#6fbcb0', '#96d8c4', '#c4f0dc')
    for y in range(8, 14):
        for x in range(4, 10): c.p(x, y, st[3] if x < 7 else st[2])
    c.rect(3, 13, 3, 2, st[1]); c.rect(8, 13, 3, 2, st[1])
    c.blob(6.5, 5, 6.5, 4.6, cap, 5, hi=False)
    for sx, sy in ((3, 4), (7, 2), (10, 5), (6, 6)): c.p(sx, sy, hx('#fff6b0')); c.p(sx+1, sy, hx('#ffe98a'))
    e = hx('#6e5c94'); c.p(5, 10, e); c.p(8, 10, e); c.p(4, 11, hx('#f8aeb0')); c.p(9, 11, hx('#f8aeb0'))
    c.p(6, 0, hx('#fff6b0')); c.p(7, 0, hx('#fff6b0')); c.p(6, 1, cap[2])
    return c
reg('comp_glow', comp_glow())
def comp_water():
    c = Cv(15, 14); b = R['blue']; g = R['mint']
    for y in range(14):
        for x in range(15):
            dx, dy = (x-7)/6.6, (y-8.5)/5
            if dx*dx+dy*dy <= 1: c.p(x, y, b[3] if dx+dy*1.3 < -.6 else b[2] if dx+dy < .45 else b[1])
    for x in range(3, 12): c.p(x, 3, g[2] if x < 8 else g[1]); 
    for x in range(4, 11): c.p(x, 2, g[3] if x < 7 else g[2])
    c.p(7, 3, b[2]); c.p(8, 1, R['pink'][2]); c.p(9, 1, R['pink'][3]); c.p(8, 0, R['pink'][3])
    e = hx('#5a6aa0'); c.p(4, 8, e); c.p(10, 8, e); c.p(4, 7, e); c.p(10, 7, e); c.p(3, 10, hx('#f8aeb0')); c.p(11, 10, hx('#f8aeb0'))
    c.p(6, 10, e); c.p(7, 11, e); c.p(8, 10, e); c.p(5, 5, hx('#ffffff'))
    return c
reg('comp_water', comp_water())
BP = {'a': hx('#fff4e4'), 'b': hx('#fbdcc8'), 'c': hx('#f0bfae'), 'e': hx('#6e5c94'), 'o': hx('#f6c070'), 'g': hx('#aee0ad'), 'h': hx('#8fcea3'), 'n': hx('#f8aeb0'), 'p': hx('#f8c8d6')}
BIRD = ["....gg.......", "...ghh.......", "...aaaaa.....", "..aaaaaab....", ".aaeaaaabb...", "ooanaaaabbpp.", ".aaaaabbbbppp", ".aaaabbbbcpp.", "..aabbbbcc...", "...bbbccc....", "....o..o....."]
reg('comp_wind', (lambda c: (c.art(0, 0, BIRD, BP), c)[1])(Cv(13, 11)))

GLOWOFF = ramp('#8e98b0', '#a8b2c4', '#c2cad6', '#dadfe6')
def glowcap(on):
    c = mushroom(R['gold'] if on else GLOWOFF, 13, 15, spots=False)
    for sx, sy in ((3, 3), (7, 2), (9, 5), (5, 6)):
        c.p(sx, sy, hx('#fffbe0') if on else GLOWOFF[3]); c.p(sx+1, sy, hx('#fff0a8') if on else GLOWOFF[2])
    return c
reg('glowcap_off', glowcap(False)); reg('glowcap_on', glowcap(True))
def shadevine():
    c = Cv(30, 20); p = ramp('#7f6aa8', '#9a86c0', '#b7a4d6', '#d6c8ea'); rnd = random.Random(21)
    c.blob(15, 12, 14, 8, p, 1, hi=False); c.blob(9, 9, 8, 7, p, 2, hi=False); c.blob(21, 9, 8, 7, p, 3, hi=False)
    for k in range(5):
        for x in range(3, 27):
            y = 5 + k*3 + int(1.6*math.sin(x*.6 + k*2.1))
            if c.get(x, y)[3]: c.p(x, y, p[0])
            if (x + k*4) % 6 == 0 and c.get(x, y-1)[3]: c.p(x, y-1, p[3])
    for _ in range(5):
        x, y = rnd.randint(5, 24), rnd.randint(5, 15)
        if c.get(x, y)[3]: c.p(x, y, hx('#5e4f8a')); c.p(x+1, y, hx('#5e4f8a')); c.p(x, y-1, R['lilac'][3])
    return c
reg('shadevine', shadevine())
def lamp(stage):
    c = Cv(13, 18); g = R['moss']
    for y in range(7, 18): c.p(6, y, g[1]); 
    c.p(5, 13, g[2]); c.p(4, 12, g[2]); c.p(3, 12, g[3]); c.p(7, 14, g[0]); c.p(8, 13, g[0]); c.p(9, 13, g[1])
    if stage == 0:
        l = ramp('#b0a4c4', '#c4bad4', '#d8d0e2', '#e8e2ee')
        c.art(4, 1, [".aa..", "abba.", "abbc.", "abcc.", ".cc.."], {'a': l[3], 'b': l[2], 'c': l[0]}); c.p(6, 6, g[1])
    else:
        p = R['pink']; core = hx('#fffbe0') if stage == 2 else hx('#f8de9c')
        c.art(1, 0, ["...aa.aa...", "..abbabba..", ".abbbbbbba.", ".abbcyycba.", ".abbyyyyba.", ".abbcyycba.", "..abbbbba..", "...accca..."],
              {'a': p[3], 'b': p[2], 'c': p[1], 'y': core})
        if stage == 2: c.p(6, 3, hx('#ffffff')); c.p(5, 4, hx('#ffffff'))
    return c
reg('lamp_bud', lamp(0)); reg('lamp_bloom', lamp(1)); reg('lamp_lit', lamp(2))
raw('ui_glow', ["...a...", ".a.a.a.", "..bbb..", "aabwbaa", "..bbb..", ".a.a.a.", "...a..."], {'a': hx('#f0c67e'), 'b': hx('#f8de9c'), 'w': hx('#fffbe6')})
raw('ui_swap', ["..a....", ".aaaaa.", "..a..a.", ".a..a..", ".aaaaa.", "....a.."], {'a': hx('#b89cab')})
