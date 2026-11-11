// v3c: richer sea (less sky wash, vivid shallows, wide rolling shore foam), Low-tier basemap distance.
import fs from 'node:fs';
const root = 'C:/Survival Fable 62/Assets/';
const results = [];
function patch(file, edits) {
  let s = fs.readFileSync(root + file, 'utf8').replace(/\r\n/g, '\n');
  for (const [from, to] of edits) {
    const ok = s.includes(from);
    results.push((ok ? 'ok   ' : 'MISS ') + file + ': ' + from.slice(0, 56).replace(/\n/g, ' '));
    if (ok) s = s.replace(from, to);
  }
  fs.writeFileSync(root + file, s);
}
patch('Shaders/Water.shader', [
  ['                float depth = _LS_ShoreParams.w > 0.5 ? max(0, i.wpos.y - ground) : 30.0;',
   '                // the waterline breathes in and out so the surf washes up the sand\n                float depth = _LS_ShoreParams.w > 0.5 ? max(0, i.wpos.y - ground + sin(_Time.y * 0.9 + b.b * 4.0) * 0.07) : 30.0;'],
  ['                float3 col = lerp(_Shallow.rgb, _Deep.rgb, pow(d, 0.65));',
   '                float3 mid = lerp(_Shallow.rgb, _Deep.rgb, 0.45) * float3(0.7, 1.15, 1.2);\n                float3 col = d < 0.5 ? lerp(_Shallow.rgb, mid, d * 2.0) : lerp(mid, _Deep.rgb, d * 2.0 - 1.0);'],
  ['                float3 sky = lerp(_LS_Horizon.rgb, zenith, saturate(r.y * 1.7));\n                float fres = 0.025 + 0.975 * pow(1.0 - saturate(dot(n, v)), 5.0);',
   '                float3 sky = lerp(lerp(_LS_Horizon.rgb, zenith, 0.45), zenith, saturate(r.y * 1.7));\n                float fres = min(0.62, 0.025 + 0.975 * pow(1.0 - saturate(dot(n, v)), 5.0));'],
  ['                float edge = 1.0 - saturate(depth / _FoamRange);\n                float foam = smoothstep(0.42, 0.72, edge + (b.b - 0.5) * 0.7);',
   '                float edge = 1.0 - saturate(depth / (_FoamRange * 1.5));\n                float foam = smoothstep(0.34, 0.62, edge + (b.b - 0.5) * 0.6);'],
  ['                foam = saturate(foam + smoothstep(0.86, 0.98, band) * saturate(1.0 - depth / (_FoamRange * 3.5)) * 0.7);',
   '                foam = saturate(foam + smoothstep(0.78, 0.95, band) * saturate(1.0 - depth / (_FoamRange * 4.5)) * 0.85);'],
  ['                alpha = max(alpha, foam * 0.9) * saturate(depth * 5.0);', '                alpha = max(alpha, foam * 0.95) * saturate(depth * 14.0);'],
]);
patch('Editor/ProjectSetup.cs', [
  ['m.SetColor("_Shallow", new Color(0.20f, 0.82f, 0.78f, 0.45f)); m.SetColor("_Deep", new Color(0.02f, 0.19f, 0.38f, 0.96f));', 'm.SetColor("_Shallow", new Color(0.16f, 0.86f, 0.80f, 0.5f)); m.SetColor("_Deep", new Color(0.01f, 0.15f, 0.42f, 0.97f));'],
]);
patch('Scripts/Game/GameManager.cs', [
  ['t.basemapDistance = q == 0 ? 30 :', 't.basemapDistance = q == 0 ? 60 :'],
]);
console.log(results.join('\n'));
