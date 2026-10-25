import { writeFile } from 'node:fs/promises';

const apiBaseUrl = (process.env.PUBLIC_API_BASE_URL ?? '').trim().replace(/\/+$/, '');
if (!apiBaseUrl.startsWith('https://')) {
  throw new Error('PUBLIC_API_BASE_URL must be an HTTPS URL for a Render production build.');
}

await writeFile(
  new URL('../public/runtime-config.js', import.meta.url),
  `window.__FITHUB_CONFIG__ = ${JSON.stringify({ apiBaseUrl })};\n`,
  'utf8',
);
