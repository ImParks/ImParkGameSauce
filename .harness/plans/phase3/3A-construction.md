# 3A: Blueprint + 건설 + 방(Room) 인식

## 컴포넌트
| Component | Fields | 직렬화 delta | schemaVersion |
|-----------|--------|-------------|:---:|
| BlueprintComponent | BuildingDefId, FacingDir(byte 0~3), Placer(EntityId), PlacementTick | FacingDir=0 시 생략 | 1 |
| ConstructionProgressComponent | WorkRemaining, WorkTotal, MaterialsDeposited(Dict) | WorkRemaining==WorkTotal 시 생략 | 1 |
| DeconstructDesignationComponent | WorkRemaining(float) | 존재/부재 마커 | 1 |
| ThingPositionComponent | Cell(Point), MapId, FacingDir | PawnPosition과 포맷 호환 | 1 |
| BuildingComponent | BuildingDefId(캐시), Size(WxH), PassThroughCost | DefRef로 DefId 흡수, 런타임 캐시만 | 1 |
| HitPointsComponent | Current, Max(int) | Current==Max 시 생략 | 1 |
| RoomComponent | RoomId(int), IsOutdoor(bool) | 타일 엔티티에 부착, RoomId=0(야외) 시 생략 | 1 |

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
| RoomFloodFillSystem | 55 | Events | 0.3ms | ← building.constructed, building.deconstructed, tile.changed → room.changed(sync) |

## 핵심 흐름
```
플레이어 클릭 → building.place-requested
  → BlueprintPlacementSystem: Blueprint 엔티티 생성
  → 폰(3E ConstructionJobGiver) 감지 → 재료 운반 → 작업
  → building.work-ticked → ConstructionProgressSystem
  → WorkRemaining=0 → Blueprint→Building 전환
  → tile.changed → PathCache 무효화 + Region 재계산
```

## 방(Room) 인식 시스템
- **RoomFloodFillSystem**: 벽/문으로 둘러싸인 폐쇄 공간을 4-connected BFS로 감지
- **트리거**: building.constructed / building.deconstructed / tile.changed → dirty queue
- **RoomComponent**: 타일 엔티티에 부착, RoomId(int) + IsOutdoor(bool)
- **RoomId**: monotonic increment, 야외=0 고정
- **room.changed** (sync): RoomId, OldSize, NewSize, IsOutdoor — 온도/인테리어/청결 시스템이 구독
- **IRoomQuery**: GetRoomAt(Point), GetRoomTiles(int roomId), IsIndoor(Point), GetRoomStats(int roomId)
- Phase 4 확장: 온도 시뮬레이션이 Room 단위로 열전도, 인테리어(Beauty/Impressiveness) 계산

```csharp
interface IRoomQuery {
    int GetRoomIdAt(Point cell, int mapId);
    IEnumerable<Point> GetRoomTiles(int roomId);
    bool IsIndoor(Point cell, int mapId);
    RoomStats GetRoomStats(int roomId); // Phase 3: size/outdoor만, Phase 4: temp/beauty 확장
}
```

## 리스크
- Blueprint→Building 전환 시 path invalidation 필요 → tile.changed 이벤트 발행으로 해결
- 건설 중 폰 사망 → ReservationService 자동 해제 (Phase 2 pawn.died 구독)
- Room 재계산 빈도 → dirty queue + 틱당 최대 4 Room 재계산으로 burst 제한
