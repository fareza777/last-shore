# ===================== V2 CONTENT =====================
DEER = M('Deer', '#a8744b'); DEERL = M('DeerLight', '#dcbd94'); DEERD = M('DeerDark', '#553a27')
MONK = M('Monkey', '#6d4c35'); MONKL = M('MonkeyLight', '#c9a37c')
SHARK = M('Shark', '#5d7280', rough=0.4); SHARKB = M('SharkBelly', '#dfe6ea', rough=0.4, double=True)
SNAKE = M('Snake', '#52803c', rough=0.5); SNAKED = M('SnakeDark', '#2f4d27', rough=0.5)
TURT = M('Turtle', '#4b6a3f'); TURTL = M('TurtleLight', '#a9b27c')
FOWL = M('Fowl', '#8a5a34'); FOWLD = M('FowlDark', '#22402f', double=True); COMB = M('Comb', '#c8322c')
PARR = M('Parrot', '#d33b2f'); PARRW = M('ParrotWing', '#2f6fd0', double=True); PARRB = M('ParrotBeak', '#f0c23c')
BFLY = M('Butterfly', '#f39a2b', double=True, emit=0.3)
PLANE = M('PlaneBody', '#d9dbdc', rough=0.45); PLANES = M('PlaneStripe', '#b4412f', rough=0.45); GLASS = M('PlaneGlass', '#27323a', rough=0.15)
PLAST = M('Plaster', '#dad5c8'); PLASTR = M('PlasterRed', '#a8402f')
CANVO = M('CanvasOld', '#b8ac8b', double=True); CLAY = M('Clay', '#8b5b3d'); BRASS = M('Brass', '#c9a24a', rough=0.3)
BAMB = M('Bamboo', '#86a84a', rough=0.5); BAMBD = M('BambooDark', '#5f7d37'); BANA = M('Banana', '#f0cf3f', rough=0.5)
PETAL = M('PetalPink', '#e8679a', double=True); PETALY = M('PetalYellow', '#f4d04a', double=True); PETALV = M('PetalViolet', '#8e63d6', double=True)
SKIN = M('Skin', '#c8906c'); SLEEVE = M('Sleeve', '#3e5b6d'); FALL = M('Waterfall', '#cfeef7', rough=0.2, double=True, emit=0.25)
VEN = M('VenisonRaw', '#b8404a', rough=0.5); VENC = M('VenisonCooked', '#70391f'); EGGM = M('Egg', '#f2ead9', rough=0.4)

def tri(pts, mat):
    bm = bmesh.new(); bm.faces.new([bm.verts.new(p) for p in pts]); return mk('p', bm, mat, recalc=False)

def legs4(body, names, xs, ys, top, r1, r2, mat, hoof=CHAR):
    for nm, x, y in zip(names, xs, ys):
        child(nm, [cyl(r1, r2, top, mat, 6, loc=(x, y, top), down=True), cyl(r2 * 1.1, r2 * 1.2, 0.06, hoof, 6, loc=(x, y, 0.06), down=True)], body, (x, y, top))

def big_tree():
    R.seed(41); H = 11.5; n = 8
    pts = [(0.5 * (i / n) ** 2, 0.3 * (i / n) ** 2, H * i / n) for i in range(n + 1)]
    rad = [1.25 - 0.75 * (i / n) + (0.5 if i == 0 else 0) for i in range(n + 1)]
    parts = [tube(pts, rad, BARK, 10)]
    for i in range(7):
        a = i * 0.9 + 0.2
        parts.append(pole((1.0 * math.cos(a), 1.0 * math.sin(a), 1.6), (3.0 * math.cos(a), 3.0 * math.sin(a), -0.2), 0.42, 0.14, BARK, 6))
    top = Vector(pts[-1])
    for i in range(6):
        a = i * 1.05; e = top + Vector((4.2 * math.cos(a), 4.2 * math.sin(a), rr(0.5, 2.2)))
        parts.append(pole(top - Vector((0, 0, rr(1.0, 3.5))), e, 0.34, 0.12, BARK))
    mats = [LEAF, LEAFD, LEAFL]
    for i in range(11):
        a = i * 0.62; d = 0 if i == 0 else rr(2.4, 5.2)
        parts.append(blob(rr(2.6, 3.8), mats[i % 3], 3, 0.3, scale=(1, 1, rr(0.6, 0.8)), seed=60 + i, loc=top + Vector((d * math.cos(a), d * math.sin(a), rr(0.8, 3.0) + (2.0 if i == 0 else 0)))))
    return reg('BigTree', J('BigTree', parts))

def dead_tree():
    R.seed(8); parts = [tube([(0.2 * (i / 5) ** 2, 0, 5.0 * i / 5) for i in range(6)], [0.34 - 0.2 * i / 5 + (0.1 if i == 0 else 0) for i in range(6)], STEM, 7)]
    for i in range(6):
        a = i * 1.1; z = rr(2.2, 4.8)
        e = Vector((rr(1.2, 2.2) * math.cos(a), rr(1.2, 2.2) * math.sin(a), z + rr(0.6, 1.6)))
        parts.append(pole((0.1, 0, z), e, 0.1, 0.03, STEM, 5)); parts.append(pole(e, e + Vector((rr(-0.6, 0.6), rr(-0.6, 0.6), 0.7)), 0.035, 0.012, STEM, 4))
    return reg('DeadTree', J('DeadTree', parts))

