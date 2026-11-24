# Tiny pixel-art toolkit: pastel ramps, shaded blobs, colour-matched selective outlines.
import numpy as np, colorsys, math, random

def hx(s):
    s = s.lstrip('#'); return (int(s[0:2],16), int(s[2:4],16), int(s[4:6],16), 255)

def ramp(*cols): return [hx(c) for c in cols]

R = dict(
 mint = ramp('#6fb596','#8fcea3','#aee0ad','#d6f2c2'),
 sage = ramp('#7db488','#9ccc8f','#bfe09c','#e4f4ba'),
 teal = ramp('#5fa39c','#80c0b2','#a2d8c4','#c8eeda'),
 pink = ramp('#d98bb0','#efa9c3','#f8c8d6','#fee6ea'),
 gold = ramp('#e0a868','#f0c67e','#f8de9c','#fdf2c8'),
 lilac= ramp('#a892d4','#c0aee6','#d8c9f2','#eee4fb'),
 bark = ramp('#9a7468','#b98e78','#d4ab8c','#e8c8a6'),
 stone= ramp('#9aa0b8','#b6bccc','#d0d5de','#eceef2'),
 wood = ramp('#b08462','#cfa57c','#e6c59a','#f6e2be'),
 cream= ramp('#d9c2a0','#eedcbc','#f8eeda','#fffaf0'),
 roof = ramp('#a877a6','#c592ba','#dcb0cf','#f0d0e2'),
 red  = ramp('#d9818a','#ee9fa0','#f8c0b8','#fde0d6'),
 blue = ramp('#7fb2d6','#9ccbe6','#bfe0f0','#e2f4fa'),
 moss = ramp('#86b87c','#a6d08c','#c6e4a2','#e4f4c0'),
 plum = ramp('#8a70b4','#a68cc8','#c4aede','#e0d0f0'),
)

def hsh(x, y, s=0):
    n = (int(x)*374761393 + int(y)*668265263 + s*1442695041) & 0xffffffff
    n = (n ^ (n >> 13)) * 1274126177 & 0xffffffff
    return ((n ^ (n >> 16)) & 0xffff) / 65535.0

def tone(c, f, sat=1.22, hue_to=0.74, hue_amt=0.10):
    """Darken a colour while keeping it colourful (hue drifts toward violet)."""
    r,g,b = c[0]/255, c[1]/255, c[2]/255
    h,s,v = colorsys.rgb_to_hsv(r,g,b)
    d = hue_to - h
    if d > .5: d -= 1
    if d < -.5: d += 1
    h = (h + d*hue_amt) % 1
    s = min(1, s*sat + 0.07); v = v*f
    r,g,b = colorsys.hsv_to_rgb(h,s,v)
    return (int(r*255), int(g*255), int(b*255), 255)

class Cv:
    def __init__(s, w, h):
        s.w, s.h = w, h
        s.a = np.zeros((h, w, 4), np.uint8)
    def p(s, x, y, c):
        x = int(x); y = int(y)
        if 0 <= x < s.w and 0 <= y < s.h and c is not None: s.a[y, x] = c
    def get(s, x, y):
        if 0 <= x < s.w and 0 <= y < s.h: return tuple(int(v) for v in s.a[y, x])
        return (0,0,0,0)
    def rect(s, x, y, w, h, c):
        for yy in range(int(y), int(y+h)):
            for xx in range(int(x), int(x+w)): s.p(xx, yy, c)
    def ell(s, cx, cy, rx, ry, c):
        for y in range(int(cy-ry)-1, int(cy+ry)+2):
            for x in range(int(cx-rx)-1, int(cx+rx)+2):
                if ((x-cx)/rx)**2 + ((y-cy)/ry)**2 <= 1.0: s.p(x, y, c)
    def line(s, x0, y0, x1, y1, c):
        n = int(max(abs(x1-x0), abs(y1-y0)))+1
        for i in range(n+1):
            t = i/max(n,1); s.p(round(x0+(x1-x0)*t), round(y0+(y1-y0)*t), c)
    def blob(s, cx, cy, rx, ry, rp, seed=0, lx=-0.55, ly=-0.83, hi=True):
        """Shaded cluster: 3 flat tones + leafy highlight ticks, light from upper-left."""
        for y in range(int(cy-ry)-1, int(cy+ry)+2):
            for x in range(int(cx-rx)-1, int(cx+rx)+2):
                dx, dy = (x-cx)/rx, (y-cy)/ry
                d = dx*dx + dy*dy
                if d > 1.0: continue
                l = dx*lx + dy*ly + (hsh(x, y, seed)-0.5)*0.22
                if l > 0.42 and d < 0.86: c = rp[2]
                elif l < -0.36: c = rp[0]
                else: c = rp[1]
                s.p(x, y, c)
        if hi:
            rnd = random.Random(seed*7919+int(cx)*31+int(cy))
            for _ in range(max(1, int(rx*ry/9))):
                ax = cx + rnd.uniform(-0.6, 0.15)*rx; ay = cy + rnd.uniform(-0.7, 0.0)*ry
                if ((ax-cx)/rx)**2 + ((ay-cy)/ry)**2 < 0.7:
                    s.p(ax, ay, rp[3]); s.p(ax+1, ay-1, rp[3])
            for _ in range(max(1, int(rx*ry/14))):
                ax = cx + rnd.uniform(-0.2, 0.7)*rx; ay = cy + rnd.uniform(0.1, 0.75)*ry
                if ((ax-cx)/rx)**2 + ((ay-cy)/ry)**2 < 0.75:
                    s.p(ax, ay, rp[1]); s.p(ax+1, ay-1, rp[1])
    def art(s, x, y, rows, pal):
        for j, row in enumerate(rows):
            for i, ch in enumerate(row):
                if ch in pal: s.p(x+i, y+j, pal[ch])
    def paste(s, o, x, y):
        for j in range(o.h):
            for i in range(o.w):
                if o.a[j, i, 3]: s.p(x+i, y+j, tuple(o.a[j, i]))
    def flip(s):
        n = Cv(s.w, s.h); n.a = s.a[:, ::-1].copy(); return n
    def outlined(s, top=0.86, bot=0.70):
        """Colour-matched selective outline: every edge pixel takes a deeper shade of the
        fill it touches; lit (top/left) edges stay lighter than shaded (bottom/right) ones."""
        n = Cv(s.w+2, s.h+2); n.a[1:-1, 1:-1] = s.a
        src = n.a.copy(); op = src[..., 3] > 0
        H, W = op.shape
        for y in range(H):
            for x in range(W):
                if op[y, x]: continue
                pick = None
                for dx, dy, f in ((0,-1,bot),(-1,0,bot),(1,0,top),(0,1,top)):
                    xx, yy = x+dx, y+dy
                    if 0 <= xx < W and 0 <= yy < H and op[yy, xx]:
                        if pick is None or f < pick[1]: pick = (tuple(src[yy, xx]), f)
                if pick: n.a[y, x] = tone(pick[0], pick[1])
        return n
