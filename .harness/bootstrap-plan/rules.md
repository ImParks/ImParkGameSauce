# 글로벌 규칙 11개 (수치화)

## 규칙 1: ECS 우선
- 게임 오브젝트 100% Entity+Component, class 상속 오브젝트 0개
- Component에 메서드 0개 (순수 데이터), 로직은 System에만
- 위반: major -5/건. 예외: 유틸리티, UI 위젯

## 규칙 2: Def 외부화
- 하드코딩 밸런스 값 0개 (매직 넘버 금지)
- 모든 게임 오브젝트 타입에 대응 Def 파일 보유
- Def 로드 시 필수 필드 누락 → 에러, 알 수 없는 필드 → 경고
- 위반: major -3/건(밸런스 하드코딩), -5/건(Def 없는 오브젝트)

## 규칙 3: 틱 예산
- 게임 틱: 200ms(1배속), 로직 예산 20ms 이내
- 예산 분배: AI 8ms(40%), 경로 4ms(20%), 니즈/기분 2ms(10%), 월드 2ms(10%), 이벤트 1ms(5%), 건설/생산 1ms(5%), 예비 2ms(10%)
- 폰당 AI: 0.04ms (200폰 기준 8ms)
- 경로탐색 1건: 0.5ms, 틱당 8건 상한
- 4배속: AI 시간분할(25%만 풀 평가, 긴급 폰 항상), 경로 캐시, 월드 2틱마다
- AI LOD: 화면내+선택(매틱), 화면내+미선택(2틱), 화면밖+활동(5틱), 화면밖+대기(10틱)
- Phase 1에서 벤치마크로 수치 확정, rules/global/tick-budget.json 업데이트
- 위반: critical -8

## 규칙 4: 직렬화
- 포맷: MessagePack (개발 모드 JSON 병행)
- 파일 구조: Header(8B, Magic+Version+Flags) → Metadata → WorldState → Checksum(CRC32)
- 버전 마이그레이션: 정수 버전, 체인 방식(vN→vN+1), 마이그레이터 영구 보존
- 컴포넌트 추가: 기본값 초기화, 삭제: 무시(skip), 필드 변경: 마이그레이터 필수
- 크기 최적화: 기본값 생략(delta), RLE(타일맵), zlib 압축, Def는 defId만 저장
- 목표: 256맵 200폰 3초/5MB 이내, 512맵 400폰 20MB 이내
- 위반: critical -8(미구현), major -5(마이그레이터 누락)

## 규칙 5: 레이어 방향
- Core → Data → Logic → Presentation (역방향 금지)
- 순환 의존 0건
- 위반: critical -8/건

## 규칙 6: 경로 분리
- src/core, src/systems, src/components, src/modules, src/ui, src/save, data/defs, data/mods
- 단일 파일 300줄, 단일 함수 50줄
- 위반: minor -2/건

## 규칙 7: 이벤트 버스 (ECS 경계)
- 자기 컴포넌트: 직접 R/W, 타 시스템 변경: 이벤트 버스
- 이벤트: 동기 디스패치, 불변 페이로드
- 토픽: "domain.action" (pawn.damaged, resource.depleted)
- 시스템 간 직접 메서드 호출 0건
- 위반: major -5/건

## 규칙 8: Def-Entity 매핑 (DEF-001~005)
- Def 읽기 전용, DefDatabase 싱글턴, DefRef 필수, 조건부 컴포넌트, statBases 분리
- 위반: major -5/건

## 규칙 9: 모듈 ON/OFF (MOD-001)
- IGameModule 구현 필수, no-op 기본값, 비활성 이벤트 미발행
- 구독자는 이벤트 부재에도 정상 작동, Def condition 비활성 시 컴포넌트 생략
- 비활성 세이브 데이터 보존
- 위반: critical -8(인터페이스 미구현), major -5(no-op 누락)

## 규칙 10: 네이밍 컨벤션 (NAMING-001)
- 파일: PascalCase + 접미사 (Component/System/Module/Event)
- Def 데이터: kebab-case.def.json
- 변수: camelCase, 상수: UPPER_SNAKE_CASE
- Def ID: PascalCase, 모딩 시 modId 접두사
- 토픽: domain.action (lowercase.kebab-action)
- 위반: minor -2/건

## 규칙 11: 테스트 (TEST-001)
- 필수: ECS 컴포넌트, 시스템 핵심 로직, Def 로더, 이벤트 버스, 경로탐색, 세이브 round-trip
- 커버리지: Core 80%, Module 60%
- 성능 테스트: 틱 처리 벤치마크 CI 회귀 감지
- 테스트 파일 위치: 소스와 동일 디렉토리 (.test.ts)
- 위반: major -3/건(필수 테스트 누락)