def banana_plant():
    R.seed(13); parts = [tube([(0, 0, 2.4 * i / 5) for i in range(6)], [0.2 - 0.07 * i / 5 for i in range(6)], BAMBD, 7)]
    for i in range(7):
        parts.append(frond(rr(1.7, 2.3), rr(0.38, 0.5), LEAFL if i % 2 else LEAF, arch=rr(0.7, 1.1), droop=rr(0.8, 1.2), fold=0.2, n=6, loc=(0, 0, 2.35), rot=(rr(-0.2, 0.2), 0, i * 0.9 + rr(-0.1, 0.1))))
    body = J('BananaPlant', parts)
    fr = [pole((0.12, 0.05, 2.2), (0.42, 0.1, 1.75), 0.03, 0.03, BAMBD, 5)]
    for i in range(7):
        a = i * 0.9
        fr.append(cyl(0.035, 0.02, 0.2, BANA, 5, loc=(0.42 + 0.07 * math.cos(a), 0.1 + 0.07 * math.sin(a), 1.75 - (i % 3) * 0.07), rot=(2.6 + 0.3 * math.cos(a), 0.3 * math.sin(a), 0)))
    child('Fruit', fr, body, (0, 0, 0))
    return reg('BananaPlant', body)

def bamboo():
    R.seed(17); parts = []
    for i in range(7):
        a = rr(0, 6.28); d = rr(0, 0.45); h = rr(4.5, 7.0); lean = rr(-0.12, 0.12)
        x, y = d * math.cos(a), d * math.sin(a); segs = 7
        for s in range(segs):
            z0 = h * s / segs; z1 = h * (s + 1) / segs
            parts.append(pole((x + lean * z0, y, z0), (x + lean * z1, y, z1 - 0.03), 0.055, 0.05, BAMB if s % 2 else BAMBD, 6))
        for k in range(4):
            parts.append(frond(rr(0.8, 1.2), 0.07, LEAFL, arch=0.4, droop=0.8, fold=0.1, n=3, loc=(x + lean * h * 0.9, y, h * rr(0.7, 1.0)), rot=(0, 0, rr(0, 6.28))))
    return reg('BambooGrove', J('BambooGrove', parts))

def flower(name, petal):
    R.seed(5); parts = []
    for i in range(4):
        x, y = rr(-0.25, 0.25), rr(-0.25, 0.25); h = rr(0.25, 0.45)
        parts.append(pole((x, y, 0), (x, y, h), 0.012, 0.01, LEAFD, 4))
        for k in range(5):
            parts.append(frond(0.09, 0.035, petal, arch=0.3, droop=0.2, fold=0.1, n=2, loc=(x, y, h), rot=(0, 0, k * 1.2566)))
        parts.append(frond(0.22, 0.05, LEAF, arch=0.6, droop=0.6, n=2, loc=(x, y, 0.02), rot=(0, 0, rr(0, 6.28))))
    return reg(name, J(name, parts))

def herb_plant():
    R.seed(31); parts = []
    for i in range(6):
        parts.append(frond(rr(0.3, 0.45), 0.09, LEAFL, arch=rr(0.9, 1.4), droop=rr(0.6, 1.0), n=3, rot=(0, 0, i * 1.05)))
    for i in range(3):
        x, y = rr(-0.08, 0.08), rr(-0.08, 0.08)
        parts.append(pole((x, y, 0), (x, y, 0.5), 0.012, 0.01, LEAFD, 4)); parts.append(blob(0.045, PETALV, 1, 0.2, loc=(x, y, 0.52), seed=i))
    return reg('HerbPlant', J('HerbPlant', parts))

def reed():
    R.seed(6); parts = []
    for i in range(9):
        x, y = rr(-0.3, 0.3), rr(-0.3, 0.3); h = rr(1.0, 1.7)
        parts.append(pole((x, y, 0), (x + rr(-0.1, 0.1), y, h), 0.015, 0.008, BAMB, 4)); parts.append(cyl(0.028, 0.02, 0.16, WOODD, 5, loc=(x, y, h - 0.1)))
    return reg('Reed', J('Reed', parts))

# ---------------- animals ----------------
def deer():
    parts = [blob(0.36, DEER, 2, 0.08, scale=(0.72, 1.55, 0.9), loc=(0, 0, 0.98), seed=2), blob(0.3, DEERL, 2, 0.05, scale=(0.6, 1.3, 0.5), loc=(0, 0.02, 0.84), seed=3),
             pole((0, -0.42, 1.08), (0, -0.68, 1.52), 0.13, 0.09, DEER, 7), blob(0.14, DEER, 2, 0.05, scale=(0.75, 1.35, 0.85), loc=(0, -0.8, 1.6), seed=4),
             cyl(0.07, 0.045, 0.16, DEERD, 6, loc=(0, -0.93, 1.56), rot=(pi / 2 + 0.2, 0, 0)), blob(0.07, DEERL, 1, 0, scale=(1, 1, 1.4), loc=(0, 0.56, 1.08))]
    for s in (1, -1):
        parts.append(cyl(0.05, 0.003, 0.15, DEERD, 4, loc=(s * 0.1, -0.74, 1.7), rot=(0, s * 0.7, 0)))
        base = Vector((s * 0.06, -0.76, 1.72)); mid = base + Vector((s * 0.16, 0.06, 0.3)); tip = mid + Vector((s * 0.1, -0.1, 0.3))
        parts += [pole(base, mid, 0.022, 0.016, TUSK, 5), pole(mid, tip, 0.016, 0.006, TUSK, 5), pole(mid, mid + Vector((s * 0.02, -0.2, 0.14)), 0.014, 0.005, TUSK, 4), pole(base + Vector((s * 0.08, 0.03, 0.15)), base + Vector((s * 0.1, -0.18, 0.24)), 0.014, 0.005, TUSK, 4)]
    body = J('Deer', parts)
    legs4(body, ('LegFL', 'LegFR', 'LegBL', 'LegBR'), (0.15, -0.15, 0.15, -0.15), (-0.38, -0.38, 0.4, 0.4), 0.82, 0.06, 0.035, DEERD)
    return reg('Deer', body)

