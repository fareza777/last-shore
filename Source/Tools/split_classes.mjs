// Moves MonoBehaviour classes into files named after the class (Unity needs this to serialize them in prefabs/scenes).
import fs from 'node:fs';
import path from 'node:path';
const root = 'C:/Survival Fable 62/Assets/Scripts/';
function split(file, names) {
  file = root + file;
  let s = fs.readFileSync(file, 'utf8').replace(/\r\n/g, '\n');
  const usings = s.slice(0, s.indexOf('namespace LastShore'));
  for (const n of names) {
    const re = new RegExp('\\n((?:    ///[^\\n]*\\n)*)    public (?:abstract )?class ' + n + '\\b');
    const m = re.exec(s);
    if (!m) { console.log('NOT FOUND', n); continue; }
    const start = m.index + 1;
    const end = s.indexOf('\n    }\n', start) + 7;
    const block = s.slice(start, end);
    s = s.slice(0, start) + s.slice(end).replace(/^\n/, '');
    fs.writeFileSync(path.join(path.dirname(file), n + '.cs'), usings + 'namespace LastShore\n{\n' + block + '}\n');
    console.log('split', n, block.split('\n').length);
  }
  fs.writeFileSync(file, s);
}
split('Game/Interactables.cs', ['ResourceNode', 'Pickup', 'PlacedStructure', 'Campfire', 'Workbench', 'Shelter', 'WaterCollector', 'NotePickup', 'LootCrate', 'RaftSite', 'ZoneTrigger']);
split('Game/Animals.cs', ['Boar', 'Crab']);
split('Core/SceneLook.cs', ['DecorFire']);
split('UI/MainMenu.cs', ['SplashScreen', 'CameraDrift']);
split('UI/Cinematics.cs', ['IntroCinematic']);
