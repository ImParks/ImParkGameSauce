# 3B: 자원/인벤토리

## 컴포넌트
| Component | Fields | 직렬화 delta |
|-----------|--------|-------------|
| ItemStackComponent | ItemDefId, Count, HitPoints(short), Quality(byte 0~6, 255=NA) | Count==1 && Quality==NA && Hp==Max 시 필드 생략 |
| StackableComponent | MaxStackSize(short, DefRef 캐시) | Def에서 복원 → 전체 생략 |
| ContainerComponent | Contents(List&lt;long&gt;), CapacityMass, CurrentMass | 빈 리스트 시 생략, CurrentMass 재계산 가능 → 생략 |
| DeteriorationComponent | DeteriorateRatePerDay, LastCheckedTick | Rate==0 시 생략 |

## Def
| Def | 주요 필드 |
|-----|----------|
| ThingDef (공통 부모) | Category(Item/Building/Plant), GraphicId, MassPerUnit, DeteriorateRate, Flammability |
| ItemDef : ThingDef | MaxStackSize(1~75), StatBases{MarketValue, MaxHp, DeteriorateRate}, IsEdible, Nutrition, StuffProps |
| StuffCategoryDef | DefaultStuffDefId (Wood/Stone/Metal 분류) |

## System
| System | Order | Phase | 틱 예산 | 역할 |
|--------|-------|-------|---------|------|
| InventoryIndexSystem | 5 | Input | 0.3ms | ItemDefId→StackEntityId 인덱스 유지 (IResourceQuery backing) |
| ItemSpawnSystem | 40 | Events | 0.1ms | ItemStack 엔티티 생성, 동일 셀 자동 merge |
| DeteriorationSystem | 80 | Events | 0.5ms(250틱마다) | 야외 아이템 HP 감소, 실질 0.002ms/tick |

## Query API
```csharp
interface IResourceQuery {
    int GetStockCount(string itemDefId);
    bool CanAfford(ResourceCost[] costs);
    IEnumerable<EntityId> FindAvailableStacks(string itemDefId, int mapId, ItemFilter filter);
    long GetTotalMarketValue(int mapId);
}

interface IItemQuery {
    IEnumerable<ItemEntity> FindHaulable(int mapId, Point near, float radius);
}
```

## 이벤트 토픽
- `item.spawn-requested` (sync) — 아이템 생성 요청
- `item.produced` (async) — 생성 완료
- `item.consumed` (sync) — 소비 (건설 재료 등)
- `item.destroyed` (async) — 열화/파괴
- `item.moved` (async) — 위치 이동
- `item.forbidden-changed` (async) — 금지 토글
