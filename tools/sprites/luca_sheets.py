#!/usr/bin/env python3
"""PixelLab 루카 애니메이션(72×72 칸, 8방향)을 게임 규격 시트로 조립한다 (2026-11-21).

입력: PixelLab 캐릭터 내려받기 zip을 푼 폴더 (Idle/animations/{walk,idle}/<방향>/frame_00N.png)
  캐릭터 79189c5c-9719-415c-9f11-8b0a6b3eaa14 "Luca pro concept 72", 걷기 = walking-4-frames, 대기 = breathing-idle (각 4프레임)
걷기 8프레임 (2026-11-24, 사람이 B안 채택): PixelLab animate_character mode="pixminimax", 8프레임 — 몸 전체가 오르내리고 흔들리는 걷기.
  76×76 칸, frame_000은 기준 그림이고 1~8이 걷기. 보관: design/refs/pixellab_2026-11-24/luca_walk8/<방향>/frame_00N.png
  오른쪽 5방향(아래·아래오른쪽·오른쪽·위오른쪽·위)만 뽑고 왼쪽 셋은 오른쪽 칸을 좌우 뒤집는다.
  아직 없는 방향은 옛 4프레임 걷기를 프레임마다 두 번씩 써서 임시로 메운다(실행 결과에 알림).
출력: assets/chars/luca_walk.png (384×576, 8프레임), luca_idle.png (96×576, 대기 4프레임 중 0·2번)
규칙 (CLAUDE.md 에셋 규격): 칸 48×72, 세로 8방향은 아래·아래오른쪽·오른쪽·위오른쪽·위·위왼쪽·왼쪽·아래왼쪽.
72×72 칸의 가운데 48폭을 자르고, 발끝이 칸 아래에서 3px 위(GameView.FootMargin)에 오도록 모두 같은 만큼 내린다.
쓰는 법: python3 tools/sprites/luca_sheets.py [푼 폴더] (없으면 보관본 design/refs/pixellab_2026-11-21/luca_anim)
"""
import pathlib
import sys

from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
ORDER = ["south", "south-east", "east", "north-east", "north", "north-west", "west", "south-west"]
FW, FH = 48, 72
FOOT_MARGIN = 3
WALK8 = ROOT / "design/refs/pixellab_2026-11-24/luca_walk8"
MIRROR = {"north-west": "north-east", "west": "east", "south-west": "south-east"}


def load(src, anim, d, i):
    base = src / "Idle/animations" if (src / "Idle/animations").exists() else src
    im = Image.open(base / anim / d / f"frame_{i:03d}.png").convert("RGBA")
    im.putalpha(im.getchannel("A").point(lambda v: 255 if v > 100 else 0))
    return im


def main():
    src = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "design/refs/pixellab_2026-11-21/luca_anim"
    frames = {(a, d, i): load(src, a, d, i) for a in ("walk", "idle") for d in ORDER for i in range(4)}
    # 2026-11-21 받은 걷기의 잘못 나온 곳을 메운다 (좌우 뒤집기, 가방 자리도 뒤집힘)
    #  - 아래(south) 4번째 프레임이 뒷모습 → 2번째 프레임을 뒤집어 반대 다리
    #  - 아래오른쪽(south-east) 줄만 망토·장식이 다른 옷 → 아래왼쪽 줄을 뒤집어 씀
    flip = lambda im: im.transpose(Image.FLIP_LEFT_RIGHT)
    frames[("walk", "south", 3)] = flip(frames[("walk", "south", 1)])
    for i in range(4):
        frames[("walk", "south-east", i)] = flip(frames[("walk", "south-west", i)])
    # 방향마다 '서 있는 발'(대기 0번 프레임의 맨 아래 줄)이 칸 아래에서 FOOT_MARGIN 위에 오게 내린다.
    # 모든 프레임의 가장 아래 줄로 맞추면 걷기에서 길게 뻗은 발 때문에 서 있을 때 발이 그림자 위로 뜬다 (2026-11-21).
    dys = {d: (FH - FOOT_MARGIN) - frames[("idle", d, 0)].getbbox()[3] for d in ORDER}
    x0 = (72 - FW) // 2

    def sheet(anim, picks):
        out = Image.new("RGBA", (FW * len(picks), FH * len(ORDER)), (0, 0, 0, 0))
        for r, d in enumerate(ORDER):
            for c, i in enumerate(picks):
                cell = Image.new("RGBA", (FW, FH), (0, 0, 0, 0))
                cell.alpha_composite(frames[(anim, d, i)].crop((x0, 0, x0 + FW, 72)), (0, dys[d]))
                out.alpha_composite(cell, (c * FW, r * FH))
        return out

    sheet("idle", [0, 2]).save(ROOT / "assets/chars/luca_idle.png")
    old_walk = sheet("walk", [0, 1, 2, 3])

    # 걷기 8프레임: 기준 그림(frame_000)의 발끝·가운데를 대기 0번 프레임에 맞춘다 → 걷는 동안의 오르내림은 그대로 남는다
    def walk8_cell(d, i):
        ref = load(WALK8, "", d, 0).getbbox()
        idle0 = frames[("idle", d, 0)].getbbox()
        x = round((idle0[0] + idle0[2]) / 2 - x0 - (ref[0] + ref[2]) / 2)
        y = (FH - FOOT_MARGIN) - ref[3]
        cell = Image.new("RGBA", (FW, FH), (0, 0, 0, 0))
        cell.alpha_composite(load(WALK8, "", d, i).crop((max(0, -x), max(0, -y), 76, 76)), (max(0, x), max(0, y)))
        return cell

    walk = Image.new("RGBA", (FW * 8, FH * len(ORDER)), (0, 0, 0, 0))
    temp = []
    for r, d in enumerate(ORDER):
        src, flip_ = MIRROR.get(d, d), d in MIRROR
        if (WALK8 / src / "frame_008.png").exists():
            cells = [walk8_cell(src, i) for i in range(1, 9)]
            cells = [c.transpose(Image.FLIP_LEFT_RIGHT) for c in cells] if flip_ else cells
        else:
            temp.append(d)
            cells = [old_walk.crop((c * FW, r * FH, (c + 1) * FW, (r + 1) * FH)) for c in (0, 0, 1, 1, 2, 2, 3, 3)]
        for c, cell in enumerate(cells):
            walk.alpha_composite(cell, (c * FW, r * FH))
    walk.save(ROOT / "assets/chars/luca_walk.png")
    print("방향별로 발끝을", sorted(set(dys.values())), "px 내림 → luca_walk.png 384×576 (8프레임), luca_idle.png 96×576")
    if temp:
        print("  ! 8프레임 걷기가 아직 없어 옛 4프레임을 두 번씩 쓴 방향:", ", ".join(temp))


if __name__ == "__main__":
    main()
