#!/usr/bin/env python3
"""Import reviewed SVG originals and generate Monica's offline icon catalog.

Only ``import-lucide --version <version>`` accesses the network. Other commands
consume checked-in inputs; the generated C# file is also checked in, so ordinary
.NET builds do not need Python or npm.
"""

from __future__ import annotations

import argparse
import base64
import hashlib
import html
import io
import json
import re
import sys
import tarfile
import tempfile
import urllib.request
import xml.etree.ElementTree as ET
from pathlib import Path, PurePosixPath


ROOT = Path(__file__).resolve().parents[1]
ASSETS_PATH = Path("Monica.UI/Icons/Assets")
CATALOG_PATH = Path("Monica.UI/Icons/MoIcons.g.cs")
GALLERY_PATH = Path(".ui-design/moicon/index.html")
SVG_NS = "http://www.w3.org/2000/svg"
GROUP = ('<g fill="none" stroke="currentColor" stroke-linecap="round" '
         'stroke-linejoin="round" style="stroke-width:var(--mo-icon-stroke-width,1.5)">')
ELEMENT_ATTRIBUTES = {
    "g": set(),
    "path": {"d"},
    "rect": {"x", "y", "width", "height", "rx", "ry"},
    "circle": {"cx", "cy", "r"},
    "ellipse": {"cx", "cy", "rx", "ry"},
    "line": {"x1", "y1", "x2", "y2"},
    "polyline": {"points"},
    "polygon": {"points"},
}
PAINT_ATTRIBUTES = {"fill", "stroke", "stroke-width", "stroke-linecap", "stroke-linejoin"}
ROOT_ATTRIBUTES = PAINT_ATTRIBUTES | {"width", "height", "viewBox", "class"}
NUMBER = r"[+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?"
NUMBER_LIST = re.compile(rf"{NUMBER}(?:[\s,]+{NUMBER})*")
RESERVED_NAMES = {"MoIcons", "Equals", "ReferenceEquals", "GetHashCode", "GetType", "ToString"}


class IconError(ValueError):
    """An icon input violates the checked-in asset contract."""


def read_json(path: Path) -> dict:
    def unique_pairs(pairs: list[tuple[str, object]]) -> dict:
        result = {}
        for key, value in pairs:
            if key in result:
                raise IconError(f"{path}: duplicate key {key!r}")
            result[key] = value
        return result

    try:
        result = json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=unique_pairs)
    except json.JSONDecodeError as error:
        raise IconError(f"{path}: {error}") from error
    if not isinstance(result, dict):
        raise IconError(f"{path}: expected a JSON object")
    return result


def local_name(name: str) -> str:
    prefix = f"{{{SVG_NS}}}"
    if name.startswith(prefix):
        return name[len(prefix):]
    if "{" in name or ":" in name:
        raise IconError(f"Unsupported SVG namespace: {name}")
    return name


def validate_paint(name: str, value: str) -> None:
    allowed = {
        "fill": {"none", "currentColor"},
        "stroke": {"none", "currentColor"},
        "stroke-linecap": {"round"},
        "stroke-linejoin": {"round"},
    }
    if name == "stroke-width":
        if not re.fullmatch(NUMBER, value) or not 0 < float(value) <= 4:
            raise IconError(f"Unsupported stroke width: {value!r}")
    elif value not in allowed[name]:
        raise IconError(f"Unsupported {name}: {value!r}")


