"""Renders the app icon from the game's own Blender models. Run: blender -b LastShore_Models.blend -P icon.py"""
import bpy, math
from mathutils import Vector

OUT = r"C:\Survival Fable 62\Assets\Icon\AppIcon.png"
sc = bpy.context.scene
keep = {}
for name in ("Palm1", "Palm2", "Rock1", "Rock2", "Campfire", "Fern"):
    keep[name] = bpy.data.objects[name]
for o in list(sc.collection.all_objects):
    o.hide_render = True

def mat(name, color, emit=0.0, rough=0.6):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    b.inputs[0].default_value = (*color, 1)
    for i in b.inputs:
        if i.identifier == 'Roughness': i.default_value = rough
        if i.identifier == 'Emission Color': i.default_value = (*color, 1)
        if i.identifier == 'Emission Strength': i.default_value = emit
    return m

def dup(name, loc, rot_z=0.0, scale=1.0):
    o = keep[name].copy(); sc.collection.objects.link(o)
    o.hide_render = False; o.location = loc; o.rotation_euler = (0, 0, rot_z); o.scale = (scale,) * 3
    return o

# island mound
bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, radius=1, location=(0, 0, -1.05))
isl = bpy.context.object; isl.scale = (4.6, 4.6, 2.0); isl.data.materials.append(mat("IconSand", (0.95, 0.72, 0.38), 0.35, 0.9))
bpy.ops.object.shade_smooth()
# sea
bpy.ops.mesh.primitive_plane_add(size=400, location=(0, 0, 0))
sea = bpy.context.object; sea.data.materials.append(mat("IconSea", (0.03, 0.36, 0.46), 0.55, 0.25))
# sun disc and sky backdrop (emissive, far behind)
bpy.ops.mesh.primitive_circle_add(vertices=96, radius=7.5, fill_type='NGON', location=(1.5, 60, 6.5), rotation=(math.pi / 2, 0, 0))
sun = bpy.context.object; sun.data.materials.append(mat("IconSun", (1.0, 0.80, 0.42), 1.0))
bpy.ops.mesh.primitive_plane_add(size=400, location=(0, 70, 30), rotation=(math.pi / 2, 0, 0))
sky = bpy.context.object
sm = bpy.data.materials.new("IconSky"); sm.use_nodes = True
nt = sm.node_tree; nt.nodes.clear()
out = nt.nodes.new('ShaderNodeOutputMaterial'); em = nt.nodes.new('ShaderNodeEmission'); ramp = nt.nodes.new('ShaderNodeValToRGB')
tc = nt.nodes.new('ShaderNodeTexCoord'); sep = nt.nodes.new('ShaderNodeSeparateXYZ'); mp = nt.nodes.new('ShaderNodeMapRange')
mp.inputs[1].default_value = -30; mp.inputs[2].default_value = -15
e = ramp.color_ramp.elements
e[0].position = 0.0; e[0].color = (1.0, 0.42, 0.12, 1)
e[1].position = 1.0; e[1].color = (0.03, 0.16, 0.32, 1)
mid = ramp.color_ramp.elements.new(0.42); mid.color = (0.95, 0.30, 0.22, 1)
mid2 = ramp.color_ramp.elements.new(0.7); mid2.color = (0.28, 0.20, 0.42, 1)
nt.links.new(tc.outputs['Object'], sep.inputs[0]); nt.links.new(sep.outputs['Y'], mp.inputs[0]); nt.links.new(mp.outputs[0], ramp.inputs[0])
nt.links.new(ramp.outputs[0], em.inputs[0]); em.inputs[1].default_value = 1.0; nt.links.new(em.outputs[0], out.inputs[0])
sky.data.materials.append(sm)

dup("Palm1", (-0.6, 0.2, 0.35), 2.2, 0.62)
dup("Palm2", (1.5, 0.9, 0.2), 0.4, 0.5)
dup("Rock1", (2.6, -0.8, 0.15), 0.5, 0.75)
dup("Rock2", (-2.9, -0.2, 0.1), 2.0, 0.6)
dup("Campfire", (0.9, -1.6, 0.52), 0.3, 0.85)
dup("Fern", (-1.9, -1.3, 0.45), 1.0, 1.0)
# fire glow
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.2, location=(0.9, -1.6, 0.9))
fl = bpy.context.object; fl.scale = (0.8, 0.8, 1.5); fl.data.materials.append(mat("IconFlame", (1.0, 0.42, 0.08), 1.6))
pl = bpy.data.lights.new("FireL", 'POINT'); pl.energy = 220; pl.color = (1.0, 0.55, 0.2); pl.shadow_soft_size = 0.4
plo = bpy.data.objects.new("FireL", pl); sc.collection.objects.link(plo); plo.location = (0.9, -1.6, 1.3)

key = bpy.data.lights.new("Key", 'SUN'); key.energy = 3.2; key.color = (1.0, 0.72, 0.5)
ko = bpy.data.objects.new("Key", key); sc.collection.objects.link(ko); ko.rotation_euler = (math.radians(62), 0, math.radians(200))
fill = bpy.data.lights.new("Fill", 'SUN'); fill.energy = 1.1; fill.color = (0.55, 0.7, 1.0)
fo = bpy.data.objects.new("Fill", fill); sc.collection.objects.link(fo); fo.rotation_euler = (math.radians(50), 0, math.radians(-35))

cam = bpy.data.cameras.new("IconCam"); cam.lens = 88
co = bpy.data.objects.new("IconCam", cam); sc.collection.objects.link(co)
co.location = (0.4, -21, 3.2)
d = Vector((0.3, 0, 2.5)) - co.location
co.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
sc.camera = co

w = bpy.data.worlds.new("IconWorld"); w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.25, 0.22, 0.3, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.6
sc.world = w
try:
    sc.render.engine = 'BLENDER_EEVEE_NEXT'
except TypeError:
    sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x = 1024; sc.render.resolution_y = 1024; sc.render.resolution_percentage = 100
sc.render.film_transparent = False
sc.view_settings.view_transform = 'Standard'
sc.render.image_settings.file_format = 'PNG'
sc.render.filepath = OUT
bpy.ops.render.render(write_still=True)
print("ICON DONE")
