# {API 이름}

## 기본 정보
- **제공사**: 
- **Base URL**: 
- **인증 방식**: 
- **API 버전**: 
- **Rate Limit**: 
- **Secret 참조**: `secrets/_keymap.json` → `{secret-ref-name}`

## 공통 헤더
```
Content-Type: application/json
Authorization: Bearer ${SECRET_REF}
```

## 공통 응답 형식
```json
{
  "status": "success | error",
  "data": {},
  "error": {
    "code": "에러 코드",
    "message": "에러 메시지"
  }
}
```

## 공통 에러 코드
| 코드 | 의미 | 대응 |
|------|------|------|
| 401 | 인증 실패 | 키 확인 |
| 429 | Rate Limit 초과 | 재시도 대기 |

## 주요 기능 요약
<!-- 하위 endpoints/_index.json에 상세 목록이 있으므로 여기는 요약만 -->
1. 기능1 - 한 줄 설명
2. 기능2 - 한 줄 설명

## 문서 출처
- 원본: {공식 문서 URL}
- 마지막 동기화: {날짜}
- 마지막 라이브 검증: {날짜}
