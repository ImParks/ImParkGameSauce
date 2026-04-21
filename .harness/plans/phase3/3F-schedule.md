# 3F: 시간대별 활동 스케줄러

## 컴포넌트
| Component | Fields | 직렬화 delta |
|-----------|--------|-------------|
| PawnScheduleComponent | byte[24] HourActivity (ScheduleActivityType enum) | DefaultTemplate과 동일 시 TemplateId만 저장 + diff |

## ScheduleActivityType (enum, byte)
```
Anything=0  Work=1  Sleep=2  Joy=3  Meditate=4
```
- Anything: Work+Joy 모두 허용

## Def
| Def | 주요 필드 |
|-----|----------|
| ScheduleAssignmentDef | LabelKey, HourActivity(byte[24]), IsDefault |

예시 템플릿: Worker (6~22 Work, 22~6 Sleep), NightOwl (반전), Anything (전부 0)

## System
| System | Order | Phase | 틱 예산 | 역할 |
|--------|-------|-------|---------|------|
| ScheduleSystem | 80 | AI | 0.2ms(시 변경 시만) | 현재 시각 슬롯 → Blackboard currentActivity 갱신 |

## WorkScheduler 연동
- ScheduleSystem.GetCurrentActivity(pawn) → Work 시간대가 아니면 Work JobGiver 전부 스킵
- BT에 GateByActivity ConditionNode 안전망 이중화 (Blackboard 읽기만)

## Need 긴급 개입
- Sleep Activity에서 NeedCritical(Hunger) 발생 시 → WorkScheduler가 Interruptible 확인 후 강제 교체
- Need Job (EatJob, SleepJob)은 Activity 무관 최우선

## Query API
```csharp
interface IScheduleQuery {
    ScheduleActivityType GetCurrentActivity(EntityId pawn);
    bool AllowsWork(EntityId pawn);
    bool AllowsSleep(EntityId pawn);
}
```

## 이벤트 토픽
- `pawn.schedule-changed` (sync) — PawnId, OldActivity, NewActivity, Hour
- `pawn.work-priority-changed` (async) — PawnId, WorkTypeDefId, OldPriority, NewPriority

## 모듈 OFF
- ScheduleSystem 비활성 시 → GetCurrentActivity = Anything (RULE-009 no-op)
