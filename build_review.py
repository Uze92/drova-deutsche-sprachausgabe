"""Compile the exact versioned application sources. Never installs or starts Drova."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import shutil
import subprocess
import urllib.request
import wave
import zipfile

ROOT = Path(__file__).resolve().parent
VERSIONS = [row['version'] for row in json.loads((ROOT / 'releases.json').read_text())]
DEPS = [
    ('melonloader.zip', 'https://github.com/LavaGang/MelonLoader/releases/download/v0.7.3/MelonLoader.x64.zip',
     '5b2b2f3d1cd42b59ec886c5bdc2663edae87a0097a4f4a8f58c0965a99dda416', 'melonloader-0.7.3-official'),
    ('modding-api.zip', 'https://github.com/Drova-Modding/Drova-Modding-API/releases/download/release-0.6.0/Modding_API.zip',
     'f8e4756166bc9925e6f39394f0877ed56be146be550cee2710aa26dafc89cdf4', 'runtime-dependencies'),
]


def sha(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()


def run(args):
    print('>', subprocess.list2cmdline([str(a) for a in args]), flush=True)
    subprocess.run([str(a) for a in args], check=True)


def verify_sources(base):
    for name, expected in json.loads((base / 'EXPORT-SHA256.json').read_text()).items():
        if sha(base / name) != expected:
            raise ValueError('Source snapshot changed: ' + name)


def dependencies(base):
    cache = ROOT / '.cache'
    cache.mkdir(exist_ok=True)
    for name, url, expected, folder in DEPS:
        archive = cache / name
        if not archive.exists():
            temporary = archive.with_suffix('.download')
            with urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'Drova-source-review'}), timeout=90) as response, temporary.open('wb') as dest:
                shutil.copyfileobj(response, dest)
            if sha(temporary) != expected:
                raise ValueError('Dependency download SHA-256 mismatch: ' + name)
            temporary.replace(archive)
        if sha(archive) != expected:
            raise ValueError('Cached dependency SHA-256 mismatch: ' + name)
        target = base / 'drova_de/research' / folder
        target.mkdir(parents=True, exist_ok=True)
        with zipfile.ZipFile(archive) as z:
            for entry in z.infolist():
                final = (target / entry.filename).resolve()
                if not final.is_relative_to(target.resolve()):
                    raise ValueError('Unsafe ZIP entry')
            z.extractall(target)


def tests(base, output, package):
    if base.name != VERSIONS[-1]:
        raise ValueError('--test is supported for the latest source snapshot only')
    for project in ['drova_de/runtime/startup-tests/StartupTests.csproj',
                    'drova_de/runtime/playback-tests/PlaybackTests.csproj',
                    'drova_de/auto_dialogue/tests/AutoDialogueTests.csproj']:
        run(['dotnet', 'run', '--project', base / project, '-c', 'Release'])
    fixture = output / 'synthetic-parser-fixture.wav'
    with wave.open(str(fixture), 'wb') as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(24000)
        f.writeframes(b'\x10\x00' * 2400)
    run(['dotnet', 'run', '--project', base / 'drova_de/runtime/tests/WaveTests.csproj', '-c', 'Release', '--', fixture])
    if package:
        run(['dotnet', 'run', '--project', base / 'drova_de/runtime/startup-tests/RouteTests.csproj', '-c', 'Release', '--', package / 'Mod-Dateien/Mods/DrovaVoices/routes.json'])


def assemble(base, output, package):
    manifest = json.loads((package / 'Paket.json').read_text('utf-8-sig'))
    if manifest['version'] != base.name:
        raise ValueError('Payload version does not match selected source')
    if (output / 'package').exists():
        raise ValueError('Review package output already exists; preserve it and use a fresh clone/output')
    for row in manifest['files']:
        source = (package / 'Mod-Dateien' / row['path']).resolve()
        if not source.is_relative_to((package / 'Mod-Dateien').resolve()) or sha(source) != row['sha256']:
            raise ValueError('Invalid/missing original payload: ' + row['path'])
    dest = output / 'package'
    shutil.copytree(base / 'installer', dest)
    shutil.copytree(package / 'Mod-Dateien', dest / 'Mod-Dateien')
    shutil.copy2(output / 'Deutsche-Stimmen-Setup.exe', dest / 'Deutsche-Stimmen-Setup.exe')
    for name, folder in [('DrovaVoices.dll', 'voices'), ('DrovaAutoDialogue.dll', 'auto-dialogue')]:
        shutil.copy2(output / folder / name, dest / 'Mod-Dateien/Mods' / name)
    manifest['version'] += '-source-review-build'
    for row in manifest['files']:
        row['sha256'] = sha(dest / 'Mod-Dateien' / row['path'])
    (dest / 'Paket.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8-sig')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--version', choices=VERSIONS, default=VERSIONS[-1])
    parser.add_argument('--game', type=Path, help='Owned Drova game folder, read-only compiler references')
    parser.add_argument('--test', action='store_true')
    parser.add_argument('--tests-only', action='store_true', help='Headless tests, no game/dependency downloads/compiler build')
    parser.add_argument('--package-root', type=Path, help='Optional extracted official mod release, verified before copying')
    args = parser.parse_args()
    base = ROOT / 'versions' / args.version
    verify_sources(base)
    output = ROOT / 'build' / args.version
    output.mkdir(parents=True, exist_ok=True)
    package = args.package_root.resolve() if args.package_root else None
    if args.tests_only:
        tests(base, output, package)
        return
    if os.name != 'nt' or not args.game:
        parser.error('Full build needs Windows x64 and --game pointing to an owned Drova installation')
    refs = (args.game / 'MelonLoader/Il2CppAssemblies').resolve()
    if not (refs / 'UnityEngine.CoreModule.dll').is_file() or not (args.game / 'Drova.exe').is_file():
        parser.error('Missing game/generated IL2CPP references; see BUILD.md')
    dependencies(base)
    for project, folder in [('drova_de/runtime/DrovaVoices.csproj', 'voices'), ('drova_de/auto_dialogue/DrovaAutoDialogue.csproj', 'auto-dialogue')]:
        run(['dotnet', 'build', base / project, '-c', 'Release', '-o', output / folder,
             '-p:UnityReferenceRoot=' + str(refs), '-p:InteropRoot=' + str(refs)])
    csc = Path(os.environ['WINDIR']) / 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    installer = base / 'installer'
    run([csc, '/nologo', '/target:winexe', '/optimize+', '/reference:System.Windows.Forms.dll',
         '/win32manifest:' + str(installer / 'Launcher.manifest'), '/win32icon:' + str(installer / 'assets/icon.ico'),
         '/out:' + str(output / 'Deutsche-Stimmen-Setup.exe'), installer / 'Launcher.cs'])
    if args.test:
        tests(base, output, package)
    if package:
        assemble(base, output, package)
    binaries = {p.relative_to(output).as_posix(): sha(p) for p in [output / 'Deutsche-Stimmen-Setup.exe', output / 'voices/DrovaVoices.dll', output / 'auto-dialogue/DrovaAutoDialogue.dll']}
    (output / 'build-result.json').write_text(json.dumps({'source_version': args.version, 'sha256': binaries, 'game_started': False, 'ingame_tested': False}, indent=2) + '\n', encoding='utf-8')
    print('Build complete. No files installed into the game.')


if __name__ == '__main__':
    main()
