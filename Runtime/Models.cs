using System;
using System.Collections.Generic;

namespace Tuteliq
{
    // =========================================================================
    // Context
    // =========================================================================

    /// <summary>
    /// Optional context for analysis.
    /// </summary>
    [Serializable]
    public class AnalysisContext
    {
        public string Language;
        public string AgeGroup;
        public string Relationship;
        public string Platform;
    }

    // =========================================================================
    // Messages
    // =========================================================================

    /// <summary>
    /// Message for grooming detection.
    /// </summary>
    [Serializable]
    public class GroomingMessage
    {
        public MessageRole Role;
        public string Content;
        /// <summary>Age of this message's sender, when your platform knows it.</summary>
        public int? SenderAge;

        public GroomingMessage() { }

        public GroomingMessage(MessageRole role, string content, int? senderAge = null)
        {
            Role = role;
            Content = content;
            SenderAge = senderAge;
        }
    }

    /// <summary>
    /// Message for emotion analysis.
    /// </summary>
    [Serializable]
    public class EmotionMessage
    {
        public string Sender;
        public string Content;

        public EmotionMessage() { }

        public EmotionMessage(string sender, string content)
        {
            Sender = sender;
            Content = content;
        }
    }

    /// <summary>
    /// Message for incident reports.
    /// </summary>
    [Serializable]
    public class ReportMessage
    {
        public string Sender;
        public string Content;

        public ReportMessage() { }

        public ReportMessage(string sender, string content)
        {
            Sender = sender;
            Content = content;
        }
    }

    // =========================================================================
    // Input Types
    // =========================================================================

    /// <summary>
    /// Input for bullying detection.
    /// </summary>
    public class DetectBullyingInput
    {
        public string Content;
        public AnalysisContext Context;
        public string ExternalId;
        /// <summary>Your end-customer identifier for multi-tenant / B2B2C routing (max 255 chars).</summary>
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Input for grooming detection.
    /// </summary>
    public class DetectGroomingInput
    {
        public List<GroomingMessage> Messages;
        /// <summary>Age of the child in the conversation.</summary>
        public int? ChildAge;
        /// <summary>Age of the other (non-minor) participant, when known.</summary>
        public int? ParticipantAge;
        public AnalysisContext Context;
        public string ExternalId;
        /// <summary>Your end-customer identifier for multi-tenant / B2B2C routing (max 255 chars).</summary>
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Input for unsafe content detection.
    /// </summary>
    public class DetectUnsafeInput
    {
        public string Content;
        public AnalysisContext Context;
        public string ExternalId;
        /// <summary>Your end-customer identifier for multi-tenant / B2B2C routing (max 255 chars).</summary>
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Input for quick analysis.
    /// </summary>
    public class AnalyzeInput
    {
        public string Content;
        public AnalysisContext Context;
        public List<string> Include;
        public string ExternalId;
        /// <summary>Your end-customer identifier for multi-tenant / B2B2C routing (max 255 chars).</summary>
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Input for emotion analysis.
    /// </summary>
    public class AnalyzeEmotionsInput
    {
        public string Content;
        public List<EmotionMessage> Messages;
        public AnalysisContext Context;
        public string ExternalId;
        /// <summary>Your end-customer identifier for multi-tenant / B2B2C routing (max 255 chars).</summary>
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Input for action plan generation.
    /// </summary>
    public class GetActionPlanInput
    {
        public string Situation;
        public int? ChildAge;
        public Audience? Audience;
        public Severity? Severity;
        public string ExternalId;
        /// <summary>Your end-customer identifier for multi-tenant / B2B2C routing (max 255 chars).</summary>
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Input for incident report generation.
    /// </summary>
    public class GenerateReportInput
    {
        public List<ReportMessage> Messages;
        public int? ChildAge;
        public string IncidentType;
        public string ExternalId;
        /// <summary>Your end-customer identifier for multi-tenant / B2B2C routing (max 255 chars).</summary>
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    // =========================================================================
    // Result Types
    // =========================================================================

