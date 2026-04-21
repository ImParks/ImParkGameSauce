# 3G: 온도/날씨/계절 기초

## 범위
월드 싱글턴 컴포넌트 3개 + 경량 System 3개. 타일별 온도 시뮬레이션은 Phase 4로 지연.

## 컴포넌트
| Component | Fields | 직렬화 delta |
|-----------|--------|-------------|
| WorldWeatherComponent | CurrentWeatherDefId, TimeInCurrentTicks, NextTransitionCheckTick, LastTransitionTick, RngStreamId | Clear+Ticks=0 시 통째 생략. schemaVersion=1 |
| WorldTemperatureComponent | OutdoorCelsius, SeasonalBaselineC, WeatherOffsetC, DailyOscillationC, LastComputedTick | 전부 기본값 시 생략. schemaVersion=1 |
| SeasonComponent | CurrentSeasonDefId, DayOfSeason, YearsElapsed, SeasonProgress01 | 기본값 시 생략. schemaVersion=1 |
| OutdoorMarkerComponent (stub) | IsOutdoor(bool) | Phase 3G: IsWalkable로 위임. Phase 4 RoofGrid 대체 |

## Def
| Def | 주요 필드 |
|-----|----------|
| WeatherDef | WeatherType(Clear/Rain/Snow), AverageDurationTicks, MinDurationTicks, RainfallRate, SnowfallRate, VisibilityMultiplier, TemperatureOffsetC, TransitionWeights(Dict), AllowedSeasons |
| SeasonDef | BaseTemperatureC, LengthInDays(15), NextSeasonDefId(순환), DailyMinC, DailyMaxC, GrowthSuitability |
| BiomeDef (기초) | BaseTemperatureOffsetC, WeatherBias(Dict), AllowedPlants. Phase 3G: TemperateForest 1개만 |

## System
| System | Order | Phase | 틱 예산 | 주기 |
|--------|-------|-------|---------|------|
| SeasonSystem | 200 | Events | 0.01ms | 매틱 (day 전이 시에만 로직) |
| WeatherSystem | 210 | Events | 0.02ms/틱 | 10틱마다 확률 전이 |
| TemperatureSystem | 220 | Events | 0.25ms/틱 | 2틱마다 계산 |

## 온도 공식
```
OutdoorC = SeasonDef.BaseTemperatureC
         + BiomeDef.BaseTemperatureOffsetC
         + WeatherDef.TemperatureOffsetC
         + cos(2π * dayProgress) * (DailyMax-DailyMin)/2
```

## 모듈 4종
| Module | 기본 | OFF 시 | no-op fallback |
|--------|------|--------|---------------|
| Season | 항상 ON (core) | N/A | N/A |
| Weather | ON | Clear 고정 | NoopWeatherQuery |
| Temperature | ON | 21.0°C 고정 (RULE-009) | NoopTemperatureQuery |
| Biome | ON | TemperateForest offset=0 | N/A |

## Query API
```csharp
interface IWeatherQuery {
    WeatherSnapshot GetCurrentWeather();
    float GetOutdoorTemp();
    bool IsPrecipitating();
    float GetVisibilityMultiplier();
}
interface ISeasonQuery {
    SeasonSnapshot GetCurrentSeason();
    float GetSeasonProgress();
    int GetDayOfSeason();
    int GetYearsElapsed();
}
interface ITemperatureQuery {
    float GetOutdoorCelsius();
    float GetTileCelsius(int x, int y); // Phase 3G: Outdoor로 위임
}
```

## 이벤트 토픽
- `season.changed` (sync) — Tick, FromDefId, ToDefId, YearsElapsed
- `weather.changed` (async) — Tick, FromDefId, ToDefId, Intensity
- `temperature.changed` (async, ΔC≥0.5°C 시) — Tick, OutdoorC, DeltaC

## Phase 4 확장 훅
- ITileTemperatureUpdater → Phase 3G: NoopTileTemperatureUpdater
- IStorytellerWeatherBias → Phase 3G: DefaultBias
- IPlantGrowthEnvironment → Phase 3G: NoopPlantGrowthEnvironment (GrowthRate=1.0)