def monkey():
    parts = [blob(0.2, MONK, 2, 0.08, scale=(0.8, 1.25, 0.85), loc=(0, 0, 0.42), seed=1), blob(0.13, MONK, 2, 0.05, loc=(0, -0.3, 0.56), seed=2),
             blob(0.075, MONKL, 1, 0.03, scale=(1, 0.7, 0.9), loc=(0, -0.4, 0.53)), blob(0.045, MONKL, 1, 0, loc=(0.13, -0.28, 0.6)), blob(0.045, MONKL, 1, 0, loc=(-0.13, -0.28, 0.6))]
    body = J('Monkey', parts)
    legs4(body, ('LegFL', 'LegFR', 'LegBL', 'LegBR'), (0.13, -0.13, 0.12, -0.12), (-0.2, -0.2, 0.2, 0.2), 0.38, 0.045, 0.035, MONK, MONKL)
    child('Tail', [tube([(0, 0.25 + 0.12 * i, 0.45 + 0.35 * math.sin(i * 0.5)) for i in range(7)], [0.03 - 0.003 * i for i in range(7)], MONK, 5)], body, (0, 0.25, 0.45))
    return reg('Monkey', body)

def shark():
    parts = [blob(0.5, SHARK, 3, 0.03, scale=(0.56, 3.1, 0.62), loc=(0, 0, 0), seed=1), blob(0.42, SHARKB, 2, 0.02, scale=(0.5, 2.8, 0.35), loc=(0, -0.05, -0.13), seed=2),
             tri([(0, -0.25, 0.28), (0, 0.45, 0.28), (0, 0.4, 0.95)], SHARKB), tri([(0.2, -0.5, -0.1), (0.25, 0.0, -0.12), (0.95, 0.05, -0.3)], SHARKB), tri([(-0.2, -0.5, -0.1), (-0.25, 0.0, -0.12), (-0.95, 0.05, -0.3)], SHARKB)]
    for p in parts[2:]: p.data.materials.clear(); p.data.materials.append(SHARK)
    body = J('Shark', parts)
    t1 = tri([(0, 1.25, 0.0), (0, 2.1, 0.85), (0, 1.7, 0.0)], SHARK); t2 = tri([(0, 1.25, 0.0), (0, 1.9, -0.5), (0, 1.7, 0.0)], SHARK)
    child('Tail', [t1, t2], body, (0, 1.25, 0))
    return reg('Shark', body)

def snake():
    head = J('Snake', [blob(0.065, SNAKE, 1, 0.05, scale=(0.9, 1.5, 0.7), loc=(0, 0, 0.05))])
    for i in range(1, 10):
        r = 0.055 - i * 0.0042
        s = J('Seg%d' % i, [blob(r, SNAKED if i % 2 else SNAKE, 1, 0.03, scale=(1, 1.6, 0.85), loc=(0, i * 0.13, r * 0.85))])
        for v in s.data.vertices: v.co -= Vector((0, i * 0.13, 0))
        s.location = (0, i * 0.13, 0); s.parent = head
    return reg('Snake', head)

def turtle():
    body = J('Turtle', [blob(0.3, TURT, 2, 0.08, scale=(1, 1.25, 0.5), flat=0.25, loc=(0, 0, 0.17), seed=4), blob(0.26, TURTL, 1, 0.03, scale=(1, 1.2, 0.2), loc=(0, 0, 0.09)),
                        blob(0.08, TURTL, 1, 0.03, scale=(0.9, 1.4, 0.8), loc=(0, -0.42, 0.14))])
    for nm, x, y, a in (('FlipFL', 0.27, -0.2, -0.5), ('FlipFR', -0.27, -0.2, 0.5), ('FlipBL', 0.24, 0.26, -2.4), ('FlipBR', -0.24, 0.26, 2.4)):
        f = blob(0.12, TURTL, 1, 0.05, scale=(0.5, 1.5, 0.2), loc=(x + math.sin(-a) * 0.12, y - math.cos(a) * 0.12, 0.08), rot=(0, 0, a))
        child(nm, [f], body, (x, y, 0.08))
    return reg('Turtle', body)

def fowl():
    parts = [blob(0.15, FOWL, 2, 0.06, scale=(0.8, 1.25, 1.0), loc=(0, 0, 0.3), seed=1), pole((0, -0.12, 0.36), (0, -0.2, 0.52), 0.06, 0.045, FOWL, 6), blob(0.06, FOWL, 1, 0.03, loc=(0, -0.22, 0.55)),
             cyl(0.02, 0.002, 0.06, PARRB, 4, loc=(0, -0.27, 0.54), rot=(pi / 2, 0, 0)), box(0.015, 0.07, 0.05, COMB, loc=(0, -0.21, 0.62))]
    for i in range(4): parts.append(frond(0.3, 0.05, FOWLD, arch=1.1, droop=0.9, n=3, loc=(0, 0.14, 0.38), rot=(0, 0, pi / 2 + (i - 1.5) * 0.25)))
    body = J('Fowl', parts)
    for nm, x in (('LegL', 0.06), ('LegR', -0.06)):
        child(nm, [cyl(0.014, 0.01, 0.2, PARRB, 4, loc=(x, 0, 0.2), down=True), box(0.05, 0.08, 0.012, PARRB, loc=(x, -0.02, 0.006))], body, (x, 0, 0.2))
    return reg('Fowl', body)

def bird(name, body_m, wing_m, beak_m, tail_m):
    body = J(name, [blob(0.11, body_m, 2, 0.05, scale=(0.8, 2.1, 0.8), loc=(0, 0, 0)), blob(0.06, body_m, 1, 0.02, loc=(0, -0.24, 0.05)), cyl(0.02, 0.002, 0.07, beak_m, 5, loc=(0, -0.29, 0.045), rot=(pi / 2, 0, 0)),
                    quad([(-0.06, 0.2, 0), (0.06, 0.2, 0), (0.07, 0.48, 0), (-0.07, 0.48, 0)], tail_m)])
    for nm, s in (('WingL', 1), ('WingR', -1)):
        w = quad([(s * 0.05, -0.09, 0.02), (s * 0.05, 0.12, 0.02), (s * 0.55, 0.1, 0.02), (s * 0.45, -0.05, 0.02)], wing_m, 2, 1)
        child(nm, [w], body, (s * 0.05, 0, 0.02))
    return reg(name, body)