    /// <summary>
    /// Result of bullying detection.
    /// </summary>
    [Serializable]
    public class BullyingResult
    {
        /// <summary>
        /// Whether the message bullies someone. False when it reports bullying instead
        /// (see <see cref="BullyingReported"/>): branch on <see cref="Action"/>, not on this.
        /// </summary>
        public bool IsBullying;
        /// <summary>
        /// True when the speaker is the target or a bystander telling of bullying. The message
        /// still needs a person (see <see cref="Action"/>), but as a report to support, not a
        /// reason to sanction the speaker.
        /// </summary>
        public bool BullyingReported;
        /// <summary>"target" or "bystander". Set with <see cref="BullyingReported"/>.</summary>
        public string BullyingReportedBy;
        /// <summary>
        /// The speaker's relationship to the conduct: "enacting", "experiencing",
        /// "reporting_other", "self_directed" or "unclear".
        /// </summary>
        public string SpeakerRole;
        public Severity Severity;
        public List<string> BullyingType;
        public float Confidence;
        public string Rationale;
        public float RiskScore;
        public string RecommendedAction;
        /// <summary><see cref="RecommendedAction"/> as an enum. Route moderation on this.</summary>
        public ModerationAction Action => ModerationActions.Parse(RecommendedAction);
        public string Language;
        public string LanguageStatus;
        public int? CreditsUsed;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Result of grooming detection.
    /// </summary>
    [Serializable]
    public class GroomingResult
    {
        public GroomingRisk GroomingRisk;
        public List<string> Flags;
        public float Confidence;
        public string Rationale;
        public float RiskScore;
        public string RecommendedAction;
        /// <summary><see cref="RecommendedAction"/> as an enum. Route moderation on this.</summary>
        public ModerationAction Action => ModerationActions.Parse(RecommendedAction);
        public List<MessageAnalysis> MessageAnalysis;
        public string Language;
        public string LanguageStatus;
        public int? CreditsUsed;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Result of unsafe content detection.
    /// </summary>
    [Serializable]
    public class UnsafeResult
    {
        public bool Unsafe;
        public List<string> Categories;
        public Severity Severity;
        public float Confidence;
        public string Rationale;
        public float RiskScore;
        public string RecommendedAction;
        /// <summary><see cref="RecommendedAction"/> as an enum. Route moderation on this.</summary>
        public ModerationAction Action => ModerationActions.Parse(RecommendedAction);
        public string Language;
        public string LanguageStatus;
        public int? CreditsUsed;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Result of quick analysis.
    /// </summary>
    [Serializable]
    public class AnalyzeResult
    {
        public RiskLevel RiskLevel;
        public float RiskScore;
        public string Summary;
        public string RecommendedAction;
        /// <summary><see cref="RecommendedAction"/> as an enum. Route moderation on this.</summary>
        public ModerationAction Action => ModerationActions.Parse(RecommendedAction);
        public BullyingResult Bullying;
        public UnsafeResult Unsafe;
        public int? CreditsUsed;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Result of emotion analysis.
    /// </summary>
    [Serializable]
    public class EmotionsResult
    {
        public List<string> DominantEmotions;
        public EmotionTrend Trend;
        public float Intensity;
        public List<string> ConcerningPatterns;
        public string RecommendedFollowup;
        public int? CreditsUsed;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Result of action plan generation.
    /// </summary>
    [Serializable]
    public class ActionPlanResult
    {
        public List<string> Steps;
        public string Tone;
        public List<string> Resources;
        public string Urgency;
        public int? CreditsUsed;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Result of incident report generation.
    /// </summary>
    [Serializable]
    public class ReportResult
    {
        public string Summary;
        public RiskLevel RiskLevel;
        public List<string> Timeline;
        public List<string> KeyEvidence;
        public List<string> RecommendedNextSteps;
        public int? CreditsUsed;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    // =========================================================================
    // Account Management (GDPR)
    // =========================================================================

    /// <summary>
    /// Result of account data deletion (GDPR Article 17).
    /// </summary>
    [Serializable]
    public class AccountDeletionResult
    {
        public string Message;
        public int DeletedCount;
    }

    /// <summary>
    /// Result of account data export (GDPR Article 20).
    /// </summary>
    [Serializable]
    public class AccountExportResult
    {
        public string UserId;
        public string ExportedAt;
        public Dictionary<string, object> Data;
    }

    // =========================================================================
    // Consent Management (GDPR Article 7)
    // =========================================================================

    [Serializable]
    public class ConsentRecord
    {
        public string Id;
        public string UserId;
        public string ConsentType;
        public string Status;
        public string Version;
        public string CreatedAt;
    }

    [Serializable]
    public class ConsentActionResult
    {
        public string Message;
        public ConsentRecord Consent;
    }

    [Serializable]
    public class ConsentStatusResult
    {
        public List<ConsentRecord> Consents;
    }

    public class RecordConsentInput
    {
        public string ConsentType;
        public string Version;
    }

    public class RectifyDataInput
    {
        public string Collection;
        public string DocumentId;
        public Dictionary<string, object> Fields;
    }

    [Serializable]
    public class RectifyDataResult
    {
        public string Message;
        public List<string> UpdatedFields;
    }

    [Serializable]
    public class AuditLogEntry
    {
        public string Id;
        public string UserId;
        public string Action;
        public string CreatedAt;
        public Dictionary<string, object> Details;
    }

    [Serializable]
    public class AuditLogsResult
    {
        public List<AuditLogEntry> AuditLogs;
    }

    /// <summary>
    /// API usage information.
    /// </summary>
    [Serializable]
    public class Usage
    {
        public int Limit;
        public int Used;
        public int Remaining;
    }

    // =========================================================================
    // Breach Management (GDPR Article 33/34)
    // =========================================================================

