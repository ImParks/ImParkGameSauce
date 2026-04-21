# Phase 3: 건설 + 경제 + 환경 기초 — 개요

## 목표
건설→자원→제작 게임플레이 핵심 루프 완성 + 환경(날씨/계절/온도) 기초 프레임 구축.

## 범위
| 서브페이즈 | 내용 | 담당 에이전트 |
|-----------|------|-------------|
| 3A | Blueprint + 건설 시스템 | building-economy |
| 3B | 자원/인벤토리 | building-economy |
| 3C | 작업대 + 제작 레시피 + 품질 7단계 | building-economy |
| 3D | 구역(Zone) 관리 4종 | building-economy |
| 3E | 작업 우선순위 + Hauling | pawn-ai |
| 3F | 시간대별 활동 스케줄러 | pawn-ai |
| 3G | 온도/날씨/계절 기초 | environment-life |

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
| building-economy | 3A~3D | 17개 | 13개 | 9종 | 가장 큰 범위 |
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
