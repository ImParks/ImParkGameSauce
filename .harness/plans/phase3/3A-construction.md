# 3A: Blueprint + 건설 시스템

## 컴포넌트
| Component | Fields | 직렬화 delta |
|-----------|--------|-------------|
| BlueprintComponent | BuildingDefId, FacingDir(byte 0~3), Placer(EntityId), PlacementTick | FacingDir=0 시 생략 |
| ConstructionProgressComponent | WorkRemaining, WorkTotal, MaterialsDeposited(Dict) | WorkRemaining==WorkTotal 시 생략 |
| DeconstructDesignationComponent | WorkRemaining(float) | 존재/부재 마커 |
| ThingPositionComponent | Cell(Point), MapId, FacingDir | PawnPosition과 포맷 호환 |
| BuildingComponent | BuildingDefId(캐시), Size(WxH), PassThroughCost | DefRef로 DefId 흡수, 런타임 캐시만 |
| HitPointsComponent | Current, Max(int) | Current==Max 시 생략 |

## Def
| Def | 주요 필드 |
|-----|----------|
| BuildingDef : ThingDef | Size, WorkToBuild, StuffCategory, Materials(ResourceCost[]), HitPointsMax, PassThroughCost, Category, PlacementRules, BlocksPath, FillPercent |

## System
| System | Order | Phase | 틱 예산 | 구독/발행 |
|--------|-------|-------|---------|----------|
| BlueprintPlacementSystem | 10 | Input | 0.2ms | ← building.place-requested(sync) → building.blueprint-placed(async), path.invalidate |
| ConstructionProgressSystem | 50 | Events | 0.2ms | ← building.work-ticked(async) → building.constructed(async), resource.consumed(sync), tile.changed |
| DeconstructionSystem | 51 | Events | 0.1ms | ← building.deconstruct-requested(sync) → building.deconstructed, item.produced |

## 핵심 흐름
```
플레이어 클릭 → building.place-requested
  → BlueprintPlacementSystem: Blueprint 엔티티 생성
  → 폰(3E ConstructionJobGiver) 감지 → 재료 운반 → 작업
  → building.work-ticked → ConstructionProgressSystem
  → WorkRemaining=0 → Blueprint→Building 전환
  → tile.changed → PathCache 무효화 + Region 재계산
```

## 리스크
- Blueprint→Building 전환 시 path invalidation 필요 → tile.changed 이벤트 발행으로 해결
- 건설 중 폰 사망 → ReservationService 자동 해제 (Phase 2 pawn.died 구독)
