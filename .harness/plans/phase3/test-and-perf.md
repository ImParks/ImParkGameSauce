# Phase 3 테스트 + 틱 예산 + 파일 분할

## 틱 예산 총괄 (RULE-003 준수)

### 기존 예산 배정 (20ms 총합)
| 카테고리 | 예산 | Phase 3 소비 |
|---------|------|-------------|
| pawnAI | 8ms | WorkScheduler 0.5 + Drivers ~3 = 3.5ms |
| pathfinding | 4ms | 기존 유지 |
| needsMood | 2ms | 기존 유지 |
| worldSim | 2ms | 환경 0.28ms |
| events | 1ms | 기존 유지 |
| buildingProduction | 1ms | 건설/제작 ~1.5ms |
| reserve | 2ms | 구역/재고 ~0.5ms |
| **합계** | **20ms** | **Phase 3 추가 ~5.8ms** |

### Phase 3 System별 세부
| System | ms/tick | 주기 | 실질 ms/tick |
|--------|---------|------|-------------|
| BlueprintPlacementSystem | 0.2 | 이벤트 | ~0 |
| ConstructionProgressSystem | 0.2 | 이벤트 | ~0 |
| DeconstructionSystem | 0.1 | 이벤트 | ~0 |
| InventoryIndexSystem | 0.3 | 매틱 | 0.3 |
| ItemSpawnSystem | 0.1 | 이벤트 | ~0 |
| DeteriorationSystem | 0.5 | 250틱 | 0.002 |
| BillProducerSystem | 0.3 | AI | 0.3 |
| BillWorkExecutionSystem | 0.3 | 이벤트 | ~0 |
| ZoneManagementSystem | 0.1 | 이벤트 | ~0 |
| StockpileAssignmentSystem | 0.2 | AI | 0.2 |
| GrowingZoneSystem | 0.2 | 60틱 | 0.003 |
| WorkSchedulerSystem | 0.5 | AI LOD | 0.5 |
| ScheduleSystem | 0.2 | 시 변경 | ~0 |
| SeasonSystem | 0.01 | 매틱 | 0.01 |
| WeatherSystem | 0.2 | 10틱 | 0.02 |
| TemperatureSystem | 0.5 | 2틱 | 0.25 |
| **합계** | | | **~1.6ms (안정시)** |

4x 속도 전략: BillProducer/WorkScheduler는 4틱당 1회 풀 스캔.

## 테스트 플랜

### building-economy (3A~3D)
| 분류 | 핵심 케이스 |
|------|-----------|
| Blueprint | 유효 타일 생성, 벽 타일 거부, 회전 검증 |
| Construction | Work→0 전환, path invalidation, 폰 사망 시 해제 |
| Inventory | 동일 셀 merge, StockCount 정확성, CanAfford 다재료 |
| Bill | 재료 부족 미발행, RepeatMode 3종, Skill 미달 제외 |
| Quality | Skill 0→Awful, Skill 20→Masterwork 분포, 합=1.0 |
| Zone | CRUD 라이프사이클, 겹침 거부, RLE 세이브 라운드트립 |
| Stockpile | 우선순위별 선택, 만석 fallback, 도달불가 제외 |
| Deterioration | 야외만 감소, HP=0 파괴 |
| Integration | 작업대 건설→Bill→Craft→Output→Stockpile 전과정 |
| Performance | 200폰+50작업대+20blueprint &lt;1.5ms/tick |

### pawn-ai (3E~3F)
| 분류 | 핵심 케이스 |
|------|-----------|
| WorkScheduler | 우선순위 순서 할당, Priority=0 스킵, Schedule 게이팅, Busy 스킵 |
| HaulingDriver | Pickup→Drop 라운드트립, 실패 시 예약 해제 |
| Schedule | 시 경계 전이, Module OFF→Anything |
| Serialization | WorkPriority diff 생략, Schedule template 참조 |
| WorkGiverDef | Module OFF 시 등록 스킵 |
| Integration | Idle→Job→BT실행→Complete→Idle 전과정 |
| BT경계 | BTRunSystem은 Job 생성 금지 확인 |
| Performance | 200폰 WorkScheduler &lt;0.5ms |

