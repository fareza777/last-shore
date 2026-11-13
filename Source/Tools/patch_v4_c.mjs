// v4 (part c): fish traps really go in the shallows.
import fs from 'node:fs';
const f = 'C:/Survival Fable 62/Assets/Scripts/Game/BuildSystem.cs';
let s = fs.readFileSync(f, 'utf8').replace(/\r\n/g, '\n');
const edits = [
  ['            _valid = World.IsLand(p, 0.35f) && World.Slope(p) < 24f && (p - gm.Player.transform.position).magnitude < 8f;',
   `            bool trap = def.PlacePrefab == "FishTrap";      // the one structure that belongs in the water
            float ground = World.Height(p);
            bool spot = trap ? ground < World.SeaLevel - 0.1f && ground > World.SeaLevel - 1.1f : World.IsLand(p, 0.35f);
            _valid = spot && World.Slope(p) < 24f && (p - gm.Player.transform.position).magnitude < 8f;
            _needsWater = trap && !spot;`],
  ['        GameObject _ghost; string _ghostId; float _rot; bool _valid; Vector3 _pos;', '        GameObject _ghost; string _ghostId; float _rot; bool _valid, _needsWater; Vector3 _pos;'],
  ['            if (!_valid) { gm.Hud.Hint("You can\'t build here."); return; }', '            if (!_valid) { gm.Hud.Hint(_needsWater ? "A fish trap has to stand in shallow water." : "You can\'t build here."); return; }'],
];
for (const [a, b] of edits) { console.log((s.includes(a) ? 'ok   ' : 'MISS ') + a.slice(0, 60)); s = s.replace(a, b); }
fs.writeFileSync(f, s);