def butterfly():
    body = J('Butterfly', [cyl(0.006, 0.004, 0.06, CHAR, 4, loc=(0, 0.03, 0), rot=(pi / 2, 0, 0))])
    for nm, s in (('WingL', 1), ('WingR', -1)):
        child(nm, [quad([(0, -0.03, 0), (0, 0.03, 0), (s * 0.07, 0.05, 0), (s * 0.08, -0.05, 0)], BFLY)], body, (0, 0, 0))
    return reg('Butterfly', body)

# ---------------- places ----------------
def watchtower():
    parts = []
    for sx in (1, -1):
        for sy in (1, -1):
            parts.append(pole((sx * 1.4, sy * 1.4, 0), (sx * 1.0, sy * 1.0, 6.0), 0.12, 0.1, BARK, 7))
            parts.append(pole((sx * 1.0, sy * 1.0, 6.0), (sx * 1.0, sy * 1.0, 8.1), 0.07, 0.06, WOOD, 6))
    for z0, z1 in ((0.6, 2.8), (3.0, 5.4)):
        for a in range(4):
            c = [(1, 1), (-1, 1), (-1, -1), (1, -1)]; p = c[a]; q = c[(a + 1) % 4]
            k0 = 1.4 - 0.4 * z0 / 6; k1 = 1.4 - 0.4 * z1 / 6
            parts.append(pole((p[0] * k0, p[1] * k0, z0), (q[0] * k1, q[1] * k1, z1), 0.05, 0.05, WOODD, 5))
    for i in range(9): parts.append(box(0.3, 2.5, 0.07, WOODL if i % 2 else WOOD, loc=(-1.12 + i * 0.28, 0, 6.03)))
    for a in range(4):
        c = [(1.05, 1.05), (-1.05, 1.05), (-1.05, -1.05), (1.05, -1.05)]; p = c[a]; q = c[(a + 1) % 4]
        if a != 2: parts.append(pole((p[0], p[1], 7.0), (q[0], q[1], 7.0), 0.04, 0.04, WOOD, 5)); parts.append(pole((p[0], p[1], 6.55), (q[0], q[1], 6.55), 0.03, 0.03, WOODD, 5))
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=4, radius1=2.1, radius2=0.05, depth=1.2)
    parts.append(mk('p', bm, THATCH, loc=(0, 0, 8.7), rot=(0, 0, pi / 4)))
    parts += [pole((-0.3, -1.45, 0), (-0.3, -1.1, 6.1), 0.04, 0.04, WOOD, 5), pole((0.3, -1.45, 0), (0.3, -1.1, 6.1), 0.04, 0.04, WOOD, 5)]
    for i in range(14): parts.append(pole((-0.3, -1.45 + 0.35 * i / 14, 0.4 + i * 0.42), (0.3, -1.45 + 0.35 * i / 14, 0.4 + i * 0.42), 0.025, 0.025, WOODD, 4))
    return reg('Watchtower', J('Watchtower', parts))

def pyre():
    R.seed(3); parts = [cyl(1.3, 1.3, 0.05, ASH, 12)]
    for i in range(12):
        a = i * 2 * pi / 12; parts.append(blob(0.28, STONE if i % 2 else STONED, 1, 0.3, scale=(1, 1, 0.8), seed=i, loc=(1.45 * math.cos(a), 1.45 * math.sin(a), 0.14)))
    for i in range(10):
        a = i * 2 * pi / 10 + 0.2; parts.append(pole((1.0 * math.cos(a), 1.0 * math.sin(a), 0.05), (-0.1 * math.cos(a), -0.1 * math.sin(a), 2.6), 0.1, 0.07, WOODD if i % 2 else BARK, 6))
    for i in range(5): parts.append(frond(1.2, 0.25, THATCH, arch=0.2, droop=0.6, n=3, loc=(0, 0, 1.2), rot=(0, 0, i * 1.26)))
    return reg('SignalPyre', J('SignalPyre', parts))

def plane_wreck():
    prof = [(0.0, 0.15), (0.6, 0.55), (1.6, 0.78), (3.2, 0.8), (4.6, 0.7)]
    fus = tube([(0, 0, z) for z, r in prof], [r for z, r in prof], PLANE, 10); fus.rotation_euler = (pi / 2, 0, 0); fus.location = (0, 0, 0.75)
    tail = tube([(0, 0, 0), (0, 0, 1.4), (0, 0, 2.6)], [0.6, 0.38, 0.14], PLANE, 8); tail.rotation_euler = (pi / 2 - 0.25, 0, 0.5); tail.location = (2.4, 6.6, 0.5)
    parts = [fus, tail, cyl(0.42, 0.42, 0.5, STONED, 10, loc=(0, 0.1, 0.75), rot=(pi / 2, 0, 0)),
             box(0.14, 0.05, 1.5, WOODD, loc=(0, 0.45, 0.9), rot=(0, 0.5, 0)), box(0.14, 0.05, 1.1, WOODD, loc=(0.3, 0.5, 0.35), rot=(0.4, 1.5, 0)),
             box(5.2, 1.5, 0.14, PLANE, loc=(2.9, -2.4, 0.95), rot=(0, 0.22, 0.12)), box(0.9, 1.52, 0.15, PLANES, loc=(5.0, -2.1, 1.47), rot=(0, 0.22, 0.12)),
             box(4.2, 1.4, 0.14, PLANE, loc=(-3.6, -1.2, 0.12), rot=(0.05, -0.04, -0.6)), box(1.9, 0.1, 0.5, GLASS, loc=(0, -1.9, 1.35), rot=(0.3, 0, pi / 2)),
             box(0.1, 1.3, 1.5, PLANES, loc=(2.75, 7.5, 1.25), rot=(-0.25, 0, 0.5)), box(2.4, 0.9, 0.1, PLANE, loc=(2.9, 7.6, 0.55), rot=(-0.2, 0.1, 0.5)),
             box(1.61, 0.9, 0.03, PLANES, loc=(0, -3.0, 1.2), rot=(0, 0, pi / 2))]
    for i in range(6): parts.append(box(rr(0.3, 0.9), rr(0.2, 0.6), 0.05, PLANE, loc=(rr(-4, 4), rr(-6, 4), 0.03), rot=(0, 0, rr(0, 3))))
    return reg('PlaneWreck', J('PlaneWreck', parts))

