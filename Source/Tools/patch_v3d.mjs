// v3d: item icons fill their frame (auto-fit to the rendered silhouette) and get a dark outline; hotbar hides behind the backpack.
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
patch('Editor/ProjectSetup.cs', [
  [`                    cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    tex.ReadPixels(new Rect(0, 0, size, size), 0, 0); tex.Apply();
                    RenderTexture.active = null;`,
   `                    var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    // render, measure the silhouette, then re-frame so the item fills the icon
                    for (int pass = 0; pass < 2; pass++)
                    {
                        cam.Render();
                        RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, size, size), 0, 0); tex.Apply(); RenderTexture.active = null;
                        if (pass == 1) break;
                        var px = tex.GetPixels32(); int x0 = size, x1 = -1, y0 = size, y1 = -1;
                        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) if (px[y * size + x].a > 8) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
                        if (x1 < 0) break;
                        float unit = cam.orthographicSize * 2f / size;
                        cam.transform.position += cam.transform.right * (((x0 + x1 + 1) * 0.5f - size * 0.5f) * unit) + cam.transform.up * (((y0 + y1 + 1) * 0.5f - size * 0.5f) * unit);
                        cam.orthographicSize *= Mathf.Max(x1 - x0 + 1, y1 - y0 + 1) / (size * 0.86f);
                    }
                    Outline(tex, size);`],
  ['        public static void RenderIcons()', `        /// <summary>Dark soft outline so icons stay readable on any slot colour.</summary>
        static void Outline(Texture2D tex, int size)
        {
            var src = tex.GetPixels32(); var dst = new Color32[src.Length]; const int R = 5;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var c = src[y * size + x]; float best = 0;
                    if (c.a < 255)
                        for (int dy = -R; dy <= R; dy++)
                            for (int dx = -R; dx <= R; dx++)
                            {
                                int xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= size || yy >= size) continue;
                                float a = src[yy * size + xx].a / 255f; if (a <= 0) continue;
                                float f = a * Mathf.Clamp01(1.25f - Mathf.Sqrt(dx * dx + dy * dy) / R); if (f > best) best = f;
                            }
                    float ca = c.a / 255f, oa = best * 0.85f, outA = ca + oa * (1 - ca);
                    if (outA <= 0) { dst[y * size + x] = new Color32(0, 0, 0, 0); continue; }
                    float k = ca / outA; var dark = new Color(0.05f, 0.07f, 0.10f);
                    dst[y * size + x] = new Color(Mathf.Lerp(dark.r, c.r / 255f, k), Mathf.Lerp(dark.g, c.g / 255f, k), Mathf.Lerp(dark.b, c.b / 255f, k), outA);
                }
            tex.SetPixels32(dst); tex.Apply();
        }

        public static void RenderIcons()`],
]);
console.log(results.join('\n'));
