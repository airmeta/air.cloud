import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import archiver from 'archiver';

const dirname = path.dirname(fileURLToPath(import.meta.url));
const appRoot = path.resolve(dirname, '..');
const distDir = path.join(appRoot, 'dist');
const zipPath = path.join(appRoot, 'dist.zip');

if (!fs.existsSync(distDir)) {
  throw new Error(`dist directory not found: ${distDir}`);
}

if (fs.existsSync(zipPath)) {
  fs.rmSync(zipPath, { force: true });
}

const output = fs.createWriteStream(zipPath);
const archive = archiver('zip', { zlib: { level: 9 } });

archive.on('error', (error) => {
  throw error;
});

archive.pipe(output);
archive.directory(distDir, false);
await archive.finalize();

await new Promise((resolve, reject) => {
  output.on('close', resolve);
  output.on('error', reject);
});

console.log(`ZIP file created: ${zipPath} (${archive.pointer()} total bytes)`);