    public class LogBreachInput
    {
        public string Title;
        public string Description;
        public string Severity;
        public List<string> AffectedUserIds;
        public List<string> DataCategories;
        public string ReportedBy;
    }

    public class UpdateBreachInput
    {
        public string Status;
        public string NotificationStatus;
        public string Notes;
    }

    [Serializable]
    public class BreachRecord
    {
        public string Id;
        public string Title;
        public string Description;
        public string Severity;
        public string Status;
        public string NotificationStatus;
        public List<string> AffectedUserIds;
        public List<string> DataCategories;
        public string ReportedBy;
        public string NotificationDeadline;
        public string CreatedAt;
        public string UpdatedAt;
    }

    [Serializable]
    public class LogBreachResult
    {
        public string Message;
        public BreachRecord Breach;
    }

    [Serializable]
    public class BreachListResult
    {
        public List<BreachRecord> Breaches;
    }

    [Serializable]
    public class BreachResult
    {
        public BreachRecord Breach;
    }

    // =========================================================================
    // Voice Analysis
    // =========================================================================

    [Serializable]
    public class TranscriptionSegment
    {
        public double Start;
        public double End;
        public string Text;
    }

    [Serializable]
    public class TranscriptionResult
    {
        public string Text;
        public string Language;
        public double? Duration;
        public List<TranscriptionSegment> Segments;
    }

    [Serializable]
    public class VoiceAnalysisResult
    {
        public string FileId;
        public TranscriptionResult Transcription;
        public Dictionary<string, object> Analysis;
        public double? OverallRiskScore;
        public string OverallSeverity;
        public int? CreditsUsed;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    // =========================================================================
    // Image Analysis
    // =========================================================================

    [Serializable]
    public class VisionResult
    {
        public string ExtractedText;
        public List<string> VisualCategories;
        public string VisualSeverity;
        public double? VisualConfidence;
        public string VisualDescription;
        public bool? ContainsText;
        public bool? ContainsFaces;
    }

    [Serializable]
    public class ImageAnalysisResult
    {
        public string FileId;
        public VisionResult Vision;
        public Dictionary<string, object> TextAnalysis;
        public double? OverallRiskScore;
        public string OverallSeverity;
        public int? CreditsUsed;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    // =========================================================================
    // Fraud / Extended Detection
    // =========================================================================

    /// <summary>
    /// Input for fraud and extended detection endpoints.
    /// </summary>
    public class DetectionInput
    {
        public string Content;
        public AnalysisContext Context;
        public bool IncludeEvidence;
        public string ExternalId;
        /// <summary>Your end-customer identifier for multi-tenant / B2B2C routing (max 255 chars).</summary>
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Input for multi-endpoint analysis.
    /// </summary>
    public class AnalyseMultiInput
    {
        public string Content;
        public List<Detection> Detections;
        public AnalysisContext Context;
        public bool IncludeEvidence;
        public string ExternalId;
        /// <summary>Your end-customer identifier for multi-tenant / B2B2C routing (max 255 chars).</summary>
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Per-message analysis from conversation-aware detection.
    /// </summary>
    [Serializable]
    public class MessageAnalysis
    {
        public int MessageIndex;
        public float RiskScore;
        public List<string> Flags;
        public string Summary;
    }

    /// <summary>
    /// A detected category within a detection result.
    /// </summary>
    [Serializable]
    public class DetectionCategory
    {
        public string Tag;
        public string Label;
        public double Confidence;
    }

    /// <summary>
    /// Evidence supporting a detection result.
    /// </summary>
    [Serializable]
    public class DetectionEvidence
    {
        public string Text;
        public string Tactic;
        public double Weight;
    }

    /// <summary>
    /// Age calibration applied to detection scoring.
    /// </summary>
    [Serializable]
    public class AgeCalibration
    {
        public bool Applied;
        public string AgeGroup;
        public double? Multiplier;
    }

    /// <summary>
    /// Result from a single fraud/extended detection endpoint.
    /// </summary>
    [Serializable]
    public class DetectionResult
    {
        public string Endpoint;
        public bool Detected;
        public double Severity;
        public double Confidence;
        public double RiskScore;
        public string Level;
        public List<DetectionCategory> Categories;
        public string RecommendedAction;
        /// <summary><see cref="RecommendedAction"/> as an enum. Route moderation on this.</summary>
        public ModerationAction Action => ModerationActions.Parse(RecommendedAction);
        public string Rationale;
        public string Language;
        public string LanguageStatus;
        public List<DetectionEvidence> Evidence;
        public AgeCalibration AgeCalibration;
        public List<MessageAnalysis> MessageAnalysis;
        public int? CreditsUsed;
        public double? ProcessingTimeMs;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Summary of multi-endpoint analysis results.
    /// </summary>
    [Serializable]
    public class AnalyseMultiSummary
    {
        public int TotalEndpoints;
        public int DetectedCount;
        public Dictionary<string, object> HighestRisk;
        public string OverallRiskLevel;
    }