### environment-life (3G)
| 분류 | 핵심 케이스 |
|------|-----------|
| Season | 일수 경과 전이, season.changed 1회, 연간 +1 |
| Weather | 고정 시드 결정성, Snow 겨울만, 전이 확률 |
| Temperature | 공식 검증 (Summer+Rain=22°C), throttle (ΔC&lt;0.5 미발행) |
| Module OFF | Weather→Clear, Temperature→21°C, 이벤트 0건 |
| Re-enable | Disable→Enable 데이터 복원 |
| Serialization | 기본값 delta 생략, schemaVersion=1 |
| DefLoader | 누락 필드 에러, 알 수 없는 필드 경고, 4계절 순환 닫힘 |
| Performance | 10,000틱 weather&lt;0.02ms, temp&lt;0.25ms, season&lt;0.01ms |

## 파일 분할 (RULE-012)

### ImPark.Building 어셈블리
```
src/ImPark.Building/
├── Domain/
│   ├── Components/      # 17 파일 (각 50줄), _PhaseDelta.md, index.cs
│   ├── Events/          # 이벤트 struct 파일들
│   └── Rules/           # QualityRollService.cs 등 domain service
├── Application/
│   ├── Construction/    # Placement/Progress/Deconstruct/JobGiver (각 150~250줄)
│   ├── Inventory/       # Index/Spawn/Merge/Deterioration/Query
│   ├── Crafting/        # BillProducer/BillExecution/Bill.cs/DoBillJobDriver
│   ├── Zones/           # ZoneManagement/Stockpile/Growing/Forbidden/ZoneService
│   └── Queries/         # IBuildingQuery 등 구현체
├── Contracts/           # IBuildingQuery, IResourceQuery, IZoneQuery, IBillQuery
└── Infrastructure/      # DefLoader, Serializer (RLE, delta)
```

### ImPark.Pawn 확장
```
src/ImPark.Pawn/
├── Domain/Components/   # +WorkPriority, PawnSchedule, Carry
├── Domain/Defs/         # +WorkTypeDef, WorkGiverDef, HaulingJobDef, ScheduleAssignmentDef
├── Application/AI/
│   ├── WorkGivers/      # Hauling/Construction/Cleaning/Bill (각 1파일)
│   ├── Drivers/         # HaulingJobDriver 등 (각 150~250줄)
│   └── WorkSchedulerSystem.cs
└── Application/Schedule/ # ScheduleSystem, ScheduleQuery
```

### ImPark.Core/Environment (신규)
```
src/ImPark.Core/Environment/
├── domain/              # 3 컴포넌트 + 3 Def + Events/ + WeatherType enum
└── application/         # 3 System + 4 Module + Fallbacks/
```

### data/defs/
```
data/defs/
├── things/items/        # food.def.json, materials.def.json 등
├── things/buildings/    # furniture.def.json, structure.def.json
├── recipes/             # 카테고리별 분할
├── work/                # work-types, work-givers, schedule-assignments
├── zones/               # zone-kinds.def.json
├── quality/             # quality-table.def.json
└── environment/         # weather, seasons, biomes + _index.json
```

### 테스트 프로젝트
```
tests/
├── ImPark.Building.Tests/   # Construction, Inventory, Crafting, Zone, Integration
├── ImPark.Pawn.Tests/       # +WorkScheduler, HaulingDriver, Schedule (기존 확장)
└── ImPark.Core.Tests/       # +Environment/ (Season, Weather, Temperature, Module)
```

## 리스크 총괄
| ID | 심각도 | 내용 | 완화 |
|----|--------|------|------|
| R-01 | major | IJobGiver 단일→다중 전환 | CompositeJobGiver 임시 도입, 3E에서 교체 |
| R-02 | major | Blueprint→Building path invalidation | tile.changed 이벤트로 해결 |
| R-03 | minor | ItemStack merge 이벤트 폭증 | sync merge + batch item.moved 1회 |
| R-04 | major | SkillsComponent 직접 수정 금지 | skill.xp-gained 이벤트만 (RULE-007) |
| R-05 | minor | RecipeDef 양방향 참조 | RecipeDef.RecipeUsers 단방향 유지 |
| R-06 | minor | ZoneId 재사용 충돌 | monotonic increment, 재사용 금지 |
| R-07 | minor | System 300줄 초과 | domain service 분리 |
| R-08 | major | BT/WorkScheduler 권한 중복 | WorkScheduler 독점 세팅, 테스트 강제 |
| R-09 | major | Phase 4 온도 필드 확장 | schemaVersion + empty 마이그레이터 선제 |
| R-10 | major | 날씨 확률 비결정성 | IRandomService 전용 stream ID |