def svg_fragment(data: bytes, source: str) -> str:
    """Validate a 24-unit outline SVG and return a self-contained body fragment.

    XML comments (including upstream license markers) remain in original files.
    Root presentation is normalized once; geometry and approved child paint stay
    intact. Arbitrary XML/SVG is deliberately unsupported.
    """
    if b"<!DOCTYPE" in data.upper() or b"<!ENTITY" in data.upper():
        raise IconError(f"{source}: XML declarations and entities are unsupported")
    try:
        root = ET.fromstring(data)
    except ET.ParseError as error:
        raise IconError(f"{source}: invalid SVG: {error}") from error
    if local_name(root.tag) != "svg":
        raise IconError(f"{source}: expected an SVG root")
    for name, value in root.attrib.items():
        if name not in ROOT_ATTRIBUTES:
            raise IconError(f"{source}: unsupported root attribute {name!r}")
        if name in PAINT_ATTRIBUTES:
            validate_paint(name, value)
    view_box = root.get("viewBox", "").split()
    if len(view_box) != 4 or any(not re.fullmatch(NUMBER, item) for item in view_box):
        raise IconError(f"{source}: viewBox must be 0 0 24 24")
    if [float(item) for item in view_box] != [0, 0, 24, 24]:
        raise IconError(f"{source}: viewBox must be 0 0 24 24")
    if root.get("fill", "none") != "none" or root.get("stroke", "currentColor") != "currentColor":
        raise IconError(f"{source}: root must use an unfilled currentColor outline")
    for dimension in ("width", "height"):
        if dimension in root.attrib and root.attrib[dimension] != "24":
            raise IconError(f"{source}: {dimension} must be 24 or omitted")

    def serialize(element: ET.Element) -> str:
        tag = local_name(element.tag)
        if tag not in ELEMENT_ATTRIBUTES:
            raise IconError(f"{source}: unsupported SVG element {tag!r}")
        if (element.text or "").strip() or (element.tail or "").strip():
            raise IconError(f"{source}: text content is unsupported")
        attributes = []
        for name, value in sorted(element.attrib.items()):
            if name in PAINT_ATTRIBUTES:
                validate_paint(name, value)
                if name == "stroke-width":
                    raise IconError(f"{source}: child stroke widths must inherit the catalog default")
            elif name not in ELEMENT_ATTRIBUTES[tag]:
                raise IconError(f"{source}: unsupported {tag} attribute {name!r}")
            elif name == "d":
                if not value or not re.fullmatch(r"[MmZzLlHhVvCcSsQqTtAa0-9eE+.,\s-]+", value):
                    raise IconError(f"{source}: invalid path data")
            elif not NUMBER_LIST.fullmatch(value):
                raise IconError(f"{source}: invalid numeric attribute {name!r}")
            attributes.append(f' {name}="{html.escape(value, quote=True)}"')
        children = "".join(serialize(child) for child in element)
        start = f"<{tag}{''.join(attributes)}"
        return f"{start}>{children}</{tag}>" if children else f"{start}/>"

    if (root.text or "").strip() or not len(root):
        raise IconError(f"{source}: expected SVG geometry")
    return GROUP + "".join(serialize(child) for child in root) + "</g>"


def snapshot_digest(icons: dict[str, bytes]) -> str:
    digest = hashlib.sha256()
    for name, data in sorted(icons.items()):
        digest.update(name.encode("utf-8") + b"\0" + data + b"\0")
    return digest.hexdigest()


def verify_integrity(data: bytes, integrity: str) -> None:
    """Require the npm SHA-512 integrity value to match before accepting assets."""
    if not isinstance(integrity, str) or not integrity.startswith("sha512-"):
        raise IconError("The upstream package must declare SHA-512 integrity")
    actual = "sha512-" + base64.b64encode(hashlib.sha512(data).digest()).decode("ascii")
    if integrity != actual:
        raise IconError("Lucide package integrity mismatch; no assets were imported")


def import_lucide(assets: Path, version: str) -> int:
    if not re.fullmatch(r"\d+\.\d+\.\d+", version):
        raise IconError("Use an explicit Lucide release version, such as 1.58.0")
    metadata_url = f"https://registry.npmjs.org/lucide-static/{version}"
    with urllib.request.urlopen(metadata_url, timeout=60) as response:
        metadata = json.load(response)
    if metadata.get("name") != "lucide-static" or metadata.get("version") != version:
        raise IconError("Unexpected package metadata")
    package_url = f"https://registry.npmjs.org/lucide-static/-/lucide-static-{version}.tgz"
    if metadata.get("dist", {}).get("tarball") != package_url:
        raise IconError("The package tarball must use the official npm registry URL")
    with urllib.request.urlopen(package_url, timeout=60) as response:
        package = response.read()
    integrity = metadata["dist"].get("integrity")
    verify_integrity(package, integrity)
    icons = {}
    license_bytes = None
    with tarfile.open(fileobj=io.BytesIO(package), mode="r:gz") as archive:
        for member in archive.getmembers():
            if member.name == "package/LICENSE":
                if not member.isfile() or license_bytes is not None:
                    raise IconError("Unexpected Lucide license entry")
                license_bytes = archive.extractfile(member).read()
            elif member.name.startswith("package/icons/"):
                name = member.name.removeprefix("package/icons/")
                if not member.isfile() or not re.fullmatch(r"[a-z0-9]+(?:-[a-z0-9]+)*\.svg", name):
                    raise IconError(f"Unexpected Lucide icon archive entry: {member.name}")
                if name in icons:
                    raise IconError(f"Duplicate Lucide archive entry: {name}")
                data = archive.extractfile(member).read()
                svg_fragment(data, name)
                icons[name] = data
    if not icons or not license_bytes or b"ISC License" not in license_bytes or b"The MIT License" not in license_bytes:
        raise IconError("The Lucide archive must contain SVGs and the ISC/Feather MIT notices")
    lock = {
        "package": "lucide-static",
        "version": version,
        "metadata_url": metadata_url,
        "tarball_url": package_url,
        "integrity": integrity,
        "package_sha256": hashlib.sha256(package).hexdigest(),
        "svg_count": len(icons),
        "svg_snapshot_sha256": snapshot_digest(icons),
        "license_sha256": hashlib.sha256(license_bytes).hexdigest(),
    }
    assets.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix=".lucide-import-", dir=assets) as temporary:
        staged = Path(temporary) / "Lucide"
        (staged / "icons").mkdir(parents=True)
        for name, data in icons.items():
            (staged / "icons" / name).write_bytes(data)
        (staged / "LICENSE").write_bytes(license_bytes)
        target = assets / "Lucide"
        backup = Path(temporary) / "previous"
        if target.exists():
            target.rename(backup)
        try:
            staged.rename(target)
        except OSError:
            if backup.exists():
                backup.rename(target)
            raise
        (assets / "lucide.lock.json").write_text(json.dumps(lock, indent=2) + "\n", encoding="utf-8", newline="\n")
    return len(icons)