def lighthouse():
    hs = [0, 3.2, 6.4, 9.6, 12.4]; rs = [2.7, 2.45, 2.2, 1.98, 1.8]; parts = []
    for i in range(4):
        parts.append(tube([(0, 0, hs[i]), (0, 0, hs[i + 1])], [rs[i], rs[i + 1]], PLASTR if i == 2 else PLAST, 12))
    parts += [cyl(2.45, 2.45, 0.22, STONED, 12, loc=(0, 0, 12.4)), box(1.1, 0.5, 2.1, CHAR, loc=(0, -2.5, 1.05)), box(1.4, 0.3, 0.25, STONE, loc=(0, -2.75, 2.25))]
    for i in range(12):
        a = i * 2 * pi / 12
        if i % 3 != 1: parts.append(pole((2.3 * math.cos(a), 2.3 * math.sin(a), 12.6), (2.3 * math.cos(a), 2.3 * math.sin(a), 13.5), 0.035, 0.035, METAL, 4))
    for i in range(6):
        a = i * 2 * pi / 6
        if i != 4: parts.append(pole((1.2 * math.cos(a), 1.2 * math.sin(a), 12.6), (1.2 * math.cos(a), 1.2 * math.sin(a), 14.9 - (0.9 if i in (3, 5) else 0)), 0.06, 0.06, METAL, 5))
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=False, segments=6, radius1=1.5, radius2=0.1, depth=1.2)
    for f in [f for f in bm.faces if f.calc_center_median().x < -0.2 and f.calc_center_median().y < 0.3]: bm.faces.remove(f)
    parts.append(mk('p', bm, PLASTR, loc=(0, 0, 15.5), recalc=False)); bpy.data.materials['PlasterRed'].use_backface_culling = False
    R.seed(9)
    for i in range(9):
        a = rr(0, 6.28); d = rr(3.0, 5.5); parts.append(blob(rr(0.3, 0.8), PLAST if i % 2 else STONE, 1, 0.35, scale=(1.2, 1, 0.7), seed=i, flat=0.4, loc=(d * math.cos(a), d * math.sin(a), 0.2)))
    return reg('Lighthouse', J('Lighthouse', parts))

def pier():
    R.seed(12); parts = []
    for i in range(22):
        if i in (9, 15, 16, 20): continue
        parts.append(box(2.2, 0.5, 0.08, WOODL if i % 3 else WOOD, loc=(rr(-0.05, 0.05), i * 0.56, 1.0 + (rr(-0.15, 0) if i > 17 else 0)), rot=(rr(-0.04, 0.04), rr(-0.06, 0.06) - (0.2 if i > 18 else 0), rr(-0.04, 0.04))))
    for i in range(5):
        for x in (-1.0, 1.0):
            parts.append(cyl(0.13, 0.11, 2.6 + (0.5 if i == 4 else 0), BARK, 7, loc=(x, i * 3.0, -1.4)))
        parts.append(box(2.2, 0.14, 0.16, WOODD, loc=(0, i * 3.0, 0.88)))
    for x in (-1.0, 1.0): parts.append(box(0.14, 12.0, 0.16, WOODD, loc=(x, 6.0, 0.9)))
    return reg('Pier', J('Pier', parts))

def rock_arch():
    parts = [blob(1, STONED, 3, 0.3, scale=(2.4, 2.6, 5.2), seed=71, loc=(-4.6, 0, 3.2)), blob(1, STONE, 3, 0.3, scale=(2.2, 2.4, 4.6), seed=72, loc=(4.4, 0.3, 2.8)),
             blob(1, STONED, 3, 0.28, scale=(7.4, 2.3, 1.9), seed=73, loc=(0, 0.1, 7.6)), blob(1, STONE, 2, 0.3, scale=(1.6, 1.6, 1.2), seed=74, loc=(-6.5, 1.2, 0.6)), blob(1, STONEL, 2, 0.3, scale=(1.3, 1.2, 1.0), seed=75, loc=(6.2, -1.0, 0.5))]
    return reg('RockArch', J('RockArch', parts))

def waterfall():
    R.seed(21); parts = []
    for i in range(9):
        a = -1.25 + i * 0.3125; d = 6.5
        parts.append(blob(1, STONED if i % 2 else STONE, 3, 0.3, scale=(rr(2.4, 3.2), rr(2.2, 2.8), rr(5.5, 7.5)), seed=80 + i, loc=(d * math.sin(a), d * math.cos(a) - 4.0, rr(3.5, 5.0))))
    parts += [blob(1, STONE, 3, 0.25, scale=(3.4, 2.6, 1.4), seed=90, loc=(0, 2.4, 10.6)), blob(1, STONEL, 2, 0.3, scale=(1.2, 1.1, 0.8), seed=91, loc=(-2.6, -1.2, 0.4)), blob(1, STONE, 2, 0.3, scale=(1.4, 1.0, 0.7), seed=92, loc=(2.9, -1.6, 0.35))]
    body = J('Waterfall', parts)
    child('Fall', [quad([(-1.3, 1.0, 11.0), (1.3, 1.0, 11.0), (1.5, -0.4, 0.2), (-1.5, -0.4, 0.2)], FALL, 3, 8, 0.5, (0, -1, 0))], body, (0, 0, 0))
    return reg('Waterfall', body)

