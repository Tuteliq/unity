using System.Collections.Generic;

namespace Tuteliq
{
    /// <summary>
    /// Reads and combines the <c>recommended_action</c> values the API returns.
    /// </summary>
    /// <example>
    /// <code>
    /// var action = ModerationActions.Parse(result.RecommendedAction);
    /// if (ModerationActions.ShouldWithhold(action)) { /* do not deliver */ }
    /// else if (ModerationActions.NeedsReview(action)) { /* send to your moderation queue */ }
    /// </code>
    /// </example>
    public static class ModerationActions
    {
        /// <summary>
        /// Maps an API value to <see cref="ModerationAction"/>. A missing value is
        /// <see cref="ModerationAction.None"/>. A value this SDK version does not know
        /// (from a newer API) is <see cref="ModerationAction.FlagForReview"/>, so an
        /// unfamiliar verdict reaches a person instead of being read as safe.
        /// </summary>
        public static ModerationAction Parse(string value)
        {
            if (string.IsNullOrEmpty(value)) return ModerationAction.None;
            switch (value)
            {
                case "none":
                case "no_action":
                    return ModerationAction.None;
                case "monitor":
                    return ModerationAction.Monitor;
                case "flag_for_review":
                case "flag_for_moderator":
                    return ModerationAction.FlagForReview;
                case "block":
                    return ModerationAction.Block;
                case "immediate_intervention":
                    return ModerationAction.ImmediateIntervention;
                default:
                    return ModerationAction.FlagForReview;
            }
        }

        /// <summary>
        /// The most severe of the given API values. Missing values are skipped.
        /// </summary>
        public static ModerationAction Strongest(IEnumerable<string> values)
        {
            var strongest = ModerationAction.None;
            foreach (var value in values)
            {
                var action = Parse(value);
                if (action > strongest) strongest = action;
            }
            return strongest;
        }

        /// <summary>
        /// True when a person should look at it: <see cref="ModerationAction.FlagForReview"/> or stronger.
        /// </summary>
        public static bool NeedsReview(ModerationAction action)
        {
            return action >= ModerationAction.FlagForReview;
        }

        /// <summary>
        /// True when the content should not be delivered: <see cref="ModerationAction.Block"/> or stronger.
        /// </summary>
        public static bool ShouldWithhold(ModerationAction action)
        {
            return action >= ModerationAction.Block;
        }
    }
}
