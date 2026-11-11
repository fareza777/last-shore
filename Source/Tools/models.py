import bpy, bmesh, math, random, json, os
from mathutils import Vector, noise

OUT = r"C:\Survival Fable 62\Assets\Models"
os.makedirs(OUT, exist_ok=True)
PALETTE = {}
COL = bpy.context.scene.collection
pi = math.pi

# ---------- reset scene ----------
for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)
for m in list(bpy.data.meshes): bpy.data.meshes.remove(m)
for m in list(bpy.data.materials): bpy.data.materials.remove(m)

def hex2lin(h):
    h = h.lstrip('#'); c = [int(h[i:i+2], 16) / 255 for i in (0, 2, 4)]
    return [(x / 12.92) if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c]

def M(name, hexcol, rough=0.85, double=False, emit=0.0):
    m = bpy.data.materials.get(name)
    if not m:
        m = bpy.data.materials.new(name); m.use_nodes = True
        b = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
        rgb = hex2lin(hexcol)
        b.inputs[0].default_value = (*rgb, 1)
        m.diffuse_color = (*rgb, 1)
        PALETTE[name] = {'hex': hexcol, 'rough': rough, 'double': double, 'emit': emit}
    return m

BARKP = M('BarkPalm', '#8f6f4e'); BARK = M('Bark', '#5e4532'); WOOD = M('Wood', '#a07a4c'); WOODD = M('WoodDark', '#6c4d2f'); WOODL = M('WoodLight', '#cda873')
LEAFP = M('LeafPalm', '#4f9d3c', double=True); LEAF = M('Leaf', '#3f8f3e'); LEAFD = M('LeafDark', '#2c6a34'); LEAFL = M('LeafLight', '#84c44e'); THATCH = M('Thatch', '#a9a14e', double=True)
BLADE = M('Blade', '#6fb04a', double=True)
STONE = M('Stone', '#8c8f93'); STONED = M('StoneDark', '#5d6166'); STONEL = M('StoneLight', '#b5b5af'); FLINT = M('Flint', '#353941', rough=0.5)
COCO = M('Coconut', '#6d4b2f'); COCOG = M('CoconutGreen', '#86b354')
BERRY = M('Berry', '#cf2740', rough=0.4); CAP = M('MushCap', '#d68f55'); STEM = M('MushStem', '#efe6d0')
ROPE = M('Rope', '#c9ae7b'); CANVAS = M('Canvas', '#ece3ca', double=True); PAPER = M('Paper', '#f0e6c9', double=True); INK = M('Ink', '#4b4135')
MEAT = M('MeatRaw', '#d9646a', rough=0.5); FAT = M('MeatFat', '#f3d8cb'); COOKED = M('MeatCooked', '#8d4d2b'); BONE = M('Bone', '#efe8d8')
BOAR = M('Boar', '#4d3c31'); BOARD = M('BoarDark', '#2f2520'); TUSK = M('Tusk', '#efe8d0')
CRAB = M('Crab', '#dc5a30', rough=0.5); CRABD = M('CrabDark', '#a93c1f')
GULL = M('Gull', '#f3f3f0'); GULLG = M('GullGrey', '#9aa4ae'); BEAK = M('Beak', '#e9a63b')
FISH = M('Fish', '#6aa6c8', rough=0.4); FISHB = M('FishBelly', '#e0eaee', rough=0.4); FISHC = M('FishCooked', '#b9793f')
HULL = M('Hull', '#ebe8e1', rough=0.5); STRIPE = M('HullStripe', '#1f507c', rough=0.5); DECK = M('Deck', '#b18657'); METAL = M('Metal', '#7b8087', rough=0.4)
ASH = M('Ash', '#3a3938'); CHAR = M('Charcoal', '#1e1c1b'); HIDE = M('Hide', '#7c5b41'); SHELL = M('Shell', '#f2dac6', rough=0.4); CLOTH = M('Cloth', '#b9c2a0')
SAND = M('SandMat', '#e6d3a3')

def mk(name, bm, mat, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1), recalc=True):
    if recalc: bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    me.materials.append(mat)
    o = bpy.data.objects.new(name, me); COL.objects.link(o)
    o.location = loc; o.rotation_euler = rot; o.scale = scale
    return o

def cyl(r1, r2, h, mat, seg=8, loc=(0, 0, 0), rot=(0, 0, 0), down=False):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=seg, radius1=r1, radius2=max(r2, 0.0005), depth=h)
    bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, -h / 2 if down else h / 2))
    return mk('p', bm, mat, loc, rot)

def pole(p0, p1, r1, r2, mat, seg=6):
    p0 = Vector(p0); p1 = Vector(p1); d = p1 - p0
    return cyl(r1, r2, d.length, mat, seg, loc=p0, rot=d.to_track_quat('Z', 'Y').to_euler())

def blob(r, mat, sub=2, amt=0.2, scale=(1, 1, 1), loc=(0, 0, 0), rot=(0, 0, 0), seed=0, freq=1.4, flat=None):
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=r)
    off = Vector((seed * 13.17, seed * 7.71, seed * 3.37))
    for v in bm.verts:
        n = noise.noise(v.co * freq / r + off)
        v.co += v.co.normalized() * n * amt * r
        if flat is not None and v.co.z < -flat * r: v.co.z = -flat * r
    return mk('p', bm, mat, loc, rot, scale)

def box(sx, sy, sz, mat, loc=(0, 0, 0), rot=(0, 0, 0)):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1)
    bmesh.ops.scale(bm, vec=(sx, sy, sz), verts=bm.verts)
    return mk('p', bm, mat, loc, rot)

def tube(pts, radii, mat, seg=8):
    bm = bmesh.new(); rings = []
    for p, r in zip(pts, radii):
        rings.append([bm.verts.new((p[0] + r * math.cos(2 * pi * i / seg), p[1] + r * math.sin(2 * pi * i / seg), p[2])) for i in range(seg)])
    for a, b in zip(rings, rings[1:]):
        for i in range(seg):
            j = (i + 1) % seg; bm.faces.new((a[i], a[j], b[j], b[i]))
    bm.faces.new(rings[-1]); bm.faces.new(list(reversed(rings[0])))
    return mk('p', bm, mat)