    /// <summary>
    /// Result of multi-endpoint analysis.
    /// </summary>
    [Serializable]
    public class AnalyseMultiResult
    {
        public List<DetectionResult> Results;
        public AnalyseMultiSummary Summary;
        public double? CrossEndpointModifier;
        public int? CreditsUsed;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    // =========================================================================
    // Video Analysis
    // =========================================================================

    /// <summary>
    /// A safety finding from video frame analysis.
    /// </summary>
    [Serializable]
    public class VideoSafetyFinding
    {
        public int FrameIndex;
        public double Timestamp;
        public string Description;
        public List<string> Categories;
        public double Severity;
    }

    /// <summary>
    /// Result of video analysis.
    /// </summary>
    [Serializable]
    public class VideoAnalysisResult
    {
        public string FileId;
        public int FramesAnalyzed;
        public List<VideoSafetyFinding> SafetyFindings;
        public double OverallRiskScore;
        public string OverallSeverity;
        public int? CreditsUsed;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    // =========================================================================
    // Webhooks
    // =========================================================================

    [Serializable]
    public class WebhookInfo
    {
        public string Id;
        public string Name;
        public string Url;
        public List<string> Events;
        public bool IsActive;
        /// <summary>Consecutive delivery failures.</summary>
        public int FailureCount;
        public string LastTriggeredAt;
        public string LastError;
        public string CreatedAt;
        public string UpdatedAt;

        [Obsolete("Use IsActive.")]
        public bool Active => IsActive;
    }

    [Serializable]
    public class WebhookListResult
    {
        public List<WebhookInfo> Webhooks;
    }

    public class CreateWebhookInput
    {
        /// <summary>Webhook name (required, max 100 characters).</summary>
        public string Name;
        /// <summary>HTTPS URL that receives the events.</summary>
        public string Url;
        /// <summary>Event types to subscribe to (1 to 5).</summary>
        public List<string> Events;
        /// <summary>Optional custom headers sent with each delivery.</summary>
        public Dictionary<string, string> Headers;
    }

    [Serializable]
    public class CreateWebhookResult
    {
        public WebhookInfo Webhook;
        /// <summary>Signing secret. Returned only on creation: store it securely.</summary>
        public string Secret;
    }

    public class UpdateWebhookInput
    {
        public string Name;
        public string Url;
        public List<string> Events;
        public bool? IsActive;
        public Dictionary<string, string> Headers;
    }

    [Serializable]
    public class UpdateWebhookResult
    {
        public WebhookInfo Webhook;
    }

    [Serializable]
    public class DeleteWebhookResult
    {
        public bool Success;
        public string Message;
    }

    [Serializable]
    public class TestWebhookResult
    {
        /// <summary>Whether the test payload was delivered.</summary>
        public bool Success;
        /// <summary>HTTP status your endpoint answered with.</summary>
        public int? StatusCode;
        public double? LatencyMs;
        public string Error;
    }

    [Serializable]
    public class RegenerateSecretResult
    {
        public string Secret;
    }

    // =========================================================================
    // Pricing
    // =========================================================================

    [Serializable]
    public class PricingPlan
    {
        public string Name;
        public string Price;
        public string Messages;
        public List<string> Features;
    }

    [Serializable]
    public class PricingResult
    {
        public List<PricingPlan> Plans;
    }

    [Serializable]
    public class PricingDetailPlan
    {
        public string Name;
        public string Tier;
        public Dictionary<string, object> Price;
        public Dictionary<string, object> Limits;
        public Dictionary<string, object> Features;
        public List<string> Endpoints;
    }

    [Serializable]
    public class PricingDetailsResult
    {
        public List<PricingDetailPlan> Plans;
    }

    // =========================================================================
    // Usage
    // =========================================================================

    [Serializable]
    public class UsageDay
    {
        public string Date;
        public int TotalRequests;
        public int SuccessRequests;
        public int ErrorRequests;
    }

    [Serializable]
    public class UsageHistoryResult
    {
        public string ApiKeyId;
        public List<UsageDay> Days;
    }

    [Serializable]
    public class UsageByToolResult
    {
        public string Date;
        public Dictionary<string, int> Tools;
        public Dictionary<string, int> Endpoints;
    }

    [Serializable]
    public class UsageMonthlyResult
    {
        public string Tier;
        public string TierDisplayName;
        public Dictionary<string, object> Billing;
        public Dictionary<string, object> UsageInfo;
        public Dictionary<string, object> RateLimit;
        public Dictionary<string, object> Recommendations;
        public Dictionary<string, object> Links;
    }

    // =========================================================================
    // Distress Signals
    // =========================================================================