def rowboat():
    h = J('rb', hull_parts(0, 1, 8, 7.0) + [box(2.0, 0.3, 0.06, DECK, loc=(0, 0.6, 0.35)), box(1.7, 0.3, 0.06, DECK, loc=(0, -1.2, 0.4))])
    h.scale = (0.5, 0.5, 0.55); h.rotation_euler = (0, 0.35, 0); h.location = (0, 0, 0.42)
    for m in h.data.materials:
        pass
    parts = [h, pole((0.9, -1.0, 0.1), (1.3, 1.4, 0.16), 0.03, 0.03, WOOD), box(0.2, 0.5, 0.03, WOODD, loc=(1.33, 1.6, 0.16), rot=(0, 0, -0.16))]
    return reg('Rowboat', J('Rowboat', parts))

def tent():
    parts = [pole((0, -1.3, 0), (0, -1.3, 1.6), 0.04, 0.04, WOOD), pole((0, 1.3, 0), (0, 1.3, 1.5), 0.04, 0.04, WOOD), pole((0, -1.4, 1.6), (0, 1.4, 1.5), 0.035, 0.035, WOOD),
             quad([(0, -1.35, 1.6), (0, 1.35, 1.5), (1.25, 1.35, 0.02), (1.25, -1.35, 0.02)], CANVO, 3, 3, 0.12, (0.5, 0, 0.5)),
             quad([(0, -1.35, 1.6), (0, 1.35, 1.5), (-1.25, 1.0, 0.02), (-1.25, -1.35, 0.02)], CANVO, 3, 3, 0.1, (-0.5, 0, 0.5))]
    for x in (1.3, -1.3): parts.append(cyl(0.02, 0.01, 0.25, WOODD, 4, loc=(x, -1.2, -0.05), rot=(0, x * 0.3, 0)))
    return reg('Tent', J('Tent', parts))

def chest():
    parts = [box(0.9, 0.55, 0.42, WOOD, loc=(0, 0, 0.21)), box(0.94, 0.59, 0.16, WOODD, loc=(0, 0, 0.5)), box(0.96, 0.6, 0.05, WOODL, loc=(0, 0, 0.6))]
    for x in (-0.3, 0.3): parts.append(box(0.07, 0.62, 0.62, METAL, loc=(x, 0, 0.31)))
    parts.append(box(0.1, 0.04, 0.12, BRASS, loc=(0, -0.3, 0.42)))
    return reg('Chest', J('Chest', parts))

def bed():
    parts = [cyl(0.07, 0.07, 2.0, BARK, 6, loc=(-0.5, -1.0, 0.12), rot=(-pi / 2, 0, 0)), cyl(0.07, 0.07, 2.0, BARK, 6, loc=(0.5, -1.0, 0.12), rot=(-pi / 2, 0, 0)),
             cyl(0.07, 0.07, 1.1, BARK, 6, loc=(-0.55, -1.0, 0.14), rot=(0, pi / 2, 0)), cyl(0.07, 0.07, 1.1, BARK, 6, loc=(-0.55, 1.0, 0.14), rot=(0, pi / 2, 0))]
    R.seed(2)
    for i in range(9): parts.append(frond(1.0, 0.22, LEAFP if i % 2 else LEAF, arch=0.04, droop=0.02, fold=0.08, n=3, loc=(-0.48, -0.9 + i * 0.22, 0.22), rot=(0, 0, rr(-0.1, 0.1))))
    parts.append(blob(0.4, HIDE, 2, 0.2, scale=(1.1, 1.6, 0.12), seed=5, loc=(0, 0.2, 0.3))); parts.append(blob(0.2, CLOTH, 1, 0.15, scale=(1.5, 0.8, 0.45), loc=(0, -0.75, 0.33)))
    return reg('Bed', J('Bed', parts))

def cooking_pot():
    parts = []
    for i in range(3):
        a = i * 2 * pi / 3 + 0.5; parts.append(pole((0.75 * math.cos(a), 0.75 * math.sin(a), 0), (0.05 * math.cos(a), 0.05 * math.sin(a), 1.5), 0.04, 0.03, WOOD))
    parts += [torus(0.08, 0.014, ROPE, 8, 4, loc=(0, 0, 1.42)), pole((0, 0, 1.4), (0, 0, 0.75), 0.012, 0.012, ROPE, 4), blob(0.26, CLAY, 2, 0.04, scale=(1, 1, 0.85), flat=0.85, loc=(0, 0, 0.55)),
              torus(0.2, 0.03, CLAY, 12, 5, loc=(0, 0, 0.72)), cyl(0.19, 0.19, 0.01, COOKED, 10, loc=(0, 0, 0.7)), cyl(0.38, 0.38, 0.02, ASH, 10)]
    for i in range(7):
        a = i * 2 * pi / 7; parts.append(blob(0.1, STONE, 1, 0.3, seed=i, loc=(0.42 * math.cos(a), 0.42 * math.sin(a), 0.06)))
    for i in range(3):
        a = i * 2.1; parts.append(pole((0.25 * math.cos(a), 0.25 * math.sin(a), 0.03), (-0.1 * math.cos(a), -0.1 * math.sin(a), 0.2), 0.04, 0.035, CHAR, 5))
    return reg('CookingPot', J('CookingPot', parts))

