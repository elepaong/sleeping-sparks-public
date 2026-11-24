#!/usr/bin/env bash
# Unity 없이 게임 화면을 확인한다: 게임 규칙(Core)으로 장면을 만들고(Snapshot.cs), GameView와 같은 규칙으로 그린다(snapshot_render.py).
# 결과: design/refs/screens/<장면>.png (1920×1080). 필요: mono(mcs), Pillow, 한글 글꼴(fonts-nanum).
set -e
cd "$(dirname "$0")/../.."
OUT="${TMPDIR:-/tmp}/sparks-snapshot"; mkdir -p "$OUT" design/refs/screens
mcs -nologo -out:"$OUT/snapshot.exe" $(find unity/Assets/Scripts/Sparks/Core -name '*.cs') tools/unity/Snapshot.cs
mono "$OUT/snapshot.exe" . "$OUT"
for f in "$OUT"/*.json; do
  python3 tools/unity/snapshot_render.py "$f" "design/refs/screens/$(basename "${f%.json}").png"
done