def torus(R, r, mat, seg=14, tseg=6, loc=(0, 0, 0), rot=(0, 0, 0)):
    bm = bmesh.new(); rings = []
    for i in range(seg):
        a = 2 * pi * i / seg; c = Vector((math.cos(a), math.sin(a), 0))
        rings.append([bm.verts.new(c * (R + r * math.cos(2 * pi * j / tseg)) + Vector((0, 0, r * math.sin(2 * pi * j / tseg)))) for j in range(tseg)])
    for i in range(seg):
        a = rings[i]; b = rings[(i + 1) % seg]
        for j in range(tseg):
            k = (j + 1) % tseg; bm.faces.new((a[j], b[j], b[k], a[k]))
    return mk('p', bm, mat, loc, rot)

def frond(L, W, mat, arch=0.5, droop=0.9, fold=0.35, n=6, loc=(0, 0, 0), rot=(0, 0, 0)):
    bm = bmesh.new(); rows = []
    for i in range(n + 1):
        t = i / n; x = L * t; z = L * (arch * t - droop * t * t)
        w = W * math.sin(pi * (0.06 + 0.94 * t)) ** 0.7
        rows.append((bm.verts.new((x, w, z - fold * w)), bm.verts.new((x, 0, z)), bm.verts.new((x, -w, z - fold * w))))
    for a, b in zip(rows, rows[1:]):
        bm.faces.new((a[0], a[1], b[1], b[0])); bm.faces.new((a[1], a[2], b[2], b[1]))
    return mk('p', bm, mat, loc, rot, recalc=False)

def quad(pts, mat, nx=1, ny=1, bulge=0.0, axis=(0, 1, 0)):
    """bilinear patch p00,p10,p11,p01 with optional bulge"""
    bm = bmesh.new(); P = [Vector(p) for p in pts]; grid = []
    for j in range(ny + 1):
        row = []
        for i in range(nx + 1):
            u = i / nx; v = j / ny
            p = (P[0] * (1 - u) + P[1] * u) * (1 - v) + (P[3] * (1 - u) + P[2] * u) * v
            p += Vector(axis) * bulge * math.sin(pi * u) * math.sin(pi * v)
            row.append(bm.verts.new(p))
        grid.append(row)
    for j in range(ny):
        for i in range(nx):
            bm.faces.new((grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]))
    return mk('p', bm, mat, recalc=False)

def J(name, parts, parent=None):
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts: o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    if len(parts) > 1: bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    o.name = name; o.data.name = name
    for p in o.data.polygons: p.use_smooth = False
    if parent: o.parent = parent
    return o

def child(name, parts, parent, pivot):
    """join parts, set origin at pivot, parent to root"""
    o = J(name, parts)
    piv = Vector(pivot)
    for v in o.data.vertices: v.co -= piv
    o.location = piv; o.parent = parent
    return o

MODELS = []
def reg(name, root): MODELS.append((name, root)); return root

R = random.Random(7)
def rr(a, b): return R.uniform(a, b)

# ================= VEGETATION =================
def palm(name, seed):
    R.seed(seed); H = rr(6.5, 8.5); bend = rr(0.8, 2.0); ang = rr(0, 2 * pi); n = 9
    pts = []; rad = []
    for i in range(n + 1):
        t = i / n
        pts.append((bend * t * t * math.cos(ang), bend * t * t * math.sin(ang), H * t))
        rad.append(0.36 - 0.2 * t + (0.1 if i == 0 else 0) + (0.03 if i % 2 else 0))
    parts = [tube(pts, rad, BARKP, 7)]
    top = Vector(pts[-1])
    for i in range(10):
        yaw = i * 2 * pi / 10 + rr(-0.2, 0.2)
        parts.append(frond(rr(2.8, 3.6), rr(0.5, 0.65), LEAFP, arch=rr(0.35, 0.7), droop=rr(0.8, 1.15), loc=top + Vector((0, 0, 0.1)), rot=(rr(-0.25, 0.25), 0, yaw)))
    for i in range(3):
        a = i * 2.1 + rr(0, 1)
        parts.append(blob(0.17, COCOG if i else COCO, 1, 0.05, loc=top + Vector((0.3 * math.cos(a), 0.3 * math.sin(a), -0.28))))
    return reg(name, J(name, parts))

def broad(name, seed, sub=3):
    R.seed(seed); H = rr(3.8, 5.2); bx = rr(-0.5, 0.5); by = rr(-0.5, 0.5); n = 6
    pts = [(bx * (i / n) ** 2, by * (i / n) ** 2, H * i / n) for i in range(n + 1)]
    rad = [0.42 - 0.24 * i / n + (0.14 if i == 0 else 0) for i in range(n + 1)]
    parts = [tube(pts, rad, BARK, 7)]
    top = Vector(pts[-1])
    for i in range(3):
        a = i * 2.1 + rr(0, 1); e = top + Vector((1.3 * math.cos(a), 1.3 * math.sin(a), rr(0.4, 1.0)))
        parts.append(pole(top - Vector((0, 0, rr(0.6, 1.4))), e, 0.13, 0.06, BARK))
    mats = [LEAF, LEAFD, LEAFL, LEAF, LEAFD]
    for i in range(5):
        a = i * 1.26 + rr(0, 0.6); d = 0 if i == 0 else rr(1.0, 1.6)
        parts.append(blob(rr(1.3, 1.9), mats[i], sub, 0.28, scale=(1, 1, rr(0.65, 0.85)), seed=seed * 5 + i,
                          loc=top + Vector((d * math.cos(a), d * math.sin(a), rr(0.5, 1.5) + (0.9 if i == 0 else 0)))))
    return reg(name, J(name, parts))

def stump():
    return reg('Stump', J('Stump', [cyl(0.44, 0.36, 0.45, BARK, 8), cyl(0.33, 0.33, 0.02, WOODL, 8, loc=(0, 0, 0.45))]))

