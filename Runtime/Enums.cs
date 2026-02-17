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
    /// Supported analysis languages.
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
        Fr
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

    internal static class EnumExtensions
    {
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

        public static Language ParseLanguage(string value)
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
                _ => Language.En
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
