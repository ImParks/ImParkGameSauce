# 에이전트 구성 v2 (6 Producer + 1 Evaluator)

## 에이전트 목록

| ID | 역할 | Core 시스템 | Module 시스템 |
|----|------|------------|--------------|
| core-engine | 엔진 기반 | 타일맵, 시간, 이벤트 버스, Def 로더, 경로탐색, 모듈 레지스트리 | - |
| pawn-ai | 폰/행동 | 폰, 니즈, AI 행동트리, 전투, 신체 부위 | 관계, 질병/의료 |
| world-event | 이벤트/스토리 | 이벤트/스토리텔러, 난이도 | 파벌/외교 |
| building-economy | 건설/경제 | 건설, 구역 4종, 자원/생산, 빌 큐, 품질 | 전력 |
| environment-life | 환경/생물 | - | 온도, 날씨, 동물 |
| ui-data | UI/데이터 | UI, 세이브/로드 | 모딩 |
| evaluator | 평가 | - | - |

## Module 8개 전수 매핑

| Module | 담당 에이전트 | Phase | 선행 의존 |
|--------|-------------|-------|----------|
| 온도 | environment-life | 3-G | 타일맵, 방 인식 |
| 날씨 | environment-life | 4-C | 온도, 이벤트 버스 |
| 관계 | pawn-ai | 3-E | 폰 AI, 니즈 |
| 전력 | building-economy | 4-B | 타일맵, 건설 |
| 질병/의료 | pawn-ai | 4-D | 신체 부위 트리 |
| 동물 | environment-life | 4-E | 폰 AI(행동트리 재활용) |
| 파벌/외교 | world-event | 5-C | 이벤트/스토리텔러, 관계 |
| 모딩 | ui-data | 1(기반)+6(완성) | Def 시스템 |

## 에이전트 간 인터페이스

### 이벤트 버스 토픽 카테고리
- time.* / tile.* / path.* (core-engine 관할)
- temperature.* / weather.* / animal.* (environment-life 관할)
- pawn.* / relation.* / medical.* / combat.* (pawn-ai 관할)
- event.* / raid.* / faction.* (world-event 관할)
- building.* / zone.* / power.* (building-economy 관할)
- resource.* / item.* / bill.* (building-economy 관할)
- save.* / mod.* / ui.* (ui-data 관할)

### 동기/비동기 규칙
- 동기: time.tick, combat.damage, resource.reserved, raid.spawned, save.started, mod.loaded
- 비동기: tile.changed, pawn.state-changed, item.produced, building.placed, temperature.changed

### Query API (읽기 전용 직접 호출)
- core-engine → ITilemapQuery, ITimeQuery
- pawn-ai → IPawnQuery
- building-economy → IResourceQuery

#### Query API 시그니처 초안
```csharp
// core-engine 제공
interface ITilemapQuery {
    TileData GetTile(int x, int y);
    bool IsWalkable(int x, int y);
    int GetRegionId(int x, int y);
}
interface ITimeQuery {
    long GetCurrentTick();
    GameSpeed GetSpeed();
    bool IsPaused();
}

// pawn-ai 제공
interface IPawnQuery {
    PawnData? GetPawnById(EntityId id);
    IReadOnlyList<EntityId> GetIdlePawns();
    IReadOnlyList<EntityId> GetPawnsInRadius(Vec2 center, float radius);
}

// building-economy 제공
interface IResourceQuery {
    int GetStockCount(string itemDefId);
    bool CanAfford(ResourceCost[] costs);
}
```

### ECS 컴포넌트 소유권 (CQRS 변형)
- 자기 관할: 직접 R/W 허용
- 타 시스템: Read는 Query API, Write는 이벤트 버스만

### Def ↔ ECS 매핑
- Def 이중 역할: 엔티티 템플릿(ThingDef 등) + 수치 외부화(NeedDef 등)
- DEF-001: Def는 읽기 전용, 런타임 상태는 컴포넌트에
- DEF-002: DefDatabase 글로벌 싱글턴, 시작 시 1회 로드
- DEF-003: 모든 엔티티에 DefRefComponent { defId } 필수
- DEF-004: 조건부 컴포넌트 (module OFF 시 생략)
- DEF-005: statBases(밸런스) vs defaults(구조적 초기값) 분리

### 모듈 ON/OFF
- IGameModule 인터페이스: onRegister/onEnable/onDisable
- No-op 폴백: 온도→21.0, 전력→항상 공급, 관계→중립, 날씨→맑음
- Def condition 필드로 비활성 모듈 컴포넌트 생략
- 비활성 모듈 세이브 데이터 보존 (재활성 시 복원)
