namespace Tuteliq
{
    /// <summary>
    /// Severity level for detected issues.
    /// </summary>
    public enum Severity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Risk level for grooming detection.
    /// </summary>
    public enum GroomingRisk
    {
        None,
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Overall risk level for content analysis.
    /// </summary>
    public enum RiskLevel
    {
        Safe,
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Emotion trend direction.
    /// </summary>
    public enum EmotionTrend
    {
        Improving,
        Stable,
        Worsening
    }

    /// <summary>
    /// Target audience for action plans.
    /// </summary>
    public enum Audience
    {
        Child,
        Parent,
        Educator,
        Platform
    }

    /// <summary>
    /// Role of message sender in grooming detection.
    /// </summary>
    public enum MessageRole
    {
        Adult,
        Child,
        Unknown
    }

    /// <summary>
    /// Supported analysis languages (ISO 639-1 codes).
    /// </summary>
    public enum Language
    {
        En,
        Es,
        Pt,
        Uk,
        Sv,
        No,
        Da,
        Fi,
        De,
        Fr,
        Nl,
        Pl,
        It,
        Tr,
        Ro,
        El,
        Cs,
        Hu,
        Bg,
        Hr,
        Sk,
        Lt,
        Lv,
        Et,
        Sl,
        Mt,
        Ga,
        Zh,
        Ja,
        Ko,
        Ar,
        Ru
    }

    /// <summary>
    /// Language support status.
    /// </summary>
    public enum LanguageStatus
    {
        Stable,
        Beta
    }

    /// <summary>
    /// Detection endpoint type.
    /// </summary>
    public enum Detection
    {
        Bullying,
        Grooming,
        Unsafe,
        SocialEngineering,
        AppFraud,
        RomanceScam,
        MuleRecruitment,
        GamblingHarm,
        CoerciveControl,
        VulnerabilityExploitation,
        Radicalisation
    }

    /// <summary>
    /// Subscription tier.
    /// </summary>
    public enum Tier
    {
        Starter,
        Indie,
        Pro,
        Business,
        Enterprise
    }

    /// <summary>
    /// The action Tuteliq recommends, ordered weakest to strongest.
    /// Branch moderation decisions on this, not on a risk score or a boolean.
    /// </summary>
    public enum ModerationAction
    {
        /// <summary>No signal.</summary>
        None = 0,
        /// <summary>Log it; no person needs to look.</summary>
        Monitor = 1,
        /// <summary>Queue for a moderator.</summary>
        FlagForReview = 2,
        /// <summary>Withhold or limit the content.</summary>
        Block = 3,
        /// <summary>Crisis path: act now.</summary>
        ImmediateIntervention = 4
    }

    /// <summary>
    /// Type of verification a session performs.
    /// </summary>
    public enum VerificationMode
    {
        Age,
        Identity
    }

    /// <summary>
    /// Document type hint for a verification session.
    /// </summary>
    public enum VerificationDocumentType
    {
        Passport,
        NationalId,
        DriversLicense
    }

    /// <summary>
    /// How a passport or ID card chip read counts as document evidence.
    /// </summary>
    public enum ChipPolicy
    {
        /// <summary>The document photo is required and a chip read adds to it (API default).</summary>
        Optional,
        /// <summary>The photo and a valid chip read are both required.</summary>
        Required,
        /// <summary>A chip read that proves itself genuine replaces the photo.</summary>
        InsteadOfPhoto
    }

    /// <summary>
    /// Status of a verification session.
    /// </summary>
    public enum VerificationSessionStatus
    {
        /// <summary>A status this SDK version does not know. Treat it as not finished.</summary>
        Unknown,
        Pending,
        InProgress,
        Completed,
        Failed,
        /// <summary>Expired, or cancelled.</summary>
        Expired,
        /// <summary>Waiting for the user to add a document photo. Not final.</summary>
        DocumentRequired
    }

    /// <summary>
    /// A moderator's decision on an incident.
    /// </summary>
    public enum ModeratorAction
    {
        Confirm,
        Downgrade,
        Escalate,
        Reclassify,
        Dismiss,
        Resolve,
        Reopen
    }

