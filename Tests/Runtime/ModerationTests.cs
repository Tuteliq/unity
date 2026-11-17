using System.Collections.Generic;
using NUnit.Framework;

namespace Tuteliq.Tests
{
    public class ModerationActionTests
    {
        [TestCase("none", ModerationAction.None)]
        [TestCase("monitor", ModerationAction.Monitor)]
        [TestCase("flag_for_review", ModerationAction.FlagForReview)]
        [TestCase("block", ModerationAction.Block)]
        [TestCase("immediate_intervention", ModerationAction.ImmediateIntervention)]
        [TestCase("flag_for_moderator", ModerationAction.FlagForReview)]
        [TestCase("no_action", ModerationAction.None)]
        public void Parse_KnownValues(string value, ModerationAction expected)
        {
            Assert.AreEqual(expected, ModerationActions.Parse(value));
        }

        [Test]
        public void Parse_UnknownValue_GoesToReviewNotSafe()
        {
            Assert.AreEqual(ModerationAction.FlagForReview, ModerationActions.Parse("quarantine"));
        }

        [Test]
        public void Parse_Missing_IsNone()
        {
            Assert.AreEqual(ModerationAction.None, ModerationActions.Parse(null));
        }

        [Test]
        public void Strongest_TakesTheMostSevere()
        {
            Assert.AreEqual(ModerationAction.Block,
                ModerationActions.Strongest(new[] { "monitor", "block", "flag_for_review", null }));
        }

        [Test]
        public void ToApiString_IsSnakeCase()
        {
            Assert.AreEqual("flag_for_review", ModerationAction.FlagForReview.ToApiString());
            Assert.AreEqual("immediate_intervention", ModerationAction.ImmediateIntervention.ToApiString());
        }

        [Test]
        public void NeedsReview_AndShouldWithhold_FollowTheOrder()
        {
            Assert.IsFalse(ModerationActions.NeedsReview(ModerationAction.Monitor));
            Assert.IsTrue(ModerationActions.NeedsReview(ModerationAction.FlagForReview));
            Assert.IsFalse(ModerationActions.ShouldWithhold(ModerationAction.FlagForReview));
            Assert.IsTrue(ModerationActions.ShouldWithhold(ModerationAction.Block));
        }
    }

    public class AnalyzeTests
    {
        private static string Bullying(string action, bool isBullying = false, string extra = "") =>
            $"{{\"is_bullying\":{(isBullying ? "true" : "false")},\"severity\":\"medium\",\"risk_score\":0.4,\"recommended_action\":\"{action}\"{extra}}}";

        private static string Unsafe(string action) =>
            $"{{\"unsafe\":false,\"severity\":\"low\",\"risk_score\":0.1,\"recommended_action\":\"{action}\"}}";

        private static AnalyzeResult Analyze(string bullyingBody, string unsafeBody)
        {
            var transport = new FakeTransport().Respond(200, bullyingBody).Respond(200, unsafeBody);
            return TestClient.Run(TestClient.Create(transport).AnalyzeAsync("message"));
        }

        [Test]
        public void AnalyzeAsync_FlagForReview_IsNotDowngradedToNone()
        {
            var result = Analyze(Bullying("flag_for_review", isBullying: true), Unsafe("none"));

            Assert.AreEqual("flag_for_review", result.RecommendedAction);
            Assert.AreEqual(ModerationAction.FlagForReview, result.Action);
        }

        [Test]
        public void AnalyzeAsync_Block_IsNotDowngradedToNone()
        {
            var result = Analyze(Bullying("monitor"), Unsafe("block"));

            Assert.AreEqual("block", result.RecommendedAction);
        }

        [Test]
        public void AnalyzeAsync_TakesTheMostSevereAction()
        {
            var result = Analyze(Bullying("immediate_intervention", isBullying: true), Unsafe("block"));

            Assert.AreEqual(ModerationAction.ImmediateIntervention, result.Action);
        }

        [Test]
        public void AnalyzeAsync_ReportedBullying_StillNeedsAttention()
        {
            var reported = Bullying("flag_for_review", isBullying: false,
                extra: ",\"bullying_reported\":true,\"bullying_reported_by\":\"target\",\"speaker_role\":\"experiencing\"");

            var result = Analyze(reported, Unsafe("none"));

            Assert.IsFalse(result.Bullying.IsBullying);
            Assert.IsTrue(result.Bullying.BullyingReported);
            Assert.AreEqual("target", result.Bullying.BullyingReportedBy);
            Assert.AreEqual("experiencing", result.Bullying.SpeakerRole);
            Assert.IsTrue(ModerationActions.NeedsReview(result.Action));
            StringAssert.Contains("reported", result.Summary);
        }

        [Test]
        public void AnalyzeAsync_NothingFound_IsNoneAndSafeSummary()
        {
            var result = Analyze(Bullying("none"), Unsafe("none"));

            Assert.AreEqual(ModerationAction.None, result.Action);
            Assert.AreEqual("No safety concerns detected.", result.Summary);
        }

        [Test]
        public void AnalyzeAsync_OnlyRequestedChecksRun()
        {
            var transport = new FakeTransport().Respond(200, Unsafe("monitor"));
            var result = TestClient.Run(TestClient.Create(transport)
                .AnalyzeAsync("message", include: new List<string> { "unsafe" }));

            Assert.AreEqual(1, transport.Requests.Count);
            StringAssert.EndsWith("/api/v1/safety/unsafe", transport.Last.Url);
            Assert.IsNull(result.Bullying);
            Assert.AreEqual(ModerationAction.Monitor, result.Action);
        }
    }
}
