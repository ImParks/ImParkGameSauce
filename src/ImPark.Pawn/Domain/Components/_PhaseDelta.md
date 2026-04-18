# Pawn Components Phase 2 Delta (improvement #5)

Phase 1 이후 추가/유지되는 Component 구분.

## Phase 1 (엔진 코어) 제공
- Shared/ECS의 IComponent marker, World, EntityId
- Shared/Defs의 DefRefComponent { string DefId }

## Phase 2B에서 신규 (pawn-ai)
| Component | 역할 | 직렬화 delta |
|-----------|------|-------------|
| PawnTag | 마커 | 생략 불가(태그) |
| PawnPositionComponent | Point + MapId | 기본값(0,0,0) 시 생략 가능 |
| PawnMoveSpeedComponent | TilesPerSecond | RaceProps.BaseMoveSpeed와 동일 시 생략 |
| WorkCapabilityComponent | 5 bool 플래그 | 전부 true(Phase 2 기본) 시 생략 |
| SkillsComponent | Dictionary<string,SkillRecord> | 빈 딕셔너리 시 생략, 키는 string table 압축 권장 |
| PawnBusyComponent | idle 판정용 마커 | 존재/부재로 표현, 기본 부재 |

## Phase 2C에서 신규
| Component | 직렬화 delta |
|-----------|-------------|
| BehaviorTreeComponent | Root transient 저장 안 함. ThinkTreeDefId+CurrentNodeIndex+BB 저장 |
| JobComponent | Driver transient, Current Job은 cancel-on-save (저장 시 폐기) |

## Phase 2D에서 신규
| Component | 직렬화 delta |
|-----------|-------------|
| HungerComponent | Level=1.0(포만) 시 생략 |
| SleepComponent | Level=1.0 시 생략 |
| MoodComponent | Current=0.5(중립) 시 생략 |
| ThoughtsComponent | 빈 리스트 생략, 만료된 Thought 저장 전 pruning |

## Phase 2E에서 신규
| Component | 직렬화 delta |
|-----------|-------------|
| BodyComponent | PartEntityIds 리스트 + BodyDefId, BodyDef에서 재구성 가능하므로 부상 없는 파트는 Def 참조로 복원 |
| BodyPartComponent | CurrentHp=MaxHp, Pain=0, BleedRate=0 시 생략 (자식 엔티티) |
| HealthComponent | 기본값(Downed=false, Dead=false, PainTotal=0) 시 생략 |

## Phase 2 미포함 (후속 Phase)
- 스킬 경험치 커브, 레벨 업 공식 (Phase 3C building-economy)
- 정신붕괴 Breakdown 컴포넌트 (Phase 4G)
- Hediff 진행 상태 관리 (Phase 4D medical)
- 장비 슬롯/갑옷 (Phase 4A 전투)
- 파벌/관계 링크 (Phase 3E, 5C)