def bush():
    R.seed(21)
    body = J('BerryBush', [blob(0.65, LEAF, 2, 0.25, loc=(0, 0, 0.55), seed=1), blob(0.5, LEAFD, 2, 0.25, loc=(0.5, 0.2, 0.42), seed=2),
                           blob(0.5, LEAFL, 2, 0.25, loc=(-0.45, -0.25, 0.42), seed=3), blob(0.42, LEAF, 1, 0.2, loc=(-0.1, 0.5, 0.38), seed=4)])
    bs = []
    for i in range(14):
        a = rr(0, 2 * pi); el = rr(0.1, 1.2); r = 0.72
        bs.append(blob(0.065, BERRY, 1, 0, loc=(r * math.cos(a) * math.cos(el) * 1.1, r * math.sin(a) * math.cos(el) * 1.1, 0.5 + r * math.sin(el) * 0.75)))
    child('Berries', bs, body, (0, 0, 0))
    return reg('BerryBush', body)

def fiber_plant():
    R.seed(5); parts = []
    for i in range(11):
        a = rr(0, 2 * pi)
        parts.append(frond(rr(0.7, 1.2), 0.05, BLADE, arch=rr(1.2, 2.2), droop=rr(0.6, 1.3), fold=0.2, n=4, loc=(rr(-0.1, 0.1), rr(-0.1, 0.1), 0), rot=(0, 0, a)))
    return reg('FiberPlant', J('FiberPlant', parts))

def fern():
    R.seed(9); parts = []
    for i in range(7):
        parts.append(frond(rr(0.7, 1.0), 0.2, LEAFD if i % 2 else LEAF, arch=rr(0.7, 1.0), droop=rr(0.9, 1.2), n=5, rot=(0, 0, i * 0.9 + rr(-0.2, 0.2))))
    return reg('Fern', J('Fern', parts))

def grass_tuft():
    R.seed(3); parts = []
    for i in range(6):
        parts.append(frond(rr(0.3, 0.5), 0.035, BLADE, arch=rr(1.5, 2.5), droop=rr(0.5, 1.2), fold=0.1, n=2, rot=(0, 0, rr(0, 6.28))))
    return reg('GrassTuft', J('GrassTuft', parts))

def rock(name, seed, sc, mat=STONE, sub=2):
    return reg(name, J(name, [blob(1, mat, sub, 0.38, scale=sc, seed=seed, flat=0.45, loc=(0, 0, 0.45 * sc[2]))]))

def flint_rock():
    parts = [blob(1, STONED, 2, 0.35, scale=(1.0, 0.85, 0.7), seed=31, flat=0.45, loc=(0, 0, 0.3))]
    R.seed(4)
    for i in range(6):
        a = rr(0, 2 * pi)
        parts.append(blob(0.22, FLINT, 1, 0.5, seed=40 + i, loc=(0.7 * math.cos(a), 0.6 * math.sin(a), rr(0.25, 0.6))))
    return reg('FlintRock', J('FlintRock', parts))

# ================= PICKUPS / ITEMS =================
def stick():
    return reg('Stick', J('Stick', [pole((-0.35, 0, 0.03), (0.3, 0.05, 0.035), 0.028, 0.02, WOOD), pole((0.05, 0.01, 0.035), (0.22, -0.14, 0.04), 0.016, 0.01, WOOD)]))
def stone():
    return reg('StoneItem', J('StoneItem', [blob(0.11, STONE, 1, 0.3, scale=(1.2, 1, 0.7), seed=3, loc=(0, 0, 0.07))]))
def flint():
    return reg('FlintItem', J('FlintItem', [blob(0.09, FLINT, 1, 0.6, scale=(1.5, 0.8, 0.5), seed=8, loc=(0, 0, 0.04))]))
def log():
    return reg('Log', J('Log', [cyl(0.17, 0.16, 1.5, BARK, 8, loc=(-0.75, 0, 0.17), rot=(0, pi / 2, 0)), cyl(0.13, 0.13, 1.52, WOODL, 8, loc=(-0.76, 0, 0.17), rot=(0, pi / 2, 0))]))
def coconut():
    return reg('CoconutItem', J('CoconutItem', [blob(0.15, COCO, 2, 0.06, scale=(1, 1, 1.12), loc=(0, 0, 0.16)), blob(0.03, BOARD, 1, 0, loc=(0.04, 0, 0.32))]))
def mushroom():
    return reg('Mushroom', J('Mushroom', [cyl(0.035, 0.028, 0.12, STEM, 6), blob(0.1, CAP, 1, 0.1, scale=(1, 1, 0.55), flat=0.2, loc=(0, 0, 0.14)),
                                          cyl(0.025, 0.02, 0.08, STEM, 6, loc=(0.12, 0.03, 0)), blob(0.065, CAP, 1, 0.1, scale=(1, 1, 0.55), flat=0.2, loc=(0.12, 0.03, 0.095))]))
def berries_item():
    parts = [blob(0.05, BERRY, 1, 0, loc=(0, 0, 0.05)), blob(0.05, BERRY, 1, 0, loc=(0.09, 0.02, 0.05)), blob(0.05, BERRY, 1, 0, loc=(0.04, 0.085, 0.05)), blob(0.045, BERRY, 1, 0, loc=(0.045, 0.035, 0.12)),
             frond(0.16, 0.05, LEAF, arch=0.3, droop=0.3, n=2, loc=(0.04, 0.03, 0.15), rot=(0, 0, 2.5))]
    return reg('BerriesItem', J('BerriesItem', parts))
def fiber_item():
    parts = [frond(0.5, 0.03, BLADE, arch=0.05, droop=0.0, fold=0.1, n=2, loc=(-0.25, (i - 3) * 0.012, 0.02 + (i % 2) * 0.012), rot=(0, 0, (i - 3) * 0.03)) for i in range(7)]
    parts.append(torus(0.045, 0.012, ROPE, 8, 4, loc=(0, 0, 0.025), rot=(0, pi / 2, 0)))
    return reg('FiberItem', J('FiberItem', parts))
def leaf_item():
    return reg('PalmLeaf', J('PalmLeaf', [frond(1.0, 0.2, LEAFP, arch=0.12, droop=0.1, fold=0.2, n=5, loc=(-0.5, 0, 0.03)), pole((-0.62, 0, 0.03), (-0.48, 0, 0.035), 0.015, 0.012, LEAFD)]))
