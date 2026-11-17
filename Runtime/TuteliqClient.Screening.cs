using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tuteliq
{
    public partial class TuteliqClient
    {
        private const int MaxNicknameLength = 64;

        // =====================================================================
        // Distress Signals
        // =====================================================================

        /// <summary>
        /// Detect linguistic distress-signal patterns.
        /// </summary>
        public async Task<DistressSignalsResult> DetectDistressSignalsAsync(DetectionInput input)
        {
            var data = await RequestAsync("POST", "/api/v1/safety/distress-signals", BuildDetectionBody(input));
            return ParseDistressSignalsResult(data);
        }

        // =====================================================================
        // Nickname Screening
        // =====================================================================

        /// <summary>
        /// Screen a display name before it is shown to other players.
        /// The result carries <see cref="NicknameResult.Recommendation"/>, not a moderation action.
        /// When <see cref="NicknameResult.ReadingComplete"/> is false, retry: a "none" signal then is not clean.
        /// </summary>
        public async Task<NicknameResult> DetectNicknameAsync(DetectNicknameInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new ValidationException("Name is required");
            if (input.Name.Length > MaxNicknameLength)
                throw new ValidationException($"Name exceeds maximum length of {MaxNicknameLength} characters");

            var data = await RequestAsync("POST", "/api/v1/safety/nickname", BuildNicknameBody(input));
            return ParseNicknameResult(data);
        }

        private static Dictionary<string, object> BuildNicknameBody(DetectNicknameInput input)
        {
            var body = new Dictionary<string, object> { { "name", input.Name } };
            if (input.Strictness.HasValue) body["strictness"] = input.Strictness.Value.ToApiString();

            var context = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(input.Language)) context["language"] = input.Language;
            if (!string.IsNullOrEmpty(input.AgeGroup)) context["age_group"] = input.AgeGroup;
            if (!string.IsNullOrEmpty(input.ConversationType)) context["conversation_type"] = input.ConversationType;
            if (input.PlatformReserved != null) context["platform_reserved"] = input.PlatformReserved;
            if (context.Count > 0) body["context"] = context;
            return body;
        }

        // =====================================================================
        // Image Match
        // =====================================================================

        /// <summary>
        /// Run face match, age estimate, explicit-content and synthetic-image checks on one image.
        /// </summary>
        public async Task<VerifyImageMatchResult> VerifyImageMatchAsync(VerifyImageMatchInput input)
        {
            if (input.Image == null || input.Image.Length == 0)
                throw new ValidationException("Image is required");

            var data = await RequestAsync("POST", "/api/v1/verify-image-match", BuildImageMatchBody(input));
            return ParseVerifyImageMatchResult(data);
        }

        private static Dictionary<string, object> BuildImageMatchBody(VerifyImageMatchInput input)
        {
            var body = new Dictionary<string, object> { { "image", Convert.ToBase64String(input.Image) } };
            if (input.ReferenceImage != null && input.ReferenceImage.Length > 0)
                body["reference_image"] = Convert.ToBase64String(input.ReferenceImage);
            if (input.Checks != null)
            {
                var checks = new List<string>();
                foreach (var check in input.Checks) checks.Add(check.ToApiString());
                body["checks"] = checks;
            }
            if (input.ExternalId != null) body["external_id"] = input.ExternalId;
            if (input.CustomerId != null) body["customer_id"] = input.CustomerId;
            if (input.Metadata != null) body["metadata"] = input.Metadata;
            return body;
        }

        // =====================================================================
        // Privacy Redaction
        // =====================================================================

        /// <summary>
        /// Remove personal data from a text or a conversation. Nothing is stored.
        /// </summary>
        public async Task<RedactResult> RedactAsync(RedactInput input)
        {
            var hasText = !string.IsNullOrEmpty(input.Text);
            var hasMessages = input.Messages != null && input.Messages.Count > 0;
            if (hasText == hasMessages)
                throw new ValidationException("Set either Text or Messages");

            var data = await RequestAsync("POST", "/api/v1/privacy/redact", BuildRedactBody(input, hasText));
            return ParseRedactResult(data);
        }

        private static Dictionary<string, object> BuildRedactBody(RedactInput input, bool hasText)
        {
            var body = new Dictionary<string, object>();
            if (hasText) body["text"] = input.Text;
            else body["messages"] = input.Messages;
            if (!string.IsNullOrEmpty(input.Language)) body["language"] = input.Language;
            if (input.Mode.HasValue) body["mode"] = input.Mode.Value.ToApiString();
            if (input.StrictAnonymisation.HasValue) body["strict_anonymisation"] = input.StrictAnonymisation.Value;
            return body;
        }

        // =====================================================================
        // Document Analysis
        // =====================================================================

        /// <summary>
        /// Analyze a document (PDF, or an image read as one page). Each page is analyzed
        /// against the selected detection endpoints.
        /// </summary>
        public async Task<DocumentAnalysisResult> AnalyzeDocumentAsync(AnalyzeDocumentInput input)
        {
            if (input.File == null || input.File.Length == 0)
                throw new ValidationException("Document file is required");
            if (string.IsNullOrEmpty(input.Filename))
                throw new ValidationException("Filename is required");

            var data = await MultipartRequestAsync("/api/v1/safety/document", BuildDocumentForm(input));
            return ParseDocumentAnalysisResult(data);
        }

        private static MultipartForm BuildDocumentForm(AnalyzeDocumentInput input)
        {
            return new MultipartForm()
                .AddFile("file", input.File, input.Filename)
                .Add("platform", ResolvePlatform(input.Platform))
                .AddIfSet("endpoints", input.Endpoints != null ? MiniJson.Serialize(input.Endpoints) : null)
                .AddIfSet("file_id", input.FileId)
                .AddIfSet("age_group", input.AgeGroup)
                .AddIfSet("language", input.Language)
                .AddIfSet("support_threshold", input.SupportThreshold)
                .AddIfSet("split_by_sender", input.SplitBySender ? "true" : null)
                .AddIfSet("external_id", input.ExternalId)
                .AddIfSet("customer_id", input.CustomerId)
                .AddIfSet("metadata", input.Metadata != null ? MiniJson.Serialize(input.Metadata) : null);
        }

        // =====================================================================
        // Parsing
        // =====================================================================

        private DistressSignalsResult ParseDistressSignalsResult(Dictionary<string, object> data)
        {
            var result = FillDetectionResult(new DistressSignalsResult(), data);
            result.DistressReading = GetString(data, "distress_reading");
            result.DistressSubject = GetString(data, "distress_subject");
            result.SupportSuggested = GetNullableBool(data, "support_suggested");
            return result;
        }

        private static NicknameResult ParseNicknameResult(Dictionary<string, object> data)
        {
            return new NicknameResult
            {
                Signal = GetString(data, "signal"),
                Categories = GetStringList(data, "categories"),
                Parts = GetList(data, "parts", part => new NicknamePart
                {
                    Text = GetString(part, "text"),
                    Reading = GetString(part, "reading"),
                    Category = GetString(part, "category")
                }),
                Rationale = GetString(data, "rationale"),
                Recommendation = GetString(data, "recommendation"),
                RecommendationDetail = GetString(data, "recommendation_detail"),
                Confidence = GetDouble(data, "confidence"),
                Language = GetString(data, "language"),
                Strictness = GetString(data, "strictness"),
                ReadingComplete = GetBool(data, "reading_complete"),
                CreditsUsed = GetNullableInt(data, "credits_used")
            };
        }

        private static VerifyImageMatchResult ParseVerifyImageMatchResult(Dictionary<string, object> data)
        {
            return new VerifyImageMatchResult
            {
                SchemaVersion = GetString(data, "schema_version"),
                ChecksRun = GetStringList(data, "checks_run"),
                FaceMatch = GetObject(data, "face_match", face => new ImageFaceMatch
                {
                    Matched = GetBool(face, "matched"),
                    Similarity = GetNullableDouble(face, "similarity"),
                    MatchScore = GetNullableDouble(face, "match_score"),
                    Reason = GetString(face, "reason")
                }),
                Age = GetObject(data, "age", age => new ImageAgeEstimate
                {
                    EstimatedAge = GetDouble(age, "estimated_age"),
                    Confidence = GetDouble(age, "confidence")
                }),
                Forensic = GetObject(data, "forensic", forensic => new ImageForensic
                {
                    Nsfw = GetBool(forensic, "nsfw"),
                    NsfwConfidence = GetNullableDouble(forensic, "nsfw_confidence"),
                    SkinExposureLevel = GetString(forensic, "skin_exposure_level")
                }),
                Synthetic = GetObject(data, "synthetic", synthetic => new ImageSynthetic
                {
                    IsSynthetic = GetNullableBool(synthetic, "is_synthetic"),
                    SyntheticProbability = GetNullableDouble(synthetic, "synthetic_probability"),
                    HasC2pa = GetNullableBool(synthetic, "has_c2pa"),
                    IsAiGenerated = GetNullableBool(synthetic, "is_ai_generated")
                }),
                CreditsUsed = GetNullableInt(data, "credits_used"),
                ProcessingMs = GetNullableDouble(data, "processing_ms"),
                ExternalId = GetString(data, "external_id"),
                CustomerId = GetString(data, "customer_id"),
                Metadata = GetDict(data, "metadata")
            };
        }

        private static RedactResult ParseRedactResult(Dictionary<string, object> data)
        {
            return new RedactResult
            {
                Text = GetString(data, "text"),
                Messages = data.ContainsKey("messages") ? GetStringList(data, "messages") : null,
                Counts = GetIntDict(data, "counts"),
                EntitiesTotal = GetInt(data, "entities_total"),
                Mode = GetString(data, "mode"),
                StrictAnonymisation = GetBool(data, "strict_anonymisation"),
                ModelApplied = GetBool(data, "model_applied"),
                CreditsUsed = GetNullableInt(data, "credits_used")
            };
        }

        private DocumentAnalysisResult ParseDocumentAnalysisResult(Dictionary<string, object> data)
        {
            return new DocumentAnalysisResult
            {
                FileId = GetString(data, "file_id"),
                DocumentHash = GetString(data, "document_hash"),
                TotalPages = GetInt(data, "total_pages"),
                PagesAnalyzed = GetInt(data, "pages_analyzed"),
                ExtractionSummary = GetObject(data, "extraction_summary", summary => new DocumentExtractionSummary
                {
                    TextLayerPages = GetInt(summary, "text_layer_pages"),
                    OcrPages = GetInt(summary, "ocr_pages"),
                    FailedPages = GetInt(summary, "failed_pages"),
                    AverageOcrConfidence = GetDouble(summary, "average_ocr_confidence")
                }),
                PageResults = GetList(data, "page_results", ParseDocumentPageResult),
                OverallRiskScore = GetDouble(data, "overall_risk_score"),
                OverallSeverity = GetString(data, "overall_severity"),
                DetectedEndpoints = GetStringList(data, "detected_endpoints"),
                FlaggedPages = GetList(data, "flagged_pages", page => new DocumentFlaggedPage
                {
                    PageNumber = GetInt(page, "page_number"),
                    RiskScore = GetDouble(page, "risk_score"),
                    Severity = GetString(page, "severity"),
                    DetectedEndpoints = GetStringList(page, "detected_endpoints")
                }),
                CreditsUsed = GetNullableInt(data, "credits_used"),
                ProcessingTimeMs = GetNullableDouble(data, "processing_time_ms"),
                Language = GetString(data, "language"),
                LanguageStatus = GetString(data, "language_status"),
                Support = GetDict(data, "support"),
                ExternalId = GetString(data, "external_id"),
                CustomerId = GetString(data, "customer_id"),
                Metadata = GetDict(data, "metadata")
            };
        }

        private DocumentPageResult ParseDocumentPageResult(Dictionary<string, object> data)
        {
            return new DocumentPageResult
            {
                PageNumber = GetInt(data, "page_number"),
                TextPreview = GetString(data, "text_preview"),
                ExtractionMethod = GetString(data, "extraction_method"),
                OcrConfidence = GetNullableDouble(data, "ocr_confidence"),
                Results = GetList(data, "results", ParseDocumentPageEndpointResult),
                PageRiskScore = GetDouble(data, "page_risk_score"),
                PageSeverity = GetString(data, "page_severity")
            };
        }

        private DocumentPageEndpointResult ParseDocumentPageEndpointResult(Dictionary<string, object> data)
        {
            return new DocumentPageEndpointResult
            {
                Endpoint = GetString(data, "endpoint"),
                Detected = GetBool(data, "detected"),
                Severity = GetDouble(data, "severity"),
                Confidence = GetDouble(data, "confidence"),
                RiskScore = GetDouble(data, "risk_score"),
                Level = GetString(data, "level"),
                Categories = GetList(data, "categories", ParseDetectionCategory),
                Evidence = GetList(data, "evidence", ParseDetectionEvidence),
                RecommendedAction = GetString(data, "recommended_action"),
                Rationale = GetString(data, "rationale")
            };
        }
    }
}
