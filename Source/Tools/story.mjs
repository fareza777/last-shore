import { key } from './keys.mjs';
import { writeFileSync } from 'node:fs';
const brief = `You are the narrative designer of "Last Shore", a grounded first-person 3D survival game with a global audience.
Premise: a solo sailor's small sailboat, the Meridian, is wrecked in a night storm. They wake alone on an uninhabited tropical island. They must survive (food, water, fire, shelter, wild boars), explore, find the weathered notes of a previous castaway (Tomas Reyes, a surveyor stranded years ago, who almost finished a raft), salvage rope and canvas from the Meridian's wreck on the north shore, build a raft and sail away before the storm season returns.
Tone: quiet, human, determined; simple but gripping. First-person inner voice for the narrator. Plain modern English.
HARD CONTENT RULES: no religion, no prayer, no spirits, ghosts, gods, fate/luck charms, magic, omens, fortune telling, rituals, altars, statues, music or singing. No alcohol. No romance. Nothing supernatural at all.
Return ONLY minified JSON with this exact shape:
{"intro":[6 strings, each 1-2 short sentences, spoken over a storm-and-shipwreck cinematic, ending with waking on the beach],
"lines":{"wake":"","sticks_done":"","axe_done":"","water_done":"","fire_done":"","shelter_done":"","first_night":"","camp_found":"","wreck_found":"","raft_parts":"","raft_built":"","low_health":"","storm":"","boar":""},
"notes":[5 objects {"title":"","body":""} = Tomas Reyes's journal pages, 40-70 words each, giving practical hints in order: fresh water from coconuts and rain; fire and boars fear of fire; his camp and tools; the raft design needing logs, rope and a sail; his final page before leaving on a passing ship, wishing the next person well],
"ending":[3 strings, spoken while sailing away at dawn]}
Each value in "lines" is one spoken sentence, max 18 words, for that story moment.`;
const models = ['anthropic/claude-sonnet-4.5', 'openai/gpt-4o', 'openai/gpt-4o-mini'];
for (const model of models) {
  const res = await fetch('https://openrouter.ai/api/v1/chat/completions', {
    method: 'POST', headers: { Authorization: 'Bearer ' + key('Open router'), 'Content-Type': 'application/json' },
    body: JSON.stringify({ model, messages: [{ role: 'user', content: brief }], temperature: 0.8, max_tokens: 2500 }),
  });
  const j = await res.json();
  const out = j.choices?.[0]?.message?.content;
  if (!out) { console.log(model, 'failed', JSON.stringify(j).slice(0, 300)); continue; }
  const s = out.slice(out.indexOf('{'), out.lastIndexOf('}') + 1);
  writeFileSync(new URL('./story.json', import.meta.url), JSON.stringify(JSON.parse(s), null, 1));
  console.log('model', model); console.log(JSON.stringify(JSON.parse(s), null, 1)); break;
}