    /// <summary>
    /// Result of distress-signal detection.
    /// </summary>
    [Serializable]
    public class DistressSignalsResult : DetectionResult
    {
        /// <summary>
        /// How the distress words are used: "literal_present", "about_other", "past_resolved",
        /// "figurative", "quoted" or "none".
        /// </summary>
        public string DistressReading;
        /// <summary>Who the distress is about: "speaker" or "other_party".</summary>
        public string DistressSubject;
        /// <summary>True when a supportive check-in is suggested rather than a moderation case.</summary>
        public bool? SupportSuggested;
    }

    // =========================================================================
    // Nickname Screening
    // =========================================================================

    /// <summary>
    /// Input for nickname (display name) screening.
    /// </summary>
    public class DetectNicknameInput
    {
        /// <summary>The display name as typed (1 to 64 characters).</summary>
        public string Name;
        /// <summary>Default: balanced.</summary>
        public NicknameStrictness? Strictness;
        /// <summary>Two-letter language code.</summary>
        public string Language;
        /// <summary>Age group of the person choosing the name, e.g. "11-13".</summary>
        public string AgeGroup;
        /// <summary>The setting the name is shown in, e.g. "gaming_chat".</summary>
        public string ConversationType;
        /// <summary>Your own staff and support names (max 20), so copies of them are recognised.</summary>
        public List<string> PlatformReserved;
    }

    /// <summary>
    /// How one part of a nickname was read.
    /// </summary>
    [Serializable]
    public class NicknamePart
    {
        public string Text;
        /// <summary>"ordinary", "concerning" or "unclear".</summary>
        public string Reading;
        public string Category;
    }

    /// <summary>
    /// Result of nickname screening. A signal about the name, not a verdict on the person.
    /// </summary>
    [Serializable]
    public class NicknameResult
    {
        /// <summary>"none", "low" or "review".</summary>
        public string Signal;
        public List<string> Categories;
        public List<NicknamePart> Parts;
        public string Rationale;
        /// <summary>"allow", "allow_and_watch", "ask_to_rename" or "hold_for_review".</summary>
        public string Recommendation;
        /// <summary>Free text for display. Do not branch on it.</summary>
        public string RecommendationDetail;
        public double Confidence;
        public string Language;
        public string Strictness;
        /// <summary>
        /// False when nothing could be analysed: the call is not charged and should be retried,
        /// and a "none" signal must not be read as clean.
        /// </summary>
        public bool ReadingComplete;
        public int? CreditsUsed;
    }

    // =========================================================================
    // Image Match
    // =========================================================================

    /// <summary>
    /// Input for verify-image-match.
    /// </summary>
    public class VerifyImageMatchInput
    {
        /// <summary>The image to check (max 15 MB). Sent base64-encoded.</summary>
        public byte[] Image;
        /// <summary>Reference image for the face match. Without it that check is skipped.</summary>
        public byte[] ReferenceImage;
        /// <summary>Checks to run (default: all).</summary>
        public List<ImageMatchCheck> Checks;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    [Serializable]
    public class ImageFaceMatch
    {
        public bool Matched;
        /// <summary>1 minus the face distance. Prefer <see cref="MatchScore"/>.</summary>
        public double? Similarity;
        /// <summary>How strongly the faces match (0-1): 0.5 at the match decision.</summary>
        public double? MatchScore;
        /// <summary>Why matching failed, e.g. "no_face_detected".</summary>
        public string Reason;
    }

    [Serializable]
    public class ImageAgeEstimate
    {
        public double EstimatedAge;
        public double Confidence;
    }

    [Serializable]
    public class ImageForensic
    {
        public bool Nsfw;
        public double? NsfwConfidence;
        /// <summary>"low", "moderate" or "high".</summary>
        public string SkinExposureLevel;
    }

    [Serializable]
    public class ImageSynthetic
    {
        public bool? IsSynthetic;
        public double? SyntheticProbability;
        public bool? HasC2pa;
        public bool? IsAiGenerated;
    }

    /// <summary>
    /// Result of verify-image-match. Each section is null when its check did not run.
    /// </summary>
    [Serializable]
    public class VerifyImageMatchResult
    {
        public string SchemaVersion;
        public List<string> ChecksRun;
        public ImageFaceMatch FaceMatch;
        /// <summary>Null when no face was detected or the check did not run.</summary>
        public ImageAgeEstimate Age;
        public ImageForensic Forensic;
        public ImageSynthetic Synthetic;
        public int? CreditsUsed;
        public double? ProcessingMs;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    // =========================================================================
    // Privacy Redaction
    // =========================================================================

    /// <summary>
    /// Input for redaction. Set either <see cref="Text"/> or <see cref="Messages"/>.
    /// </summary>
    public class RedactInput
    {
        /// <summary>One text to redact.</summary>
        public string Text;
        /// <summary>The messages of one conversation, in order. The same value gets the same placeholder in every message.</summary>
        public List<string> Messages;
        /// <summary>Two-letter language code, when known.</summary>
        public string Language;
        /// <summary>Default: protective.</summary>
        public RedactionMode? Mode;
        /// <summary>
        /// Also removes details such as age and school year. Safety analysis reads these,
        /// so use it only for text shared after analysis.
        /// </summary>
        public bool? StrictAnonymisation;
    }