    /// <summary>
    /// Why a moderator made a decision on an incident.
    /// </summary>
    public enum ModeratorReasonCode
    {
        ConfirmedAccurate,
        FalsePositive,
        OutOfContext,
        InsufficientSeverity,
        IncorrectCategory,
        RequiresLawEnforcement,
        ParentNotified,
        Other
    }

    /// <summary>
    /// How readily a doubtful reading of a nickname is acted on.
    /// </summary>
    public enum NicknameStrictness
    {
        Cautious,
        Balanced,
        Strict
    }

    /// <summary>
    /// Redaction mode for the privacy endpoint.
    /// </summary>
    public enum RedactionMode
    {
        /// <summary>Also removes near misses, usually a mistyped real number (API default).</summary>
        Protective,
        /// <summary>Removes only values that match their format exactly.</summary>
        Precise
    }

    /// <summary>
    /// Checks that verify-image-match can run.
    /// </summary>
    public enum ImageMatchCheck
    {
        FaceMatch,
        Age,
        Forensic,
        Synthetic
    }

    /// <summary>
    /// Bucket size for incident trends.
    /// </summary>
    public enum IncidentTrendBucket
    {
        Hour,
        Day,
        Week
    }

    internal static class EnumExtensions
    {
        public static string ToApiString(this ModerationAction action) => ToSnakeCase(action.ToString());
        public static string ToApiString(this VerificationMode mode) => ToSnakeCase(mode.ToString());
        public static string ToApiString(this VerificationDocumentType type) => ToSnakeCase(type.ToString());
        public static string ToApiString(this ChipPolicy policy) => ToSnakeCase(policy.ToString());
        public static string ToApiString(this ModeratorAction action) => ToSnakeCase(action.ToString());
        public static string ToApiString(this ModeratorReasonCode code) => ToSnakeCase(code.ToString());
        public static string ToApiString(this NicknameStrictness strictness) => ToSnakeCase(strictness.ToString());
        public static string ToApiString(this RedactionMode mode) => ToSnakeCase(mode.ToString());
        public static string ToApiString(this ImageMatchCheck check) => ToSnakeCase(check.ToString());
        public static string ToApiString(this IncidentTrendBucket bucket) => ToSnakeCase(bucket.ToString());

        public static VerificationSessionStatus ParseVerificationSessionStatus(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "pending" => VerificationSessionStatus.Pending,
                "in_progress" => VerificationSessionStatus.InProgress,
                "completed" => VerificationSessionStatus.Completed,
                "failed" => VerificationSessionStatus.Failed,
                "expired" => VerificationSessionStatus.Expired,
                "document_required" => VerificationSessionStatus.DocumentRequired,
                _ => VerificationSessionStatus.Unknown
            };
        }

