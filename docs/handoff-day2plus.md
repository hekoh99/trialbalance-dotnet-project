# TriBalance — Implementation Handoff

## Product Purpose (North Star)

TriBalance는 회계사가 Trial Balance CSV를 업로드하면 시스템이 자동으로 GL 계정을 분류하고
이상 항목을 탐지해서 검증 결과를 **실시간으로** 제공하는 서비스.

수동으로 계정 유형을 하나씩 확인하던 작업을 자동화해서 회계사가 **이상 항목과 검토가 필요한
항목에만 집중**할 수 있게 해주는 것이 핵심 가치.

### 핵심 기능
1. Trial Balance CSV 업로드 및 파싱
2. GL 계정 자동 분류 (Asset / Liability / Equity / Revenue / Expense)
3. 차변 = 대변 균형 검증
4. 이상 항목 탐지 (음수 잔액, 미분류 계정, 낮은 confidence)
5. 처리 상태 실시간 반영

### 성공 시나리오
- **시나리오 1 (정상)**: CSV 업로드 → 분류 완료 → Balanced ✅ + 계정 분류 결과 표시
- **시나리오 2 (이상 탐지)**: Asset 음수 잔액, 미분류 계정 존재 → 플래그 항목 하이라이트, 검토 필요 항목 즉시 파악
- **시나리오 3 (실패)**: Worker 오류 → Failed ❌ 실시간 반영, 재시도 가능

엔드투엔드 실행 절차는 **`docs/scenario-testing.md`** 참고.

---

## Current Implementation State

### Day 1–4 완료

**아키텍처**
- Repo: `apps/{api-dotnet, worker-python, web-angular}` + `infra/terraform` + `docs/`
- .NET 8 DDD 솔루션: Domain / Application / Infrastructure / Api (CQRS — Command/Query + dispatcher + pipeline behaviors)
- Angular (Tailwind v4 + Spartan UI 패키지) — standalone components
- Python Worker (FastAPI + Service Bus + Cosmos + Azure OpenAI + OpenTelemetry)

**백엔드 엔드포인트**
- `POST /api/engagements` / `GET` / `GET /:id`
- `POST /api/engagements/{id}/trial-balance` (CSV 업로드 → Postgres)
- `POST /api/engagements/{id}/validate` — validation_job (Queued) + Service Bus publish + SignalR push
- `GET /api/engagements/{id}/status` — 최신 validation job
- `GET /api/engagements/{id}/validation` — Cosmos 분류 결과
- `/hubs/validation` — SignalR hub (engagementId 그룹 구독)

**파이프라인 (CQRS)**
- 모든 유스케이스는 `ICommand<T>` / `IQuery<T>` + Handler로 구현
- `ICommandDispatcher` / `IQueryDispatcher`가 handler resolve + pipeline 실행
- `IPipelineBehavior<,>` — cross-cutting concerns 체인. 현재 `LoggingBehavior` 하나 (소요시간 + 에러 자동 로그)
- `ValidationResultConsumer` (BackgroundService)도 동일 dispatcher를 거쳐 `ApplyValidationResultCommand` 실행 → HTTP 경로와 Service Bus 경로가 같은 handler·같은 pipeline 공유

**Worker**
- Pydantic 모델 camelCase alias (.NET과 호환)
- `processing` → `completed` | `failed` 세 단계 결과 발행
- Rule-based post-processing: `low_confidence`, `negative_balance` (Asset), `unclassified`
- 공유 `balance_tolerance`(0.01) — .NET `BalanceTolerance.Epsilon`과 일치
- **Azure Monitor OpenTelemetry 연동** — OpenAI 호출은 `openai.classify` span으로 측정

**Angular**
- 엔게이지먼트 리스트/디테일, CSV 드래그앤드롭 업로드
- 업로드 → `/validate` 자동 트리거 → 대시보드 자동 네비게이션
- **Validation dashboard**:
  - 실시간 상태 뱃지 (SignalR)
  - Balance / Debits / Credits 3카드
  - **계정 유형별 6카드** (Asset / Liability / Equity / Revenue / Expense / Unclassified) — 카드 클릭 시 테이블 필터
  - Flagged items 섹션 — 유형별 필터 드롭다운
  - Classifications 테이블 — 검색, 유형 필터, "Needs review only" 체크박스, 컬럼 헤더 클릭 정렬, 필터 리셋
  - Failed 상태에서 Retry 버튼 → 새 validation_job 생성
- 엔게이지먼트 디테일에 "Re-validate" 버튼

**Observability**
- **.NET**: `Microsoft.ApplicationInsights.AspNetCore` — 자동 request/dependency tracking + `LoggingBehavior` 구조화 로그
- **Worker**: `azure-monitor-opentelemetry` — FastAPI / requests / logging 자동 instrumentation + 커스텀 `openai.classify` span (deployment, batch size 속성)

