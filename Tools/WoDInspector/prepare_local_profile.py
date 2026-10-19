"""Build a private explicit-language profile from an existing isolated Windows/VFS view.
No plugin rewrite, asset copy, archive extraction, installation or upload operation.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path, PureWindowsPath
import re
import sys
from inspect_wod import plugin_header, reject_links

MAX_PROFILE = 16 * 1024 * 1024


def reject_lite(path):
    if any(part.casefold() == 'cru-female-lite' for part in PureWindowsPath(str(path)).parts):
        raise ValueError('This project must remain separate from CRU-Female-Lite')


def prepare(profile_path, language, output, table_root=None):
    if not re.fullmatch('[a-z]{2,24}', language):
        raise ValueError('Explicit literal lower-case resolver language required')
    source = Path(os.path.abspath(profile_path))
    reject_links(source)
    reject_lite(source)
    if source.stat().st_size > MAX_PROFILE:
        raise ValueError('Profile exceeds 16 MiB')
    profile = json.loads(source.read_text(encoding='utf-8-sig'))
    data = Path(os.path.abspath(profile['DataRoot']))
    tables = Path(os.path.abspath(table_root)) if table_root else data / 'Strings'
    originals = [source, data, tables, *map(Path, profile['ProtectedRoots'])]
    destination = Path(os.path.abspath(output))
    reject_lite(destination)
    reject_links(destination)
    for item in originals:
        reject_lite(item)
        item = Path(os.path.abspath(item))
        reject_links(item)
        if item == destination or item in destination.parents or destination in item.parents:
            raise ValueError('Output must be separate from original profile/resources')
    names = profile['LoadOrder']
    if not names or len(names) > 4096 or len({n.casefold() for n in names}) != len(names):
        raise ValueError('Bounded unique explicit load order required')
    if not tables.is_dir():
        raise ValueError('Selected loose table view missing; BA2-only resources require verified archive route')
    available = {}
    for count, file in enumerate(tables.iterdir(), 1):
        if count > 16384:
            raise ValueError('Selected table directory exceeds inventory budget')
        if file.name.casefold() in available:
            raise ValueError('Ambiguous case-colliding table names')
        available[file.name.casefold()] = file
    selected, seen, total = [], set(), 0
    for name in names:
        if Path(name).name != name or '/' in name or '\\' in name:
            raise ValueError('Plugin basenames required')
        path = data / name
        reject_links(path)
        header = plugin_header(path)  # Existing metadata inspector; never reads NPC payloads.
        if any(master.casefold() not in seen for master in header['masters']):
            raise ValueError('Missing or out-of-order master')
        seen.add(name.casefold())
        found = 0
        for kind in ('strings', 'dlstrings', 'ilstrings'):
            filename = f'{Path(name).stem}_{language}.{kind}'
            path = available.get(filename.casefold())
            if path is None:
                continue
            reject_links(path)
            if path.with_suffix('.cpoverride').exists():
                raise ValueError('Unsealed encoding sidecar is unsupported')
            if not path.is_file() or path.stat().st_size > 32 * 1024 * 1024:
                raise ValueError('Loose table exceeds 32 MiB or is not a file')
            total += path.stat().st_size
            if total > 128 * 1024 * 1024:
                raise ValueError('Loose table pool exceeds 128 MiB')
            digest = hashlib.sha256()
            with path.open('rb') as stream:
                for chunk in iter(lambda: stream.read(1024 * 1024), b''):
                    digest.update(chunk)
            selected.append({'Plugin': name, 'Kind': kind, 'FilePath': str(path), 'Sha256': digest.hexdigest().upper()})
            found += 1
        if header['localized'] and not found:
            raise ValueError(f'Selected language tables missing for {name}; no language fallback or BA2 extraction')
    if not selected:
        raise ValueError('No explicit language resources found')
    profile['Localization'] = {'Language': language, 'Tables': selected}
    if str(tables) not in profile['ProtectedRoots']:
        profile['ProtectedRoots'].append(str(tables))
    content = json.dumps(profile, ensure_ascii=False, indent=2).encode('utf-8')
    if len(content) > MAX_PROFILE:
        raise ValueError('Prepared profile exceeds 16 MiB')
    with destination.open('xb') as stream:
        stream.write(content)
        stream.flush()
        os.fsync(stream.fileno())
    return {'status': 'PRIVATE-PROFILE-ONLY-NOT-CONVERTED', 'tables': len(selected), 'tableBytes': total}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--profile', required=True)
    parser.add_argument('--language', required=True)
    parser.add_argument('--output', required=True)
    parser.add_argument('--table-root')
    args = parser.parse_args()
    try:
        print(json.dumps(prepare(args.profile, args.language, args.output, args.table_root)))
        return 0
    except (OSError, ValueError, KeyError) as error:
        print(str(error), file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
