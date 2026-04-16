# ImParkGameSauce - Harness Engineering Workflow

## Overview
이 프로젝트는 자기 진화형 하네스 엔지니어링 방식으로 운영됩니다.
에이전트 구성, 규칙, 평가 기준이 모두 동적으로 관리되며, 실수로부터 학습합니다.

## Workflow: 하네스 엔지니어링 파이프라인

### 0단계: 요청 분류 (Request Triage)
- 메인 에이전트가 사용자 요청의 성격을 판단
- **"구현해줘"** 유형 → 1단계(계획 토론)으로 직행
- **"어떻게 하면 좋을까?"** / **"방법이 뭐가 있어?"** 유형 → 0.5단계(사전 탐색)으로

### 0.5단계: 사전 탐색 (Discovery)
- 메인 에이전트가 서브 에이전트들에게 **각자 도메인 관점에서 선택지 탐색을 요청**
- 서브 에이전트들은 **병렬로** 다음을 반환:
  - 가능한 선택지 나열
  - 각 선택지의 장단점 분석
  - 추천안과 추천 이유
- 메인 에이전트가 종합하여:
  - 중복 제거, 선택지 정리
  - **비교표 형태로 사용자에게 제안**
- 사용자가 방향을 선택하면 → 1단계(계획 토론)으로 진행
- 사전 탐색은 **평가 대상이 아님** (채점 없이 사용자에게 직접 보고)

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

## Wiki System: 외부 API 문서 관리

### 문서 파편화 원칙
외부 API 문서는 컨텍스트 절약을 위해 계층적으로 파편화하여 저장합니다.
**절대로 API 문서 전체를 한 번에 로드하지 않습니다.**

### 문서 구조 (API 1개당)
```
.harness/wiki/{api-id}/
├── overview.md              # API 개요, 인증, 공통사항 (먼저 이것만 읽음)
├── endpoints/
│   ├── _index.json          # 엔드포인트 목록 (필요한 것만 선택)
│   ├── create-payment.md    # 개별 엔드포인트 상세
│   └── ...
├── schemas/
│   ├── _index.json          # 데이터 구조체 목록
│   ├── payment-object.json  # 개별 스키마 정의
│   └── ...
└── snapshots/
    ├── _index.json          # 라이브 호출 결과 목록
    └── {snapshot-file}.json # 실제 API 응답 스냅샷
```

### 에이전트의 문서 조회 순서
1. `wiki/_index.json` → 어떤 API가 등록되어 있는지 확인
2. `wiki/{api-id}/overview.md` → API 개요만 읽음 (여기서 대부분 충분)
3. `wiki/{api-id}/endpoints/_index.json` → 필요한 엔드포인트 식별
4. `wiki/{api-id}/endpoints/{endpoint}.md` → **필요한 엔드포인트만** 읽음
5. `wiki/{api-id}/snapshots/` → 문서 vs 실제 응답 차이 확인

### 라이브 API 검증
공식 문서는 종종 실제 API와 다릅니다. 다음 방법으로 검증합니다:
1. **MCP 호출**: MCP 서버를 통해 API를 직접 호출
2. **코드 실행**: 에이전트가 테스트 코드를 작성·실행하여 실제 응답 구조 확인
3. **스냅샷 저장**: 호출 결과를 `snapshots/`에 저장 (키 값은 반드시 마스킹)
4. **문서 대조**: 스냅샷과 문서를 비교하여 차이점을 `docComparison`에 기록
5. **차이 발견 시**: 해당 엔드포인트 문서에 "라이브 검증" 섹션을 업데이트

### 시크릿/키 관리
- **실제 키 값은 이 리포지토리의 어떤 파일에도 저장하지 않습니다**
- `.harness/secrets/_keymap.json`: 키 메타데이터(변수명, 용도)만 저장. 실제 값 없음.

#### 키 저장 위치 (2곳, 용도 분리)
| 파일 | 용도 | Git 포함 |
|------|------|:--------:|
| `.env` | 로컬 서버 실행용 (dotenv로 로드) | X (gitignore) |
| `~/.zshrc` | MCP 서버용 (`export KEY=VALUE`) | 해당없음 (홈 디렉토리) |
| `.env.example` | 필요한 환경변수 목록 템플릿 (값 없음) | O |

#### 새 키 추가 시 절차
```
1. .env.example에 변수명 추가 (값은 비움)
2. _keymap.json의 keys 배열에 메타데이터 등록
3. 사용자에게 안내:
   - .env에 실제 값 추가 (로컬 서버용)
   - ~/.zshrc에 export 추가 (MCP용)
```

#### 보안 규칙
- 실제 키 값을 리포지토리의 어떤 파일에도 저장하지 않는다
- 에이전트가 코드를 생성할 때 키를 하드코딩하지 않고 `process.env.KEY_NAME`으로 참조한다
- 스냅샷 저장 시 키 값은 반드시 마스킹한다 (예: `sk-****1234`)
- 키를 가져올 수 없으면 사용자에게 요청하고, 절대 임의 값을 사용하지 않는다
- `.env` 파일은 `.gitignore`에 반드시 포함한다

### 새 API 문서 등록 절차
사용자가 공식 문서를 제공하면:
1. `wiki/{api-id}/` 폴더를 `_api-template/` 기반으로 생성
2. 문서를 파편화하여 overview, endpoints, schemas로 분리
3. `wiki/_index.json`에 API 메타데이터 등록
4. `secrets/_keymap.json`에 키 매핑 추가
5. `.env.example`에 환경변수 추가
6. 가능하면 라이브 검증 실행하여 스냅샷 저장

## Bootstrap: 초기 부트스트랩

프로젝트 첫 실행 시 또는 `.harness/config.json`의 `bootstrapped`가 `false`인 경우:
1. 메타 토론을 통해 에이전트 구성, 평가 기준, 피드백 루프 상한을 결정
2. 결정 사항을 각 설정 파일에 기록
3. `bootstrapped`를 `true`로 변경
