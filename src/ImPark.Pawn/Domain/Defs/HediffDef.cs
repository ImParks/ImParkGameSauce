// Phase 2 scope: core fields only (Stages[] + SeverityPerDay defined).
// Phase 4D medical module: stage progression logic lives there.
// When medical module OFF (Phase 2 default), HediffDef.Stages and
// SeverityPerDay are STORED but NOT PROGRESSED per improvement #4.
// BodyPartSystem treats all damage as immediate HP reduction without
// stage advancement.
//
// Medical OFF→ON 세이브 마이그레이션 절차 (improvement #4):
//  1. OFF 상태 저장: HediffDef 메타데이터(Stages[]/SeverityPerDay)는 Def 레지스트리에
//     유지되므로 세이브 파일에 별도 기록 불필요. 폰별 Hediff 인스턴스는 현재 HP/통증만 저장.
//  2. ON 전환 시(Phase 4D 활성): 기존 Hediff 인스턴스의 currentSeverity=0으로 시작.
//     이후 MinSeverity 기준으로 현재 stage를 재계산(GetCurrentStage(severity)).
//  3. 역전환 (ON→OFF): severity/stage 필드는 저장 유지하되 SeverityPerDay 진행 중단.
//  4. Def 변경 호환성: Stages[] 순서/개수 변경 시 마이그레이터가 MinSeverity 기준으로
//     가장 가까운 새 stage로 재매핑 (upgrade 전용, downgrade 금지).
using System;
using ImPark.Shared.Defs;

namespace ImPark.Pawn.Domain.Defs;

[DefType("HediffDef")]
public class HediffDef : Def
{
    public HediffStage[] Stages { get; init; } = Array.Empty<HediffStage>();
    public float SeverityPerDay { get; init; } = 0f;
}

public class HediffStage
{
    public float MinSeverity { get; init; }
    public string Label { get; init; } = string.Empty;
    public float PainOffset { get; init; }
}
