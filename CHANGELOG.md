# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- `.meta` files for the package, so installs from a git URL import the scripts. CI checks that every package file has one.
- The `Basic Usage` sample listed in `package.json` (`Samples~/BasicUsage`).

## [2.5.0] - 2026-11-17

### Fixed
- `AnalyzeAsync` now returns the most severe `recommended_action` across its checks. It previously matched a value the API never returns, so `flag_for_review` and `block` came back as `none`.
- Bullying results now include `BullyingReported`, `BullyingReportedBy` and `SpeakerRole`. A target or bystander reporting bullying has `IsBullying` false but still needs attention.
- Requests and responses use the invariant culture for numbers. Under German, French or Swedish locales, decimals were sent as `0,5` (invalid JSON).
- Retries honour `Retry-After` and only repeat rate limits, server errors, network errors and timeouts. Other errors (validation, conflicts, unknown statuses) are no longer retried. `maxRetries` now counts retries after the first attempt, so `maxRetries: 0` sends once.
- Errors read the API error envelope: `Code`, `RequestId` and `HttpStatus` on every `TuteliqException`, and `RetryAfterSeconds` on `RateLimitException` and `ServerException`. A 429 for a used-up allowance is a `QuotaExceededException`.
- A successful response whose body is empty or not a JSON object (for example a proxy or captive portal page) now throws a `TuteliqException` instead of reading as a clean result. `GetConsentStatusAsync` escapes the consent type in the query.
- Webhooks: create sends the required `Name`; update uses `PUT` with `is_active`; test and regenerate-secret use the API's routes; `WebhookInfo` reads `IsActive`, `Name`, `FailureCount`, `LastTriggeredAt` and `LastError`.
- `GetUsageByToolAsync` calls `/api/v1/usage/by-tool`.
- `Language` lists the 32 supported codes, and an unknown code no longer parses as English.
- Grooming results include `MessageAnalysis`; detection results include it when the API returns it.

### Added
- `ModerationAction`, `ModerationActions` and an `Action` property on detection results.
- Grooming: `SenderAge` per message and `ParticipantAge`.
- Verification sessions: `CreateVerificationSessionAsync`, `GetVerificationSessionAsync`, `CancelVerificationSessionAsync`.
- Incidents: `ListIncidentsAsync`, `GetIncidentAsync`, `GetIncidentsOverviewAsync`, `GetIncidentTrendsAsync`, `ReviewIncidentAsync`, `BatchReviewIncidentsAsync`.
- `DetectNicknameAsync`, `VerifyImageMatchAsync`, `RedactAsync`, `DetectDistressSignalsAsync` and `AnalyzeDocumentAsync`.
- CI compiles the Runtime scripts and runs the tests.

### Changed
- Webhook results follow the API: `CreateWebhookResult` has `Webhook` and `Secret`; `TestWebhookResult` has `Success`, `StatusCode`, `LatencyMs` and `Error`; `RegenerateSecretResult` has `Secret`; `DeleteWebhookResult` has `Success`. Fields the API never returned were removed, and `CreateWebhookInput.Active` was removed (new webhooks are active). `WebhookInfo.Active` is kept as an obsolete alias of `IsActive`.
- `EnumExtensions.ParseLanguage` returns `Language?`.

### Removed
- README documentation for `VerifyAgeAsync` and `VerifyIdentityAsync`, which never existed in this SDK. Use verification sessions.

## [2.2.0] - 2026-02-16

### Added
- `CreditsUsed` field on all result types for tracking API credit consumption
- Voice analysis (`AnalyzeVoiceAsync`) with multipart file upload
- Image analysis (`AnalyzeImageAsync`) with multipart file upload
- GDPR account management (delete, export, consent, rectify, audit logs)
- Breach management (log, list, get, update)
- Webhook management (list, create, update, delete, test, regenerate secret)
- Pricing endpoints (overview and detailed)
- Usage tracking (history, by tool, monthly)

## [1.0.0] - 2025-02-06

### Added
- Initial release
- Full support for all Tuteliq API endpoints
- Bullying detection
- Grooming detection
- Unsafe content detection
- Quick analysis (combined detection)
- Emotion analysis
- Action plan generation
- Incident report generation
- External ID and metadata tracking
- Automatic retry with exponential backoff
- Comprehensive error handling
- Unity 2021.3+ support
