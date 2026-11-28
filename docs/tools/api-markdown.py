#!/usr/bin/env python3
"""Turns DocFX's Markdown metadata into the API reference pages under docs/api.

DocFX (`docfx metadata` with "outputFormat": "markdown") writes one page per type, named by
UID, and leaves cross-references as <xref> tags for a DocFX build to resolve. This script:

- names each page after its type in lower case (BarcodeBuilder -> barcodebuilder.md) and turns
  the namespace page into index.md, below the curated introduction in docs/tools/api-index.md;
- resolves <xref> tags into links (to these pages, or to learn.microsoft.com for System types);
- drops the "Inherited Members" lists of System.Object members, which every type repeats.

The result is plain Markdown that renders on GitHub and that the terrafluent.dev mirror script
can copy as is.

Usage: python3 docs/tools/api-markdown.py <docfx-output-dir> <docs/api>
"""
import html
import pathlib
import re
import shutil
import sys

NAMESPACE = "TerraFluent.Barcode"

src = pathlib.Path(sys.argv[1])
dest = pathlib.Path(sys.argv[2])
intro = (pathlib.Path(__file__).parent / "api-index.md").read_text(encoding="utf-8")

# UID of each type page -> new file name.
types = {}
for path in src.glob(f"{NAMESPACE}.*.md"):
    uid = path.stem
    types[uid] = uid[len(NAMESPACE) + 1:].lower() + ".md"
pages = dict(types)
pages[NAMESPACE] = "index.md"


def anchor(uid: str) -> str:
    return re.sub(r"[^A-Za-z0-9]", "_", uid)


def link(text: str, href: str) -> str:
    # An HTML link, not Markdown: DocFX puts <remarks> in <p> blocks, where Markdown is not parsed.
    return f'<a href="{href}">{html.escape(text)}</a>'


def resolve(uid: str) -> str:
    uid = html.unescape(uid).replace("%2c", ",").replace("%2C", ",")
    if uid.startswith("System."):
        name = uid.split("(")[0]
        return link(name.rsplit('.', 1)[-1], f"https://learn.microsoft.com/dotnet/api/{name.lower()}")
    if uid in pages:
        return link(uid.rsplit('.', 1)[-1], pages[uid])
    # A member: the longest type UID that prefixes it owns the page.
    owner = max((t for t in types if uid.startswith(t + ".")), key=len, default=None)
    if owner is None:
        return f"`{uid}`"
    member = uid[len(owner) + 1:].split("(")[0]
    if member == "#ctor":
        member = owner.rsplit(".", 1)[-1]
    text = f"{owner.rsplit('.', 1)[-1]}.{member}"
    return link(text, f"{types[owner]}#{anchor(uid)}")


xref = re.compile(r'<xref href="([^"]+)"[^>]*>\s*</xref>')
page_link = re.compile(r"\]\((" + re.escape(NAMESPACE) + r"(?:\.[A-Za-z0-9]+)*)\.md(#[^)]*)?\)")
inherited = re.compile(r"#### Inherited Members\n.*?(?=\n#|\Z)", re.S)
ms_url = re.compile(r"\]\((https://learn\.microsoft\.com[^)\s]*?)\)(?=[,\s]|$)", re.M)


def convert(text: str) -> str:
    text = xref.sub(lambda m: resolve(m.group(1)), text)
    text = page_link.sub(lambda m: f"]({pages.get(m.group(1), m.group(1) + '.md')}{m.group(2) or ''})", text)
    text = inherited.sub("", text)
    # DocFX escapes Markdown characters inside URLs; GitHub and kramdown would keep the backslashes.
    text = ms_url.sub(lambda m: "](" + re.sub(r"\\(.)", r"\1", m.group(1)) + ")", text)
    return re.sub(r"\n{3,}", "\n\n", text).rstrip() + "\n"


if dest.exists():
    shutil.rmtree(dest)
dest.mkdir(parents=True)

for uid, name in pages.items():
    text = convert((src / f"{uid}.md").read_text(encoding="utf-8"))
    if name == "index.md":
        # Keep the namespace page's type lists, under the curated introduction.
        text = intro.rstrip() + "\n\n" + re.sub(r"^# .*\n", "", text, count=1).lstrip()
    else:
        text = re.sub(r"^# <a id=\"[^\"]*\"></a> ", "# ", text, count=1)
        if text.startswith("# Enum "):
            # DocFX gives enum fields no anchors, so links to a value would have nowhere to land.
            text = re.sub(r"^`(\w+) = ", lambda m: f'<a id="{anchor(uid + "." + m.group(1))}"></a>`{m.group(1)} = ', text, flags=re.M)
    (dest / name).write_text(text, encoding="utf-8")

print(f"Wrote {len(pages)} API pages to {dest}")