def verify_snapshot(assets: Path) -> dict:
    lock = read_json(assets / "lucide.lock.json")
    version = lock.get("version", "")
    if (lock.get("package") != "lucide-static" or not isinstance(version, str)
            or not re.fullmatch(r"\d+\.\d+\.\d+", version)
            or lock.get("metadata_url") != f"https://registry.npmjs.org/lucide-static/{version}"
            or lock.get("tarball_url") != f"https://registry.npmjs.org/lucide-static/-/lucide-static-{version}.tgz"
            or not re.fullmatch(r"sha512-[A-Za-z0-9+/]{86}==", str(lock.get("integrity", "")))):
        raise IconError("Invalid Lucide provenance; run import-lucide with an explicit version")
    icons = {path.name: path.read_bytes() for path in (assets / "Lucide/icons").glob("*.svg")}
    license_bytes = (assets / "Lucide/LICENSE").read_bytes()
    if (len(icons) != lock.get("svg_count") or snapshot_digest(icons) != lock.get("svg_snapshot_sha256")
            or hashlib.sha256(license_bytes).hexdigest() != lock.get("license_sha256")):
        raise IconError("The Lucide snapshot differs from its verified import; restore it or import explicitly")
    return lock


def load_catalog(assets: Path) -> dict[str, tuple[str, str]]:
    catalog = read_json(assets / "catalog.json")
    if not catalog:
        raise IconError("The icon catalog is empty")
    result = {}
    for name, source in sorted(catalog.items()):
        if not re.fullmatch(r"[A-Z][A-Za-z0-9]*", name) or name in RESERVED_NAMES:
            raise IconError(f"Invalid public icon name: {name!r}")
        if not isinstance(source, str) or "\\" in source:
            raise IconError(f"{name}: use an asset-relative forward-slash SVG path")
        relative = PurePosixPath(source)
        if (relative.is_absolute() or ".." in relative.parts or "." in source.split("/")
                or str(relative) != source or relative.suffix != ".svg"
                or not (source.startswith("Monica/") or source.startswith("Lucide/icons/"))):
            raise IconError(f"{name}: unsupported source path {source!r}")
        path = (assets / source).resolve()
        if not path.is_relative_to(assets.resolve()):
            raise IconError(f"{name}: source must stay inside the assets directory")
        result[name] = (source, svg_fragment(path.read_bytes(), source))
    return result


def render_catalog(catalog: dict[str, tuple[str, str]], version: str) -> str:
    lines = [
        "// <auto-generated />",
        "// Run: python scripts/mo_icons.py generate",
        f"// Lucide originals: lucide-static {version}; see Icons/Assets/Lucide/LICENSE.",
        "namespace Monica.UI.Icons;", "",
        "/// <summary>",
        "/// Reviewed, monochrome SVG fragments for MoIcon and 24-unit SVG icon slots.",
        "/// </summary>",
        "/// <remarks>",
        "/// Fragments inherit currentColor and use --mo-icon-stroke-width with a 1.5-unit fallback.",
        "/// They contain no outer SVG element. No registration or runtime resource loading is required.",
        "/// </remarks>",
        "public static class MoIcons", "{",
    ]
    for name, (source, fragment) in catalog.items():
        words = re.sub(r"(?<!^)([A-Z])", r" \1", name).lower()
        lines += [
            "    /// <summary>",
            f"    /// Gets the {words} icon. Source: {html.escape(source)}.",
            "    /// </summary>",
            f'    public static string {name} => """{fragment}""";', "",
        ]
    return "\n".join(lines).rstrip() + "\n}\n"


