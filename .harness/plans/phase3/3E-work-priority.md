# 3E: 작업 우선순위 + Hauling

## 컴포넌트
| Component | Fields | 직렬화 delta |
|-----------|--------|-------------|
| WorkPriorityComponent | Dictionary&lt;string,byte&gt; Priorities (WorkTypeDefId→0~4) | Def 기본값과 동일 시 diff만 저장 |
| CarryComponent | CarriedItemEntityId(long), StackCount(int) | EntityId=0 시 생략 |

## Def
| Def | 주요 필드 |
|-----|----------|
| WorkTypeDef | LabelKey, DefaultPriority(1~4), NaturalPriority, RequiredCapability(WorkCapabilityFlag), AllowedScheduleActivities |
| WorkGiverDef | WorkTypeDefId, GiverClassName(IJobGiver FQN), Order, ModuleCondition |
| HaulingJobDef : JobDef | DriverDefId, MaxStackPerHaul(25), SearchRadius(60), Interruptible=true |

## System
| System | Order | Phase | 틱 예산 | 역할 |
|--------|-------|-------|---------|------|
| WorkSchedulerSystem | 90 | AI | 0.5ms | Idle 폰 순회, 우선순위별 JobGiver 호출, 첫 Job 할당 |

## WorkScheduler 알고리즘
```
for each idle pawn:
  1. activity = ScheduleSystem.GetCurrentActivity(pawn)
     → Sleep이면 Work 스킵, Need Job만
  2. workTypes = WorkPriority 1→4 순회 (0=불가 제외)
     → 동점 시 WorkTypeDef.NaturalPriority로 tie-break
  3. for each workType (허용 Activity만):
     for each giver in WorkGiverDefRegistry.GetByWorkType(workType):
       job = giver.GetJobFor(pawn)
       if job != null && ReservationService.TryReserve(pawn, target, jobId):
         assign job → publish pawn.job-assigned → break
  4. 5ms 누적 시 다음 틱 이월 (LOD queue)
```

## BT vs WorkScheduler 경계 (핵심)
| 책임 | 담당 |
|------|------|
| Job 할당 (JobComponent.Current 세팅) | WorkSchedulerSystem 독점 |
| Job 실행 (Driver.Tick 호출) | BTRunSystem (Order=100) |
| Job 완료 (pawn.job-completed 발행) | BTRunSystem |
| Need 긴급 개입 | WorkScheduler가 Interruptible 확인 후 교체 |

## HaulingJobDriver 상태 머신
```
FindSource → Reserve(Src) → Path(Src) → Pickup(CarryComponent)
  → Reserve(Dst) → Path(Dst) → Drop → Complete
```
- Pickup~Drop 구간: Interruptible=false
- 실패 시: ReleaseAllByJob + CarryComponent 있으면 강제 Drop
- 세이브: CarryComponent 영속화, 로드 후 Drop부터 재개

## IJobGiver 구현체 4종
| WorkGiver | WorkType | 소스 |
|-----------|----------|------|
| HaulingWorkGiver | Haul | IItemQuery.FindHaulable |
| ConstructionWorkGiver | Construct | IBlueprintQuery.GetPendingBlueprints |
| BillWorkGiver | BillWork/Cook | IBillQuery.GetPendingBills |
| CleaningWorkGiver | Clean | Filth 엔티티 감지 |

## 이벤트 토픽
- `pawn.job-assigned` (sync) — PawnId, JobDefId, JobId, TargetThing, WorkType
- `pawn.job-completed` (sync) — PawnId, JobId, Success, FailReason
- `pawn.haul-requested` (async) — ItemEntityId, DestCell, Reason
