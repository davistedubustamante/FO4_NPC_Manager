"""Deterministic additive output interface; serializer and pinned source revision unchanged."""
import hashlib,json,pathlib,subprocess,sys
manifest=json.loads(pathlib.Path(__file__).with_name('esp-stream-overlay.json').read_text())
root=pathlib.Path(sys.argv[1]).resolve()
revision=subprocess.check_output(['git','-C',str(root),'rev-parse','HEAD'],text=True).strip()
if revision!=manifest['pin']: raise SystemExit('Original dependency pin mismatch')
source=root/manifest['file']; blob=source.read_bytes().replace(b'\r\n',b'\n')
original=subprocess.check_output(['git','-C',str(root),'show','HEAD:'+manifest['file']])
if hashlib.sha256(original).hexdigest()!=manifest['originalSha256']:raise SystemExit('Pinned source blob checksum mismatch')
digest=lambda b:hashlib.sha256(b).hexdigest()
if digest(blob)==manifest['patchedSha256']:
 print('ESP stream overlay already verified');sys.exit(0)
if digest(blob)!=manifest['originalSha256']:raise SystemExit('Original writer SHA256 mismatch; no file changed')
text=blob.decode('utf-8-sig')
for before,after in manifest['replacements']:
 if text.count(before)!=1:raise SystemExit('Overlay context mismatch; no file changed')
 text=text.replace(before,after)
result=('\ufeff'+text).encode('utf-8')
if digest(result)!=manifest['patchedSha256']:raise SystemExit('Patched writer SHA256 mismatch; no file changed')
source.write_bytes(result)
print('Original ESP writer output interface patched and SHA256 verified')
