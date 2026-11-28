#!/usr/bin/env python3
"""Checks that the regenerated PNGs in docs/images show the same pixels as the committed ones.

PNG bytes depend on the zlib build behind System.IO.Compression (the .NET in Ubuntu's archive
uses the system zlib, Microsoft's builds bundle zlib-ng), so the same image can compress to
different bytes on different machines. This compares the decompressed image data instead.
Standard library only.

Usage: python3 docs/tools/png-pixels-equal.py   (from the repository root, after docs/build.sh)
"""
import subprocess
import sys
import zlib


def pixels(data: bytes) -> tuple:
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a PNG")
    pos, header, idat, other = 8, b"", bytearray(), []
    while pos < len(data):
        length = int.from_bytes(data[pos:pos + 4], "big")
        kind = data[pos + 4:pos + 8]
        body = data[pos + 8:pos + 8 + length]
        pos += 12 + length
        if kind == b"IHDR":
            header = body
        elif kind == b"IDAT":
            idat += body
        elif kind != b"IEND":
            other.append((kind, body))  # PLTE, tRNS, ...: stored, not compressed
    return header, tuple(other), zlib.decompress(bytes(idat))


changed = subprocess.run(
    ["git", "diff", "--name-only", "--", "docs/images/*.png"],
    check=True, capture_output=True, text=True).stdout.split()
failures = 0
for path in changed:
    committed = subprocess.run(["git", "show", f"HEAD:{path}"], check=True, capture_output=True).stdout
    with open(path, "rb") as f:
        regenerated = f.read()
    if pixels(committed) == pixels(regenerated):
        print(f"same pixels, different compression: {path}")
    else:
        print(f"::error file={path}::{path} shows different pixels from the committed image")
        failures += 1
sys.exit(1 if failures else 0)