    /// <summary>
    /// Result of redaction. Nothing is stored by the API.
    /// </summary>
    [Serializable]
    public class RedactResult
    {
        /// <summary>The redacted text, when <see cref="RedactInput.Text"/> was sent.</summary>
        public string Text;
        /// <summary>The redacted messages, when <see cref="RedactInput.Messages"/> was sent.</summary>
        public List<string> Messages;
        /// <summary>Replaced occurrences per type.</summary>
        public Dictionary<string, int> Counts;
        public int EntitiesTotal;
        public string Mode;
        public bool StrictAnonymisation;
        /// <summary>True when full name and place recognition ran.</summary>
        public bool ModelApplied;
        public int? CreditsUsed;
    }

    // =========================================================================
    // Document Analysis
    // =========================================================================

    /// <summary>
    /// Input for document analysis.
    /// </summary>
    public class AnalyzeDocumentInput
    {
        /// <summary>PDF, or a PNG, JPEG or WebP image read as a one-page document.</summary>
        public byte[] File;
        /// <summary>Original filename (e.g. "report.pdf").</summary>
        public string Filename;
        /// <summary>Detection endpoints to run on each page (e.g. "unsafe", "coercive-control").</summary>
        public List<string> Endpoints;
        /// <summary>Your file reference (echoed in the response).</summary>
        public string FileId;
        public string AgeGroup;
        /// <summary>Language hint (ISO 639-1).</summary>
        public string Language;
        /// <summary>Your platform or app name.</summary>
        public string Platform;
        /// <summary>Minimum severity to include crisis helplines: low, medium, high (default) or critical.</summary>
        public string SupportThreshold;
        /// <summary>Images only: split a messaging screenshot into messages by sender.</summary>
        public bool SplitBySender;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Summary of text extraction methods used across the document.
    /// </summary>
    [Serializable]
    public class DocumentExtractionSummary
    {
        public int TextLayerPages;
        public int OcrPages;
        public int FailedPages;
        public double AverageOcrConfidence;
    }

    /// <summary>
    /// Detection result for a single endpoint on a single page.
    /// </summary>
    [Serializable]
    public class DocumentPageEndpointResult
    {
        public string Endpoint;
        public bool Detected;
        public double Severity;
        public double Confidence;
        public double RiskScore;
        public string Level;
        public List<DetectionCategory> Categories;
        public List<DetectionEvidence> Evidence;
        public string RecommendedAction;
        /// <summary><see cref="RecommendedAction"/> as an enum.</summary>
        public ModerationAction Action => ModerationActions.Parse(RecommendedAction);
        public string Rationale;
    }

    /// <summary>
    /// Analysis results for a single page of the document.
    /// </summary>
    [Serializable]
    public class DocumentPageResult
    {
        public int PageNumber;
        public string TextPreview;
        /// <summary>"text_layer" or "ocr".</summary>
        public string ExtractionMethod;
        public double? OcrConfidence;
        public List<DocumentPageEndpointResult> Results;
        public double PageRiskScore;
        public string PageSeverity;
    }

    /// <summary>
    /// A page that was flagged.
    /// </summary>
    [Serializable]
    public class DocumentFlaggedPage
    {
        public int PageNumber;
        public double RiskScore;
        public string Severity;
        public List<string> DetectedEndpoints;
    }

    /// <summary>
    /// Result of document analysis.
    /// </summary>
    [Serializable]
    public class DocumentAnalysisResult
    {
        public string FileId;
        /// <summary>SHA-256 of the uploaded file, for chain of custody.</summary>
        public string DocumentHash;
        public int TotalPages;
        public int PagesAnalyzed;
        public DocumentExtractionSummary ExtractionSummary;
        public List<DocumentPageResult> PageResults;
        public double OverallRiskScore;
        /// <summary>"none", "low", "medium", "high" or "critical".</summary>
        public string OverallSeverity;
        public List<string> DetectedEndpoints;
        public List<DocumentFlaggedPage> FlaggedPages;
        public int? CreditsUsed;
        public double? ProcessingTimeMs;
        public string Language;
        public string LanguageStatus;
        public Dictionary<string, object> Support;
        public string ExternalId;
        public string CustomerId;
        public Dictionary<string, object> Metadata;
    }

    // =========================================================================
    // Verification Sessions
    // =========================================================================

    /// <summary>
    /// Input for creating a verification session.
    /// </summary>
    public class CreateVerificationSessionInput
    {
        public VerificationMode Mode = VerificationMode.Age;
        /// <summary>Preferred document type (a hint for the hosted page).</summary>
        public VerificationDocumentType? DocumentType;
        /// <summary>How a chip read counts as document evidence. The API defaults to optional.</summary>
        public ChipPolicy? ChipPolicy;
        /// <summary>Whether the hosted page takes the document photo itself. The API defaults to true.</summary>
        public bool? AutoCapture;
    }

