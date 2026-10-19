"""Offline, read-only installation inventory. Outputs remain local; no upload feature."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import stat
import struct
import sys

MAX_HEADER = 2 * 1024 * 1024
MAX_NAMES = 2_000_000


def reject_links(path):
    current = Path(os.path.abspath(path))
    for part in (current, *current.parents):
        if part.exists() or part.is_symlink():
            mode = part.lstat()
            if stat.S_ISLNK(mode.st_mode) or getattr(mode, 'st_file_attributes', 0) & 0x400:
                raise ValueError('Symlink/reparse path refused')


def plugin_header(path):
    with open(path, 'rb') as stream:
        header = stream.read(24)
        if len(header) != 24 or header[:4] != b'TES4':
            raise ValueError('Not a TES4 plugin')
        size, flags = struct.unpack_from('<II', header, 4)
        if size > MAX_HEADER:
            raise ValueError('TES4 header exceeds inspection bound')
        body = stream.read(size)
        if len(body) != size:
            raise ValueError('Truncated TES4 header')
    masters, version, offset = [], None, 0
    while offset < len(body):
        if offset + 6 > len(body):
            raise ValueError('Truncated TES4 subrecord')
        sig, length = struct.unpack_from('<4sH', body, offset)
        offset += 6
        if offset + length > len(body):
            raise ValueError('Truncated TES4 subrecord payload')
        data = body[offset:offset + length]
        if sig == b'HEDR':
            if length != 12:
                raise ValueError('Invalid HEDR')
            version = struct.unpack_from('<f', data)[0]
        elif sig == b'MAST':
            if not data.endswith(b'\0'):
                raise ValueError('Unterminated master name')
            masters.append(data[:-1].decode('cp1252'))
        offset += length
    if version is None:
        raise ValueError('Missing HEDR')
    return {'flags': flags, 'headerVersion': version, 'masters': masters,
            'localized': bool(flags & 0x80), 'light': bool(flags & 0x200)}


def archive_names(path):
    """BA2 name table only; never read compressed payloads or extract files."""
    with open(path, 'rb') as stream:
        header = stream.read(24)
        if len(header) != 24 or header[:4] != b'BTDX':
            raise ValueError('Not a BA2 archive')
        version, kind, count, offset = struct.unpack_from('<I4sIQ', header, 4)
        if version not in (1, 2, 3, 7, 8) or kind not in (b'GNRL', b'DX10'):
            raise ValueError('Unsupported BA2 version/type; no guessed decoding')
        length = os.fstat(stream.fileno()).st_size
        if count > MAX_NAMES or offset < 24 or offset > length:
            raise ValueError('Invalid BA2 name table bounds')
        stream.seek(offset)
        for _ in range(count):
            raw = stream.read(2)
            if len(raw) != 2:
                raise ValueError('Truncated BA2 name length')
            size = struct.unpack('<H', raw)[0]
            if not 0 < size <= 32768:
                raise ValueError('Invalid BA2 name size')
            raw = stream.read(size)
            if len(raw) != size:
                raise ValueError('Truncated BA2 name')
            name = raw.decode('utf-8', errors='strict').replace('\\', '/')
            if name.startswith('/') or ':' in name or any(p in ('', '..', '.') for p in name.split('/')):
                raise ValueError('Unsafe BA2 member name')
            yield name


def category(name):
    name = name.replace('\\', '/').lower()
    if 'facegeom/' in name or 'facegendata/' in name or 'facecustomization/' in name:
        return 'facegen'
    if '/character/' in name or '/actors/' in name:
        return 'actor-assets'
    if '/armor/' in name or '/clothes/' in name:
        return 'equipment'
    if name.startswith('f4se/') or name.endswith(('.pex', '.psc')):
        return 'runtime-or-script'
    return 'other'


def inspect(roots, output, index_archives=False):
    output = Path(os.path.abspath(output))
    reject_links(output)
    sources = {}
    for label, source in roots.items():
        if not label or not label.replace('-', '').replace('_', '').isalnum():
            raise ValueError('Root labels must be simple identifiers')
        source = Path(os.path.abspath(source))
        reject_links(source)
        if not source.is_dir():
            raise ValueError('Input root is not a directory')
        # Also refuse an output ancestor of input: never create a destination around inputs.
        if source == output or source in output.parents or output in source.parents:
            raise ValueError('Output must be separate from every input root')
        sources[label] = source
    if not sources:
        raise ValueError('At least one explicit root required')
    output.mkdir(parents=True, exist_ok=False)
    errors, skipped, files, resources = [], [], 0, 0
    def error(label, relative, exc):
        errors.append({'root': label, 'path': relative, 'kind': type(exc).__name__,
                       'reason': str(exc).replace(str(sources[label]), '<input>')})
    with (output / 'inventory.jsonl').open('x', encoding='utf-8') as inventory, \
         (output / 'resources.jsonl').open('x', encoding='utf-8') as members:
        for label, source in sources.items():
            def walk_error(exc):
                error(label, '<directory>', exc)
            for parent, directories, names in os.walk(source, followlinks=False, onerror=walk_error):
                directories.sort()
                for child in list(directories):
                    entry = Path(parent) / child
                    try:
                        reject_links(entry)
                    except (ValueError, OSError):
                        directories.remove(child)
                        skipped.append({'root': label, 'path': entry.relative_to(source).as_posix(), 'reason': 'reparse'})
                for name in sorted(names):
                    path = Path(parent) / name
                    relative = path.relative_to(source).as_posix()
                    try:
                        reject_links(path)
                        info = path.stat()
                        if not stat.S_ISREG(info.st_mode):
                            raise ValueError('Non-regular file refused')
                        row = {'root': label, 'path': relative, 'size': info.st_size,
                               'modifiedNs': info.st_mtime_ns, 'category': category(relative)}
                        suffix = path.suffix.lower()
                        if suffix in ('.esm', '.esp', '.esl'):
                            row['plugin'] = plugin_header(path)
                        elif name.lower() in ('plugins.txt', 'loadorder.txt', 'modlist.txt'):
                            if info.st_size > MAX_HEADER:
                                raise ValueError('Profile list exceeds bound')
                            content = path.read_bytes()
                            row['sha256'] = hashlib.sha256(content).hexdigest()
                            row['profileLines'] = content.decode('utf-8-sig').splitlines()
                        if index_archives and suffix == '.ba2':
                            for member in archive_names(path):
                                members.write(json.dumps({'root': label, 'archive': relative, 'member': member, 'category': category(member)}) + '\n')
                                resources += 1
                        # Detect changes during inspection; never claim a consistent snapshot otherwise.
                        after = path.stat()
                        if (info.st_size, info.st_mtime_ns) != (after.st_size, after.st_mtime_ns):
                            raise ValueError('Input changed during inspection')
                        inventory.write(json.dumps(row, ensure_ascii=False) + '\n')
                        files += 1
                    except (ValueError, OSError, UnicodeError, struct.error) as exc:
                        error(label, relative, exc)
    report = {'schemaVersion': 1, 'status': 'partial' if errors or skipped else 'complete',
              'filesInventoried': files, 'archiveMembersIndexed': resources,
              'archiveIndexRequested': index_archives, 'errors': errors, 'skipped': skipped,
              'readOnly': True, 'uploaded': False, 'gameCompatibilityValidated': False,
              'limitations': ['No asset extraction', 'No winner resolution without a confirmed mod-manager profile',
                              'Metadata may expose private mod names; review before sharing',
                              'Concurrent filesystem replacement is not prevented; inspect a quiescent installation']}
    (output / 'summary.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    return report


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', action='append', required=True, metavar='LABEL=PATH')
    parser.add_argument('--output', required=True)
    parser.add_argument('--archive-index', action='store_true')
    args = parser.parse_args(argv)
    try:
        roots = {}
        for spec in args.root:
            label, source = spec.split('=', 1)
            if label in roots:
                raise ValueError('Duplicate root label')
            roots[label] = source
        report = inspect(roots, args.output, args.archive_index)
        print(json.dumps({k: report[k] for k in ('status', 'filesInventoried', 'archiveMembersIndexed')}))
        return 0 if report['status'] == 'complete' else 2
    except (ValueError, OSError) as exc:
        print(str(exc), file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