def fish_trap():
    parts = [tube([(0, 0, 0), (0, 0, 0.25), (0, 0, 0.6), (0, 0, 0.9)], [0.3, 0.32, 0.24, 0.06], WOODL, 9)]
    for z in (0.02, 0.3, 0.6): parts.append(torus(0.31 if z < 0.5 else 0.25, 0.015, ROPE, 10, 4, loc=(0, 0, z)))
    o = J('ft', parts); o.rotation_euler = (pi / 2, 0, 0.3); o.location = (0, 0.45, 0.32)
    return reg('FishTrap', J('FishTrap', [o, pole((0.5, -0.4, 0), (0.5, -0.4, 0.9), 0.03, 0.025, WOOD), pole((0.5, -0.4, 0.8), (0.1, -0.2, 0.4), 0.008, 0.008, ROPE, 4)]))

def torch_stand():
    return reg('TorchStand', J('TorchStand', [cyl(0.045, 0.035, 1.7, WOOD, 6), cyl(0.07, 0.08, 0.2, CHAR, 7, loc=(0, 0, 1.68)), lash(1.66, 0.075), blob(0.13, STONE, 1, 0.3, loc=(0.08, 0.05, 0.05), seed=1), blob(0.12, STONED, 1, 0.3, loc=(-0.08, -0.04, 0.05), seed=2)]))

def fence():
    parts = []
    for i in range(6): parts.append(cyl(0.07, 0.02, 1.3 + (0.12 if i % 2 else 0), BARK if i % 2 else WOODD, 6, loc=(-1.0 + i * 0.4, 0, 0)))
    parts += [pole((-1.15, 0.06, 0.45), (1.15, 0.06, 0.5), 0.035, 0.035, WOOD), pole((-1.15, 0.06, 0.95), (1.15, 0.06, 0.9), 0.035, 0.035, WOOD)]
    return reg('Fence', J('Fence', parts))

# ---------------- items ----------------
def bow():
    pts = [(0, 0.16 * (1 - ((i - 5) / 5) ** 2), -0.55 + 1.1 * i / 10) for i in range(11)]
    return reg('Bow', J('Bow', [tube(pts, [0.012 + 0.012 * (1 - abs(i - 5) / 5) for i in range(11)], WOODD, 5), pole((0, 0, -0.55), (0, 0, 0.55), 0.004, 0.004, STEM, 3), lash(0.0, 0.028), lash(0.05, 0.028)]))
def arrow():
    return reg('Arrow', J('Arrow', [cyl(0.008, 0.008, 0.7, WOODL, 5, loc=(0, 0, -0.35)), cyl(0.02, 0.001, 0.07, FLINT, 4, loc=(0, 0, 0.35)),
                                    tri([(0, 0, -0.33), (0.035, 0, -0.3), (0, 0, -0.2)], GULL), tri([(0, 0, -0.33), (-0.035, 0, -0.3), (0, 0, -0.2)], GULL), tri([(0, 0, -0.33), (0, 0.035, -0.3), (0, 0, -0.2)], GULL)]))
def knife():
    return reg('Knife', J('Knife', [cyl(0.02, 0.022, 0.16, WOOD, 6, loc=(0, 0, -0.08)), blob(0.07, FLINT, 1, 0.2, scale=(0.5, 0.18, 1.6), seed=4, loc=(0, 0, 0.17)), lash(0.06, 0.024), lash(0.085, 0.024)]))
def bamboo_item():
    parts = [cyl(0.03, 0.03, 0.9, BAMB if i % 2 else BAMBD, 6, loc=(-0.45, (i - 1) * 0.07, 0.035 + (0.05 if i == 1 else 0)), rot=(0, pi / 2, 0)) for i in range(3)]
    parts.append(torus(0.07, 0.012, ROPE, 8, 4, loc=(0, 0, 0.05), rot=(0, pi / 2, 0)))
    return reg('BambooItem', J('BambooItem', parts))
def banana_item():
    return reg('BananaItem', J('BananaItem', [cyl(0.035, 0.015, 0.2, BANA, 5, loc=(0, (i - 1) * 0.045, 0.03), rot=(pi / 2 - 0.35, (i - 1) * 0.35, 0)) for i in range(3)] + [cyl(0.02, 0.02, 0.05, BAMBD, 5, loc=(0, -0.02, 0.03))]))
def egg():
    return reg('EggItem', J('EggItem', [blob(0.05, EGGM, 2, 0.0, scale=(1, 1, 1.3), loc=(0, 0, 0.06)), blob(0.05, EGGM, 2, 0.0, scale=(1, 1, 1.3), loc=(0.09, 0.03, 0.06), rot=(0.5, 0, 0)), frond(0.2, 0.08, THATCH, arch=0.2, droop=0.2, n=2, loc=(-0.08, -0.05, 0.01))]))
def feather():
    return reg('FeatherItem', J('FeatherItem', [frond(0.3, 0.05, GULL, arch=0.3, droop=0.3, fold=0.15, n=4, loc=(-0.15, 0, 0.01)), frond(0.26, 0.045, GULLG, arch=0.3, droop=0.3, fold=0.15, n=4, loc=(-0.12, 0.05, 0.02), rot=(0, 0, 0.4))]))
def herb_item():
    parts = [frond(0.25, 0.07, LEAFL, arch=0.2, droop=0.1, n=3, loc=(-0.12, (i - 1) * 0.03, 0.01), rot=(0, 0, (i - 1) * 0.35)) for i in range(3)]
    parts += [blob(0.03, PETALV, 1, 0.2, loc=(0.1, 0.02, 0.03)), blob(0.03, PETALV, 1, 0.2, loc=(0.08, -0.04, 0.03))]
    return reg('HerbItem', J('HerbItem', parts))
def clam():
    return reg('ClamItem', J('ClamItem', [blob(0.08, SHELL, 1, 0.12, scale=(1.2, 1, 0.4), flat=0.1, loc=(0, 0, 0.03)), blob(0.08, SHELL, 1, 0.12, scale=(1.2, 1, 0.4), flat=0.1, loc=(0, 0.02, 0.06), rot=(0.5, 0, 0)), blob(0.03, FAT, 1, 0, loc=(0, -0.02, 0.05))]))
