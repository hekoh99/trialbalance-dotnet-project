# TriBalance — Implementation Handoff

## Product Purpose (North Star)

TriBalance는 회계사가 Trial Balance CSV를 업로드하면 시스템이 자동으로 GL 계정을 분류하고
이상 항목을 탐지해서 검증 결과를 **실시간으로** 제공하는 서비스.

수동으로 계정 유형을 하나씩 확인하던 작업을 자동화해서 회계사가 **이상 항목과 검토가 필요한
항목에만 집중**할 수 있게 해주는 것이 핵심 가치.

### 핵심 기능 (모두 end-user가 경험해야 함)
1. Trial Balance CSV 업로드 및 파싱
2. GL 계정 자동 분류 (Asset / Liability / Equity / Revenue / Expense)
3. 차변 = 대변 균형 검증
4. 이상 항목 탐지 (음수 잔액, 미분류 계정, 낮은 confidence)
5. 처리 상태 실시간 반영

### 성공 시나리오 (구현 완료 시 전부 작동해야 함)
- **시나리오 1 (정상)**: CSV 업로드 → 분류 완료 → Balanced ✅ + 계정 분류 결과 표시
- **시나리오 2 (이상 탐지)**: Asset 음수 잔액, 미분류 계정 존재 → 플래그 항목 하이라이트, 검토 필요 항목 즉시 파악
- **시나리오 3 (실패)**: Worker 오류 → Failed ❌ 실시간 반영, 재시도 가능

---

## Current Implementation State

### 완료 (Day 1-3)

**Day 1-2 기반**
- [x] Repo 구조 + .NET 8 DDD 솔루션 (Domain/Application/Infrastructure/Api)
- [x] EF Core 마이그레이션 → Azure PostgreSQL (4개 테이블)
- [x] Terraform 모듈 (postgres, cosmos, servicebus, keyvault, observability, container_apps)
- [x] Angular (Tailwind + Spartan UI): 엔게이지먼트 리스트/디테일, CSV 업로드 드래그앤드롭
- [x] Python Worker 코드 (Azure OpenAI SDK, Service Bus consumer, Cosmos 저장)
- [x] CORS, Swagger, 업로드→Postgres 검증됨

**Day 3 전체 파이프라인**
- [x] `.NET API` 엔드포인트 전체:
  - `POST /api/engagements` / `GET /api/engagements` / `GET /api/engagements/{id}`
  - `POST /api/engagements/{id}/trial-balance` (CSV 업로드 → Postgres 저장)
  - `POST /api/engagements/{id}/validate` — validation_job 생성(Queued) → Service Bus 발행 → SignalR push
  - `GET /api/engagements/{id}/status` — 최신 validation job 상태
  - `GET /api/engagements/{id}/validation` — Cosmos DB에서 분류 결과 조회
- [x] `ServiceBusPublisher` — tb-validation-request 발행 (camelCase JSON)
- [x] `ValidationResultConsumer` (BackgroundService) — tb-validation-result 구독 → job 상태 전이 → SignalR push
- [x] `ValidationHub` + `/hubs/validation` — SignalR 그룹(engagementId 기준) 구독
- [x] 공유 메시지 계약 (`ValidationRequestMessage`, `ValidationResultMessage`) — camelCase 통일
- [x] Python Worker:
  - Pydantic 모델에 camelCase alias 적용 (.NET과 호환)
  - 처리 시작 시 `processing`, 성공 시 `completed`, 실패 시 `failed` + errorMessage 발행
  - Rule-based post-processing: `low_confidence`, `negative_balance` (Asset), `unclassified` 플래그
  - 공유 `balance_tolerance` 사용
- [x] Angular `validation-dashboard` — SignalR 연결, 상태 뱃지, Balanced/Variance, 계정별 집계, 플래그 항목 하이라이트, 분류 결과 테이블, 실패 시 Retry 버튼
- [x] 업로드 → `/validate` 자동 트리거 → 대시보드로 자동 네비게이션 (UX 자동화)
- [x] 엔게이지먼트 디테일에 "Re-validate" 버튼 (시나리오 3 수동 재시도)
- [x] Local-dev graceful degradation: Service Bus / Cosmos 연결 문자열 미설정 시 API는 여전히 기동, `/validate`는 503, `/validation`은 404로 응답 (Day 1-2 기능은 영향 없음)