def rope_item():
    return reg('RopeCoil', J('RopeCoil', [torus(0.16, 0.03, ROPE, 14, 5, loc=(0, 0, 0.03 + i * 0.05)) for i in range(3)] + [pole((0.16, 0, 0.13), (0.3, 0.1, 0.02), 0.025, 0.025, ROPE)]))
def canvas_item():
    return reg('CanvasRoll', J('CanvasRoll', [cyl(0.12, 0.12, 0.8, CANVAS, 10, loc=(-0.4, 0, 0.12), rot=(0, pi / 2, 0)), torus(0.125, 0.015, ROPE, 10, 4, loc=(-0.2, 0, 0.12), rot=(0, pi / 2, 0)),
                                              torus(0.125, 0.015, ROPE, 10, 4, loc=(0.2, 0, 0.12), rot=(0, pi / 2, 0))]))
def hide_item():
    return reg('HideItem', J('HideItem', [blob(0.3, HIDE, 2, 0.35, scale=(1.3, 1, 0.08), seed=12, loc=(0, 0, 0.03))]))
def meat(name, m):
    return reg(name, J(name, [blob(0.13, m, 2, 0.2, scale=(1.3, 1, 0.75), seed=6, loc=(0, 0, 0.09)), pole((0.12, 0, 0.09), (0.33, 0, 0.11), 0.022, 0.02, BONE), blob(0.035, BONE, 1, 0, loc=(0.34, 0, 0.11)),
                              blob(0.06, FAT if m is MEAT else WOODD, 1, 0.2, scale=(1, 1, 0.5), loc=(-0.05, 0.02, 0.17))]))
def fish(name, body, belly):
    parts = [blob(0.1, body, 2, 0.05, scale=(0.45, 2.2, 1.0), loc=(0, 0, 0.1)), blob(0.085, belly, 1, 0.03, scale=(0.4, 1.9, 0.6), loc=(0, 0, 0.07))]
    bm = bmesh.new(); a = bm.verts.new((0, 0.19, 0.1)); b = bm.verts.new((0, 0.36, 0.2)); c = bm.verts.new((0, 0.36, 0.0)); bm.faces.new((a, b, c))
    parts.append(mk('p', bm, M('FishFin', '#3f7fa6', double=True), recalc=False))
    bm = bmesh.new(); a = bm.verts.new((0, -0.05, 0.19)); b = bm.verts.new((0, 0.1, 0.19)); c = bm.verts.new((0, 0.06, 0.27)); bm.faces.new((a, b, c))
    parts.append(mk('p', bm, bpy.data.materials['FishFin'], recalc=False))
    return reg(name, J(name, parts))
def bandage():
    return reg('Bandage', J('Bandage', [cyl(0.06, 0.06, 0.12, CLOTH, 10, loc=(0, 0, 0.06), rot=(pi / 2, 0, 0)), cyl(0.025, 0.025, 0.125, LEAFD, 8, loc=(0, 0.002, 0.06), rot=(pi / 2, 0, 0)),
                                        box(0.1, 0.11, 0.004, CLOTH, loc=(0.07, -0.06, 0.004))]))
def flask():
    return reg('Flask', J('Flask', [blob(0.11, COCO, 2, 0.04, scale=(1, 1, 1.1), loc=(0, 0, 0.12)), cyl(0.035, 0.03, 0.06, WOODL, 8, loc=(0, 0, 0.22)), cyl(0.025, 0.028, 0.035, WOODD, 8, loc=(0, 0, 0.28)),
                                    torus(0.095, 0.01, ROPE, 12, 4, loc=(0, 0, 0.14))]))
def page():
    parts = [box(0.21, 0.29, 0.004, PAPER, loc=(0, 0, 0.004), rot=(0, 0, 0.15))]
    for i in range(6): parts.append(box(0.15 - (0.05 if i == 5 else 0), 0.006, 0.002, INK, loc=(0.005 * i - (0.025 if i == 5 else 0), 0.1 - i * 0.035, 0.0075), rot=(0, 0, 0.15)))
    parts.append(blob(0.05, STONED, 1, 0.3, scale=(1.2, 1, 0.6), loc=(0.06, -0.11, 0.03), seed=2))
    return reg('JournalPage', J('JournalPage', parts))

# ================= TOOLS (handle along +Z, grip near origin) =================
def lash(z, r=0.03): return torus(r, 0.008, ROPE, 8, 4, loc=(0, 0, z))
def axe():
    parts = [cyl(0.022, 0.026, 0.62, WOOD, 6, loc=(0, 0, -0.12)), blob(0.09, STONE, 1, 0.25, scale=(1.5, 0.35, 1.0), seed=3, loc=(0.07, 0, 0.42)),
             lash(0.38), lash(0.42), lash(0.46)]
    return reg('Axe', J('Axe', parts))
def pickaxe():
    parts = [cyl(0.022, 0.026, 0.66, WOOD, 6, loc=(0, 0, -0.12)), cyl(0.05, 0.005, 0.24, STONED, 5, loc=(0.02, 0, 0.47), rot=(0, pi / 2 + 0.25, 0)),
             cyl(0.05, 0.005, 0.24, STONED, 5, loc=(-0.02, 0, 0.47), rot=(0, -pi / 2 - 0.25, 0)), lash(0.44), lash(0.5)]
    return reg('Pickaxe', J('Pickaxe', parts))
def spear():
    parts = [cyl(0.02, 0.017, 1.7, WOOD, 6, loc=(0, 0, -0.5)), cyl(0.035, 0.002, 0.22, FLINT, 4, loc=(0, 0, 1.18)), lash(1.14, 0.026), lash(1.18, 0.026)]
    return reg('Spear', J('Spear', parts))
def torch():
    parts = [cyl(0.02, 0.024, 0.55, WOOD, 6, loc=(0, 0, -0.15)), cyl(0.045, 0.05, 0.14, CHAR, 7, loc=(0, 0, 0.38)), lash(0.38, 0.048), lash(0.5, 0.05)]
    return reg('Torch', J('Torch', parts))