**Secret 관리**
- `.NET`: `Azure.Extensions.AspNetCore.Configuration.Secrets` 연동 완료. `Azure:KeyVault:Uri`가 설정되면 Key Vault secret이 `IConfiguration`을 덮어씀. 로컬에서는 `az login` 자격증명 사용, 프로덕션에서는 Container App managed identity
- **Secret 이름 규약**: Key Vault는 `:`를 허용하지 않으므로 `--`로 대체 (provider가 자동 변환)
  - `ConnectionStrings--Postgres`
  - `Azure--ServiceBus--ConnectionString`
  - `Azure--CosmosDb--ConnectionString`
  - `Azure--OpenAI--ApiKey`, `Azure--OpenAI--Endpoint`
  - `ApplicationInsights--ConnectionString`
- Worker는 Key Vault 직접 로드 없이 환경변수만 읽음. 로컬은 `.env`, 배포는 Container App이 Key Vault secret을 env var로 주입 (Day 5)

### Azure 리소스 현황
- [x] PostgreSQL Flexible Server, Cosmos DB (2 containers), Service Bus (2 queues), Key Vault, Application Insights + Log Analytics, ACR, Container App Environment
- [ ] Azure OpenAI — 별도 리소스로 관리. Terraform `modules/openai`는 주석 처리
- [ ] Container Apps — 아직 미배포 (Day 5)

---

## 메시지 계약 (큐 스키마)

**Request queue** `tb-validation-request` — API → Worker, camelCase JSON:
```json
{
  "engagementId": "uuid",
  "trialBalanceId": "uuid",
  "validationJobId": "uuid",
  "glEntries": [
    { "accountCode": "...", "accountName": "...", "debit": 0, "credit": 0, "balance": 0 }
  ]
}
```

**Result queue** `tb-validation-result` — Worker → API, camelCase JSON:
```json
{
  "validationJobId": "uuid",
  "status": "processing | completed | failed",
  "errorMessage": "string | null"
}
```

---

## 아키텍처 원칙 — 유지해야 할 결정들

1. **Worker는 Postgres를 건드리지 않는다.** 상태 전이는 전부 API 책임. 통신은 Service Bus 결과 큐.
2. **IsBalanced는 공유 tolerance (0.01)**. `.NET BalanceTolerance.Epsilon`과 Worker `settings.balance_tolerance`가 동일.
3. **이상 탐지는 LLM + 결정론 규칙 혼합.** `low_confidence`, `negative_balance` (Asset), `unclassified`는 Worker rule-based 후처리. LLM은 분류 판단과 `unusual_account_naming`처럼 판단이 필요한 flag만.
4. **재시도는 새 job 생성.** `/validate` 호출마다 새 `validation_jobs` 레코드 INSERT — 과거 job은 audit용으로 보존.
5. **업로드 → 검증 자동 연결.** 업로드 성공 후 Angular가 즉시 `/validate` 호출 + 대시보드 이동.
6. **Service Bus DLQ** — Worker의 명시적 `failed` 발행이 1차 경로. DLQ 폴러는 Day 5+.
7. **camelCase 통일.** `.NET JsonNamingPolicy.CamelCase` ↔ Worker Pydantic `alias_generator=to_camel`.
8. **Command/Query 패턴이 단일 진실 공급원.** 새 유스케이스는 Command/Query + Handler를 먼저 만들고, Endpoint나 BackgroundService는 dispatcher를 얇게 호출만. Cross-cutting 추가도 `IPipelineBehavior`로.

---

## Day 5 작업 항목 (배포)

1. `.NET API` Dockerfile 작성 + ACR push
2. Python Worker Dockerfile는 이미 있음 → ACR push
3. `modules/container_apps/main.tf` 주석 해제 + `terraform apply`
4. Container App managed identity 활성화 + Key Vault access policy 부여
5. Container App secret reference (Key Vault) + env var 매핑
6. Front-end 배포 (Static Web Apps)
7. README 마무리

---

## 환경변수 / Secret 정책

- **로컬**: `appsettings.Development.json` (.NET) + `.env` (Worker) — 둘 다 `.gitignore`
- **프로덕션**: Key Vault가 source of truth. `.NET`은 Key Vault provider로 직접 읽음. Worker는 Container App이 Key Vault secret을 env var로 주입
- Azure OpenAI endpoint/key는 별도 리소스로 관리. Terraform `modules/openai`는 주석 처리 상태

---

## 상호 참조: 코드 위치 ↔ 책임 매핑