### Azure 리소스 현황
- [x] PostgreSQL, Cosmos DB, Service Bus, Key Vault, App Insights, ACR, Container App Environment
- [ ] Azure OpenAI — **별도 리소스로 관리**. Terraform `modules/openai`는 주석 처리된 상태. key/endpoint는 root `variables.tf`에서 받아 Key Vault에 저장
- [ ] Container Apps — Docker 이미지 빌드 전이라 `modules/container_apps/main.tf`에서 주석 처리됨

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

Worker는 메시지 수신 직후 `processing`을 발행하고, 작업 종료 시점에 `completed` 또는
`failed`를 발행. API의 `ValidationResultConsumer`가 이 전이를 받아 Postgres `validation_jobs.status`를
업데이트하고 SignalR `ValidationStatusUpdated` 이벤트로 그룹에 푸시한다. Worker는 Postgres에
직접 접근하지 않음 (결합도 최소화, Issue E의 옵션 A).

## 아키텍처 원칙 — 유지해야 할 결정들

1. **Worker는 Postgres를 건드리지 않는다.** 상태 전이는 전부 API 책임. 통신 채널은 Service Bus 결과 큐.
2. **IsBalanced는 공유 tolerance 사용.** `.NET BalanceTolerance.Epsilon`과 Worker `settings.balance_tolerance`가 동일 임계값 (0.01). 두 곳의 판정이 달라지면 안 됨.
3. **이상 탐지는 LLM + 결정론 규칙 혼합.** `low_confidence`, `negative_balance` (Asset), `unclassified`는 Worker 내 rule-based 후처리에서 발행. LLM은 분류 판단과 "unusual_account_naming"처럼 판단이 필요한 flag만 담당. 재현성 확보.
4. **재시도는 새 job 생성.** `/validate` 호출 시 기존 job이 있어도 새 `validation_jobs` 레코드 INSERT → 새 메시지 발행. Retry 버튼 구현에서 그대로 활용.
5. **업로드 → 검증은 자동 연결.** 업로드 성공 후 Angular가 즉시 `/validate` 호출하고 대시보드로 이동. "자동화" 가치 유지.
6. **Service Bus DLQ는 현재 단계에서는 Worker의 명시적 `failed` 발행으로 충분.** DLQ 폴러는 운영 단계에서 별도 고려.
7. **camelCase 통일.** .NET `JsonNamingPolicy.CamelCase`, Worker Pydantic `alias_generator=to_camel`. 양쪽 `model_dump(by_alias=True)` / `PropertyNameCaseInsensitive` 옵션 사용.

---

## Day 4 작업 항목 (UX 다듬기 + 운영 기초)

Day 3로 핵심 파이프라인은 완성됐음. Day 4는 회계사가 실사용할 때 필요한 UX 세부와
운영 관찰성 기초.

1. **connection string 주입** — `appsettings.Development.json`에 Service Bus / Cosmos 연결 문자열 채우기. Worker도 `.env` 작성. Azure OpenAI는 이미 구성됨. 이 단계가 끝나야 실제 엔드투엔드 테스트 가능.
2. **시나리오 테스트 3종**
   - 시나리오 1: 정상 CSV → Completed 도달, 분류/집계 표시
   - 시나리오 2: 음수 잔액 Asset + 애매한 계정명 포함 CSV → flagged items 하이라이트
   - 시나리오 3: Worker 강제 실패 (예: OpenAI key 빈값) → Failed + Retry 버튼 동작
3. **대시보드 다듬기** (선택)
   - flagged items 필터/정렬, GL entries 검색
   - 계정 유형별 집계를 카드 형태로 강조 (현재는 뱃지)
4. **Application Insights 연동** — `.NET API`, `Worker` 로그를 App Insights로 흘려서 Service Bus 지연, Cosmos RU, OpenAI latency를 관찰
5. **Key Vault 연결** — 로컬은 환경변수 그대로, 배포 환경에서는 Container App이 managed identity로 Key Vault 접근