def bone():
    return reg('BoneItem', J('BoneItem', [pole((-0.15, 0, 0.03), (0.15, 0, 0.03), 0.018, 0.018, BONE)] + [blob(0.03, BONE, 1, 0, loc=(x, y, 0.03)) for x in (-0.16, 0.16) for y in (-0.02, 0.02)]))
def antidote():
    return reg('Antidote', J('Antidote', [blob(0.11, COCO, 2, 0.04, scale=(1, 1, 1.1), loc=(0, 0, 0.12)), cyl(0.035, 0.03, 0.06, WOODL, 8, loc=(0, 0, 0.22)), cyl(0.025, 0.028, 0.035, LEAFD, 8, loc=(0, 0, 0.28)), torus(0.1, 0.018, PETALV, 12, 4, loc=(0, 0, 0.14))]))
def stew():
    return reg('Stew', J('Stew', [blob(0.14, CLAY, 2, 0.03, scale=(1, 1, 0.6), flat=0.6, loc=(0, 0, 0.06)), cyl(0.125, 0.125, 0.01, COOKED, 10, loc=(0, 0, 0.1)), blob(0.03, MEAT, 1, 0.2, loc=(0.03, 0.02, 0.11)), blob(0.025, LEAFL, 1, 0.2, loc=(-0.04, -0.02, 0.11)), blob(0.025, BANA, 1, 0.2, loc=(-0.01, 0.05, 0.11))]))
def compass():
    return reg('Compass', J('Compass', [cyl(0.09, 0.09, 0.03, BRASS, 14), cyl(0.075, 0.075, 0.005, PAPER, 14, loc=(0, 0, 0.03)), box(0.012, 0.13, 0.006, COMB, loc=(0, 0, 0.038), rot=(0, 0, 0.6)), torus(0.085, 0.01, BRASS, 14, 4, loc=(0, 0, 0.035)), torus(0.02, 0.006, BRASS, 8, 4, loc=(0, 0.1, 0.015), rot=(pi / 2, 0, 0))]))
def backpack():
    return reg('Backpack', J('Backpack', [blob(0.2, HIDE, 2, 0.1, scale=(1, 0.7, 1.25), flat=0.8, loc=(0, 0, 0.22)), blob(0.16, WOODD, 1, 0.1, scale=(1.1, 0.75, 0.45), loc=(0, -0.02, 0.4)), box(0.06, 0.02, 0.08, BRASS, loc=(0, -0.15, 0.3)),
                                          torus(0.1, 0.014, ROPE, 8, 4, loc=(0.11, 0.13, 0.25), rot=(pi / 2, 0, 0.4)), torus(0.1, 0.014, ROPE, 8, 4, loc=(-0.11, 0.13, 0.25), rot=(pi / 2, 0, -0.4)), blob(0.07, CLOTH, 1, 0.1, scale=(1.6, 0.8, 0.8), loc=(0, 0, 0.5))]))
def arm():
    o = J('Arm', [cyl(0.055, 0.042, 0.46, SLEEVE, 8, loc=(0, 0, -0.56)), cyl(0.043, 0.038, 0.1, SKIN, 8, loc=(0, 0, -0.1)), blob(0.052, SKIN, 2, 0.04, scale=(1.05, 0.85, 1.15), loc=(0, 0, 0.02)),
                  blob(0.022, SKIN, 1, 0, scale=(1, 1, 1.5), loc=(0.045, -0.02, 0.03)), torus(0.047, 0.012, ROPE, 8, 4, loc=(0, 0, -0.1))])
    return reg('Arm', o)

big_tree(); dead_tree(); banana_plant(); bamboo(); flower('FlowerPink', PETAL); flower('FlowerYellow', PETALY); herb_plant(); reed()
deer(); monkey(); shark(); snake(); turtle(); fowl(); bird('Parrot', PARR, PARRW, PARRB, PARRW); butterfly()
watchtower(); pyre(); plane_wreck(); lighthouse(); pier(); rock_arch(); waterfall(); rowboat(); tent(); chest(); bed(); cooking_pot(); fish_trap(); torch_stand(); fence()
bow(); arrow(); knife(); bamboo_item(); banana_item(); egg(); feather(); herb_item(); clam(); bone(); antidote(); stew(); compass(); backpack(); arm()
meat('VenisonRaw', VEN); meat('VenisonCooked', VENC)

# ---------------- ambient occlusion baked into face-corner colours ----------------
from mathutils.bvhtree import BVHTree
_DIRS = []
_rs = random.Random(99)
while len(_DIRS) < 18:
    d = Vector((_rs.uniform(-1, 1), _rs.uniform(-1, 1), _rs.uniform(0.15, 1)))
    if 0.2 < d.length <= 1: _DIRS.append(d.normalized())

def bake_ao(obj):
    me = obj.data
    if not me.polygons: return
    bm = bmesh.new(); bm.from_mesh(me); bvh = BVHTree.FromBMesh(bm)
    col = me.color_attributes.get('Col') or me.color_attributes.new('Col', 'BYTE_COLOR', 'CORNER')
    reach = max(0.25, max(obj.dimensions) * 0.3)
    for poly in me.polygons:
        n = poly.normal; c = poly.center
        t = n.orthogonal().normalized(); b = n.cross(t)
        for li in poly.loop_indices:
            p = me.vertices[me.loops[li].vertex_index].co.lerp(c, 0.3) + n * (reach * 0.02 + 0.004)
            hit = 0
            for d in _DIRS:
                if bvh.ray_cast(p, t * d.x + b * d.y + n * d.z, reach)[0] is not None: hit += 1
            ao = 0.32 + 0.68 * (1 - hit / len(_DIRS)) ** 1.3
            col.data[li].color = (ao, ao, ao, 1)
    bm.free()
