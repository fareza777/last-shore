import { readFileSync } from 'node:fs';
const txt = readFileSync('E:/Game Dev Tools.txt', 'utf8');
export function key(label) {
  const line = txt.split(/\r?\n/).find((l) => l.toLowerCase().startsWith(label.toLowerCase()));
  if (!line) throw new Error('no key: ' + label);
  return line.slice(line.indexOf(':') + 1).trim();
}
