import { createInterface } from 'node:readline/promises';
import { stdin as input, stdout as output } from 'node:process';

export const controls = ['claim','sharing','occupied','release','detected','clear','heartbeat','disconnect','maintenance'];
export function requestFor(control, cardUid) {
  const map = {
    claim: ['claim', { cardUid }], sharing: ['action', { action: 'sharing-allowed', cardUid }], occupied: ['action', { action: 'occupied', cardUid }], release: ['action', { action: 'release', cardUid }],
    detected: ['sensor', { detected: true }], clear: ['sensor', { detected: false }], heartbeat: ['heartbeat', undefined], disconnect: ['disconnect', undefined], maintenance: ['action', { action: 'maintenance' }]
  };
  if (!map[control]) throw new Error(`Unknown control: ${control}`); return map[control];
}
export async function send(control, settings, fetcher = fetch) {
  const [path, body] = requestFor(control, settings.cardUid); const response = await fetcher(`${settings.apiUrl}/api/device/equipment/${settings.equipmentId}/${path}`, { method:'POST', headers:{'content-type':'application/json','X-FitHub-Device-Id':settings.deviceId,'X-FitHub-Device-Key':settings.deviceKey}, body: body ? JSON.stringify(body) : undefined });
  if (!response.ok) throw new Error(`${response.status} ${await response.text()}`); return `${control}: accepted`;
}
async function main() {
  const settings={apiUrl:process.env.FITHUB_API_URL??'http://localhost:8080',equipmentId:process.env.FITHUB_EQUIPMENT_ID??'',deviceId:process.env.FITHUB_DEVICE_ID??'',deviceKey:process.env.FITHUB_DEVICE_KEY??'',cardUid:process.env.FITHUB_CARD_UID??''};
  if (process.argv.includes('--show-controls')) { for (const control of controls) console.log(JSON.stringify({control, request:requestFor(control, settings.cardUid)})); return; }
  if (process.argv.includes('--demo')) {
    if (!settings.equipmentId || !settings.deviceId || !settings.deviceKey || !settings.cardUid) throw new Error('The demo requires FITHUB_EQUIPMENT_ID, FITHUB_DEVICE_ID, FITHUB_DEVICE_KEY and FITHUB_CARD_UID.');
    for (const control of ['heartbeat','claim','detected','clear','sharing','occupied','release','maintenance','disconnect']) console.log(await send(control, settings));
    return;
  }
  const rl=createInterface({input,output}); console.log('FitHub equipment simulator — uses only the production device HTTP API.');
  for (const key of Object.keys(settings)) if (!settings[key]) settings[key]=await rl.question(`${key}: `);
  while(true){const control=(await rl.question(`Control (${controls.join(', ')}, quit): `)).trim().toLowerCase();if(control==='quit')break;try{console.log(await send(control,settings));}catch(error){console.error(error.message);}} rl.close();
}
if (process.argv[1]?.endsWith('simulator.mjs')) await main();
