# {METHOD} {PATH}

## 요약
{한 줄 설명}

## Request

### Headers
| 키 | 값 | 필수 |
|----|-----|------|
| Authorization | Bearer ${SECRET_REF} | Y |

### Path Parameters
| 파라미터 | 타입 | 필수 | 설명 |
|----------|------|------|------|

### Query Parameters
| 파라미터 | 타입 | 필수 | 기본값 | 설명 |
|----------|------|------|--------|------|

### Request Body
```json
{
  "field": "type - 설명"
}
```

## Response

### 성공 (200)
```json
{
  "field": "type - 설명"
}
```

### 에러
| 코드 | 상황 | 응답 예시 |
|------|------|----------|

## 예제
```bash
curl -X {METHOD} "{BASE_URL}{PATH}" \
  -H "Authorization: Bearer ${SECRET_REF}" \
  -H "Content-Type: application/json" \
  -d '{}'
```

## 라이브 검증
- **검증 여부**: 미검증 | 검증됨
- **검증일**: 
- **문서 대비 차이점**: 없음 | {차이점 설명}
- **스냅샷 파일**: `../snapshots/{snapshot-file}.json`
