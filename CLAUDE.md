# ImParkGameSauce - Harness Engineering Workflow

## Overview
이 프로젝트는 자기 진화형 하네스 엔지니어링 방식으로 운영됩니다.
에이전트 구성, 규칙, 평가 기준이 모두 동적으로 관리되며, 실수로부터 학습합니다.

## Workflow: 하네스 엔지니어링 파이프라인

### 1단계: 계획 토론 (Plan Discussion)
- 메인 에이전트가 사용자 요청을 분석하고, `.harness/agents.json`에 등록된 서브 에이전트들에게 각 담당 영역의 설계를 요청
- 서브 에이전트들은 **병렬로** 각자 전문 영역에 대한 설계 제안을 반환
- 메인 에이전트가 모든 제안을 종합하여 통합 계획서를 작성
- 계획 작성 시 `.harness/rules/` 에서 관련 규칙을 반드시 조회하여 반영

### 2단계: 계획 평가 (Plan Evaluation)
- 평가 에이전트가 통합 계획서를 `.harness/evaluation-criteria.json` 기준으로 채점
- 100점 만점, 평균 95점 미만 시 감점 요인과 수정사항을 피드백으로 반환
- 피드백은 `.harness/feedback-log/`에 기록

### 3단계: 보고 (Report)
- 95점 이상 통과된 계획을 사용자에게 보고
- 사용자 승인 후 다음 단계로 진행

### 4단계: 수정/구현 (Implementation)
- 서브 에이전트들이 **병렬로** 각 담당 영역을 구현
- 구현 전 반드시 `.harness/rules/` 에서 작업 경로와 기능에 매칭되는 규칙을 조회
- 예외적 처리가 필요한 경우, 예외 사유를 명시하여 평가 에이전트에 전달

### 5단계: 수정 평가 (Implementation Evaluation)
- 평가 에이전트가 구현물을 채점
- 95점 미만 시 피드백 → 4단계로 복귀
- 피드백 루프 최대 횟수는 `.harness/config.json`의 `maxFeedbackLoops` 참조

### 6단계: 보고 (Final Report)
- 완료된 작업을 사용자에게 최종 보고

## Rules System: 규칙 조회 방식

에이전트가 작업을 시작하기 전, 다음 순서로 관련 규칙을 수집합니다:

1. **global/** - 항상 적용되는 전역 규칙
2. **by-path/** - 작업 대상 파일 경로와 매칭되는 규칙
   - 파일명 규칙: 경로의 `/`를 `__`로 치환 (예: `src/components/` → `src__components.json`)
   - 상위 경로 규칙도 포함 (예: `src/components/Button.tsx` 작업 시 `src.json`, `src__components.json` 모두 참조)
3. **by-feature/** - 작업 기능 도메인과 매칭되는 규칙
4. **by-agent/** - 해당 에이전트 역할에 특화된 규칙

## Rule Learning: 실수 학습 메커니즘

평가에서 감점이 발생하면:
1. 감점 항목을 분석하여 방지 규칙을 생성
2. 해당 규칙을 적절한 rules/ 하위 폴더에 저장
3. `_index.json`에 메타데이터 추가 (태그, 경로, 생성일)
4. 이후 동일 경로/기능 작업 시 자동으로 참조됨

## Dynamic Agent Management: 에이전트 동적 관리

- 에이전트 추가/삭제는 메인 에이전트가 토론 결과를 바탕으로 `.harness/agents.json` 수정
- 새 에이전트 추가 시: 역할, 담당 도메인, 전문 영역을 명시
- 에이전트 삭제 시: 해당 에이전트의 규칙은 보존 (by-agent/ 폴더는 유지)

## Exception Handling: 예외 처리 전달

생산자 에이전트가 규칙/훅과 충돌하는 예외적 처리를 해야 할 경우:
```json
{
  "exception": true,
  "rule_id": "충돌하는 규칙 ID",
  "reason": "예외 처리가 필요한 이유",
  "approved_by": "user | bootstrap_discussion",
  "context": "상세 설명"
}
```
이 정보를 평가 에이전트에 함께 전달하여 감점 대상에서 제외할 수 있습니다.

## Bootstrap: 초기 부트스트랩

프로젝트 첫 실행 시 또는 `.harness/config.json`의 `bootstrapped`가 `false`인 경우:
1. 메타 토론을 통해 에이전트 구성, 평가 기준, 피드백 루프 상한을 결정
2. 결정 사항을 각 설정 파일에 기록
3. `bootstrapped`를 `true`로 변경