        /// <summary>
        /// "FlagForReview" to "flag_for_review".
        /// </summary>
        internal static string ToSnakeCase(string pascal)
        {
            var sb = new System.Text.StringBuilder(pascal.Length + 4);
            for (int i = 0; i < pascal.Length; i++)
            {
                var c = pascal[i];
                if (char.IsUpper(c))
                {
                    if (i > 0) sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string ToApiString(this Severity severity)
        {
            return severity.ToString().ToLowerInvariant();
        }

        public static string ToApiString(this GroomingRisk risk)
        {
            return risk.ToString().ToLowerInvariant();
        }

        public static string ToApiString(this RiskLevel level)
        {
            return level.ToString().ToLowerInvariant();
        }

        public static string ToApiString(this EmotionTrend trend)
        {
            return trend.ToString().ToLowerInvariant();
        }

        public static string ToApiString(this Audience audience)
        {
            return audience.ToString().ToLowerInvariant();
        }

        public static string ToApiString(this MessageRole role)
        {
            return role.ToString().ToLowerInvariant();
        }

        public static string ToApiString(this Language language)
        {
            return language.ToString().ToLowerInvariant();
        }

        public static string ToApiString(this LanguageStatus status)
        {
            return status.ToString().ToLowerInvariant();
        }

        public static string ToApiString(this Detection d)
        {
            return d switch
            {
                Detection.SocialEngineering => "social-engineering",
                Detection.AppFraud => "app-fraud",
                Detection.RomanceScam => "romance-scam",
                Detection.MuleRecruitment => "mule-recruitment",
                Detection.GamblingHarm => "gambling-harm",
                Detection.CoerciveControl => "coercive-control",
                Detection.VulnerabilityExploitation => "vulnerability-exploitation",
                Detection.Radicalisation => "radicalisation",
                _ => d.ToString().ToLowerInvariant()
            };
        }

        public static string ToApiString(this Tier tier)
        {
            return tier.ToString().ToLowerInvariant();
        }

        public static Severity ParseSeverity(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "low" => Severity.Low,
                "medium" => Severity.Medium,
                "high" => Severity.High,
                "critical" => Severity.Critical,
                _ => Severity.Low
            };
        }

        public static GroomingRisk ParseGroomingRisk(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "none" => GroomingRisk.None,
                "low" => GroomingRisk.Low,
                "medium" => GroomingRisk.Medium,
                "high" => GroomingRisk.High,
                "critical" => GroomingRisk.Critical,
                _ => GroomingRisk.None
            };
        }

        public static RiskLevel ParseRiskLevel(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "safe" => RiskLevel.Safe,
                "low" => RiskLevel.Low,
                "medium" => RiskLevel.Medium,
                "high" => RiskLevel.High,
                "critical" => RiskLevel.Critical,
                _ => RiskLevel.Safe
            };
        }

        public static EmotionTrend ParseEmotionTrend(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "improving" => EmotionTrend.Improving,
                "stable" => EmotionTrend.Stable,
                "worsening" => EmotionTrend.Worsening,
                _ => EmotionTrend.Stable
            };
        }

        /// <summary>
        /// Parses a language code. Returns null for a code the SDK does not know,
        /// rather than assuming English.
        /// </summary>
        public static Language? ParseLanguage(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "en" => Language.En,
                "es" => Language.Es,
                "pt" => Language.Pt,
                "uk" => Language.Uk,
                "sv" => Language.Sv,
                "no" => Language.No,
                "da" => Language.Da,
                "fi" => Language.Fi,
                "de" => Language.De,
                "fr" => Language.Fr,
                "nl" => Language.Nl,
                "pl" => Language.Pl,
                "it" => Language.It,
                "tr" => Language.Tr,
                "ro" => Language.Ro,
                "el" => Language.El,
                "cs" => Language.Cs,
                "hu" => Language.Hu,
                "bg" => Language.Bg,
                "hr" => Language.Hr,
                "sk" => Language.Sk,
                "lt" => Language.Lt,
                "lv" => Language.Lv,
                "et" => Language.Et,
                "sl" => Language.Sl,
                "mt" => Language.Mt,
                "ga" => Language.Ga,
                "zh" => Language.Zh,
                "ja" => Language.Ja,
                "ko" => Language.Ko,
                "ar" => Language.Ar,
                "ru" => Language.Ru,
                _ => (Language?)null
            };
        }

        public static LanguageStatus ParseLanguageStatus(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "stable" => LanguageStatus.Stable,
                "beta" => LanguageStatus.Beta,
                _ => LanguageStatus.Stable
            };
        }

        public static Detection ParseDetection(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "bullying" => Detection.Bullying,
                "grooming" => Detection.Grooming,
                "unsafe" => Detection.Unsafe,
                "social-engineering" => Detection.SocialEngineering,
                "app-fraud" => Detection.AppFraud,
                "romance-scam" => Detection.RomanceScam,
                "mule-recruitment" => Detection.MuleRecruitment,
                "gambling-harm" => Detection.GamblingHarm,
                "coercive-control" => Detection.CoerciveControl,
                "vulnerability-exploitation" => Detection.VulnerabilityExploitation,
                "radicalisation" => Detection.Radicalisation,
                _ => Detection.Bullying
            };
        }

        public static Tier ParseTier(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "starter" => Tier.Starter,
                "indie" => Tier.Indie,
                "pro" => Tier.Pro,
                "business" => Tier.Business,
                "enterprise" => Tier.Enterprise,
                _ => Tier.Starter
            };
        }
    }
}
