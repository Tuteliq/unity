using System;
using System.Collections.Generic;

namespace Tuteliq
{
    /// <summary>
    /// Combines the bullying and unsafe results of <see cref="TuteliqClient.AnalyzeAsync"/>.
    /// </summary>
    internal static class AnalyzeResultBuilder
    {
        public static AnalyzeResult Build(BullyingResult bullying, UnsafeResult unsafeResult)
        {
            var riskScore = Math.Max(bullying?.RiskScore ?? 0f, unsafeResult?.RiskScore ?? 0f);
            var action = StrongestAction(bullying, unsafeResult);
            return new AnalyzeResult
            {
                RiskLevel = RiskLevelFor(riskScore),
                RiskScore = riskScore,
                Summary = Summarize(bullying, unsafeResult, action),
                RecommendedAction = action.ToApiString(),
                Bullying = bullying,
                Unsafe = unsafeResult,
                CreditsUsed = TotalCredits(bullying, unsafeResult)
            };
        }

        private static ModerationAction StrongestAction(BullyingResult bullying, UnsafeResult unsafeResult)
        {
            var actions = new List<string>();
            if (bullying != null) actions.Add(bullying.RecommendedAction);
            if (unsafeResult != null) actions.Add(unsafeResult.RecommendedAction);
            return ModerationActions.Strongest(actions);
        }

        // Display band for the score only. Moderation decisions follow RecommendedAction.
        private static RiskLevel RiskLevelFor(float riskScore)
        {
            if (riskScore >= 0.9f) return RiskLevel.Critical;
            if (riskScore >= 0.7f) return RiskLevel.High;
            if (riskScore >= 0.5f) return RiskLevel.Medium;
            if (riskScore >= 0.3f) return RiskLevel.Low;
            return RiskLevel.Safe;
        }

        private static string Summarize(BullyingResult bullying, UnsafeResult unsafeResult, ModerationAction action)
        {
            var findings = new List<string>();
            if (bullying?.IsBullying == true)
                findings.Add($"Bullying detected ({bullying.Severity.ToApiString()})");
            if (bullying?.BullyingReported == true)
                findings.Add(bullying.BullyingReportedBy != null
                    ? $"Bullying reported by {bullying.BullyingReportedBy}"
                    : "Bullying reported");
            if (unsafeResult?.Unsafe == true)
                findings.Add($"Unsafe content: {string.Join(", ", unsafeResult.Categories)}");
            if (findings.Count > 0) return string.Join(". ", findings);
            return ModerationActions.NeedsReview(action) ? "Needs review." : "No safety concerns detected.";
        }

        private static int? TotalCredits(BullyingResult bullying, UnsafeResult unsafeResult)
        {
            if (bullying?.CreditsUsed == null && unsafeResult?.CreditsUsed == null) return null;
            return (bullying?.CreditsUsed ?? 0) + (unsafeResult?.CreditsUsed ?? 0);
        }
    }
}