### .NET — Application (use cases)
| 책임 | 경로 |
|------|------|
| Command/Query/Handler/Dispatcher 인프라 | `TriBalance.Application/Common/Messaging/` |
| Pipeline behavior (logging) | `TriBalance.Application/Common/Behaviors/LoggingBehavior.cs` |
| DI 등록 (assembly 스캔) | `TriBalance.Application/Common/Messaging/ApplicationServiceCollectionExtensions.cs` |
| Engagement commands/queries | `TriBalance.Application/Engagements/Commands/…`, `.../Queries/…` |
| Validation commands/queries | `TriBalance.Application/Validation/Commands/…`, `.../Queries/…` |
| Application-layer DTOs | `TriBalance.Application/Engagements/EngagementDtos.cs`, `.../Validation/ValidationDtos.cs`, `IValidationResultReader.cs` |
| Application exceptions | `TriBalance.Application/Engagements/ApplicationExceptions.cs` |
| Outbound ports (publisher/notifier/reader/parser) | `TriBalance.Application/Validation/I*.cs`, `TriBalance.Application/Engagements/IGlEntryCsvParser.cs` |

### .NET — Domain
| 책임 | 경로 |
|------|------|
| Aggregates + Entities | `TriBalance.Domain/Engagement/`, `.../Validation/` |
| 공유 Balance Tolerance | `TriBalance.Domain/Engagement/BalanceTolerance.cs` |
| 리포지토리 인터페이스 | `TriBalance.Domain/{Engagement,Validation}/I*Repository.cs` |

### .NET — Infrastructure (adapters)
| 책임 | 경로 |
|------|------|
| EF Core DbContext / 마이그레이션 | `TriBalance.Infrastructure/Persistence/PostgreSQL/` |
| Postgres 리포지토리 구현 | `.../PostgreSQL/Repositories/Postgres*Repository.cs` |
| CSV 파서 구현 | `.../PostgreSQL/CsvParsing/CsvHelperGlEntryParser.cs` |
| Cosmos 옵션/문서/리포지토리 | `TriBalance.Infrastructure/Persistence/CosmosDB/` |
| Service Bus Publisher | `TriBalance.Infrastructure/Messaging/ServiceBusPublisher.cs` |
| Service Bus Result Consumer | `TriBalance.Infrastructure/Messaging/ValidationResultConsumer.cs` |
| Service Bus JSON/옵션/메시지 DTO | `.../Messaging/ServiceBus*.cs`, `ValidationMessages.cs` |
| Disabled fallback (로컬 dev) | `.../Messaging/DisabledValidationRequestPublisher.cs`, `.../CosmosDB/DisabledValidationResultRepository.cs` |

### .NET — Api
| 책임 | 경로 |
|------|------|
| Program.cs (Key Vault, App Insights, DI) | `TriBalance.Api/Program.cs` |
| Endpoint 매핑 (얇음 — dispatcher만 호출) | `TriBalance.Api/Endpoints/*.cs` |
| SignalR Hub + Notifier 구현 | `TriBalance.Api/Hubs/ValidationHub.cs`, `.../ValidationStatusNotifier.cs` |

### Worker
| 책임 | 경로 |
|------|------|
| FastAPI 진입점 + App Insights 초기화 | `apps/worker-python/app/main.py` |
| Service Bus 소비 루프 + 실패 경로 | `apps/worker-python/app/worker.py` |
| Azure OpenAI 분류 + 결정론적 flag + OTel span | `apps/worker-python/app/services/classification_service.py` |
| Result queue 발행 | `apps/worker-python/app/services/servicebus_service.py` |
| Cosmos 저장 | `apps/worker-python/app/services/cosmos_service.py` |
| Pydantic 모델 (camelCase alias) | `apps/worker-python/app/domain/models.py` |
| 설정 / 공유 임계값 | `apps/worker-python/app/config.py` |

### Angular
| 책임 | 경로 |
|------|------|
| Engagement list / detail | `src/app/features/engagements/…` |
| CSV 업로드 → validate 자동 연결 | `.../trial-balance-upload/trial-balance-upload.component.ts` |
| Validation dashboard (카드/필터/정렬/재시도) | `src/app/features/validation/validation-dashboard/…` |
| Engagement / Validation service | `src/app/core/services/{engagement,validation}.service.ts` |
| SignalR service | `src/app/core/services/signalr.service.ts` |
| 도메인 모델 | `src/app/core/models/*.model.ts` |

### Docs / 픽스처
| 목적 | 경로 |
|------|------|
| 시나리오 테스트 절차 | `docs/scenario-testing.md` |
| 시나리오 1 CSV (정상) | `docs/sample-trial-balance.csv` |
| 시나리오 2 CSV (이상 탐지) | `docs/sample-trial-balance-anomalies.csv` |
| Unbalanced 샘플 | `docs/sample-trial-balance-unbalanced.csv` |
| 아키텍처 / 도메인 요약 | `docs/architecture.md`, `docs/domain.md` |
