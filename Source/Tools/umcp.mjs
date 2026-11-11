// Usage: node umcp.mjs <tool> [jsonArgs | @file.json]   |   node umcp.mjs --schema <tool>
import { readFileSync } from 'node:fs';
const URL_ = 'http://127.0.0.1:8080/';
const H = { 'Content-Type': 'application/json', Accept: 'application/json, text/event-stream' };
async function rpc(body, sid) {
  const res = await fetch(URL_, { method: 'POST', headers: sid ? { ...H, 'Mcp-Session-Id': sid } : H, body: JSON.stringify(body) });
  const text = await res.text();
  const line = text.split('\n').filter((l) => l.startsWith('data:')).pop();
  return { sid: res.headers.get('mcp-session-id'), json: line ? JSON.parse(line.slice(5)) : (text.trim() ? JSON.parse(text) : null) };
}
const init = await rpc({ jsonrpc: '2.0', id: 1, method: 'initialize', params: { protocolVersion: '2025-03-26', capabilities: {}, clientInfo: { name: 'cli', version: '1' } } });
const sid = init.sid;
await rpc({ jsonrpc: '2.0', method: 'notifications/initialized' }, sid);
let [a, b] = process.argv.slice(2);
if (a === '--exec') { // --exec <file.cs | inline body>  (method body mode)
  let code = b; try { code = readFileSync(b, 'utf8'); } catch {}
  a = 'script-execute'; b = JSON.stringify({ csharpCode: code, isMethodBody: true, className: 'X' + Date.now(), methodName: 'Run' });
}
if (a === '--schema') {
  const r = await rpc({ jsonrpc: '2.0', id: 2, method: 'tools/list' }, sid);
  const t = r.json.result.tools.find((x) => x.name === b);
  console.log(JSON.stringify(t, null, 1));
} else {
  let args = {};
  if (b) args = JSON.parse(b.startsWith('@') ? readFileSync(b.slice(1), 'utf8') : b);
  const r = await rpc({ jsonrpc: '2.0', id: 2, method: 'tools/call', params: { name: a, arguments: args } }, sid);
  const res = r.json?.result ?? r.json;
  for (const c of res?.content ?? []) {
    if (c.type === 'text') console.log(c.text);
    else if (c.type === 'image') { const p = process.env.UMCP_IMG || 'shot.png'; (await import('node:fs')).writeFileSync(p, Buffer.from(c.data, 'base64')); console.log('[image saved ' + p + ']'); }
  }
  if (!res?.content) console.log(JSON.stringify(r.json));
  if (res?.isError) process.exitCode = 1;
}