def rod():
    pts = [(0, 0.0 + 0.25 * (i / 8) ** 2, -0.2 + 2.1 * i / 8) for i in range(9)]
    parts = [tube(pts, [0.02 - 0.014 * i / 8 for i in range(9)], WOODL, 5), lash(0.0, 0.024), lash(0.08, 0.024),
             pole((0, 0.25, 1.9), (0, 0.27, 1.2), 0.003, 0.003, STEM, 3), blob(0.025, BERRY, 1, 0, loc=(0, 0.27, 1.18))]
    return reg('FishingRod', J('FishingRod', parts))

# ================= STRUCTURES =================
def campfire():
    R.seed(2); parts = [cyl(0.5, 0.5, 0.03, ASH, 10)]
    for i in range(9):
        a = i * 2 * pi / 9
        parts.append(blob(0.14, STONE if i % 3 else STONED, 1, 0.3, scale=(1, 1, 0.8), seed=i, loc=(0.58 * math.cos(a), 0.58 * math.sin(a), 0.08)))
    for i in range(5):
        a = i * 2 * pi / 5 + 0.3
        parts.append(pole((0.36 * math.cos(a), 0.36 * math.sin(a), 0.04), (-0.05 * math.cos(a), -0.05 * math.sin(a), 0.5), 0.055, 0.045, CHAR if i % 2 else WOODD, 6))
    return reg('Campfire', J('Campfire', parts))

def shelter():
    R.seed(4); parts = []
    for x in (-1.2, 1.2):
        parts.append(pole((x, -0.9, 0), (x, -0.9, 1.7), 0.06, 0.05, WOOD))
        parts.append(pole((x, -0.9, 1.7), (x, 1.3, 0.05), 0.05, 0.04, WOOD))
    parts.append(pole((-1.35, -0.9, 1.7), (1.35, -0.9, 1.7), 0.055, 0.05, WOOD))
    for i in range(5): parts.append(pole((-1.2 + i * 0.6, -0.9, 1.7), (-1.2 + i * 0.6, 1.3, 0.05), 0.035, 0.03, WOODD))
    for row in range(5):
        for i in range(6):
            t = row / 5; y = -0.95 + t * 2.3; z = 1.78 - t * 1.68
            parts.append(frond(1.0, 0.24, LEAFP if (row + i) % 3 else THATCH, arch=-0.72, droop=0.02, fold=0.12, n=3, loc=(-1.25 + i * 0.5 + rr(-0.05, 0.05), y, z), rot=(0, 0, pi / 2 + rr(-0.12, 0.12))))
    for i in range(5): parts.append(frond(1.7, 0.3, LEAF, arch=0.03, droop=0.0, fold=0.05, n=3, loc=(-0.9 + i * 0.42, 0.7, 0.04), rot=(0, 0, -pi / 2 + rr(-0.1, 0.1))))
    parts.append(pole((-1.2, -0.9, 0.9), (-1.2, 1.0, 0.25), 0.03, 0.03, WOODD)); parts.append(pole((1.2, -0.9, 0.9), (1.2, 1.0, 0.25), 0.03, 0.03, WOODD))
    for x in (-1.2, 1.2): parts.append(torus(0.07, 0.012, ROPE, 8, 4, loc=(x, -0.9, 1.68)))
    return reg('Shelter', J('Shelter', parts))

def workbench():
    parts = []
    for x in (-0.6, 0.6):
        for y in (-0.3, 0.3): parts.append(cyl(0.07, 0.065, 0.8, BARK, 7, loc=(x, y, 0)))
    for i in range(4): parts.append(box(1.5, 0.19, 0.06, WOODL if i % 2 else WOOD, loc=(0, -0.3 + i * 0.2, 0.83)))
    parts.append(box(1.2, 0.05, 0.05, WOODD, loc=(0, -0.3, 0.35))); parts.append(box(1.2, 0.05, 0.05, WOODD, loc=(0, 0.3, 0.35)))
    parts.append(blob(0.09, STONE, 1, 0.3, scale=(1.3, 1, 0.6), loc=(0.4, 0.1, 0.9), seed=5))
    parts.append(pole((-0.5, -0.15, 0.885), (-0.05, 0.05, 0.885), 0.02, 0.02, WOOD)); parts.append(blob(0.06, FLINT, 1, 0.4, scale=(1.4, 0.6, 0.5), loc=(-0.02, 0.06, 0.89), seed=9))
    for x in (-0.6, 0.6): parts.append(torus(0.08, 0.012, ROPE, 8, 4, loc=(x, -0.3, 0.7)))
    return reg('Workbench', J('Workbench', parts))

def water_collector():
    parts = []
    for i in range(3):
        a = i * 2 * pi / 3
        parts.append(pole((0.6 * math.cos(a), 0.6 * math.sin(a), 0), (0.08 * math.cos(a), 0.08 * math.sin(a), 1.5), 0.04, 0.03, WOOD))
    parts.append(torus(0.1, 0.014, ROPE, 8, 4, loc=(0, 0, 1.4)))
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=False, segments=8, radius1=0.04, radius2=0.5, depth=0.45)
    parts.append(mk('p', bm, LEAFP, loc=(0, 0, 1.0), recalc=False))
    parts.append(blob(0.2, COCO, 2, 0.04, scale=(1, 1, 0.8), flat=0.9, loc=(0, 0, 0.16)))
    parts.append(cyl(0.165, 0.165, 0.01, M('WaterBlue', '#4aa3c7', rough=0.1), 10, loc=(0, 0, 0.2)))
    return reg('WaterCollector', J('WaterCollector', parts))

def crate():
    parts = [box(0.8, 0.6, 0.55, WOOD, loc=(0, 0, 0.275))]
    for z in (0.08, 0.47):
        parts.append(box(0.84, 0.64, 0.07, WOODD, loc=(0, 0, z)))
    for x in (-0.38, 0.38):
        for y in (-0.28, 0.28): parts.append(box(0.07, 0.07, 0.57, WOODD, loc=(x, y, 0.285)))
    parts.append(box(0.82, 0.62, 0.04, WOODL, loc=(0, 0, 0.57)))
    return reg('Crate', J('Crate', parts))

def barrel():
    pts = [(0, 0, z) for z in (0, 0.2, 0.45, 0.7, 0.9)]
    parts = [tube(pts, [0.26, 0.31, 0.33, 0.31, 0.26], WOOD, 10)]
    for z in (0.14, 0.76): parts.append(torus(0.305, 0.014, METAL, 12, 4, loc=(0, 0, z)))
    return reg('Barrel', J('Barrel', parts))