    /// <summary>
    /// A created verification session. Open <see cref="MobileUrl"/> in a browser or web view.
    /// </summary>
    [Serializable]
    public class VerificationSession
    {
        public string SessionId;
        /// <summary>URL the user opens to complete verification.</summary>
        public string MobileUrl;
        /// <summary>Expiry, in epoch milliseconds.</summary>
        public long ExpiresAt;
        /// <summary>"age" or "identity".</summary>
        public string Mode;
        /// <summary>"optional", "required" or "instead_of_photo".</summary>
        public string ChipPolicy;
        public bool? AutoCapture;
        /// <summary>The check configured for this key: "full" or "liveness_only".</summary>
        public string VerificationMode;
        /// <summary>Camera width, in pixels, the capture step should request.</summary>
        public int? RecommendedImageWidth;
    }

    [Serializable]
    public class VerificationFaceMatch
    {
        public bool Matched;
        /// <summary>1 minus the face distance. Kept for compatibility; prefer <see cref="MatchScore"/>.</summary>
        public double Confidence;
        /// <summary>How strongly the faces match (0-1): 0.5 at the match decision.</summary>
        public double? MatchScore;
    }

    [Serializable]
    public class VerificationLiveness
    {
        public bool Valid;
    }

    [Serializable]
    public class VerificationAgeRange
    {
        public double Low;
        public double High;
    }

    [Serializable]
    public class VerificationNationalId
    {
        public string Value;
        public bool Valid;
        public string Type;
        public string CountryCode;
    }

    /// <summary>
    /// Fields read from the document. Personal data: the extracted fields are released
    /// once, on the first poll after the session finishes.
    /// </summary>
    [Serializable]
    public class VerificationDocument
    {
        public double OcrConfidence;
        public string NameExtracted;
        public string FirstName;
        public string LastName;
        public string DobExtracted;
        public string DocumentNumber;
        public bool? DocumentNumberValid;
        public string Sex;
        public string CountryCode;
        public string CountryName;
        public string DocumentType;
        public string ExpirationDate;
        public bool? Expired;
        public string IssueDate;
        public double? HeightCm;
        public string PlaceOfBirth;
        public string Nationality;
        public string IssuingAuthority;
        public bool? ChipVerified;
        public bool? ChipIssuerTrusted;
        public VerificationNationalId NationalId;
    }

    /// <summary>
    /// The outcome of a finished verification session.
    /// </summary>
    [Serializable]
    public class VerificationResult
    {
        /// <summary>"verified", "failed" or "needs_review".</summary>
        public string Status;
        public double? Age;
        /// <summary>When the age came from the selfie alone: the range the true age likely falls in.</summary>
        public VerificationAgeRange AgeRange;
        public bool? IsMinor;
        /// <summary>Released once.</summary>
        public string DateOfBirth;
        /// <summary>"photo", "chip" or "photo_and_chip".</summary>
        public string DocumentEvidence;
        public VerificationDocument Document;
        public VerificationFaceMatch FaceMatch;
        public VerificationLiveness Liveness;
        /// <summary>Free text, not a closed list. Released once.</summary>
        public List<string> FailureReasons;
    }

    /// <summary>
    /// Status and outcome of a verification session.
    /// </summary>
    [Serializable]
    public class VerificationSessionResult
    {
        public string SessionId;
        public VerificationSessionStatus Status;
        /// <summary>The status as the API sent it, useful when <see cref="Status"/> is Unknown.</summary>
        public string RawStatus;
        /// <summary>Null until there is a result.</summary>
        public VerificationResult Result;
        /// <summary>"full" or "liveness_only", once there is a result.</summary>
        public string VerificationMode;
        public string DocumentEvidence;
        /// <summary>The step the user has reached, for progress UI. Not a closed list.</summary>
        public string StepProgress;
        public string ChipPolicy;
        /// <summary>Epoch milliseconds.</summary>
        public long? CreatedAt;
        /// <summary>Epoch milliseconds.</summary>
        public long? ExpiresAt;
    }

    // =========================================================================
    // Incidents
    // =========================================================================

    /// <summary>
    /// Filters for listing incidents. Every field is optional.
    /// </summary>
    public class ListIncidentsInput
    {
        /// <summary>risk_category, e.g. "bullying" or "grooming".</summary>
        public string Category;
        /// <summary>"low", "moderate" (or "medium"), "high" or "critical".</summary>
        public string Severity;
        /// <summary>"new", "reviewed", "escalated", "resolved" or "dismissed".</summary>
        public string Status;
        /// <summary>"text", "voice", "image", "video" or "video_stream".</summary>
        public string Source;
        /// <summary>ISO 8601 inclusive lower bound on created_at.</summary>
        public string From;
        /// <summary>ISO 8601 exclusive upper bound on created_at.</summary>
        public string To;
        public string Platform;
        public string ExternalId;
        public string CustomerId;
        /// <summary>1 to 100 rows per page (default 25).</summary>
        public int? Limit;
        /// <summary>The previous page's <see cref="ListIncidentsResult.NextCursor"/>.</summary>
        public string Cursor;
        /// <summary>Include the summary text on each row. Costs an extra credit per row.</summary>
        public bool IncludeSummary;
    }

