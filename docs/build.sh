#!/usr/bin/env bash
# Regenerates the parts of the docs that come from the code: the example images
# (docs/images, from docs/tools/DocImages) and the API reference (docs/api, from
# the XML documentation comments via DocFX). Run from anywhere; needs the .NET 10
# SDK and Python 3. The docs workflow runs this and fails if the result differs
# from what is committed.
set -euo pipefail

DOCS="$(cd "$(dirname "$0")" && pwd)"
RAW="$(mktemp -d)"
trap 'rm -rf "$RAW"' EXIT

cd "$DOCS"
rm -rf images
dotnet run --configuration Release --project tools/DocImages -- "$DOCS/images"

dotnet tool restore
dotnet docfx metadata docfx.json --output "$RAW"
# docfx.json's metadata.dest is relative to --output.
python3 tools/api-markdown.py "$RAW/api" "$DOCS/api"