def hull_parts(u0, u1, n=10, L=7.0):
    hb = bmesh.new(); db = bmesh.new(); secs = []; dsecs = []
    for i in range(n + 1):
        u = u0 + (u1 - u0) * i / n; uu = max(u, 0.015)
        y = -L / 2 + L * uu
        w = 1.2 * (1 - (1 - uu) ** 2.2) * (1 - 0.18 * uu ** 3)
        d = 0.85 * (1 - (1 - uu) ** 3) * (1 - 0.25 * uu ** 4)
        top = 0.55 + 0.3 * (1 - uu) ** 2
        s = [(-w, y, top), (-w * 0.78, y, -d * 0.5), (0, y, -d), (w * 0.78, y, -d * 0.5), (w, y, top)]
        secs.append([hb.verts.new(p) for p in s])
        dsecs.append((db.verts.new((-w * 0.98, y, top - 0.06)), db.verts.new((w * 0.98, y, top - 0.06))))
    for a, b in zip(secs, secs[1:]):
        for k in range(4): hb.faces.new((a[k], a[k + 1], b[k + 1], b[k]))
    hb.faces.new(secs[-1])
    if u0 > 0.05: hb.faces.new(list(reversed(secs[0])))
    for a, b in zip(dsecs, dsecs[1:]): db.faces.new((a[0], a[1], b[1], b[0]))
    return [mk('p', hb, HULL), mk('p', db, DECK, recalc=False)]

def sailboat():
    parts = hull_parts(0, 1)
    parts.append(box(1.3, 1.9, 0.45, HULL, loc=(0, 0.3, 0.75))); parts.append(box(1.34, 1.94, 0.05, STRIPE, loc=(0, 0.3, 0.99)))
    parts.append(box(0.08, 1.6, 0.9, STRIPE, loc=(0, 0.2, -1.1)))
    parts.append(cyl(0.07, 0.045, 7.5, WOODL, 8, loc=(0, -0.8, 0.6))); parts.append(pole((0, -0.75, 1.5), (0, 2.6, 1.6), 0.045, 0.04, WOODL))
    parts.append(quad([(0, -0.72, 1.6), (0, 2.5, 1.7), (0, -0.72, 7.8), (0, -0.72, 7.8)], CANVAS, 4, 4, 0.35, (1, 0, 0)))
    parts.append(quad([(0, -3.35, 0.95), (0, -0.9, 1.3), (0, -0.85, 6.6), (0, -0.85, 6.6)], CANVAS, 3, 3, 0.25, (1, 0, 0)))
    parts.append(box(1.9, 0.06, 0.06, STRIPE, loc=(0, 3.3, 0.6)))
    return reg('Sailboat', J('Sailboat', parts))

def wreck():
    stern = J('ws', hull_parts(0.42, 1) + [box(1.3, 1.5, 0.45, HULL, loc=(0, 1.1, 0.75)), box(0.08, 1.2, 0.7, STRIPE, loc=(0, 0.9, -1.0))])
    stern.rotation_euler = (0.1, 0.55, 0.2); stern.location = (0, 0.5, 0.75)
    bow = J('wb', hull_parts(0, 0.38, 6))
    bow.rotation_euler = (-0.25, -0.9, 0.7); bow.location = (-3.2, -2.8, 0.35)
    parts = [stern, bow, pole((1.2, -1.5, 0.15), (5.5, -3.2, 0.5), 0.07, 0.05, WOODL, 8), pole((-1.5, 2.5, 0.1), (0.8, 3.6, 0.3), 0.045, 0.04, WOODL),
             quad([(1.6, -1.6, 0.25), (4.6, -2.8, 0.45), (4.0, -4.4, 0.05), (1.9, -3.4, 0.04)], CANVAS, 3, 3, 0.25, (0, 0, 1)),
             box(0.7, 0.2, 0.04, DECK, loc=(-1.2, -0.8, 0.03), rot=(0, 0, 0.6)), box(0.9, 0.18, 0.04, DECK, loc=(2.2, 1.6, 0.03), rot=(0, 0, -0.4)), box(0.5, 0.2, 0.04, HULL, loc=(-2.0, 1.2, 0.03), rot=(0, 0, 1.2))]
    return reg('Wreck', J('Wreck', parts))

def raft_logs():
    parts = []
    for i in range(7):
        x = (i - 3) * 0.36
        parts.append(cyl(0.18, 0.17, 3.6, BARK if i % 2 else WOODD, 8, loc=(x, -1.8, 0.18), rot=(-pi / 2, 0, 0)))
        parts.append(cyl(0.14, 0.14, 3.62, WOODL, 8, loc=(x, -1.81, 0.18), rot=(-pi / 2, 0, 0)))
    for y in (-1.2, 1.2):
        parts.append(cyl(0.09, 0.09, 2.7, WOOD, 7, loc=(-1.35, y, 0.42), rot=(0, pi / 2, 0)))
        for i in range(7): parts.append(torus(0.1, 0.014, ROPE, 8, 4, loc=((i - 3) * 0.36, y, 0.4), rot=(0, pi / 2, 0)))
    return parts
def raft_frame(): return reg('RaftFrame', J('RaftFrame', raft_logs()))
def raft():
    parts = raft_logs()
    parts.append(cyl(0.07, 0.05, 3.0, WOODL, 8, loc=(0, -0.3, 0.35))); parts.append(pole((-1.1, -0.3, 2.9), (1.1, -0.3, 2.9), 0.04, 0.04, WOODL))
    parts.append(pole((-1.0, -0.3, 0.9), (1.0, -0.3, 0.9), 0.035, 0.035, WOODL))
    parts.append(quad([(-1.0, -0.3, 0.92), (1.0, -0.3, 0.92), (1.05, -0.3, 2.88), (-1.05, -0.3, 2.88)], CANVAS, 4, 4, 0.35, (0, -1, 0)))
    parts.append(pole((0, -0.3, 3.3), (0, -1.75, 0.4), 0.012, 0.012, ROPE, 4)); parts.append(pole((0, -0.3, 3.3), (0, 1.7, 0.4), 0.012, 0.012, ROPE, 4))
    parts.append(pole((0.3, 1.0, 0.9), (0.5, 2.6, 0.0), 0.035, 0.03, WOOD)); parts.append(box(0.06, 0.6, 0.3, WOODD, loc=(0.52, 2.75, -0.1), rot=(0.5, 0, -0.12)))
    parts.append(box(0.6, 0.45, 0.4, WOOD, loc=(-0.6, 0.6, 0.62))); parts.append(box(0.64, 0.49, 0.05, WOODD, loc=(-0.6, 0.6, 0.83)))
    return reg('Raft', J('Raft', parts))

