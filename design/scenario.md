# Scenario 이미지 생성 도구

이미지 생성은 Scenario MCP 커넥터로 하고, [scenario-labs/skills](https://github.com/scenario-labs/skills)는 그 사용법 안내서입니다. 무료 플랜에서도 OAuth 연결로 생성되는 것을 확인했습니다 (2026-11-06).

## 작업 공간
- 팀: 'Ele Paong's Organization' (Free 플랜), 프로젝트: 'Default Project'
- 생성물은 Scenario 계정에 저장됩니다. 다른 Claude 계정이라도 같은 Scenario 계정으로 로그인하면 그대로 보입니다.

## 연결
- **Claude 앱:** Customize → Connectors → Add custom connector에 `https://mcp.scenario.com/mcp`를 넣고 OAuth로 로그인합니다. Claude 무료 플랜은 커스텀 커넥터를 1개까지 쓸 수 있습니다. 연결 후에는 새 채팅에서 요청합니다.
- **Claude Code:**

```bash
npx skills add scenario-labs/skills
```

```bash
claude mcp add --transport http scenario https://mcp.scenario.com/mcp
```

## 무료 플랜 한계

| 항목 | 내용 |
|---|---|
| 크레딧 | 하루 50 CU, 한국 시간 오전 9시(00:00 UTC) 초기화, 이월 없음 |
| 동시 생성 | 2개 |
| 불가 | 추가 CU 구매, Relax 모드 |
| 유료 전용 | API 키, 벡터화·픽셀화·텍스처 생성·에셋 메뉴의 배경 제거 (Starter 이상), 캔버스·모델 학습 (Pro 이상) |
| 투명 배경 | GPT Image의 `background: transparent` 옵션으로 대체 |

## 실측 비용

| 모델 | 설정 | 비용 |
|---|---|---|
| GPT Image 2.5 Flare | medium, 1024×1024 | 장당 3 CU |
| Retro Diffusion Tile | 타일셋 1회 | 15 CU |

## 운영 원칙
- 생성 전에 남은 CU와 예상 비용을 먼저 확인합니다.
- **상업적 이용:** 무료 플랜 결과물은 개인·평가용이고, 업그레이드해도 소급되지 않습니다. 게임에 쓸 그림은 유료 플랜에서 다시 생성하고, 지금 시안은 레퍼런스로만 씁니다.
- **목표 수준:** 무료 단계에서는 그림체를 정하는 컨셉 시안까지. 32×32 타일과 24×24 스프라이트 완성본은 유료 도구가 필요합니다.

## 출처
- [Scenario MCP 설정 문서](https://mcp.scenario.com/docs)
- [Scenario 요금제](https://help.scenario.com/en/articles/pricing-plans/)
- [scenario-labs/skills](https://github.com/scenario-labs/skills)
