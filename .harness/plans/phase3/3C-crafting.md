# 3C: 작업대 + 제작 레시피 + 품질

## 컴포넌트
| Component | Fields | 직렬화 delta | schemaVersion |
|-----------|--------|-------------|:---:|
| WorkbenchComponent | BillList(List&lt;Bill&gt;), WorkSpeedMultiplier, NextBillIndex | BillList 전체 직렬화 (사용자 설정 손실 금지) | 1 |
| BillWorkProgressComponent | RecipeDefId, WorkRemaining, WorkTotal, InputStackIds | cancel-on-save (Phase 2 Job 정책 동일) | 1 |
| IngredientReservationComponent | ReservedStackIds(long[]) | 로드 시 Bill 재평가로 재생성 → 저장 생략 | 1 |

## Def
| Def | 주요 필드 |
|-----|----------|
| RecipeDef | Ingredients(IngredientCount[]), Products(ItemProduct[]), WorkAmount, SkillRequired{Id,MinLevel}, SkillXpGained, RecipeUsers(BuildingDefId[]), ProductQualityScale, AllowedStuffCategories |
| QualityCategoryTable | BucketThresholds(float[7]: Awful~Legendary), SkillCurve{Level→MeanRoll} |
| BillConfigDef | DefaultRepeatMode(Forever/XTimes/DoUntilX), DefaultStockpileSearchRadius |

## System
| System | Order | Phase | 틱 예산 | 역할 |
|--------|-------|-------|---------|------|
| BillProducerSystem | 31 | AI | 0.3ms | Bill 큐 평가, IResourceQuery.CanAfford 검증, idle Craft/Cook 폰에 Job 제안 |
| BillWorkExecutionSystem | 52 | Events | 0.3ms | BillWorkProgress 틱, 완료 시 품질 롤 + 산출물 생성 |
| QualityRollService | N/A | Domain | N/A | Skill Level → 품질 카테고리 순수 함수 |

## 품질 7단계
```
0=Awful  1=Poor  2=Normal  3=Good  4=Excellent  5=Masterwork  6=Legendary
```
- QualityCategoryTable Def로 외부화 (RULE-002)
- Skill Level → 확률 분포 커브로 결정
- Inspired Creativity(Phase 4G)용 +2 버킷 보너스 훅 포인트

## Query API
```csharp
interface IBillQuery {
    IEnumerable<(EntityId Workbench, int BillIndex, Bill Bill)> GetPendingBills(WorkTypeFilter filter, int mapId);
}
```

## Bill 반복 모드
- **Forever** — 재료 있는 한 무한 반복
- **XTimes** — N회 제작 후 정지
- **DoUntilX** — 산출물이 X개 될 때까지