# ================= ANIMALS (face -Y; no facial features) =================
def boar():
    body = J('Boar', [blob(0.42, BOAR, 2, 0.12, scale=(0.85, 1.5, 0.95), loc=(0, 0, 0.62), seed=2), blob(0.27, BOAR, 2, 0.1, scale=(0.9, 1.15, 0.95), loc=(0, -0.72, 0.6), seed=5),
                      cyl(0.14, 0.09, 0.26, BOARD, 7, loc=(0, -0.92, 0.55), rot=(pi / 2, 0, 0)),
                      cyl(0.022, 0.003, 0.14, TUSK, 5, loc=(0.085, -1.02, 0.5), rot=(0.5, 0.3, 0)), cyl(0.022, 0.003, 0.14, TUSK, 5, loc=(-0.085, -1.02, 0.5), rot=(0.5, -0.3, 0)),
                      cyl(0.07, 0.003, 0.14, BOARD, 4, loc=(0.15, -0.6, 0.8), rot=(0, 0.5, 0)), cyl(0.07, 0.003, 0.14, BOARD, 4, loc=(-0.15, -0.6, 0.8), rot=(0, -0.5, 0))] +
                     [box(0.05, 0.14, 0.1, BOARD, loc=(0, -0.55 + i * 0.2, 1.03 - abs(i - 2.5) * 0.02), rot=(0.3, 0, 0)) for i in range(6)] +
                     [cyl(0.02, 0.005, 0.2, BOARD, 4, loc=(0, 0.62, 0.72), rot=(-2.2, 0, 0))])
    for nm, x, y in (('LegFL', 0.2, -0.42), ('LegFR', -0.2, -0.42), ('LegBL', 0.2, 0.42), ('LegBR', -0.2, 0.42)):
        child(nm, [cyl(0.075, 0.05, 0.42, BOARD, 6, loc=(x, y, 0.42), down=True), cyl(0.055, 0.06, 0.06, CHAR, 6, loc=(x, y, 0.06), down=True)], body, (x, y, 0.42))
    return reg('Boar', body)

def crab():
    parts = [blob(0.16, CRAB, 2, 0.12, scale=(1.35, 1, 0.5), loc=(0, 0, 0.12), seed=3)]
    for s in (1, -1):
        parts.append(pole((s * 0.16, -0.08, 0.12), (s * 0.27, -0.2, 0.14), 0.03, 0.025, CRABD, 5))
        parts.append(blob(0.065, CRAB, 1, 0.15, scale=(0.8, 1.3, 0.7), loc=(s * 0.28, -0.25, 0.15), seed=7))
        for i in range(3):
            y = 0.0 + i * 0.07
            parts.append(pole((s * 0.17, y, 0.11), (s * 0.3, y + 0.03, 0.16), 0.018, 0.014, CRABD, 4)); parts.append(pole((s * 0.3, y + 0.03, 0.16), (s * 0.36, y + 0.05, 0.0), 0.014, 0.006, CRABD, 4))
    return reg('Crab', J('Crab', parts))

def gull():
    body = J('Gull', [blob(0.11, GULL, 2, 0.05, scale=(0.8, 2.1, 0.8), loc=(0, 0, 0)), blob(0.06, GULL, 1, 0.02, loc=(0, -0.24, 0.05)), cyl(0.018, 0.002, 0.08, BEAK, 5, loc=(0, -0.29, 0.045), rot=(pi / 2, 0, 0)),
                      quad([(-0.06, 0.2, 0), (0.06, 0.2, 0), (0.09, 0.36, 0), (-0.09, 0.36, 0)], GULLG)])
    for nm, s in (('WingL', 1), ('WingR', -1)):
        w = quad([(s * 0.05, -0.09, 0.02), (s * 0.05, 0.12, 0.02), (s * 0.62, 0.1, 0.02), (s * 0.5, -0.05, 0.02)], GULLG, 2, 1)
        child(nm, [w], body, (s * 0.05, 0, 0.02))
    return reg('Gull', body)

# ---------- build everything ----------
for i in range(3): palm('Palm%d' % (i + 1), 11 + i * 7)
for i in range(3): broad('Tree%d' % (i + 1), 3 + i * 5)
for i in range(3): broad('Tree%d_L' % (i + 1), 3 + i * 5, 1)
stump(); bush(); fiber_plant(); fern(); grass_tuft()
rock('Rock1', 1, (1.0, 0.85, 0.75)); rock('Rock2', 5, (1.3, 0.9, 0.6), STONED); rock('Rock3', 9, (0.8, 1.1, 1.0), STONEL)
rock('Boulder', 14, (2.6, 2.2, 2.3), STONE, 3); rock('Cliff', 23, (3.5, 2.4, 4.2), STONED, 3)
flint_rock()
stick(); stone(); flint(); log(); coconut(); mushroom(); berries_item(); fiber_item(); leaf_item(); rope_item(); canvas_item(); hide_item()
meat('MeatRaw', MEAT); meat('MeatCooked', COOKED); fish('FishRaw', FISH, FISHB); fish('FishCooked', FISHC, COOKED); bandage(); flask(); page()
axe(); pickaxe(); spear(); torch(); rod()
campfire(); shelter(); workbench(); water_collector(); crate(); barrel(); sailboat(); wreck(); raft_frame(); raft()
boar(); crab(); gull()
reg('Driftwood', J('Driftwood', [pole((-0.9, 0, 0.1), (0.9, 0.2, 0.16), 0.11, 0.07, STEM, 6), pole((0.2, 0.05, 0.13), (0.6, -0.4, 0.3), 0.05, 0.02, STEM, 5)]))
reg('SeaShell', J('SeaShell', [blob(0.08, SHELL, 1, 0.15, scale=(1.2, 1, 0.45), flat=0.3, loc=(0, 0, 0.02))]))

