# 잠든 스파크의 수도원 (Sleeping Sparks)

> **ポートフォリオ公開用のリポジトリです。** 開発中のリポジトリから、作業用の AI ツール類（`.claude/`）を除いた内容を公開しています。

## 概要（日本語）

魔女として処刑された科学者が遺した、物理現象の生き物「スパーク」を、封印パズルを解いてひとつずつ目覚めさせるパズルアドベンチャーです。レイトン教授シリーズのような「扉パズル」で光・磁気・静電気・熱などの原理を学び、手に入れたスパークを使ってフィールドの空間パズルを解いていきます。

| 項目 | 内容 |
| --- | --- |
| ジャンル | パズルアドベンチャー ＋ 収集（トップダウン 2D ドット） |
| エンジン | Unity / C#（Godot で試作したあと移行） |
| 現在の段階 | 企画・プロトタイプ（プロローグ＋第1章） |

### 見どころ

- **企画書から作る：** 世界観・ステージ構成・パズルの方針を [`design/plan.md`](design/plan.md) にまとめ、実装はその文書に沿って進めています。決めたことは必ず `design/` か `data/` に残すルールです。
- **データ駆動の設計：** パズルの問題・会話・配置・スパークと物の反応はすべて `data/` の JSON にあり、コードには書いていません。スパークの反応は物理エンジンではなく反応表（`data/interactions.json`）で決めるので、表にない組み合わせは起きず、パズルの答えを設計どおりに保てます。
- **パズルを自動で検証：** フィールドパズルは全パターンを計算して答えがひとつだけかを確認し（`tools/verify/field_check.py`）、扉パズルも問題ごとの検証スクリプトで答えを確かめています。科学的な内容は現実と同じ現象だけを使い、別の作業で事実確認をしてから採用します。
- **ルールと画面を分けた構成：** ゲームのルール（`Core`）は Unity に依存しないように書き、画面（`View`）と分けました。そのため Unity を開かなくても、最初から最後までの自動プレイテストとコンパイルチェックが回ります（`tools/unity/check.sh`）。
- **アセットの管理：** ドット絵の規格（サイズ・色数・方向）を表で決めてスクリプトで検査し（`tools/verify/asset_check.py`）、画像生成ツールごとに商用利用の条件を確認して [`design/licenses.md`](design/licenses.md) に記録しています。

### 開発の進め方

AI（Claude）を活用して実装を進め、何を作るか・どう設計するかの判断は自分で行いました。

---

## 소개 (한국어)

마녀로 몰려 죽은 과학자가 숨겨둔 **스파크**(물리 현상의 생명체)를, 봉인 퍼즐을 풀어 하나씩 깨우는 퍼즐 어드벤처.
레이튼식 문 퍼즐로 원리를 배우고, 얻은 스파크로 필드의 공간 퍼즐을 풀며, 발명품으로 연구실을 키워 다음 지역을 연다.

| 항목 | 내용 |
|---|---|
| 장르 | 퍼즐 어드벤처 + 수집 (하프 오픈월드, 탑다운 2D) |
| 플랫폼 | PC(Steam) 우선 |
| 엔진 | Unity 2021.3 LTS 이상 / C# (2026-11-16에 Godot에서 바꿈 — [`design/unity.md`](design/unity.md)) |
| 규모 | 지역 7 + 허브, 스파크 20~30종, 문 퍼즐 100+, 필드 퍼즐 60+ |

> 기획은 **초안** 단계입니다. 기획서: [`design/plan.md`](design/plan.md)

## 현재 단계
**0단계 — 기획·프로토타입.** 목표는 관문 1: "문 퍼즐과 스파크 배치가 이어지는 루프가 재밌다"를 프롤로그+1장 프로토타입(방 3개, 연구실은 스크롤되는 큰 맵)으로 확인하는 것.

지금 있는 것과 다음 할 일은 [`design/progress.md`](design/progress.md) 맨 위에 있습니다. 이 README에는 진행 상황을 적지 않습니다.