    [Serializable]
    public class IncidentSummary
    {
        public string Id;
        public string RiskCategory;
        public string RiskLevel;
        public double? ConfidenceScore;
        public List<string> DetectedPatterns;
        public List<string> RecommendedActions;
        public string Platform;
        public string Language;
        public string Source;
        public string FileId;
        public string Status;
        public Dictionary<string, object> PolicyAction;
        /// <summary>Advisory next status for a moderator. Never applied automatically.</summary>
        public string RecommendedStatus;
        public string SpeakerStance;
        public bool? IsTest;
        public string ExternalId;
        public string CustomerId;
        public string CreatedAt;
        /// <summary>A string, or an encrypted envelope (dictionary) when the field is end-to-end encrypted.</summary>
        public object Summary;
        /// <summary>Fields you must decrypt with your own private key.</summary>
        public List<string> E2eEnvelopeFields;
    }

    [Serializable]
    public class ListIncidentsResult
    {
        public List<IncidentSummary> Incidents;
        /// <summary>Pass back as <see cref="ListIncidentsInput.Cursor"/>. Null when there are no more pages.</summary>
        public string NextCursor;
        public int TotalReturned;
    }

    [Serializable]
    public class IncidentDetail : IncidentSummary
    {
        public object Metadata;
        public Dictionary<string, object> SourceData;
        public List<string> VisualCategories;
        public string VisualSeverity;
        public double? VisualConfidence;
        public bool? ContainsText;
        public bool? ContainsFaces;
        public List<string> EmotionalIndicators;
        public Dictionary<string, object> Review;
        public List<MessageAnalysis> MessageAnalysis;
    }

    [Serializable]
    public class IncidentPlatformCount
    {
        public string Platform;
        public int Count;
    }

    [Serializable]
    public class IncidentsOverview
    {
        public string From;
        public string To;
        public int TotalIncidents;
        /// <summary>High or critical incidents nobody has acted on yet.</summary>
        public int RequiresReviewCount;
        public int Last24hCount;
        public int Last7dCount;
        public int Last30dCount;
        public Dictionary<string, int> CountsByCategory;
        public Dictionary<string, int> CountsBySeverity;
        public Dictionary<string, int> CountsBySource;
        public Dictionary<string, int> CountsByStatus;
        public List<IncidentPlatformCount> TopPlatforms;
    }

    [Serializable]
    public class IncidentTrendsBucket
    {
        public string BucketStart;
        public int Total;
        public Dictionary<string, int> BySeverity;
    }

    [Serializable]
    public class IncidentTrends
    {
        public string BucketSize;
        public string From;
        public string To;
        public List<IncidentTrendsBucket> Series;
    }

    /// <summary>
    /// A moderator's review of one incident.
    /// </summary>
    public class ReviewIncidentInput
    {
        public ModeratorAction Action;
        public ModeratorReasonCode ReasonCode;
        public string ReasonComment;
        /// <summary>Required for downgrade and escalate: "low", "moderate", "high" or "critical".</summary>
        public string NewRiskLevel;
        /// <summary>Required for reclassify.</summary>
        public string NewRiskCategory;
        /// <summary>Stable identifier of the moderator making the decision (required).</summary>
        public string ModeratorExternalId;
        /// <summary>Retention class of the audit receipt, e.g. "limited-risk" (the default).</summary>
        public string RetentionClass;
    }

    /// <summary>
    /// The same review applied to up to 100 incidents.
    /// </summary>
    public class BatchReviewIncidentsInput : ReviewIncidentInput
    {
        public List<string> IncidentIds;
    }

    [Serializable]
    public class IncidentClassification
    {
        public string RiskCategory;
        public string RiskLevel;
    }

    [Serializable]
    public class IncidentAuditReceipt
    {
        public string RequestId;
        public string Timestamp;
        public string Signature;
    }

    [Serializable]
    public class ReviewIncidentResult
    {
        public string IncidentId;
        public IncidentClassification Original;
        public IncidentClassification Revised;
        /// <summary>Null when the receipt could not be written.</summary>
        public IncidentAuditReceipt AuditReceipt;
    }

    [Serializable]
    public class BatchReviewItemResult : ReviewIncidentResult
    {
        public bool Ok;
        /// <summary>Set when <see cref="Ok"/> is false.</summary>
        public string Error;
    }

    [Serializable]
    public class BatchReviewIncidentsResult
    {
        public int Total;
        public int Succeeded;
        public int Failed;
        /// <summary>Per-incident outcome, in input order.</summary>
        public List<BatchReviewItemResult> Results;
    }
}
