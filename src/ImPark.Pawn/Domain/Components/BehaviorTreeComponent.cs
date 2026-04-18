using ImPark.Pawn.Domain.AI.BT;
using ImPark.Shared.ECS;
using BB = ImPark.Pawn.Domain.AI.Blackboard.Blackboard;

namespace ImPark.Pawn.Domain.Components;

// RULE-001 deviation: holds class references (Blackboard, IBTNode Root).
// Root is transient and rebuilt from ThinkTreeDef at load time via IBTLeafRegistry.
// ThinkTreeDefId is the serialized anchor.
//
// CurrentNodeIndex 인코딩 규격 (improvement #6):
//  - 인코딩: ThinkTreeDef.RootNode에서 pre-order DFS로 순회한 선형 인덱스 (루트=0, 첫 자식=1, ...)
//  - 의미: 현재 Running 상태인 노드의 DFS 번호. -1=미실행(트리 재시작 필요), 0=루트부터 시작.
//  - ThinkTreeDef 변경 시(stages 추가/삭제): CurrentNodeIndex 무효화 → -1로 리셋 후 루트부터 재실행.
//  - 저장/복원: Root는 저장 안 함(transient). ThinkTreeDefId + CurrentNodeIndex + BB로 재구성.
//  - 직렬화 호환성: DFS 순회 방식이 결정적이므로 동일 ThinkTreeDef 로드 시 재개 지점 일치.
public struct BehaviorTreeComponent : IComponent
{
    public string ThinkTreeDefId;
    public BB BB;
    public IBTNode? Root;
    public long CurrentNodeIndex;
}
