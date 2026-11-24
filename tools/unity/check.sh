#!/usr/bin/env bash
# Unity 없이 하는 점검 (mono의 mcs 필요: apt-get install mono-mcs mono-runtime).
#  1) 게임 규칙(Core)만 컴파일해 처음부터 끝까지 자동 시험을 돌린다.
#  2) 화면 코드와 숲 프로토타입까지 Unity API 껍데기(stubs)에 대고 컴파일해 문법·형식 오류를 잡는다
#     (옛 입력 방식과 새 Input System 양쪽으로).
set -e
cd "$(dirname "$0")/../.."
OUT="${TMPDIR:-/tmp}/sparks-check"; mkdir -p "$OUT"
CORE=$(find unity/Assets/Scripts/Sparks/Core -name '*.cs')
ALL=$(find unity/Assets/Scripts -name '*.cs')
mcs -nologo -out:"$OUT/playthrough.exe" $CORE tools/unity/Playthrough.cs
mono "$OUT/playthrough.exe" .
mcs -nologo -nowarn:0649,0169,0414,0219,0660,0661 -target:library -out:"$OUT/all.dll" $ALL tools/unity/stubs/UnityEngine.cs
mcs -nologo -nowarn:0649,0169,0414,0219,0660,0661 -d:ENABLE_INPUT_SYSTEM -target:library -out:"$OUT/all_newinput.dll" $ALL tools/unity/stubs/UnityEngine.cs
echo "COMPILE OK (화면 코드 · 숲 프로토타입, 입력 방식 2종)"
