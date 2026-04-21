# 3D: 구역(Zone) 관리

## 구역 4종
| Kind | 역할 | 설정 |
|------|------|------|
| Stockpile | 자원 저장 | Priority(0~4), AllowedItemFilter, QualityRange, HpRange |
| Growing | 농작물 재배 | PlantDefId, SowPriority, AllowCut |
| Animal | 동물 방목 (Phase 4) | AnimalDefFilter (Phase 4 확장) |
| Forbidden | 진입/사용 금지 | 마커만 |

## 컴포넌트
| Component | Fields | 직렬화 delta | schemaVersion |
|-----------|--------|-------------|:---:|
| ZoneMembershipComponent | ZoneId, ZoneKind(byte) | ZoneService 비트맵 RLE로 별도 직렬화 | 1 |
| ForbiddenComponent | IsForbidden(bool) 마커 | false 시 생략 | 1 |
| StockpileSettingsComponent | Priority, AllowedItemFilter, QualityRange, HpRange | 기본 필터(모두 허용) 시 생략 | 1 |
| GrowingZoneComponent | PlantDefId, SowPriority, AllowCut | PlantDefId=default 시 생략 | 1 |

## Def
| Def | 주요 필드 |
|-----|----------|
| ZoneKindDef | Kind(enum), DefaultColor(RGBA), DefaultPriority, AllowPawnPathing, DefaultFilters |

## System
| System | Order | Phase | 틱 예산 | 역할 |
|--------|-------|-------|---------|------|
| ZoneManagementSystem | 11 | Input | 0.1ms | ZoneService 비트맵 관리 (생성/수정/삭제) |
| StockpileAssignmentSystem | 32 | AI | 0.2ms | 아이템별 최적 저장구역 결정 → haul.request 발행 |
| GrowingZoneSystem | 81 | Events | 0.2ms(60틱마다) | 빈 타일 감지 → plant.sow-requested 발행 |

## Query API
```csharp
interface IZoneQuery {
    int GetZoneIdAt(Point cell, int mapId);
    ZoneInfo GetZoneInfo(int zoneId);
    IEnumerable<Point> GetCells(int zoneId);
    bool IsForbidden(Point cell, int mapId);
    int? FindBestStockpile(string itemDefId, byte quality, int mapId);
}

interface IStockpileQuery {
    Point? FindBestCell(string itemDefId, int stackCount, int mapId);
}
```

## ZoneService 내부
- 타일→ZoneId 비트맵 기반 (O(1) 조회)
- ZoneId는 monotonic increment (삭제 후 재사용 금지)
- 직렬화: RLE 압축 (RULE-004 최적화)