# ===================== V2 CONTENT =====================
DEER = M('Deer', '#a8744b'); DEERL = M('DeerLight', '#dcbd94'); DEERD = M('DeerDark', '#553a27')
MONK = M('Monkey', '#6d4c35'); MONKL = M('MonkeyLight', '#c9a37c')
SHARK = M('Shark', '#5d7280', rough=0.4); SHARKB = M('SharkBelly', '#dfe6ea', rough=0.4); SHARKF = M('SharkFin', '#566a77', rough=0.4, double=True)
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
             tri([(0, -0.25, 0.28), (0, 0.45, 0.28), (0, 0.4, 0.95)], SHARKF), tri([(0.2, -0.5, -0.1), (0.25, 0.0, -0.12), (0.95, 0.05, -0.3)], SHARKF), tri([(-0.2, -0.5, -0.1), (-0.25, 0.0, -0.12), (-0.95, 0.05, -0.3)], SHARKF)]
    body = J('Shark', parts)
    t1 = tri([(0, 1.25, 0.0), (0, 2.1, 0.85), (0, 1.7, 0.0)], SHARKF); t2 = tri([(0, 1.25, 0.0), (0, 1.9, -0.5), (0, 1.7, 0.0)], SHARKF)
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
    fus = tube([(0, 0, z) for z, r in prof], [r for z, r in prof], PLANE, 10); fus.rotation_euler = (-pi / 2, 0, 0); fus.location = (0, 0, 0.75)
    tail = tube([(0, 0, 0), (0, 0, 1.4), (0, 0, 2.6)], [0.6, 0.38, 0.14], PLANE, 8); tail.rotation_euler = (pi / 2 - 0.25, 0, 0.5); tail.location = (2.4, 6.6, 0.5)
    parts = [fus, tail, cyl(0.42, 0.42, 0.5, STONED, 10, loc=(0, 0.1, 0.75), rot=(pi / 2, 0, 0)),
             box(0.14, 0.05, 1.5, WOODD, loc=(0, -0.45, 0.9), rot=(0, 0.5, 0)), box(0.14, 0.05, 1.1, WOODD, loc=(0.3, -0.5, 0.35), rot=(0.4, 1.5, 0)),
             box(5.2, 1.5, 0.14, PLANE, loc=(2.9, 2.0, 0.95), rot=(0, 0.22, 0.12)), box(0.9, 1.52, 0.15, PLANES, loc=(5.0, 2.3, 1.47), rot=(0, 0.22, 0.12)),
             box(4.2, 1.4, 0.14, PLANE, loc=(-3.6, 3.0, 0.12), rot=(0.05, -0.04, -0.6)), box(0.9, 0.08, 0.45, GLASS, loc=(0, 1.25, 1.5), rot=(0.6, 0, 0)),
             box(0.1, 1.3, 1.5, PLANES, loc=(2.75, 7.5, 1.25), rot=(-0.25, 0, 0.5)), box(2.4, 0.9, 0.1, PLANE, loc=(2.9, 7.6, 0.55), rot=(-0.2, 0.1, 0.5))]
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
    parts.append(mk('p', bm, PLASTR, loc=(0, 0, 15.5), recalc=False))
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

PLANTS = ("Palm", "Tree", "BigTree", "DeadTree", "BerryBush", "FiberPlant", "Fern", "GrassTuft", "BananaPlant", "BambooGrove", "Flower", "HerbPlant", "Reed")

SHARED = bpy.data.materials.new("Shared")

def bake_ao(obj, wind=0.0):
    me = obj.data
    if not me.polygons: return
    bm = bmesh.new(); bm.from_mesh(me); bvh = BVHTree.FromBMesh(bm)
    col = me.color_attributes.get('Col') or me.color_attributes.new('Col', 'BYTE_COLOR', 'CORNER')
    reach = max(0.25, max(obj.dimensions) * 0.3)
    for poly in me.polygons:
        mname = me.materials[poly.material_index].name if me.materials else ""
        base = hex2lin(PALETTE[mname]["hex"]) if mname in PALETTE else [0.8, 0.8, 0.8]
        n = poly.normal; c = poly.center
        t = n.orthogonal().normalized(); b = n.cross(t)
        for li in poly.loop_indices:
            p = me.vertices[me.loops[li].vertex_index].co.lerp(c, 0.3) + n * (reach * 0.02 + 0.004)
            hit = 0
            for d in _DIRS:
                if bvh.ray_cast(p, t * d.x + b * d.y + n * d.z, reach)[0] is not None: hit += 1
            ao = 0.32 + 0.68 * (1 - hit / len(_DIRS)) ** 1.3
            k = 0.5 + 0.5 * ao
            col.data[li].color = (base[0] * k, base[1] * k, base[2] * k, wind)
    bm.free()
    me.materials.clear(); me.materials.append(SHARED)
    for poly in me.polygons: poly.material_index = 0

def export(name, root):
    for o in [root] + list(root.children_recursive): bake_ao(o, 1.0 if name.startswith(PLANTS) else 0.0)
    bpy.ops.object.select_all(action='DESELECT')
    root.select_set(True)
    for c in root.children_recursive: c.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + '.fbx'), use_selection=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=True, object_types={'MESH', 'EMPTY'}, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False, colors_type='SRGB')

bpy.context.view_layer.update()
tris = {}
for name, root in MODELS:
    export(name, root)
    tris[name] = sum(len(p.vertices) - 2 for o in [root] + list(root.children_recursive) for p in o.data.polygons)
x = 0; y = 0
for i, (name, root) in enumerate(MODELS):
    d = max(root.dimensions.x, root.dimensions.y, 1.0)
    root.location = (x + d / 2, y, 0); x += d + 0.8
    if x > 45: x = 0; y += 9
with open(os.path.join(OUT, 'palette.json'), 'w') as f: json.dump(PALETTE, f, indent=1)
print('EXPORTED', len(MODELS), tris)

bpy.ops.wm.save_as_mainfile(filepath=r'C:Survival Fable 62SourceBlenderLastShore_Models.blend')
