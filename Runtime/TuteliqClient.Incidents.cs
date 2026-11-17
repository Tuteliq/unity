using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tuteliq
{
    public partial class TuteliqClient
    {
        private const int MaxBatchReviewIncidents = 100;

        // =====================================================================
        // Incidents
        // =====================================================================

        /// <summary>
        /// List the account's incidents, newest first. Pass <see cref="ListIncidentsResult.NextCursor"/>
        /// back as <see cref="ListIncidentsInput.Cursor"/> for the next page.
        /// </summary>
        public async Task<ListIncidentsResult> ListIncidentsAsync(ListIncidentsInput input = null)
        {
            var data = await RequestAsync("GET", "/api/v1/incidents" + BuildListIncidentsQuery(input ?? new ListIncidentsInput()));
            return new ListIncidentsResult
            {
                Incidents = GetList(data, "incidents", row => FillIncidentSummary(new IncidentSummary(), row)),
                NextCursor = GetString(data, "next_cursor"),
                TotalReturned = GetInt(data, "total_returned")
            };
        }

        /// <summary>
        /// Get one incident's full detail.
        /// </summary>
        public async Task<IncidentDetail> GetIncidentAsync(string incidentId)
        {
            RequireId(incidentId, "Incident ID");
            var data = await RequestAsync("GET", $"/api/v1/incidents/{Uri.EscapeDataString(incidentId)}");
            return ParseIncidentDetail(data);
        }

        /// <summary>
        /// Incident counts over a window (default: the last 30 days).
        /// </summary>
        public async Task<IncidentsOverview> GetIncidentsOverviewAsync(string from = null, string to = null)
        {
            var data = await RequestAsync("GET", "/api/v1/incidents/overview" + BuildQuery(("from", from), ("to", to)));
            return ParseIncidentsOverview(data);
        }

        /// <summary>
        /// Incident counts per time bucket (default: daily, last 30 days).
        /// </summary>
        public async Task<IncidentTrends> GetIncidentTrendsAsync(IncidentTrendBucket? bucket = null, string from = null, string to = null)
        {
            var query = BuildQuery(("bucket", bucket?.ToApiString()), ("from", from), ("to", to));
            var data = await RequestAsync("GET", "/api/v1/incidents/trends" + query);
            return ParseIncidentTrends(data);
        }

        /// <summary>
        /// Record a moderator's decision on an incident. A signed audit receipt is written for it.
        /// </summary>
        public async Task<ReviewIncidentResult> ReviewIncidentAsync(string incidentId, ReviewIncidentInput input)
        {
            RequireId(incidentId, "Incident ID");
            ValidateReview(input);
            var data = await ChangeAsync("POST", $"/api/v1/incidents/{Uri.EscapeDataString(incidentId)}/review", BuildReviewBody(input));
            return FillReviewResult(new ReviewIncidentResult(), data);
        }

        /// <summary>
        /// Apply the same moderator decision to up to 100 incidents. Each incident still gets its own
        /// audit receipt; a failure on one does not stop the others.
        /// </summary>
        public async Task<BatchReviewIncidentsResult> BatchReviewIncidentsAsync(BatchReviewIncidentsInput input)
        {
            if (input.IncidentIds == null || input.IncidentIds.Count == 0)
                throw new ValidationException("IncidentIds must not be empty");
            if (input.IncidentIds.Count > MaxBatchReviewIncidents)
                throw new ValidationException($"At most {MaxBatchReviewIncidents} incidents per batch");
            ValidateReview(input);

            var body = BuildReviewBody(input);
            body["incident_ids"] = input.IncidentIds;
            var data = await ChangeAsync("POST", "/api/v1/incidents/batch-review", body);
            return new BatchReviewIncidentsResult
            {
                Total = GetInt(data, "total"),
                Succeeded = GetInt(data, "succeeded"),
                Failed = GetInt(data, "failed"),
                Results = GetList(data, "results", item =>
                {
                    var result = FillReviewResult(new BatchReviewItemResult(), item);
                    result.Ok = GetBool(item, "ok");
                    result.Error = GetString(item, "error");
                    return result;
                })
            };
        }

        private static string BuildListIncidentsQuery(ListIncidentsInput input)
        {
            return BuildQuery(
                ("category", input.Category),
                ("severity", input.Severity),
                ("status", input.Status),
                ("source", input.Source),
                ("from", input.From),
                ("to", input.To),
                ("platform", input.Platform),
                ("external_id", input.ExternalId),
                ("customer_id", input.CustomerId),
                ("limit", input.Limit.HasValue ? FormatInt(input.Limit.Value) : null),
                ("cursor", input.Cursor),
                ("include_summary", input.IncludeSummary ? "true" : null));
        }

        private static void ValidateReview(ReviewIncidentInput input)
        {
            if (string.IsNullOrEmpty(input.ModeratorExternalId))
                throw new ValidationException("ModeratorExternalId is required");
            if (input.Action == ModeratorAction.Reclassify && string.IsNullOrEmpty(input.NewRiskCategory))
                throw new ValidationException("Reclassify requires NewRiskCategory");
        }

        private static Dictionary<string, object> BuildReviewBody(ReviewIncidentInput input)
        {
            var body = new Dictionary<string, object>
            {
                { "action", input.Action.ToApiString() },
                { "reason_code", input.ReasonCode.ToApiString() },
                { "moderator_external_id", input.ModeratorExternalId }
            };
            if (input.ReasonComment != null) body["reason_comment"] = input.ReasonComment;
            if (input.NewRiskLevel != null) body["new_risk_level"] = input.NewRiskLevel;
            if (input.NewRiskCategory != null) body["new_risk_category"] = input.NewRiskCategory;
            if (input.RetentionClass != null) body["retention_class"] = input.RetentionClass;
            return body;
        }

        // =====================================================================
        // Parsing
        // =====================================================================

        private static T FillIncidentSummary<T>(T incident, Dictionary<string, object> data) where T : IncidentSummary
        {
            incident.Id = GetString(data, "id");
            incident.RiskCategory = GetString(data, "risk_category");
            incident.RiskLevel = GetString(data, "risk_level");
            incident.ConfidenceScore = GetNullableDouble(data, "confidence_score");
            incident.DetectedPatterns = GetStringList(data, "detected_patterns");
            incident.RecommendedActions = GetStringList(data, "recommended_actions");
            incident.Platform = GetString(data, "platform");
            incident.Language = GetString(data, "language");
            incident.Source = GetString(data, "source");
            incident.FileId = GetString(data, "file_id");
            incident.Status = GetString(data, "status");
            incident.PolicyAction = GetDict(data, "policy_action");
            incident.RecommendedStatus = GetString(data, "recommended_status");
            incident.SpeakerStance = GetString(data, "speaker_stance");
            incident.IsTest = GetNullableBool(data, "is_test");
            incident.ExternalId = GetString(data, "external_id");
            incident.CustomerId = GetString(data, "customer_id");
            incident.CreatedAt = GetString(data, "created_at");
            incident.Summary = GetRaw(data, "summary");
            incident.E2eEnvelopeFields = GetStringList(data, "_e2e_envelope_fields");
            return incident;
        }

        private static IncidentDetail ParseIncidentDetail(Dictionary<string, object> data)
        {
            var incident = FillIncidentSummary(new IncidentDetail(), data);
            incident.Metadata = GetRaw(data, "metadata");
            incident.SourceData = GetDict(data, "source_data");
            incident.VisualCategories = GetStringList(data, "visual_categories");
            incident.VisualSeverity = GetString(data, "visual_severity");
            incident.VisualConfidence = GetNullableDouble(data, "visual_confidence");
            incident.ContainsText = GetNullableBool(data, "contains_text");
            incident.ContainsFaces = GetNullableBool(data, "contains_faces");
            incident.EmotionalIndicators = GetStringList(data, "emotional_indicators");
            incident.Review = GetDict(data, "review");
            incident.MessageAnalysis = ParseMessageAnalysisList(data);
            return incident;
        }

        private static IncidentsOverview ParseIncidentsOverview(Dictionary<string, object> data)
        {
            var timeframe = GetDict(data, "timeframe") ?? new Dictionary<string, object>();
            return new IncidentsOverview
            {
                From = GetString(timeframe, "from"),
                To = GetString(timeframe, "to"),
                TotalIncidents = GetInt(data, "total_incidents"),
                RequiresReviewCount = GetInt(data, "requires_review_count"),
                Last24hCount = GetInt(data, "last_24h_count"),
                Last7dCount = GetInt(data, "last_7d_count"),
                Last30dCount = GetInt(data, "last_30d_count"),
                CountsByCategory = GetIntDict(data, "counts_by_category"),
                CountsBySeverity = GetIntDict(data, "counts_by_severity"),
                CountsBySource = GetIntDict(data, "counts_by_source"),
                CountsByStatus = GetIntDict(data, "counts_by_status"),
                TopPlatforms = GetList(data, "top_platforms", row => new IncidentPlatformCount
                {
                    Platform = GetString(row, "platform"),
                    Count = GetInt(row, "count")
                })
            };
        }

        private static IncidentTrends ParseIncidentTrends(Dictionary<string, object> data)
        {
            var timeframe = GetDict(data, "timeframe") ?? new Dictionary<string, object>();
            return new IncidentTrends
            {
                BucketSize = GetString(data, "bucket_size"),
                From = GetString(timeframe, "from"),
                To = GetString(timeframe, "to"),
                Series = GetList(data, "series", bucket => new IncidentTrendsBucket
                {
                    BucketStart = GetString(bucket, "bucket_start"),
                    Total = GetInt(bucket, "total"),
                    BySeverity = GetIntDict(bucket, "by_severity")
                })
            };
        }

        private static T FillReviewResult<T>(T result, Dictionary<string, object> data) where T : ReviewIncidentResult
        {
            result.IncidentId = GetString(data, "incident_id");
            result.Original = GetObject(data, "original", ParseIncidentClassification);
            result.Revised = GetObject(data, "revised", ParseIncidentClassification);
            result.AuditReceipt = GetObject(data, "audit_receipt", receipt => new IncidentAuditReceipt
            {
                RequestId = GetString(receipt, "request_id"),
                Timestamp = GetString(receipt, "timestamp"),
                Signature = GetString(receipt, "signature")
            });
            return result;
        }

        private static IncidentClassification ParseIncidentClassification(Dictionary<string, object> data)
        {
            return new IncidentClassification
            {
                RiskCategory = GetString(data, "risk_category"),
                RiskLevel = GetString(data, "risk_level")
            };
        }
    }
}