def theme_palette_css(root: Path) -> str:
    """Snapshot the actual Default theme palette for a standalone, offline gallery."""
    path = root / "Monica.UI/Theming/Definitions/DefaultTheme.cs"
    text = path.read_text(encoding="utf-8")
    blocks = []
    for mode in ("Light", "Dark"):
        match = re.search(rf"Create{mode}Palette\(\) => new\(\)\s*\{{(.*?)\n    \}};", text, re.S)
        if not match:
            raise IconError(f"Cannot read the {mode} palette from {path}")
        values = []
        for name, value in re.findall(r'\b(\w+)\s*=\s*"([^"\n]+)"', match.group(1)):
            token = re.sub(r"(?<!^)([A-Z])", r"-\1", name).lower()
            values.append(f"  --mud-palette-{token}: {value};")
        blocks.append(f':root[data-theme="default-{mode.lower()}"] {{\n' + "\n".join(values) + "\n}")
    return "\n".join(blocks)


def render_gallery(catalog: dict[str, tuple[str, str]], version: str, palette_css: str) -> str:
    def icon(name: str, size: int = 20) -> str:
        return f'<svg width="{size}" height="{size}" viewBox="0 0 24 24" aria-hidden="true" focusable="false">{catalog[name][1]}</svg>'

    cards = []
    for name, (source, _) in catalog.items():
        sizes = "".join(f'<span>{icon(name, size)}<small>{size}px</small></span>' for size in (16, 18, 20, 24))
        cards.append(f'<article data-name="{name.lower()}"><button class="copy" data-copy="MoIcons.{name}" title="Copy MoIcons.{name}"><code>MoIcons.{name}</code>{icon(name)}</button><div class="sizes">{sizes}</div><small>{source}</small></article>')
    examples = "".join(f'<button class="action {kind}">{icon(name)}{label}</button>' for name, label, kind in (
        ("Search", "Search", ""), ("Save", "Save changes", "primary"),
        ("Copy", "Copy", ""), ("Delete", "Delete", "danger")))
    return '''<!doctype html>
<!-- Generated by scripts/mo_icons.py gallery. Palette values come from DefaultTheme.cs. -->
<html lang="en" data-theme="default-light">
<head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>MoIcon · Monica icon catalog</title>
<link rel="icon" href="../../Monica.UI/wwwroot/favicon.ico">
<link rel="stylesheet" href="../../Monica.UI/wwwroot/css/mo-fonts.css">
<link rel="stylesheet" href="../../Monica.UI/wwwroot/css/themes/mo-theme-default.css">
<style>
''' + palette_css + '''
* { box-sizing: border-box; }
body { margin: 0; background: var(--mud-palette-background); color: var(--mud-palette-text-primary); font: 15px/1.5 var(--mo-font-family-body, sans-serif); }
main { max-width: 1400px; margin: auto; padding: 40px 28px; }
header, .controls, .examples, .action, .copy { display: flex; align-items: center; gap: 12px; }
header { justify-content: space-between; flex-wrap: wrap; }
h1 { margin: 0; font-size: 32px; letter-spacing: -.04em; }
p, small { color: var(--mud-palette-text-secondary); }
.controls, .examples { flex-wrap: wrap; margin: 22px 0; }
button, input, select { font: inherit; color: inherit; background: var(--mud-palette-surface); border: 1px solid var(--mud-palette-lines-default); border-radius: 6px; padding: 8px 12px; }
button { cursor: pointer; }
button:hover { border-color: var(--mud-palette-primary); }
button:focus-visible, input:focus-visible, select:focus-visible { outline: 2px solid var(--mud-palette-primary); outline-offset: 3px; }
input { flex: 1; min-width: 180px; }
svg { flex: none; display: inline-block; vertical-align: -.125em; }
.primary { background: var(--mud-palette-primary); color: var(--mud-palette-primary-contrast-text); }
.danger { color: var(--mud-palette-error); }
.disabled { color: var(--mud-palette-action-disabled); }
.examples { padding: 18px; background: var(--mud-palette-surface); border: 1px solid var(--mud-palette-lines-default); border-radius: 8px; }
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 14px; }
article { min-width: 0; padding: 16px; border: 1px solid var(--mud-palette-lines-default); background: var(--mud-palette-surface); border-radius: 8px; }
.copy { width: 100%; padding: 0; border: 0; justify-content: space-between; }
code { font-size: 12px; }
.sizes { display: flex; align-items: end; justify-content: space-between; gap: 12px; padding: 24px 0 18px; }
.sizes span { display: grid; justify-items: center; gap: 12px; }
article > small { display: block; font-size: 11px; overflow-wrap: anywhere; }
footer { margin-top: 24px; color: var(--mud-palette-text-secondary); }
[hidden] { display: none !important; }
</style></head><body><main>
<header><div><h1>MoIcon</h1><p>Monica's shared outline vocabulary · ''' + str(len(catalog)) + ''' curated icons · Lucide ''' + html.escape(version) + '''</p></div><button id="theme" type="button">Switch to dark</button></header>
<div class="controls"><input id="filter" type="search" placeholder="Find an icon" aria-label="Find an icon"><label>Stroke <select id="stroke"><option>1.5</option><option>1.75</option><option>2</option></select></label><span id="count">''' + str(len(catalog)) + ''' icons</span></div>
<div class="examples" aria-label="Usage examples">''' + examples + '<span class="action disabled">' + icon("Refresh") + '''Disabled action</span></div>
<p>Compare the actual 16, 18, 20 and 24px sizes. Click an icon name to copy its C# expression.</p>
<div class="grid">''' + "\n".join(cards) + '''</div>
<footer><span id="feedback" role="status" aria-live="polite">All assets are local. Colors inherit Monica's Default theme palette.</span></footer>
</main><script>
const root = document.documentElement;
document.querySelector('#theme').addEventListener('click', event => {
  const dark = root.dataset.theme === 'default-light';
  root.dataset.theme = dark ? 'default-dark' : 'default-light';
  event.currentTarget.textContent = dark ? 'Switch to light' : 'Switch to dark';
});
document.querySelector('#stroke').addEventListener('change', event => root.style.setProperty('--mo-icon-stroke-width', event.target.value));
document.querySelector('#filter').addEventListener('input', event => {
  const query = event.target.value.toLowerCase().trim();
  let count = 0;
  document.querySelectorAll('article').forEach(card => { card.hidden = !card.dataset.name.includes(query); count += !card.hidden; });
  document.querySelector('#count').textContent = `${count} icons`;
});
document.querySelectorAll('[data-copy]').forEach(button => button.addEventListener('click', async () => {
  const expression = button.dataset.copy;
  try {
    await navigator.clipboard.writeText(expression);
    document.querySelector('#feedback').textContent = `Copied ${expression}`;
  } catch {
    document.querySelector('#feedback').textContent = `Copy this expression: ${expression}`;
  }
}));
</script></body></html>
'''