---

## Day 5 (배포)

- `.NET API` Dockerfile 추가 (현재 없음), ACR push
- Python Worker Dockerfile 이미 있음, ACR push
- `modules/container_apps/main.tf` 주석 해제, `terraform apply`
- Container App에 Key Vault 참조 설정 (managed identity + secret mount)
- README 작성

---

## 환경변수 / Secret 정책

- 로컬 개발: `appsettings.Development.json` (.NET), `.env` (Worker) — 모두 `.gitignore`
- 프로덕션: Key Vault가 source of truth. Container App이 managed identity로 Key Vault 접근, 환경변수로 주입. Worker의 `app/config.py`는 이미 환경변수 기반으로 설계되어 있어 그대로 동작
- OpenAI endpoint/key: 별도 리소스로 관리 중. `terraform.tfvars`의 `azure_openai_key`, `azure_openai_endpoint` 값으로 Key Vault에 저장됨

---

## 상호 참조: 코드 위치 ↔ 작업 매핑 (Day 3 기준)

| 책임 | 파일 경로 |
|------|----------|
| `/validate`, `/validation`, `/status` 엔드포인트 | `TriBalance.Api/Endpoints/ValidationEndpoints.cs` |
| Service Bus 메시지 DTO | `TriBalance.Infrastructure/Messaging/ValidationMessages.cs` |
| Service Bus JSON 옵션 | `TriBalance.Infrastructure/Messaging/ServiceBusJson.cs` |
| Service Bus 옵션 바인딩 | `TriBalance.Infrastructure/Messaging/ServiceBusOptions.cs` |
| Service Bus Publisher | `TriBalance.Infrastructure/Messaging/ServiceBusPublisher.cs` |
| Service Bus Publisher (disabled fallback) | `TriBalance.Infrastructure/Messaging/DisabledValidationRequestPublisher.cs` |
| Service Bus Result Consumer (BackgroundService) | `TriBalance.Infrastructure/Messaging/ValidationResultConsumer.cs` |
| SignalR Hub | `TriBalance.Api/Hubs/ValidationHub.cs` |
| SignalR Notifier | `TriBalance.Api/Hubs/ValidationStatusNotifier.cs` |
| SignalR ↔ Infrastructure 어댑터 | `TriBalance.Api/Hubs/ValidationStatusNotifierAdapter.cs` |
| Cosmos Options | `TriBalance.Infrastructure/Persistence/CosmosDB/CosmosOptions.cs` |
| Cosmos 문서 DTO | `TriBalance.Infrastructure/Persistence/CosmosDB/ClassificationResultDocument.cs` |
| Cosmos 조회 리포지토리 | `TriBalance.Infrastructure/Persistence/CosmosDB/CosmosValidationResultRepository.cs` |
| Cosmos 리포지토리 (disabled fallback) | `TriBalance.Infrastructure/Persistence/CosmosDB/DisabledValidationResultRepository.cs` |
| 공유 Balance Tolerance | `TriBalance.Domain/Engagement/BalanceTolerance.cs` |
| Worker 실패/성공/processing 발행 | `apps/worker-python/app/worker.py`, `app/services/servicebus_service.py` |
| Worker 후처리 플래그 | `apps/worker-python/app/services/classification_service.py` (`_deterministic_flags`) |
| Worker Pydantic alias (camelCase) | `apps/worker-python/app/domain/models.py` |
| Angular validation service | `apps/web-angular/src/app/core/services/validation.service.ts` |
| Angular SignalR service | `apps/web-angular/src/app/core/services/signalr.service.ts` |
| Angular validation dashboard | `apps/web-angular/src/app/features/validation/validation-dashboard/` |
| Angular 업로드 → validate 연결 | `apps/web-angular/src/app/features/engagements/trial-balance-upload/trial-balance-upload.component.ts` |
| Angular Re-validate 버튼 | `apps/web-angular/src/app/features/engagements/engagement-detail/engagement-detail.component.ts` |