## 실행
**Unity판 (프롤로그+1장):** `unity/Assets/` 안의 내용을 Unity 2D 프로젝트의 `Assets/`로 복사하고 Play. 구조·실행·점검은 [`design/unity.md`](design/unity.md), 조작·정답·확인 순서는 [`design/ch1-prototype.md`](design/ch1-prototype.md) 10절. Unity 없이 도는 자동 시험: `bash tools/unity/check.sh`.

**Godot 프로토타입(참고용):** Godot 4.3 이상에서 저장소 폴더를 프로젝트로 엽니다. 시작 장면은 프롤로그+1장 프로토타입 `scenes/main.tscn`입니다. 조작·정답·확인 순서는 [`design/ch1-prototype.md`](design/ch1-prototype.md) 10절. 자동 시험은 `godot --headless --path . res://scenes/test/playthrough.tscn`. 예전 조합 시험 장면은 `scenes/sandbox/archive_test.tscn`에 남아 있습니다.

## 점검
```bash
bash tools/unity/check.sh             # Unity판: 처음부터 끝까지 자동 시험 + 컴파일 점검 (mono 필요)
python3 tools/unity/sync.py           # data/·assets/ 를 Unity 쪽으로 옮기기 (고친 뒤에)
python3 tools/verify/asset_check.py   # assets/ 그림이 규격표와 맞는지 (Pillow 필요)
python3 tools/verify/data_check.py    # data/ JSON이 서로 맞물리는지
python3 tools/verify/field_check.py   # 필드 퍼즐을 전수 계산해 정답이 하나인지
for f in tools/verify/D-*.py; do python3 "$f"; done   # 문 퍼즐 검증 스크립트
```

## 문서
| 문서 | 내용 |
|---|---|
| [`CLAUDE.md`](CLAUDE.md) | 프로젝트 규칙, 에셋 규격표 (모든 세션이 먼저 읽음) |
| [`design/progress.md`](design/progress.md) | 현재 상태, 다음 할 일, 작업 로그 |
| [`design/plan.md`](design/plan.md) | 기획서 (초안 원본은 [`plan-v0-draft.pdf`](design/plan-v0-draft.pdf)) |
| [`design/art-direction.md`](design/art-direction.md) | 그림체 방향, 프롬프트, 시안과 채택 기록 |
| [`design/background-prompts.md`](design/background-prompts.md) | 배경 제작 계획, 필드 화면 방식, 광원 위치 |
| [`design/licenses.md`](design/licenses.md) | 그림 도구별 사용 조건, 에셋 출처 |
| [`design/chickxel.md`](design/chickxel.md) | 도트 편집기 Chickxel 사용설명서 |
| [`design/local-generation.md`](design/local-generation.md) | 로컬 이미지 생성 (ComfyUI·Draw Things) |
| [`design/scenario.md`](design/scenario.md) | Scenario 연결과 무료 플랜 한계 |

## 저장소 구조
```
sleeping-sparks/
├─ CLAUDE.md              ← 프로젝트 규칙 (모든 세션이 먼저 읽음)
├─ unity/                 ← Unity판 (Assets, 그림·맵 생성기)
├─ project.godot          ← Godot 프로토타입 (참고용)
├─ design/                ← 기획 문서, progress.md
│  └─ refs/               ← 시안·채택 원본 이미지
├─ data/
│  ├─ sparks.json         ← 스파크 목록
│  ├─ interactions.json   ← 스파크 × 사물 상호작용표
│  ├─ inventions.json     ← 발명품 목록·해금 대상
│  ├─ puzzles/            ← 문 퍼즐 JSON (하나에 하나)
│  ├─ dialogue/           ← 대사
│  ├─ strings/            ← 문자열 테이블 (ko.json)
│  └─ zones/              ← 지역별 배치·필드 퍼즐
├─ scenes/  scripts/      ← Godot 장면과 스크립트 (main = 프로토타입, test = 자동 시험, sandbox = 옛 시험 장면)
├─ assets/                ← 게임에 쓰는 이미지 (임시 → 정식 교체)
├─ tools/verify/          ← 규격·데이터 점검 스크립트
└─ tools/comfyui/  tools/drawthings/  ← 로컬 이미지 생성 스크립트
```
