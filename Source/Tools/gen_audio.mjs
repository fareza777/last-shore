// Generates VO (Deepgram Aura) and ambience/SFX (ElevenLabs sound generation). Keys are read from a local file, never stored in the project.
import { key } from './keys.mjs';
import { writeFileSync, existsSync, mkdirSync, readFileSync } from 'node:fs';
const OUT = 'C:/Survival Fable 62/Assets/Resources/Audio/';
mkdirSync(OUT + 'VO', { recursive: true }); mkdirSync(OUT + 'SFX', { recursive: true }); mkdirSync(OUT + 'Amb', { recursive: true });
const story = JSON.parse(readFileSync(new URL('../Story/story.json', import.meta.url), 'utf8'));

function wav(pcm, rate) {
  const h = Buffer.alloc(44); h.write('RIFF', 0); h.writeUInt32LE(36 + pcm.length, 4); h.write('WAVEfmt ', 8); h.writeUInt32LE(16, 16); h.writeUInt16LE(1, 20); h.writeUInt16LE(1, 22);
  h.writeUInt32LE(rate, 24); h.writeUInt32LE(rate * 2, 28); h.writeUInt16LE(2, 32); h.writeUInt16LE(16, 34); h.write('data', 36); h.writeUInt32LE(pcm.length, 40);
  return Buffer.concat([h, pcm]);
}
async function vo(name, text) {
  const f = OUT + 'VO/' + name + '.wav'; if (existsSync(f)) return;
  const r = await fetch('https://api.deepgram.com/v1/speak?model=aura-2-orion-en&encoding=linear16&container=wav&sample_rate=44100', {
    method: 'POST', headers: { Authorization: 'Token ' + key('Deepgram'), 'Content-Type': 'application/json' }, body: JSON.stringify({ text }) });
  if (r.status !== 200) { console.log('VO FAIL', name, r.status, (await r.text()).slice(0, 200)); return; }
  writeFileSync(f, Buffer.from(await r.arrayBuffer())); console.log('vo', name);
}
async function sfx(dir, name, text, dur, loop = false) {
  if (existsSync(OUT + dir + '/' + name + '.wav') || existsSync(OUT + dir + '/' + name + '.mp3')) return;
  for (const fmt of ['pcm_44100', 'mp3_44100_128']) {
    const r = await fetch('https://api.elevenlabs.io/v1/sound-generation?output_format=' + fmt, {
      method: 'POST', headers: { 'xi-api-key': key('Eleven Labs'), 'Content-Type': 'application/json' },
      body: JSON.stringify({ text, duration_seconds: dur, prompt_influence: 0.45, ...(loop ? { loop: true, model_id: 'eleven_text_to_sound_v2' } : {}) }) });
    const b = Buffer.from(await r.arrayBuffer());
    if (r.status !== 200) { console.log('SFX FAIL', name, fmt, r.status, b.toString().slice(0, 200)); continue; }
    if (fmt.startsWith('pcm')) writeFileSync(OUT + dir + '/' + name + '.wav', wav(b, 44100)); else writeFileSync(OUT + dir + '/' + name + '.mp3', b);
    console.log('sfx', name, fmt); return;
  }
}
const N = ' No music, no instruments, no voices.';
const jobs = [];
story.intro.forEach((t, i) => jobs.push(() => vo('intro_' + (i + 1), t)));
story.ending.forEach((t, i) => jobs.push(() => vo('ending_' + (i + 1), t)));
for (const [k, t] of Object.entries(story.lines)) jobs.push(() => vo(k, t));
const A = (n, t, d = 22) => jobs.push(() => sfx('Amb', n, t + N, d, true));
const S = (n, t, d) => jobs.push(() => sfx('SFX', n, t + N, d));
A('ocean', 'Calm ocean waves gently washing onto a sandy tropical beach, steady soft surf.');
A('wind', 'Soft steady wind blowing through palm leaves, gentle rustling foliage.');
A('birds', 'Tropical forest daytime ambience, distant birds chirping and calling, light insects.');
A('night', 'Tropical night ambience, crickets and cicadas, distant frogs, calm.');
A('rain', 'Steady heavy rain falling on leaves and sand, no thunder.');
A('storm', 'Violent storm at sea, howling wind, heavy rain, large waves crashing, creaking wooden boat.');
A('fire', 'Campfire crackling close up, wood popping, warm steady fire.', 12);
A('heartbeat', 'Slow heavy human heartbeat, low thumps.', 6);
S('chop', 'Single axe chop into a tree trunk, dry wood impact, close.', 0.8);
S('mine', 'Single stone pickaxe strike on rock, sharp crack with small debris.', 0.8);
S('tree_fall', 'Tree trunk cracking and falling to the ground with rustling leaves.', 3);
S('rock_break', 'Rock crumbling and breaking apart into stones.', 1.5);
S('pickup', 'Quick soft grab of a small object from sand, light rustle.', 0.5);
S('eat', 'Person biting and chewing crunchy food, two bites.', 1.4);
S('drink', 'Person drinking water, three gulps.', 1.5);
S('craft', 'Hands working with wood and rope, tying a tight knot, wooden clacks.', 1.6);
S('place', 'Heavy wooden object set down on the ground, solid thud.', 1);
S('swing', 'Fast whoosh of a stick swung through the air.', 0.5);
S('hit', 'Blunt thud impact on a body, dull hit.', 0.5);
S('hurt', 'Short pained grunt of a man being hit.', 0.7);
S('boar_grunt', 'Wild boar grunting and snorting aggressively.', 1.6);
S('boar_squeal', 'Wild boar squeal of pain, short.', 1.2);
S('thunder', 'Loud thunder crack followed by a long rumble.', 5);
S('splash', 'Body splashing into water, wading.', 1.4);
S('step_sand', 'Single footstep on soft dry sand.', 0.5);
S('step_grass', 'Single footstep on grass and dry leaves.', 0.5);
S('ui_click', 'Soft short wooden tick, single clean tap.', 0.5);
S('ui_open', 'Leather bag being opened, soft cloth rustle.', 0.6);
S('page', 'Old paper page being picked up and unfolded.', 1);
S('cast', 'Fishing line cast whoosh, then a small plop into water.', 1.6);
S('fish_bite', 'Fish tugging and splashing at the water surface.', 1.2);
S('ignite', 'Fire igniting with a whoosh, flames catching dry wood.', 2);
S('sail', 'Canvas sail flapping then catching wind, rope tightening.', 3);
S('gull', 'Seagulls calling over the sea, a few cries.', 2.5);
S('wave_crash', 'Huge ocean wave crashing over a boat deck, water rushing.', 4);
S('quest', 'Two soft hollow wooden knocks, gentle and satisfying.', 0.6);
S('sleep', 'Slow calm breathing of a sleeping person, two breaths.', 3);
A('waterfall', 'Waterfall pouring into a rocky pool, steady rushing water, close.');
A('jungle', 'Dense tropical jungle ambience, layered insects, distant parrots, leaves rustling.');
S('monkey_cry', 'Small monkey screeching and chattering, short.', 1.5);
S('snake_hiss', 'Snake hissing loudly, close and threatening.', 1.5);
S('fowl', 'Wild jungle hen clucking in alarm and flapping wings.', 1.5);
S('parrot', 'Parrot squawking twice in a tropical forest.', 1.5);
S('deer', 'Deer alarm snort and hooves bounding away on soft ground.', 2);
S('bow', 'Bow string release twang and arrow whoosh.', 0.8);
S('arrow_hit', 'Arrow thudding into wood, sharp impact.', 0.6);
S('bubble', 'Thick stew bubbling in a clay pot over fire.', 2.5);
S('discover', 'Deep airy whoosh swelling and fading, like wind rushing past, no tone.', 2.5);
S('ship_horn', 'Distant large ship horn across open sea, one long blast.', 4);
S('rumble', 'Earthquake rumble, ground shaking, rocks rattling.', 6);
S('bite', 'Large animal bite snap with water thrash.', 1);
S('charge', 'Wild boar charging, heavy hooves pounding dirt with angry snort.', 2);
S('ladder', 'Climbing a creaky wooden ladder, several steps.', 2.5);
S('roar_fire', 'Large bonfire igniting with a roar, flames surging.', 3.5);
// small concurrency pool
let i = 0; const workers = Array.from({ length: 3 }, async () => { while (i < jobs.length) { const j = jobs[i++]; try { await j(); } catch (e) { console.log('ERR', e.message); } } });
await Promise.all(workers); console.log('AUDIO DONE');
