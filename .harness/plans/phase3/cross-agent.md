# Phase 3 Cross-Agent 인터페이스 + 이벤트 + 모듈

## 에이전트 간 인터페이스

### building-economy ↔ pawn-ai
| 인터페이스 | 소유 | 소비 | 용도 |
|-----------|------|------|------|
| IBillQuery | building-economy | pawn-ai (BillWorkGiver) | 대기 Bill 조회 |
| IBlueprintQuery | building-economy | pawn-ai (ConstructionWorkGiver) | 미완성 Blueprint 조회 |
| IItemQuery | building-economy | pawn-ai (HaulingWorkGiver) | Haulable 아이템 검색 |
| IStockpileQuery | building-economy | pawn-ai (HaulingJobDriver) | 최적 저장 셀 결정 |
| IJobGiver 구현체 | pawn-ai | building-economy (Job 제안 이벤트) | ConstructionWorkGiver 등 |

### building-economy ↔ environment-life
| 인터페이스 | 소유 | 용도 |
|-----------|------|------|
| ISeasonQuery | environment-life | Growing zone 계절 참조 |
| ITemperatureQuery | environment-life | Deterioration 실내/냉장 보정 (Phase 4) |
| plant.sow-requested | building-economy 발행 | environment-life 소비 (Phase 4) |

### pawn-ai → pawn 도메인 (자기 영역)
| 인터페이스 | 용도 |
|-----------|------|
| SkillsComponent (읽기) | 품질 롤 참조 |
| skill.xp-gained 이벤트 | XP 증가 (building-economy 발행 → pawn 도메인 핸들러) |

## 이벤트 토픽 전체 (Phase 3 신규)

### building.* (sync/async)
| 토픽 | 모드 | 발행자 |
|------|------|--------|
| building.place-requested | sync | UI/Input |
| building.blueprint-placed | async | BlueprintPlacementSystem |
| building.work-ticked | async | JobDriver |
| building.constructed | async | ConstructionProgressSystem |
| building.deconstruct-requested | sync | UI/Input |
| building.deconstructed | async | DeconstructionSystem |
| building.damaged | async | CombatSystem(Phase 4) |

### resource.* / item.*
| 토픽 | 모드 | 발행자 |
|------|------|--------|
| resource.reserved | sync | ReservationService |
| resource.consumed | sync | ConstructionProgressSystem |
| item.spawn-requested | sync | 다수 |
| item.produced | async | ItemSpawnSystem |
| item.consumed | sync | BillWorkExecutionSystem |
| item.destroyed | async | DeteriorationSystem |
| item.moved | async | HaulingJobDriver |
| item.forbidden-changed | async | UI |

### bill.* / zone.*
| 토픽 | 모드 | 발행자 |
|------|------|--------|
| bill.changed | async | UI |
| bill.job-offered | async | BillProducerSystem |
| bill.completed | async | BillWorkExecutionSystem |
| zone.changed | async | ZoneManagementSystem |
| haul.request | async | StockpileAssignmentSystem |
| skill.xp-gained | async | BillWorkExecutionSystem |

### pawn.* (Phase 3 신규)
| 토픽 | 모드 | 발행자 |
|------|------|--------|
| pawn.job-assigned | sync | WorkSchedulerSystem |
| pawn.job-completed | sync | BTRunSystem |
| pawn.schedule-changed | sync | ScheduleSystem |
| pawn.work-priority-changed | async | UI |
| pawn.haul-requested | async | building-economy/environment |

### weather/season/temperature (3G)
| 토픽 | 모드 | 발행자 |
|------|------|--------|
| season.changed | sync | SeasonSystem |
| weather.changed | async | WeatherSystem |
| temperature.changed | async(throttle) | TemperatureSystem |

## RULE-007 토픽 카테고리 추가 등록
syncTopics 추가: `season.changed`
asyncTopics 추가: (이미 등록됨)

## 모듈 ON/OFF 총괄
| 모듈 | 기본 상태 | OFF 시 동작 |
|------|----------|------------|
| Building/Economy (core) | 항상 ON | Disable 불가 |
| Power (Phase 4) | OFF | IPowerQuery.IsPowered → true |
| Temperature | ON | 21.0°C 고정 |
| Weather | ON | Clear 고정 |
| Season | 항상 ON (core) | N/A |
| Schedule | ON (core) | GetCurrentActivity → Anything |
| Haul/Construct/Clean | core | 항상 등록 |
| BillWork | building-economy | IBillQuery 미제공 시 등록 생략 |
| TrainAnimal | animal module | OFF 시 WorkGiverDef 스캔 제외 |
