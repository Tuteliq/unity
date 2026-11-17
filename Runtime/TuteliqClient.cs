using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Tuteliq
{
    /// <summary>
    /// Tuteliq API client for child safety analysis.
    /// </summary>
    /// <remarks>
    /// The API key grants full access to your account. Use this client from a
    /// game server or backend you control, or point <c>baseUrl</c> at your own
    /// proxy that adds the key. Do not ship the key inside a game build.
    /// </remarks>
    /// <example>
    /// <code>
    /// var client = new TuteliqClient(apiKey);
    /// var result = await client.DetectBullyingAsync("Some text to analyze");
    /// if (ModerationActions.NeedsReview(result.Action))
    /// {
    ///     Debug.Log($"Needs review: {result.Rationale}");
    /// }
    /// </code>
    /// </example>
    public partial class TuteliqClient
    {
        private const string DefaultBaseUrl = "https://api.tuteliq.ai";
        private const string SdkIdentifier = "Unity SDK";

        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly float _timeout;
        private readonly int _maxRetries;
        private readonly float _retryDelay;
        private readonly ITransport _transport;
        private readonly Func<TimeSpan, Task> _delay;

        /// <summary>
        /// Current usage statistics (updated after each request).
        /// </summary>
        public Usage Usage { get; private set; }

        /// <summary>
        /// Request ID from the last API call.
        /// </summary>
        public string LastRequestId { get; private set; }

        /// <summary>
        /// Creates a new Tuteliq client.
        /// </summary>
        /// <param name="apiKey">Your Tuteliq API key, or a token your own proxy accepts.</param>
        /// <param name="timeout">Request timeout in seconds (default: 30).</param>
        /// <param name="maxRetries">Retries after the first attempt for rate limits, server errors, network errors and timeouts (default: 3).</param>
        /// <param name="retryDelay">Initial retry delay in seconds, doubled on each retry (default: 1). A Retry-After from the API takes precedence.</param>
        /// <param name="baseUrl">API base URL, or the URL of your proxy.</param>
        public TuteliqClient(
            string apiKey,
            float timeout = 30f,
            int maxRetries = 3,
            float retryDelay = 1f,
            string baseUrl = DefaultBaseUrl)
            : this(apiKey, new UnityWebRequestTransport(), null, timeout, maxRetries, retryDelay, baseUrl)
        {
        }

        internal TuteliqClient(
            string apiKey,
            ITransport transport,
            Func<TimeSpan, Task> delay,
            float timeout = 30f,
            int maxRetries = 3,
            float retryDelay = 1f,
            string baseUrl = DefaultBaseUrl)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("API key is required", nameof(apiKey));
            if (apiKey.Length < 10)
                throw new ArgumentException("API key appears to be invalid", nameof(apiKey));

            _apiKey = apiKey;
            _timeout = timeout;
            _maxRetries = Math.Max(0, maxRetries);
            _retryDelay = retryDelay;
            _baseUrl = baseUrl;
            _transport = transport;
            _delay = delay ?? (wait => Task.Delay(wait));
        }

        // =====================================================================
        // Safety Detection
        // =====================================================================

        /// <summary>
        /// Detect bullying in content.
        /// </summary>
        public async Task<BullyingResult> DetectBullyingAsync(
            string content,
            AnalysisContext context = null,
            string externalId = null,
            string customerId = null,
            Dictionary<string, object> metadata = null)
        {
            var body = new Dictionary<string, object> { { "text", content } };
            if (context != null)
                body["context"] = ContextToDict(context);
            else
                body["context"] = new Dictionary<string, object> { { "platform", ResolvePlatform() } };
            if (externalId != null) body["external_id"] = externalId;
            if (customerId != null) body["customer_id"] = customerId;
            if (metadata != null) body["metadata"] = metadata;

            var response = await RequestAsync("/api/v1/safety/bullying", body);
            return ParseBullyingResult(response);
        }

        /// <summary>
        /// Detect grooming patterns in a conversation.
        /// </summary>
        public async Task<GroomingResult> DetectGroomingAsync(DetectGroomingInput input)
        {
            var messages = new List<object>();
            foreach (var msg in input.Messages)
                messages.Add(GroomingMessageToDict(msg));

            var body = new Dictionary<string, object> { { "messages", messages } };

            var context = new Dictionary<string, object>();
            if (input.ChildAge.HasValue) context["child_age"] = input.ChildAge.Value;
            if (input.ParticipantAge.HasValue) context["participant_age"] = input.ParticipantAge.Value;
            if (input.Context != null)
            {
                foreach (var kvp in ContextToDict(input.Context))
                    context[kvp.Key] = kvp.Value;
            }
            if (!context.ContainsKey("platform"))
                context["platform"] = ResolvePlatform();
            body["context"] = context;

            if (input.ExternalId != null) body["external_id"] = input.ExternalId;
            if (input.CustomerId != null) body["customer_id"] = input.CustomerId;
            if (input.Metadata != null) body["metadata"] = input.Metadata;

            var response = await RequestAsync("/api/v1/safety/grooming", body);
            return ParseGroomingResult(response);
        }

        private static Dictionary<string, object> GroomingMessageToDict(GroomingMessage msg)
        {
            var dict = new Dictionary<string, object>
            {
                { "sender_role", msg.Role.ToApiString() },
                { "text", msg.Content }
            };
            if (msg.SenderAge.HasValue) dict["sender_age"] = msg.SenderAge.Value;
            return dict;
        }

        /// <summary>
        /// Detect unsafe content.
        /// </summary>
        public async Task<UnsafeResult> DetectUnsafeAsync(
            string content,
            AnalysisContext context = null,
            string externalId = null,
            string customerId = null,
            Dictionary<string, object> metadata = null)
        {
            var body = new Dictionary<string, object> { { "text", content } };
            if (context != null)
                body["context"] = ContextToDict(context);
            else
                body["context"] = new Dictionary<string, object> { { "platform", ResolvePlatform() } };
            if (externalId != null) body["external_id"] = externalId;
            if (customerId != null) body["customer_id"] = customerId;
            if (metadata != null) body["metadata"] = metadata;

            var response = await RequestAsync("/api/v1/safety/unsafe", body);
            return ParseUnsafeResult(response);
        }

        /// <summary>
        /// Quick analysis - runs bullying and unsafe detection.
        /// <see cref="AnalyzeResult.RecommendedAction"/> is the most severe action across the checks that ran.
        /// </summary>
        public async Task<AnalyzeResult> AnalyzeAsync(
            string content,
            AnalysisContext context = null,
            List<string> include = null,
            string externalId = null,
            string customerId = null,
            Dictionary<string, object> metadata = null)
        {
            var checks = include ?? new List<string> { "bullying", "unsafe" };

            BullyingResult bullyingResult = null;
            UnsafeResult unsafeResult = null;

            if (checks.Contains("bullying"))
                bullyingResult = await DetectBullyingAsync(content, context, externalId, customerId, metadata);

            if (checks.Contains("unsafe"))
                unsafeResult = await DetectUnsafeAsync(content, context, externalId, customerId, metadata);

            var result = AnalyzeResultBuilder.Build(bullyingResult, unsafeResult);
            result.ExternalId = externalId;
            result.CustomerId = customerId;
            result.Metadata = metadata;
            return result;
        }

        // =====================================================================
        // Emotion Analysis
        // =====================================================================

        /// <summary>
        /// Analyze emotions in content.
        /// </summary>
        public async Task<EmotionsResult> AnalyzeEmotionsAsync(
            string content,
            AnalysisContext context = null,
            string externalId = null,
            string customerId = null,
            Dictionary<string, object> metadata = null)
        {
            var body = new Dictionary<string, object>
            {
                { "messages", new List<Dictionary<string, string>>
                    {
                        new Dictionary<string, string> { { "sender", "user" }, { "text", content } }
                    }
                }
            };
            if (context != null)
                body["context"] = ContextToDict(context);
            else
                body["context"] = new Dictionary<string, object> { { "platform", ResolvePlatform() } };
            if (externalId != null) body["external_id"] = externalId;
            if (customerId != null) body["customer_id"] = customerId;
            if (metadata != null) body["metadata"] = metadata;

            var response = await RequestAsync("/api/v1/analysis/emotions", body);
            return ParseEmotionsResult(response);
        }

        // =====================================================================
        // Guidance
        // =====================================================================

        /// <summary>
        /// Get age-appropriate action guidance.
        /// </summary>
        public async Task<ActionPlanResult> GetActionPlanAsync(GetActionPlanInput input)
        {
            var body = new Dictionary<string, object>
            {
                { "role", (input.Audience ?? Audience.Parent).ToApiString() },
                { "situation", input.Situation },
                { "context", new Dictionary<string, object> { { "platform", ResolvePlatform() } } }
            };

            if (input.ChildAge.HasValue) body["child_age"] = input.ChildAge.Value;
            if (input.Severity.HasValue) body["severity"] = input.Severity.Value.ToApiString();
            if (input.ExternalId != null) body["external_id"] = input.ExternalId;
            if (input.CustomerId != null) body["customer_id"] = input.CustomerId;
            if (input.Metadata != null) body["metadata"] = input.Metadata;

            var response = await RequestAsync("/api/v1/guidance/action-plan", body);
            return ParseActionPlanResult(response);
        }

        // =====================================================================
        // Reports
        // =====================================================================

        /// <summary>
        /// Generate an incident report.
        /// </summary>
        public async Task<ReportResult> GenerateReportAsync(GenerateReportInput input)
        {
            var messages = new List<Dictionary<string, string>>();
            foreach (var msg in input.Messages)
            {
                messages.Add(new Dictionary<string, string>
                {
                    { "sender", msg.Sender },
                    { "text", msg.Content }
                });
            }

            var body = new Dictionary<string, object> { { "messages", messages } };

            var meta = new Dictionary<string, object>();
            if (input.ChildAge.HasValue) meta["child_age"] = input.ChildAge.Value;
            if (input.IncidentType != null) meta["type"] = input.IncidentType;
            if (meta.Count > 0) body["meta"] = meta;

            body["context"] = new Dictionary<string, object> { { "platform", ResolvePlatform() } };

            if (input.ExternalId != null) body["external_id"] = input.ExternalId;
            if (input.CustomerId != null) body["customer_id"] = input.CustomerId;
            if (input.Metadata != null) body["metadata"] = input.Metadata;

            var response = await RequestAsync("/api/v1/reports/incident", body);
            return ParseReportResult(response);
        }

        // =====================================================================
        // Account Management (GDPR)
        // =====================================================================

        /// <summary>
        /// Delete all account data (GDPR Article 17 — Right to Erasure).
        /// </summary>
        public async Task<AccountDeletionResult> DeleteAccountDataAsync()
        {
            var response = await RequestAsync("DELETE", "/api/v1/account/data");
            return new AccountDeletionResult
            {
                Message = response.ContainsKey("message") ? response["message"].ToString() : "",
                DeletedCount = response.ContainsKey("deleted_count") ? Convert.ToInt32(response["deleted_count"]) : 0
            };
        }

        /// <summary>
        /// Export all account data as JSON (GDPR Article 20 — Right to Data Portability).
        /// </summary>
        public async Task<AccountExportResult> ExportAccountDataAsync()
        {
            var response = await RequestAsync("GET", "/api/v1/account/export");
            return new AccountExportResult
            {
                UserId = response.ContainsKey("userId") ? response["userId"].ToString() : "",
                ExportedAt = response.ContainsKey("exportedAt") ? response["exportedAt"].ToString() : "",
                Data = response.ContainsKey("data") ? response["data"] as Dictionary<string, object> : new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// Record user consent (GDPR Article 7).
        /// </summary>
        public async Task<ConsentActionResult> RecordConsentAsync(RecordConsentInput input)
        {
            var body = new Dictionary<string, object>
            {
                { "consent_type", input.ConsentType },
                { "version", input.Version },
                { "context", new Dictionary<string, object> { { "platform", ResolvePlatform() } } }
            };
            var response = await RequestAsync("POST", "/api/v1/account/consent", body);
            return ParseConsentActionResult(response);
        }

        /// <summary>
        /// Get current consent status (GDPR Article 7).
        /// </summary>
        public async Task<ConsentStatusResult> GetConsentStatusAsync(string consentType = null)
        {
            var query = consentType != null ? $"?type={consentType}" : "";
            var response = await RequestAsync("GET", $"/api/v1/account/consent{query}");
            var consents = new List<ConsentRecord>();
            if (response.ContainsKey("consents") && response["consents"] is List<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dict)
                        consents.Add(ParseConsentRecord(dict));
                }
            }
            return new ConsentStatusResult { Consents = consents };
        }

        /// <summary>
        /// Withdraw consent (GDPR Article 7.3).
        /// </summary>
        public async Task<ConsentActionResult> WithdrawConsentAsync(string consentType)
        {
            var response = await RequestAsync("DELETE", $"/api/v1/account/consent/{consentType}");
            return ParseConsentActionResult(response);
        }

        /// <summary>
        /// Rectify user data (GDPR Article 16 — Right to Rectification).
        /// </summary>
        public async Task<RectifyDataResult> RectifyDataAsync(RectifyDataInput input)
        {
            var body = new Dictionary<string, object>
            {
                { "collection", input.Collection },
                { "document_id", input.DocumentId },
                { "fields", input.Fields },
                { "context", new Dictionary<string, object> { { "platform", ResolvePlatform() } } }
            };
            var response = await RequestAsync("PATCH", "/api/v1/account/data", body);
            var updatedFields = new List<string>();
            if (response.ContainsKey("updated_fields") && response["updated_fields"] is List<object> list)
            {
                foreach (var item in list) updatedFields.Add(item.ToString());
            }
            return new RectifyDataResult
            {
                Message = response.ContainsKey("message") ? response["message"].ToString() : "",
                UpdatedFields = updatedFields
            };
        }

        /// <summary>
        /// Get audit logs (GDPR Article 15 — Right of Access).
        /// </summary>
        public async Task<AuditLogsResult> GetAuditLogsAsync(string action = null, int? limit = null)
        {
            var parameters = new List<string>();
            if (action != null) parameters.Add($"action={action}");
            if (limit.HasValue) parameters.Add($"limit={limit.Value}");
            var query = parameters.Count > 0 ? $"?{string.Join("&", parameters)}" : "";
            var response = await RequestAsync("GET", $"/api/v1/account/audit-logs{query}");
            var logs = new List<AuditLogEntry>();
            if (response.ContainsKey("audit_logs") && response["audit_logs"] is List<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dict)
                    {
                        logs.Add(new AuditLogEntry
                        {
                            Id = dict.ContainsKey("id") ? dict["id"].ToString() : "",
                            UserId = dict.ContainsKey("user_id") ? dict["user_id"].ToString() : "",
                            Action = dict.ContainsKey("action") ? dict["action"].ToString() : "",
                            CreatedAt = dict.ContainsKey("created_at") ? dict["created_at"].ToString() : "",
                        });
                    }
                }
            }
            return new AuditLogsResult { AuditLogs = logs };
        }

        // =====================================================================
        // Breach Management (GDPR Article 33/34)
        // =====================================================================

        /// <summary>
        /// Log a new data breach.
        /// </summary>
        public async Task<LogBreachResult> LogBreachAsync(LogBreachInput input)
        {
            var body = new Dictionary<string, object>
            {
                { "title", input.Title },
                { "description", input.Description },
                { "severity", input.Severity },
                { "affected_user_ids", input.AffectedUserIds },
                { "data_categories", input.DataCategories },
                { "reported_by", input.ReportedBy },
                { "context", new Dictionary<string, object> { { "platform", ResolvePlatform() } } }
            };
            var response = await RequestAsync("/api/v1/admin/breach", body);
            return new LogBreachResult
            {
                Message = GetString(response, "message"),
                Breach = ParseBreachRecord(response.ContainsKey("breach") && response["breach"] is Dictionary<string, object> d ? d : new Dictionary<string, object>())
            };
        }

        /// <summary>
        /// List data breaches.
        /// </summary>
        public async Task<BreachListResult> ListBreachesAsync(string status = null, int? limit = null)
        {
            var parameters = new List<string>();
            if (status != null) parameters.Add($"status={status}");
            if (limit.HasValue) parameters.Add($"limit={limit.Value}");
            var query = parameters.Count > 0 ? $"?{string.Join("&", parameters)}" : "";
            var response = await RequestAsync("GET", $"/api/v1/admin/breach{query}");
            var breaches = new List<BreachRecord>();
            if (response.ContainsKey("breaches") && response["breaches"] is List<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dict)
                        breaches.Add(ParseBreachRecord(dict));
                }
            }
            return new BreachListResult { Breaches = breaches };
        }

        /// <summary>
        /// Get a single breach by ID.
        /// </summary>
        public async Task<BreachResult> GetBreachAsync(string id)
        {
            var response = await RequestAsync("GET", $"/api/v1/admin/breach/{id}");
            return new BreachResult
            {
                Breach = ParseBreachRecord(response.ContainsKey("breach") && response["breach"] is Dictionary<string, object> d ? d : new Dictionary<string, object>())
            };
        }

        /// <summary>
        /// Update a breach's status.
        /// </summary>
        public async Task<BreachResult> UpdateBreachStatusAsync(string id, UpdateBreachInput input)
        {
            var body = new Dictionary<string, object> { { "status", input.Status } };
            if (input.NotificationStatus != null) body["notification_status"] = input.NotificationStatus;
            if (input.Notes != null) body["notes"] = input.Notes;
            body["context"] = new Dictionary<string, object> { { "platform", ResolvePlatform() } };
            var response = await RequestAsync("PATCH", $"/api/v1/admin/breach/{id}", body);
            return new BreachResult
            {
                Breach = ParseBreachRecord(response.ContainsKey("breach") && response["breach"] is Dictionary<string, object> d ? d : new Dictionary<string, object>())
            };
        }

        private BreachRecord ParseBreachRecord(Dictionary<string, object> data)
        {
            return new BreachRecord
            {
                Id = GetString(data, "id"),
                Title = GetString(data, "title"),
                Description = GetString(data, "description"),
                Severity = GetString(data, "severity"),
                Status = GetString(data, "status"),
                NotificationStatus = GetString(data, "notification_status"),
                AffectedUserIds = GetStringList(data, "affected_user_ids"),
                DataCategories = GetStringList(data, "data_categories"),
                ReportedBy = GetString(data, "reported_by"),
                NotificationDeadline = GetString(data, "notification_deadline"),
                CreatedAt = GetString(data, "created_at"),
                UpdatedAt = GetString(data, "updated_at"),
            };
        }

        private ConsentActionResult ParseConsentActionResult(Dictionary<string, object> response)
        {
            var result = new ConsentActionResult
            {
                Message = response.ContainsKey("message") ? response["message"].ToString() : "",
            };
            if (response.ContainsKey("consent") && response["consent"] is Dictionary<string, object> dict)
            {
                result.Consent = ParseConsentRecord(dict);
            }
            return result;
        }

        private ConsentRecord ParseConsentRecord(Dictionary<string, object> dict)
        {
            return new ConsentRecord
            {
                Id = dict.ContainsKey("id") ? dict["id"].ToString() : "",
                UserId = dict.ContainsKey("user_id") ? dict["user_id"].ToString() : "",
                ConsentType = dict.ContainsKey("consent_type") ? dict["consent_type"].ToString() : "",
                Status = dict.ContainsKey("status") ? dict["status"].ToString() : "",
                Version = dict.ContainsKey("version") ? dict["version"].ToString() : "",
                CreatedAt = dict.ContainsKey("created_at") ? dict["created_at"].ToString() : "",
            };
        }

        // =====================================================================
        // Voice Analysis
        // =====================================================================

        /// <summary>
        /// Analyze voice/audio content for safety concerns.
        /// </summary>
        public async Task<VoiceAnalysisResult> AnalyzeVoiceAsync(
            byte[] file,
            string filename,
            string analysisType = "all",
            string fileId = null,
            string externalId = null,
            string customerId = null,
            Dictionary<string, object> metadata = null,
            string ageGroup = null,
            string language = null,
            string platform = null,
            int? childAge = null)
        {
            var form = new MultipartForm()
                .AddFile("file", file, filename)
                .Add("analysis_type", analysisType)
                .Add("platform", ResolvePlatform(platform))
                .AddIfSet("file_id", fileId)
                .AddIfSet("external_id", externalId)
                .AddIfSet("customer_id", customerId)
                .AddIfSet("metadata", metadata != null ? MiniJson.Serialize(metadata) : null)
                .AddIfSet("age_group", ageGroup)
                .AddIfSet("language", language)
                .AddIfSet("child_age", childAge.HasValue ? FormatInt(childAge.Value) : null);

            var data = await MultipartRequestAsync("/api/v1/safety/voice", form);
            return ParseVoiceAnalysisResult(data);
        }

        // =====================================================================
        // Image Analysis
        // =====================================================================

        /// <summary>
        /// Analyze image content for safety concerns.
        /// </summary>
        public async Task<ImageAnalysisResult> AnalyzeImageAsync(
            byte[] file,
            string filename,
            string analysisType = "all",
            string fileId = null,
            string externalId = null,
            string customerId = null,
            Dictionary<string, object> metadata = null,
            string ageGroup = null,
            string language = null,
            string platform = null,
            int? childAge = null)
        {
            var form = new MultipartForm()
                .AddFile("file", file, filename)
                .Add("analysis_type", analysisType)
                .Add("platform", ResolvePlatform(platform))
                .AddIfSet("file_id", fileId)
                .AddIfSet("external_id", externalId)
                .AddIfSet("customer_id", customerId)
                .AddIfSet("metadata", metadata != null ? MiniJson.Serialize(metadata) : null)
                .AddIfSet("age_group", ageGroup)
                .AddIfSet("language", language)
                .AddIfSet("child_age", childAge.HasValue ? FormatInt(childAge.Value) : null);

            var data = await MultipartRequestAsync("/api/v1/safety/image", form);
            return ParseImageAnalysisResult(data);
        }

        // =====================================================================
        // Fraud / Extended Detection
        // =====================================================================

        /// <summary>
        /// Detect social engineering attempts.
        /// </summary>
        public async Task<DetectionResult> DetectSocialEngineeringAsync(DetectionInput input)
        {
            var data = await RequestAsync("POST", "/api/v1/fraud/social-engineering", BuildDetectionBody(input));
            return ParseDetectionResult(data);
        }

        /// <summary>
        /// Detect app fraud patterns.
        /// </summary>
        public async Task<DetectionResult> DetectAppFraudAsync(DetectionInput input)
        {
            var data = await RequestAsync("POST", "/api/v1/fraud/app-fraud", BuildDetectionBody(input));
            return ParseDetectionResult(data);
        }

        /// <summary>
        /// Detect romance scam patterns.
        /// </summary>
        public async Task<DetectionResult> DetectRomanceScamAsync(DetectionInput input)
        {
            var data = await RequestAsync("POST", "/api/v1/fraud/romance-scam", BuildDetectionBody(input));
            return ParseDetectionResult(data);
        }

        /// <summary>
        /// Detect mule recruitment attempts.
        /// </summary>
        public async Task<DetectionResult> DetectMuleRecruitmentAsync(DetectionInput input)
        {
            var data = await RequestAsync("POST", "/api/v1/fraud/mule-recruitment", BuildDetectionBody(input));
            return ParseDetectionResult(data);
        }

        /// <summary>
        /// Detect gambling harm indicators.
        /// </summary>
        public async Task<DetectionResult> DetectGamblingHarmAsync(DetectionInput input)
        {
            var data = await RequestAsync("POST", "/api/v1/fraud/gambling-harm", BuildDetectionBody(input));
            return ParseDetectionResult(data);
        }

        /// <summary>
        /// Detect coercive control patterns.
        /// </summary>
        public async Task<DetectionResult> DetectCoerciveControlAsync(DetectionInput input)
        {
            var data = await RequestAsync("POST", "/api/v1/fraud/coercive-control", BuildDetectionBody(input));
            return ParseDetectionResult(data);
        }

        /// <summary>
        /// Detect vulnerability exploitation attempts.
        /// </summary>
        public async Task<DetectionResult> DetectVulnerabilityExploitationAsync(DetectionInput input)
        {
            var data = await RequestAsync("POST", "/api/v1/fraud/vulnerability-exploitation", BuildDetectionBody(input));
            return ParseDetectionResult(data);
        }

        /// <summary>
        /// Detect radicalisation indicators.
        /// </summary>
        public async Task<DetectionResult> DetectRadicalisationAsync(DetectionInput input)
        {
            var data = await RequestAsync("POST", "/api/v1/fraud/radicalisation", BuildDetectionBody(input));
            return ParseDetectionResult(data);
        }

        // =====================================================================
        // Multi-Endpoint Analysis
        // =====================================================================

        /// <summary>
        /// Run multiple detection endpoints in a single request.
        /// </summary>
        public async Task<AnalyseMultiResult> AnalyseMultiAsync(AnalyseMultiInput input)
        {
            var endpoints = new List<string>();
            if (input.Detections != null)
            {
                foreach (var d in input.Detections)
                    endpoints.Add(d.ToApiString());
            }

            var body = new Dictionary<string, object>
            {
                ["text"] = input.Content,
                ["endpoints"] = endpoints
            };

            var ctx = input.Context != null ? ContextToDict(input.Context) : new Dictionary<string, object>();
            ctx["platform"] = ResolvePlatform(input.Context?.Platform);
            body["context"] = ctx;

            var options = new Dictionary<string, object>();
            if (input.IncludeEvidence) options["include_evidence"] = true;
            if (options.Count > 0) body["options"] = options;

            if (input.ExternalId != null) body["external_id"] = input.ExternalId;
            if (input.CustomerId != null) body["customer_id"] = input.CustomerId;
            if (input.Metadata != null) body["metadata"] = input.Metadata;

            var data = await RequestAsync("POST", "/api/v1/analyse/multi", body);
            return ParseAnalyseMultiResult(data);
        }

        // =====================================================================
        // Video Analysis
        // =====================================================================

        /// <summary>
        /// Analyze video content for safety concerns.
        /// </summary>
        public async Task<VideoAnalysisResult> AnalyzeVideoAsync(
            byte[] file,
            string filename,
            string analysisType = "all",
            string fileId = null,
            string externalId = null,
            string customerId = null,
            Dictionary<string, object> metadata = null,
            string ageGroup = null,
            string language = null,
            string platform = null,
            int? childAge = null)
        {
            var form = new MultipartForm()
                .AddFile("file", file, filename)
                .Add("analysis_type", analysisType)
                .Add("platform", ResolvePlatform(platform))
                .AddIfSet("file_id", fileId)
                .AddIfSet("external_id", externalId)
                .AddIfSet("customer_id", customerId)
                .AddIfSet("metadata", metadata != null ? MiniJson.Serialize(metadata) : null)
                .AddIfSet("age_group", ageGroup)
                .AddIfSet("language", language)
                .AddIfSet("child_age", childAge.HasValue ? FormatInt(childAge.Value) : null);

            var data = await MultipartRequestAsync("/api/v1/safety/video", form);
            return ParseVideoAnalysisResult(data);
        }

        // =====================================================================
        // Webhooks
        // =====================================================================

        /// <summary>
        /// List all webhooks.
        /// </summary>
        public async Task<WebhookListResult> ListWebhooksAsync()
        {
            var response = await RequestAsync("GET", "/api/v1/webhooks");
            return ParseWebhookListResult(response);
        }

        /// <summary>
        /// Create a new webhook. The signing secret is returned only here: store it securely.
        /// </summary>
        public async Task<CreateWebhookResult> CreateWebhookAsync(CreateWebhookInput input)
        {
            if (string.IsNullOrEmpty(input.Name))
                throw new ValidationException("Webhook name is required");

            var body = new Dictionary<string, object>
            {
                { "name", input.Name },
                { "url", input.Url },
                { "events", input.Events }
            };
            if (input.Headers != null) body["headers"] = input.Headers;
            var response = await RequestAsync("POST", "/api/v1/webhooks", body);
            return new CreateWebhookResult
            {
                Webhook = ParseWebhookInfo(response),
                Secret = GetString(response, "secret")
            };
        }

        /// <summary>
        /// Update an existing webhook. Only the fields that are set are sent.
        /// </summary>
        public async Task<UpdateWebhookResult> UpdateWebhookAsync(string webhookId, UpdateWebhookInput input)
        {
            var body = new Dictionary<string, object>();
            if (input.Name != null) body["name"] = input.Name;
            if (input.Url != null) body["url"] = input.Url;
            if (input.Events != null) body["events"] = input.Events;
            if (input.IsActive.HasValue) body["is_active"] = input.IsActive.Value;
            if (input.Headers != null) body["headers"] = input.Headers;
            var response = await RequestAsync("PUT", $"/api/v1/webhooks/{Uri.EscapeDataString(webhookId)}", body);
            return new UpdateWebhookResult { Webhook = ParseWebhookInfo(response) };
        }

        /// <summary>
        /// Delete a webhook.
        /// </summary>
        public async Task<DeleteWebhookResult> DeleteWebhookAsync(string webhookId)
        {
            var response = await RequestAsync("DELETE", $"/api/v1/webhooks/{Uri.EscapeDataString(webhookId)}");
            return new DeleteWebhookResult
            {
                Success = GetBool(response, "success"),
                Message = GetString(response, "message")
            };
        }

        /// <summary>
        /// Test a webhook by sending a test event.
        /// </summary>
        public async Task<TestWebhookResult> TestWebhookAsync(string webhookId)
        {
            var body = new Dictionary<string, object> { { "webhook_id", webhookId } };
            var response = await RequestAsync("POST", "/api/v1/webhooks/test", body);
            return new TestWebhookResult
            {
                Success = GetBool(response, "success"),
                StatusCode = GetNullableInt(response, "status_code"),
                LatencyMs = GetNullableDouble(response, "latency_ms"),
                Error = GetString(response, "error")
            };
        }

        /// <summary>
        /// Regenerate the secret for a webhook. The old secret stops working at once.
        /// </summary>
        public async Task<RegenerateSecretResult> RegenerateWebhookSecretAsync(string webhookId)
        {
            var response = await RequestAsync("POST", $"/api/v1/webhooks/{Uri.EscapeDataString(webhookId)}/regenerate-secret");
            return new RegenerateSecretResult { Secret = GetString(response, "secret") };
        }

        // =====================================================================
        // Pricing
        // =====================================================================

        /// <summary>
        /// Get pricing plans overview.
        /// </summary>
        public async Task<PricingResult> GetPricingAsync()
        {
            var response = await RequestAsync("GET", "/api/v1/pricing");
            return ParsePricingResult(response);
        }

        /// <summary>
        /// Get detailed pricing information.
        /// </summary>
        public async Task<PricingDetailsResult> GetPricingDetailsAsync()
        {
            var response = await RequestAsync("GET", "/api/v1/pricing/details");
            return ParsePricingDetailsResult(response);
        }

        // =====================================================================
        // Usage
        // =====================================================================

        /// <summary>
        /// Get usage history for the API key.
        /// </summary>
        public async Task<UsageHistoryResult> GetUsageHistoryAsync(int? days = null)
        {
            var query = days.HasValue ? $"?days={days.Value}" : "";
            var response = await RequestAsync("GET", $"/api/v1/usage/history{query}");
            return ParseUsageHistoryResult(response);
        }

        /// <summary>
        /// Get usage breakdown by tool/endpoint.
        /// </summary>
        public async Task<UsageByToolResult> GetUsageByToolAsync(string date = null)
        {
            var query = date != null ? $"?date={date}" : "";
            var response = await RequestAsync("GET", $"/api/v1/usage/by-tool{query}");
            return ParseUsageByToolResult(response);
        }

        /// <summary>
        /// Get monthly usage summary.
        /// </summary>
        public async Task<UsageMonthlyResult> GetUsageMonthlyAsync()
        {
            var response = await RequestAsync("GET", "/api/v1/usage/monthly");
            return ParseUsageMonthlyResult(response);
        }

        // =====================================================================
        // Private Methods
        // =====================================================================

        private Task<Dictionary<string, object>> RequestAsync(
            string path,
            Dictionary<string, object> body)
        {
            return RequestAsync("POST", path, body);
        }

        private Task<Dictionary<string, object>> RequestAsync(
            string method,
            string path,
            Dictionary<string, object> body = null)
        {
            return RetryPolicy.ExecuteAsync(
                () => SendJsonOnceAsync(method, path, body),
                _maxRetries, _retryDelay, _delay);
        }

        /// <summary>
        /// Sends without retrying, for calls that must not be repeated (each one creates something new).
        /// </summary>
        private Task<Dictionary<string, object>> SendJsonOnceAsync(
            string method,
            string path,
            Dictionary<string, object> body)
        {
            var request = NewRequest(method, path);
            if (body != null) request.JsonBody = MiniJson.Serialize(body);
            return SendAsync(request);
        }

        private Task<Dictionary<string, object>> MultipartRequestAsync(string path, MultipartForm form)
        {
            return RetryPolicy.ExecuteAsync(
                () =>
                {
                    var request = NewRequest("POST", path);
                    request.Form = form;
                    return SendAsync(request);
                },
                _maxRetries, _retryDelay, _delay);
        }

        private TransportRequest NewRequest(string method, string path)
        {
            var request = new TransportRequest
            {
                Method = method,
                Url = _baseUrl + path,
                TimeoutSeconds = (int)_timeout
            };
            request.Headers["Authorization"] = $"Bearer {_apiKey}";
            return request;
        }

        private async Task<Dictionary<string, object>> SendAsync(TransportRequest request)
        {
            var response = await _transport.SendAsync(request);
            RecordResponseHeaders(response);

            if (response.TransportError != null)
            {
                if (response.TimedOut)
                    throw new TimeoutException($"Request timed out after {request.TimeoutSeconds}s");
                throw new NetworkException(response.TransportError);
            }

            if (response.StatusCode < 200 || response.StatusCode >= 300)
                throw ApiErrors.FromResponse(response.StatusCode, response.Body, response.Header("retry-after"), LastRequestId);

            return ParseJson(response.Body);
        }

        private void RecordResponseHeaders(TransportResponse response)
        {
            LastRequestId = response.Header("x-request-id");

            if (int.TryParse(response.Header("x-monthly-limit"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int limit) &&
                int.TryParse(response.Header("x-monthly-used"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int used) &&
                int.TryParse(response.Header("x-monthly-remaining"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int remaining))
            {
                Usage = new Usage { Limit = limit, Used = used, Remaining = remaining };
            }
        }

        private static string ResolvePlatform(string platform = null)
        {
            if (!string.IsNullOrEmpty(platform))
                return $"{platform} - {SdkIdentifier}";
            return SdkIdentifier;
        }

        private Dictionary<string, object> ContextToDict(AnalysisContext context)
        {
            var dict = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(context.Language)) dict["language"] = context.Language;
            if (!string.IsNullOrEmpty(context.AgeGroup)) dict["age_group"] = context.AgeGroup;
            if (!string.IsNullOrEmpty(context.Relationship)) dict["relationship"] = context.Relationship;
            dict["platform"] = ResolvePlatform(context.Platform);
            return dict;
        }

        private static string FormatInt(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, object> ParseJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return new Dictionary<string, object>();
            return MiniJson.Deserialize(json) as Dictionary<string, object> ?? new Dictionary<string, object>();
        }

        private BullyingResult ParseBullyingResult(Dictionary<string, object> data)
        {
            return new BullyingResult
            {
                IsBullying = GetBool(data, "is_bullying"),
                BullyingReported = GetBool(data, "bullying_reported"),
                BullyingReportedBy = GetString(data, "bullying_reported_by"),
                SpeakerRole = GetString(data, "speaker_role"),
                Severity = EnumExtensions.ParseSeverity(GetString(data, "severity")),
                BullyingType = GetStringList(data, "bullying_type"),
                Confidence = GetFloat(data, "confidence"),
                Rationale = GetString(data, "rationale"),
                RiskScore = GetFloat(data, "risk_score"),
                RecommendedAction = GetString(data, "recommended_action"),
                Language = GetString(data, "language"),
                LanguageStatus = GetString(data, "language_status"),
                CreditsUsed = GetNullableInt(data, "credits_used"),
                ExternalId = GetString(data, "external_id"),
                CustomerId = GetString(data, "customer_id"),
                Metadata = GetDict(data, "metadata")
            };
        }

        private GroomingResult ParseGroomingResult(Dictionary<string, object> data)
        {
            return new GroomingResult
            {
                GroomingRisk = EnumExtensions.ParseGroomingRisk(GetString(data, "grooming_risk")),
                Flags = GetStringList(data, "flags"),
                Confidence = GetFloat(data, "confidence"),
                Rationale = GetString(data, "rationale"),
                RiskScore = GetFloat(data, "risk_score"),
                RecommendedAction = GetString(data, "recommended_action"),
                MessageAnalysis = ParseMessageAnalysisList(data),
                Language = GetString(data, "language"),
                LanguageStatus = GetString(data, "language_status"),
                CreditsUsed = GetNullableInt(data, "credits_used"),
                ExternalId = GetString(data, "external_id"),
                CustomerId = GetString(data, "customer_id"),
                Metadata = GetDict(data, "metadata")
            };
        }

        private UnsafeResult ParseUnsafeResult(Dictionary<string, object> data)
        {
            return new UnsafeResult
            {
                Unsafe = GetBool(data, "unsafe"),
                Categories = GetStringList(data, "categories"),
                Severity = EnumExtensions.ParseSeverity(GetString(data, "severity")),
                Confidence = GetFloat(data, "confidence"),
                Rationale = GetString(data, "rationale"),
                RiskScore = GetFloat(data, "risk_score"),
                RecommendedAction = GetString(data, "recommended_action"),
                Language = GetString(data, "language"),
                LanguageStatus = GetString(data, "language_status"),
                CreditsUsed = GetNullableInt(data, "credits_used"),
                ExternalId = GetString(data, "external_id"),
                CustomerId = GetString(data, "customer_id"),
                Metadata = GetDict(data, "metadata")
            };
        }

        private EmotionsResult ParseEmotionsResult(Dictionary<string, object> data)
        {
            return new EmotionsResult
            {
                DominantEmotions = GetStringList(data, "dominant_emotions"),
                Trend = EnumExtensions.ParseEmotionTrend(GetString(data, "trend")),
                Intensity = GetFloat(data, "intensity"),
                ConcerningPatterns = GetStringList(data, "concerning_patterns"),
                RecommendedFollowup = GetString(data, "recommended_followup"),
                CreditsUsed = GetNullableInt(data, "credits_used"),
                ExternalId = GetString(data, "external_id"),
                CustomerId = GetString(data, "customer_id"),
                Metadata = GetDict(data, "metadata")
            };
        }

        private ActionPlanResult ParseActionPlanResult(Dictionary<string, object> data)
        {
            return new ActionPlanResult
            {
                Steps = GetStringList(data, "steps"),
                Tone = GetString(data, "tone"),
                Resources = GetStringList(data, "resources"),
                Urgency = GetString(data, "urgency"),
                CreditsUsed = GetNullableInt(data, "credits_used"),
                ExternalId = GetString(data, "external_id"),
                CustomerId = GetString(data, "customer_id"),
                Metadata = GetDict(data, "metadata")
            };
        }

        private ReportResult ParseReportResult(Dictionary<string, object> data)
        {
            return new ReportResult
            {
                Summary = GetString(data, "summary"),
                RiskLevel = EnumExtensions.ParseRiskLevel(GetString(data, "risk_level")),
                Timeline = GetStringList(data, "timeline"),
                KeyEvidence = GetStringList(data, "key_evidence"),
                RecommendedNextSteps = GetStringList(data, "recommended_next_steps"),
                CreditsUsed = GetNullableInt(data, "credits_used"),
                ExternalId = GetString(data, "external_id"),
                CustomerId = GetString(data, "customer_id"),
                Metadata = GetDict(data, "metadata")
            };
        }

        private VoiceAnalysisResult ParseVoiceAnalysisResult(Dictionary<string, object> data)
        {
            var result = new VoiceAnalysisResult
            {
                FileId = GetString(data, "file_id"),
                Analysis = GetDict(data, "analysis"),
                OverallRiskScore = GetNullableDouble(data, "overall_risk_score"),
                OverallSeverity = GetString(data, "overall_severity"),
                CreditsUsed = GetNullableInt(data, "credits_used"),
                ExternalId = GetString(data, "external_id"),
                CustomerId = GetString(data, "customer_id"),
                Metadata = GetDict(data, "metadata")
            };

            if (data.ContainsKey("transcription") && data["transcription"] is Dictionary<string, object> transDict)
            {
                result.Transcription = new TranscriptionResult
                {
                    Text = GetString(transDict, "text"),
                    Language = GetString(transDict, "language"),
                    Duration = GetNullableDouble(transDict, "duration"),
                    Segments = ParseTranscriptionSegments(transDict)
                };
            }

            return result;
        }

        private List<TranscriptionSegment> ParseTranscriptionSegments(Dictionary<string, object> data)
        {
            var segments = new List<TranscriptionSegment>();
            if (data.ContainsKey("segments") && data["segments"] is IList<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> seg)
                    {
                        segments.Add(new TranscriptionSegment
                        {
                            Start = GetDouble(seg, "start"),
                            End = GetDouble(seg, "end"),
                            Text = GetString(seg, "text")
                        });
                    }
                }
            }
            return segments;
        }

        private ImageAnalysisResult ParseImageAnalysisResult(Dictionary<string, object> data)
        {
            var result = new ImageAnalysisResult
            {
                FileId = GetString(data, "file_id"),
                TextAnalysis = GetDict(data, "text_analysis"),
                OverallRiskScore = GetNullableDouble(data, "overall_risk_score"),
                OverallSeverity = GetString(data, "overall_severity"),
                CreditsUsed = GetNullableInt(data, "credits_used"),
                ExternalId = GetString(data, "external_id"),
                CustomerId = GetString(data, "customer_id"),
                Metadata = GetDict(data, "metadata")
            };

            if (data.ContainsKey("vision") && data["vision"] is Dictionary<string, object> visionDict)
            {
                result.Vision = new VisionResult
                {
                    ExtractedText = GetString(visionDict, "extracted_text"),
                    VisualCategories = GetStringList(visionDict, "visual_categories"),
                    VisualSeverity = GetString(visionDict, "visual_severity"),
                    VisualConfidence = GetNullableDouble(visionDict, "visual_confidence"),
                    VisualDescription = GetString(visionDict, "visual_description"),
                    ContainsText = GetNullableBool(visionDict, "contains_text"),
                    ContainsFaces = GetNullableBool(visionDict, "contains_faces")
                };
            }

            return result;
        }

        private Dictionary<string, object> BuildDetectionBody(DetectionInput input)
        {
            var body = new Dictionary<string, object> { ["text"] = input.Content };
            var ctx = input.Context != null ? ContextToDict(input.Context) : new Dictionary<string, object>();
            ctx["platform"] = ResolvePlatform(input.Context?.Platform);
            body["context"] = ctx;
            if (input.IncludeEvidence) body["include_evidence"] = true;
            if (input.ExternalId != null) body["external_id"] = input.ExternalId;
            if (input.CustomerId != null) body["customer_id"] = input.CustomerId;
            if (input.Metadata != null) body["metadata"] = input.Metadata;
            return body;
        }

        private DetectionCategory ParseDetectionCategory(Dictionary<string, object> data)
        {
            return new DetectionCategory
            {
                Tag = GetString(data, "tag"),
                Label = GetString(data, "label"),
                Confidence = GetDouble(data, "confidence")
            };
        }

        private DetectionEvidence ParseDetectionEvidence(Dictionary<string, object> data)
        {
            return new DetectionEvidence
            {
                Text = GetString(data, "text"),
                Tactic = GetString(data, "tactic"),
                Weight = GetDouble(data, "weight")
            };
        }

        private AgeCalibration ParseAgeCalibration(Dictionary<string, object> data)
        {
            return new AgeCalibration
            {
                Applied = GetBool(data, "applied"),
                AgeGroup = GetString(data, "age_group"),
                Multiplier = GetNullableDouble(data, "multiplier")
            };
        }

        private DetectionResult ParseDetectionResult(Dictionary<string, object> data)
        {
            return FillDetectionResult(new DetectionResult(), data);
        }

        private T FillDetectionResult<T>(T result, Dictionary<string, object> data) where T : DetectionResult
        {
            result.Endpoint = GetString(data, "endpoint");
            result.Detected = GetBool(data, "detected");
            result.Severity = GetDouble(data, "severity");
            result.Confidence = GetDouble(data, "confidence");
            result.RiskScore = GetDouble(data, "risk_score");
            result.Level = GetString(data, "level");
            result.RecommendedAction = GetString(data, "recommended_action");
            result.Rationale = GetString(data, "rationale");
            result.Language = GetString(data, "language");
            result.LanguageStatus = GetString(data, "language_status");
            result.MessageAnalysis = ParseMessageAnalysisList(data);
            result.CreditsUsed = GetNullableInt(data, "credits_used");
            result.ProcessingTimeMs = GetNullableDouble(data, "processing_time_ms");
            result.ExternalId = GetString(data, "external_id");
            result.CustomerId = GetString(data, "customer_id");
            result.Metadata = GetDict(data, "metadata");

            var categories = new List<DetectionCategory>();
            if (data.ContainsKey("categories") && data["categories"] is IList<object> catList)
            {
                foreach (var item in catList)
                {
                    if (item is Dictionary<string, object> dict)
                        categories.Add(ParseDetectionCategory(dict));
                }
            }
            result.Categories = categories;

            var evidence = new List<DetectionEvidence>();
            if (data.ContainsKey("evidence") && data["evidence"] is IList<object> evList)
            {
                foreach (var item in evList)
                {
                    if (item is Dictionary<string, object> dict)
                        evidence.Add(ParseDetectionEvidence(dict));
                }
            }
            result.Evidence = evidence;

            if (data.ContainsKey("age_calibration") && data["age_calibration"] is Dictionary<string, object> ageDict)
                result.AgeCalibration = ParseAgeCalibration(ageDict);

            return result;
        }

        private AnalyseMultiSummary ParseAnalyseMultiSummary(Dictionary<string, object> data)
        {
            return new AnalyseMultiSummary
            {
                TotalEndpoints = GetInt(data, "total_endpoints"),
                DetectedCount = GetInt(data, "detected_count"),
                HighestRisk = GetDict(data, "highest_risk"),
                OverallRiskLevel = GetString(data, "overall_risk_level")
            };
        }

        private AnalyseMultiResult ParseAnalyseMultiResult(Dictionary<string, object> data)
        {
            var result = new AnalyseMultiResult
            {
                CrossEndpointModifier = GetNullableDouble(data, "cross_endpoint_modifier"),
                CreditsUsed = GetNullableInt(data, "credits_used"),
                ExternalId = GetString(data, "external_id"),
                CustomerId = GetString(data, "customer_id"),
                Metadata = GetDict(data, "metadata")
            };

            var results = new List<DetectionResult>();
            if (data.ContainsKey("results") && data["results"] is IList<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dict)
                        results.Add(ParseDetectionResult(dict));
                }
            }
            result.Results = results;

            if (data.ContainsKey("summary") && data["summary"] is Dictionary<string, object> summaryDict)
                result.Summary = ParseAnalyseMultiSummary(summaryDict);

            return result;
        }

        private VideoSafetyFinding ParseVideoSafetyFinding(Dictionary<string, object> data)
        {
            return new VideoSafetyFinding
            {
                FrameIndex = GetInt(data, "frame_index"),
                Timestamp = GetDouble(data, "timestamp"),
                Description = GetString(data, "description"),
                Categories = GetStringList(data, "categories"),
                Severity = GetDouble(data, "severity")
            };
        }

        private VideoAnalysisResult ParseVideoAnalysisResult(Dictionary<string, object> data)
        {
            var result = new VideoAnalysisResult
            {
                FileId = GetString(data, "file_id"),
                FramesAnalyzed = GetInt(data, "frames_analyzed"),
                OverallRiskScore = GetDouble(data, "overall_risk_score"),
                OverallSeverity = GetString(data, "overall_severity"),
                CreditsUsed = GetNullableInt(data, "credits_used"),
                ExternalId = GetString(data, "external_id"),
                CustomerId = GetString(data, "customer_id"),
                Metadata = GetDict(data, "metadata")
            };

            var findings = new List<VideoSafetyFinding>();
            if (data.ContainsKey("safety_findings") && data["safety_findings"] is IList<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dict)
                        findings.Add(ParseVideoSafetyFinding(dict));
                }
            }
            result.SafetyFindings = findings;

            return result;
        }

        private WebhookInfo ParseWebhookInfo(Dictionary<string, object> data)
        {
            return new WebhookInfo
            {
                Id = GetString(data, "id"),
                Name = GetString(data, "name"),
                Url = GetString(data, "url"),
                Events = GetStringList(data, "events"),
                IsActive = GetBool(data, "is_active"),
                FailureCount = GetInt(data, "failure_count"),
                LastTriggeredAt = GetString(data, "last_triggered_at"),
                LastError = GetString(data, "last_error"),
                CreatedAt = GetString(data, "created_at"),
                UpdatedAt = GetString(data, "updated_at")
            };
        }

        private WebhookListResult ParseWebhookListResult(Dictionary<string, object> data)
        {
            var webhooks = new List<WebhookInfo>();
            if (data.ContainsKey("webhooks") && data["webhooks"] is IList<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dict)
                        webhooks.Add(ParseWebhookInfo(dict));
                }
            }
            return new WebhookListResult { Webhooks = webhooks };
        }

        private PricingResult ParsePricingResult(Dictionary<string, object> data)
        {
            var plans = new List<PricingPlan>();
            if (data.ContainsKey("plans") && data["plans"] is IList<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dict)
                    {
                        plans.Add(new PricingPlan
                        {
                            Name = GetString(dict, "name"),
                            Price = GetString(dict, "price"),
                            Messages = GetString(dict, "messages"),
                            Features = GetStringList(dict, "features")
                        });
                    }
                }
            }
            return new PricingResult { Plans = plans };
        }

        private PricingDetailsResult ParsePricingDetailsResult(Dictionary<string, object> data)
        {
            var plans = new List<PricingDetailPlan>();
            if (data.ContainsKey("plans") && data["plans"] is IList<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dict)
                    {
                        plans.Add(new PricingDetailPlan
                        {
                            Name = GetString(dict, "name"),
                            Tier = GetString(dict, "tier"),
                            Price = GetDict(dict, "price"),
                            Limits = GetDict(dict, "limits"),
                            Features = GetDict(dict, "features"),
                            Endpoints = GetStringList(dict, "endpoints")
                        });
                    }
                }
            }
            return new PricingDetailsResult { Plans = plans };
        }

        private UsageHistoryResult ParseUsageHistoryResult(Dictionary<string, object> data)
        {
            var days = new List<UsageDay>();
            if (data.ContainsKey("days") && data["days"] is IList<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dict)
                    {
                        days.Add(new UsageDay
                        {
                            Date = GetString(dict, "date"),
                            TotalRequests = GetInt(dict, "total_requests"),
                            SuccessRequests = GetInt(dict, "success_requests"),
                            ErrorRequests = GetInt(dict, "error_requests")
                        });
                    }
                }
            }
            return new UsageHistoryResult
            {
                ApiKeyId = GetString(data, "api_key_id"),
                Days = days
            };
        }

        private UsageByToolResult ParseUsageByToolResult(Dictionary<string, object> data)
        {
            return new UsageByToolResult
            {
                Date = GetString(data, "date"),
                Tools = GetIntDict(data, "tools"),
                Endpoints = GetIntDict(data, "endpoints")
            };
        }

        private UsageMonthlyResult ParseUsageMonthlyResult(Dictionary<string, object> data)
        {
            return new UsageMonthlyResult
            {
                Tier = GetString(data, "tier"),
                TierDisplayName = GetString(data, "tier_display_name"),
                Billing = GetDict(data, "billing"),
                UsageInfo = GetDict(data, "usage"),
                RateLimit = GetDict(data, "rate_limit"),
                Recommendations = GetDict(data, "recommendations"),
                Links = GetDict(data, "links")
            };
        }

        private static string GetString(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out var value) ? value?.ToString() : null;
        }

        private static bool GetBool(Dictionary<string, object> data, string key)
        {
            if (data.TryGetValue(key, out var value))
            {
                if (value is bool b) return b;
                if (bool.TryParse(value?.ToString(), out var parsed)) return parsed;
            }
            return false;
        }

        private static float GetFloat(Dictionary<string, object> data, string key)
        {
            if (data.TryGetValue(key, out var value))
            {
                if (value is float f) return f;
                if (value is double d) return (float)d;
                if (float.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return parsed;
            }
            return 0f;
        }

        private static List<string> GetStringList(Dictionary<string, object> data, string key)
        {
            var list = new List<string>();
            if (data.TryGetValue(key, out var value) && value is IList<object> items)
            {
                foreach (var item in items)
                    list.Add(item?.ToString() ?? "");
            }
            return list;
        }

        private static Dictionary<string, object> GetDict(Dictionary<string, object> data, string key)
        {
            if (data.TryGetValue(key, out var value) && value is Dictionary<string, object> dict)
                return dict;
            return null;
        }

        private static int GetInt(Dictionary<string, object> data, string key)
        {
            if (data.TryGetValue(key, out var value))
            {
                if (value is long l) return (int)l;
                if (value is int i) return i;
                if (value is double d) return (int)d;
                if (int.TryParse(value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) return parsed;
            }
            return 0;
        }

        private static double GetDouble(Dictionary<string, object> data, string key)
        {
            if (data.TryGetValue(key, out var value))
            {
                if (value is double d) return d;
                if (value is float f) return f;
                if (value is long l) return l;
                if (double.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return parsed;
            }
            return 0.0;
        }

        private static int? GetNullableInt(Dictionary<string, object> data, string key)
        {
            if (data.TryGetValue(key, out var value) && value != null)
            {
                if (value is int i) return i;
                if (value is long l) return (int)l;
                if (value is double d) return (int)d;
                if (int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) return parsed;
            }
            return null;
        }

        private static double? GetNullableDouble(Dictionary<string, object> data, string key)
        {
            if (data.TryGetValue(key, out var value) && value != null)
            {
                if (value is double d) return d;
                if (value is float f) return f;
                if (value is long l) return l;
                if (double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return parsed;
            }
            return null;
        }

        private static bool? GetNullableBool(Dictionary<string, object> data, string key)
        {
            if (data.TryGetValue(key, out var value) && value != null)
            {
                if (value is bool b) return b;
                if (bool.TryParse(value.ToString(), out var parsed)) return parsed;
            }
            return null;
        }

        private static long? GetNullableLong(Dictionary<string, object> data, string key)
        {
            if (data.TryGetValue(key, out var value) && value != null)
            {
                if (value is long l) return l;
                if (value is int i) return i;
                if (value is double d) return (long)d;
                if (long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) return parsed;
            }
            return null;
        }

        private static List<T> GetList<T>(Dictionary<string, object> data, string key, Func<Dictionary<string, object>, T> parse)
        {
            var result = new List<T>();
            if (data.TryGetValue(key, out var value) && value is IList<object> items)
            {
                foreach (var item in items)
                {
                    if (item is Dictionary<string, object> dict)
                        result.Add(parse(dict));
                }
            }
            return result;
        }

        private static T GetObject<T>(Dictionary<string, object> data, string key, Func<Dictionary<string, object>, T> parse) where T : class
        {
            return data.TryGetValue(key, out var value) && value is Dictionary<string, object> dict ? parse(dict) : null;
        }

        private static object GetRaw(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out var value) ? value : null;
        }

        private static List<MessageAnalysis> ParseMessageAnalysisList(Dictionary<string, object> data)
        {
            if (!data.ContainsKey("message_analysis")) return null;
            return GetList(data, "message_analysis", item => new MessageAnalysis
            {
                MessageIndex = GetInt(item, "message_index"),
                RiskScore = GetFloat(item, "risk_score"),
                Flags = GetStringList(item, "flags"),
                Summary = GetString(item, "summary")
            });
        }

        /// <summary>
        /// "?a=1&amp;b=2" from the pairs whose value is set, values URL-encoded; "" when none are.
        /// </summary>
        private static string BuildQuery(params (string Key, string Value)[] pairs)
        {
            var parts = new List<string>();
            foreach (var (key, value) in pairs)
            {
                if (!string.IsNullOrEmpty(value))
                    parts.Add($"{key}={Uri.EscapeDataString(value)}");
            }
            return parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        }

        private static Dictionary<string, int> GetIntDict(Dictionary<string, object> data, string key)
        {
            var result = new Dictionary<string, int>();
            if (data.TryGetValue(key, out var value) && value is Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    if (kvp.Value is long l) result[kvp.Key] = (int)l;
                    else if (kvp.Value is int i) result[kvp.Key] = i;
                    else if (kvp.Value is double d) result[kvp.Key] = (int)d;
                    else if (int.TryParse(kvp.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) result[kvp.Key] = parsed;
                }
            }
            return result;
        }
    }
}