def outputs(root: Path) -> dict[Path, str]:
    assets = root / ASSETS_PATH
    lock = verify_snapshot(assets)
    catalog = load_catalog(assets)
    return {
        CATALOG_PATH: render_catalog(catalog, lock["version"]),
        GALLERY_PATH: render_gallery(catalog, lock["version"], theme_palette_css(root)),
    }


def write_output(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    importer = commands.add_parser("import-lucide", help="Explicitly download and verify a pinned SVG snapshot")
    importer.add_argument("--version", required=True)
    commands.add_parser("generate", help="Regenerate checked-in C# and the local gallery offline")
    commands.add_parser("check", help="Validate inputs and reject stale generated output offline")
    commands.add_parser("gallery", help="Regenerate only the local developer gallery offline")
    args = parser.parse_args(argv)
    try:
        if args.command == "import-lucide":
            count = import_lucide(ROOT / ASSETS_PATH, args.version)
            print(f"Imported {count} verified Lucide {args.version} SVG originals.")
        else:
            generated = outputs(ROOT)
            if args.command == "gallery":
                generated = {GALLERY_PATH: generated[GALLERY_PATH]}
            stale = []
            for relative, text in generated.items():
                path = ROOT / relative
                if args.command == "check":
                    if not path.is_file() or path.read_bytes() != text.encode("utf-8"):
                        stale.append(str(relative))
                else:
                    write_output(path, text)
                    print(f"Generated {relative}.")
            if stale:
                raise IconError("Stale generated output: " + ", ".join(stale) + "; run generate")
            if args.command == "check":
                print("MoIcon assets, provenance, catalog and gallery are valid and current.")
    except (IconError, OSError, tarfile.TarError, ValueError) as error:
        print(f"[error] {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
