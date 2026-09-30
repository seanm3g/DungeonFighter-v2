import http from 'node:http';
import {readFile} from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
import path from 'node:path';
const root = path.dirname(fileURLToPath(import.meta.url));
const types = {'.html':'text/html; charset=utf-8','.css':'text/css','.js':'text/javascript','.png':'image/png','.json':'application/json'};
const reference = path.resolve(root, './painted-mace-reference.png');
const server = http.createServer(async (req,res) => {
  try {
    const pathname = decodeURIComponent(new URL(req.url, 'http://localhost').pathname);
    const file = pathname === '/reference.png' ? reference : path.resolve(root, '.' + (pathname === '/' ? '/index.html' : pathname));
    if (file !== reference && !file.startsWith(root + path.sep)) {res.writeHead(403).end(); return;}
    const content = await readFile(file);
    res.writeHead(200, {'Content-Type':types[path.extname(file)] || 'application/octet-stream','Cache-Control':'no-store'}).end(content);
  } catch {res.writeHead(404).end('Not found');}
});
server.listen(Number(process.env.PORT || 4318), '127.0.0.1', () => console.log('Weapon Forge: http://127.0.0.1:4318'));
