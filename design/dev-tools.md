# Claude Code 작업 도구 (스킬·GSD)

> 공개용 저장소에는 `.claude/` 폴더(작업 도구)가 들어 있지 않습니다. 이 문서는 개발 저장소에서의 설정 기록입니다.

2026-11-08 설치·검증. 모두 프로젝트 폴더 `.claude/`에 들어 있어 저장소를 받으면 같이 따라온다.

| 도구 | 위치 | 출처 (공식) | 용도 |
|---|---|---|---|
| agent-browser (스킬) | `.claude/skills/agent-browser` | vercel-labs/agent-browser | 화면 검증 — 웹 화면 열기·클릭·스크린샷 |
| agent-browser (CLI) | 전역 npm `agent-browser@0.41.2` | npm `agent-browser` | 스킬이 부르는 실제 프로그램 |
| find-skills | `.claude/skills/find-skills` | vercel-labs/skills | 필요한 스킬 찾기·설치 |
| GSD Core 1.20.1 | `.claude/commands/gsd-*.md` 등 72개 명령, `.claude/agents`, `.claude/hooks`, `.claude/gsd-core`, 훅 설정 `.claude/settings.local.json` | open-gsd/gsd-core (npm `@opengsd/gsd-core`) | 장기 작업을 단계(phase)로 나눠 계획·실행·검증 |
| design-taste-frontend | `.claude/skills/design-taste-frontend` (약 40MB, 예시 사진 포함) | Leonxlnx/taste-skill | 웹 화면 디자인 품질 |
| mcp-builder | `.claude/skills/mcp-builder` | anthropics/skills | MCP 서버 제작 가이드·평가 스크립트 |

스킬 버전 고정은 `skills-lock.json`. 스킬 원본은 고치지 않는다 (업데이트는 `npx skills update -p`).

## 설치 명령 (다시 깔 때)
```bash
npx skills add vercel-labs/agent-browser --skill agent-browser --agent claude-code --copy -y
npx skills add vercel-labs/skills --skill find-skills --agent claude-code --copy -y
npx skills add https://github.com/Leonxlnx/taste-skill --skill design-taste-frontend --agent claude-code --copy -y
npx skills add anthropics/skills --skill mcp-builder --agent claude-code --copy -y
npx @opengsd/gsd-core@latest --claude --local
npm install -g agent-browser && agent-browser install
```

## 환경별 주의
- **클라우드 세션**: Chrome 다운로드 서버가 막혀 있어 `agent-browser install`은 실패한다. 대신 환경 변수 `AGENT_BROWSER_EXECUTABLE_PATH=/opt/pw-browsers/chromium`(설치된 Chromium)을 쓴다. 클라우드 환경 `sleeping-sparks`에 이 변수, `skills.sh` 허용, CLI 설치 setup script를 넣어 두었다.
- **클라우드 세션의 `npx skills find`**: Node 내장 fetch가 프록시를 안 써서 결과가 비어 나온다. `NODE_USE_ENV_PROXY=1 npx skills find <검색어>`로 실행한다.
- **Node 버전**: agent-browser와 GSD Core는 Node 24 이상을 요구한다. 지금 클라우드는 Node 22라 경고가 나지만, 시험한 기능(브라우저 열기·스냅샷·스크린샷, gsd-tools, 비밀 파일 차단 훅)은 동작했다. 문제가 생기면 Node 24로 올린다.
- **GSD 훅**: `.claude/settings.local.json`에 훅이 등록돼 있다. `.env`·`.secrets` 읽기를 막는 훅도 포함. 훅이 작업을 방해하면 `/gsd-settings`로 끄거나 이 파일에서 해당 훅을 뺀다.
- 설치 후 Claude Code를 다시 시작해야 새 스킬·명령이 보인다.

## 부르는 법 (예시)
- 화면 검증: "agent-browser로 `tools/verify/index.html` 열고 스크린샷 찍어 줘" / 직접: `agent-browser open <url>` → `agent-browser snapshot -i` → `agent-browser screenshot out.png` → `agent-browser close`
- 스킬 찾기: "Godot용 스킬 있는지 find-skills로 찾아 줘" / 직접: `npx skills find godot`
- 장기 작업: `/gsd-onboard`(기존 저장소 시작) → `/gsd-discuss-phase` → `/gsd-plan-phase` → `/gsd-execute-phase` → `/gsd-verify-work`, 현황은 `/gsd-progress`, 도움말 `/gsd-help`
- 디자인: "design-taste-frontend로 이 소개 페이지 다듬어 줘" (스킬 범위는 랜딩·소개 페이지 위주, 게임 내 UI는 아님)
- MCP 서버: "mcp-builder로 퍼즐 JSON 검증용 MCP 서버 만들어 줘"

## Godot 조합 시험 프로젝트 (2026-11-09)
- 저장소 루트가 Godot 4.3+ 프로젝트다(`project.godot`). 시작 장면 `scenes/sandbox/archive_test.tscn`, 스크립트 `scripts/sandbox/archive_test.gd`.
- 화면: 가상 해상도 640×360, 창 1920×1080 정수 배율, 텍스처 필터 Nearest(도트 선명).
- 내용: 서고 배경 + 루카 8방향 걷기/대기 + 찌릿·루미가 루카를 따라다님. 1/2로 발동, F1로 이동 영역(`WALK_POLY`) 표시.
- 조작: 방향키/WASD 이동, 1 찌릿 발동, 2 루미 발동, F1 이동 영역.
- 클라우드 세션에는 Godot가 깔려 있지 않지만, GitHub 릴리스에서 Linux판(`Godot_v4.7.2-stable_linux.x86_64.zip`)을 받아 헤드리스로 자동 시험을 돌릴 수 있다 (2026-11-10 확인: `--headless --path . --import` 뒤 `res://scenes/test/playthrough.tscn`). 창을 띄워 눈으로 보는 확인은 여전히 사람 몫이다. 조합 시험 장면은 2026-11-09에 파서 오류 1건(`WALK_POLY` 상수 → 변수)을 고쳤고, 그 뒤 정상 실행 여부는 기록이 없다. 사람이 열어 보고 결과를 `progress.md`에 남길 것.
- 캐릭터·스파크 배율 상수(`scripts/core/config.gd`의 `CHAR_SCALE`·`SPARK_SCALE`)는 1.0으로 확정했다 (2026-11-10). 정수가 아닌 배율은 도트가 고르지 않게 보이므로 쓰지 않는다.
- 시험 장면은 키와 화면 문장을 코드에 직접 적었다. 정식 장면으로 옮길 때 입력 맵과 문자열 테이블을 쓴다.
- 아직 없는 것: 가림 레이어(앞쪽 난간·책), 가구 충돌(이동 영역 다각형 하나로 대신함), 불꽃·광원.
