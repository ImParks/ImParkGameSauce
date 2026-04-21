# Phase 3: 건설 + 경제 + 환경 기초 — 개요

## 목표
건설→자원→제작 게임플레이 핵심 루프 완성 + 환경(날씨/계절/온도) 기초 프레임 구축.

## 범위
| 서브페이즈 | 내용 | 담당 에이전트 |
|-----------|------|-------------|
| 3A | Blueprint + 건설 + 방(Room) 인식 | building-economy |
| 3B | 자원/인벤토리 | building-economy |
| 3C | 작업대 + 제작 레시피 + 품질 7단계 | building-economy |
| 3D | 구역(Zone) 관리 4종 | building-economy |
| 3E | 작업 우선순위 + Hauling | pawn-ai |
| 3F | 시간대별 활동 스케줄러 | pawn-ai |
| 3G | 온도/날씨/계절 기초 | environment-life |

## 원안 대비 스펙 재편성

원안(`phases.md`) Phase 3 항목 중 일부를 다음 사유로 재편성합니다:

| 원안 | 현 계획 | 사유 |
|------|---------|------|
| 3A 건물+Room | 3A 유지 | Room 플러드 필 포함 |
| 3B 구역 4종 | → 3D로 이동 | 자원/인벤토리(3B)를 먼저 구축해야 구역 필터가 동작 |
| 3C 자원+품질 | → 3B(자원)+3C(제작+품질) 분할 | SRP: 자원 스택과 제작 레시피를 분리 |
| 3D 빌 큐 | → 3C에 통합 | 빌 큐는 제작 시스템의 일부 |
| **3E 관계** | → **Phase 4G로 이관** | 관계 시스템은 사회적 상호작용(Phase 4 mood/breakdown)과 밀접. 건설/경제 루프에 선행 불필요 |
| **3F 레크리에이션** | → **Phase 4G로 이관** | 오락 관용도는 mood/breakdown의 하위 시스템. 기초 경제 루프 구축이 우선 |
| 3E(신) 작업 우선순위 | 신규 | Phase 2 NoOpJobGiver를 실구현으로 교체하는 핵심 시스템. 건설/제작 Job 소비에 필수 |
| 3F(신) 스케줄러 | 신규 | 작업 우선순위와 결합하여 24시간 활동 관리. 건설/경제 루프 완성에 필요 |
| 3G 온도 | 3G 축소 | 타일별 열전도는 Room(3A) 이후 Phase 4C로. 3G는 월드 레벨 기초만 |

## 신규 어셈블리
- **ImPark.Building** — 건설/경제 Bounded Context (RULE-005/006)
  - refs: ImPark.Shared, ImPark.Core
  - domain/, application/, infrastructure/, contracts/

## Phase 의존성 DAG
```
Phase 1 (완료) ──► Phase 3A/3B (건설/자원 기반)
Phase 2 (완료) ──► Phase 3E/3F (AI Job 확장)
                    Phase 3A ──► 3C (작업대는 건물)
                    Phase 3B ──► 3D (구역은 자원 저장)
                    Phase 3A~3D ──► 3E (Job 소스 필요)
Phase 1 (완료) ──► Phase 3G (환경, 독립)
```

## 병렬 실행 전략
- **그룹 1** (병렬): 3A+3B+3G — 상호 의존 없음
- **그룹 2** (그룹1 후): 3C+3D — 3A/3B 인터페이스 소비
- **그룹 3** (그룹2 후): 3E+3F — 모든 Job 소스 준비 후

## 에이전트 부하 분배
| 에이전트 | 서브페이즈 | 컴포넌트 | System | Def | 비고 |
|---------|----------|---------|--------|-----|------|
| building-economy | 3A~3D | 18개 | 14개 | 9종 | 가장 큰 범위 (+Room) |
| pawn-ai | 3E~3F | 3개 | 3개 | 5종 | Phase 2 확장 |
| environment-life | 3G | 4개 | 3개 | 3종 | 첫 구현 |

## Phase 2 소비 인터페이스
- IPawnQuery (Idle 폰 검색, WorkTypeFilter)
- IReservationService (자원/건물/작업대 점유)
- IJobGiver / IJobDriver (Job 할당/실행 계약)
- IBTLeafRegistry (신규 Leaf 등록)
- IPathRequestService (경로 요청, IsReachable)
- ITilemapQuery (배치 검증, 야외 판정)
- IEventBus (sync/async 이벤트)
- DefDatabase (Def 조회)
- IRandomService (결정적 RNG)
- ModuleRegistry / IGameModule (모듈 ON/OFF)
