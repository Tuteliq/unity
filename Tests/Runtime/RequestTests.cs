using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace Tuteliq.Tests
{
    public class WebhookRequestTests
    {
        [Test]
        public void CreateWebhook_SendsNameAndNoActiveFlag()
        {
            var transport = new FakeTransport().Respond(201,
                "{\"id\":\"wh_1\",\"name\":\"Alerts\",\"url\":\"https://example.com/hook\",\"secret\":\"whsec_test\",\"events\":[\"incident.created\"],\"is_active\":true,\"created_at\":\"2026-01-01T00:00:00Z\"}");

            var result = TestClient.Run(TestClient.Create(transport).CreateWebhookAsync(new CreateWebhookInput
            {
                Name = "Alerts",
                Url = "https://example.com/hook",
                Events = new List<string> { "incident.critical" }
            }));

            Assert.AreEqual("POST", transport.Last.Method);
            StringAssert.EndsWith("/api/v1/webhooks", transport.Last.Url);
            var body = transport.LastBody;
            Assert.AreEqual("Alerts", body["name"]);
            Assert.IsFalse(body.ContainsKey("active"));
            Assert.AreEqual("wh_1", result.Webhook.Id);
            Assert.IsTrue(result.Webhook.IsActive);
            Assert.AreEqual("whsec_test", result.Secret);
        }

        [Test]
        public void CreateWebhook_WithoutName_IsRejectedLocally()
        {
            var transport = new FakeTransport();
            Assert.Throws<ValidationException>(() => TestClient.Run(TestClient.Create(transport)
                .CreateWebhookAsync(new CreateWebhookInput { Url = "https://example.com/hook" })));
            Assert.AreEqual(0, transport.Requests.Count);
        }

        [Test]
        public void UpdateWebhook_UsesPutAndIsActive()
        {
            var transport = new FakeTransport().Respond(200, "{\"id\":\"wh_1\",\"is_active\":false}");

            var result = TestClient.Run(TestClient.Create(transport)
                .UpdateWebhookAsync("wh_1", new UpdateWebhookInput { IsActive = false }));

            Assert.AreEqual("PUT", transport.Last.Method);
            StringAssert.EndsWith("/api/v1/webhooks/wh_1", transport.Last.Url);
            Assert.AreEqual(false, transport.LastBody["is_active"]);
            Assert.IsFalse(transport.LastBody.ContainsKey("active"));
            Assert.IsFalse(result.Webhook.IsActive);
        }

        [Test]
        public void TestWebhook_PostsWebhookIdToTheTestRoute()
        {
            var transport = new FakeTransport().Respond(200, "{\"success\":true,\"status_code\":200,\"latency_ms\":41}");

            var result = TestClient.Run(TestClient.Create(transport).TestWebhookAsync("wh_1"));

            Assert.AreEqual("POST", transport.Last.Method);
            StringAssert.EndsWith("/api/v1/webhooks/test", transport.Last.Url);
            Assert.AreEqual("wh_1", transport.LastBody["webhook_id"]);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual(41, result.LatencyMs);
        }

        [Test]
        public void RegenerateSecret_UsesRegenerateSecretRoute()
        {
            var transport = new FakeTransport().Respond(200, "{\"secret\":\"whsec_new\"}");

            var result = TestClient.Run(TestClient.Create(transport).RegenerateWebhookSecretAsync("wh_1"));

            Assert.AreEqual("POST", transport.Last.Method);
            StringAssert.EndsWith("/api/v1/webhooks/wh_1/regenerate-secret", transport.Last.Url);
            Assert.AreEqual("whsec_new", result.Secret);
        }

        [Test]
        public void ListWebhooks_ReadsIsActive()
        {
            var transport = new FakeTransport().Respond(200,
                "{\"webhooks\":[{\"id\":\"wh_1\",\"name\":\"Alerts\",\"is_active\":true,\"failure_count\":2}]}");

            var result = TestClient.Run(TestClient.Create(transport).ListWebhooksAsync());

            Assert.IsTrue(result.Webhooks[0].IsActive);
            Assert.AreEqual(2, result.Webhooks[0].FailureCount);
            Assert.AreEqual("Alerts", result.Webhooks[0].Name);
        }

        [Test]
        public void UsageByTool_UsesByToolRoute()
        {
            var transport = new FakeTransport().Respond(200, "{\"date\":\"2026-01-01\",\"tools\":{\"bullying\":3}}");

            var result = TestClient.Run(TestClient.Create(transport).GetUsageByToolAsync("2026-01-01"));

            StringAssert.EndsWith("/api/v1/usage/by-tool?date=2026-01-01", transport.Last.Url);
            Assert.AreEqual(3, result.Tools["bullying"]);
        }
    }

    public class DetectionRequestTests
    {
        private CultureInfo _original;

        [SetUp]
        public void SaveCulture() => _original = Thread.CurrentThread.CurrentCulture;

        [TearDown]
        public void RestoreCulture() => Thread.CurrentThread.CurrentCulture = _original;

        [Test]
        public void Request_SendsBearerKey()
        {
            var transport = new FakeTransport();
            TestClient.Run(TestClient.Create(transport).DetectUnsafeAsync("text"));

            Assert.AreEqual($"Bearer {TestClient.ApiKey}", transport.Last.Headers["Authorization"]);
        }

        [Test]
        public void Request_UnderGermanLocale_SendsValidJson()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            var transport = new FakeTransport();

            TestClient.Run(TestClient.Create(transport).DetectBullyingAsync("text",
                metadata: new Dictionary<string, object> { { "score", 0.5 } }));

            StringAssert.Contains("\"score\":0.5", transport.Last.JsonBody);
            Assert.AreEqual(0.5, ((Dictionary<string, object>)transport.LastBody["metadata"])["score"]);
        }

        [Test]
        public void Response_UnderGermanLocale_ParsesScores()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            var transport = new FakeTransport().Respond(200, "{\"is_bullying\":true,\"risk_score\":0.75,\"recommended_action\":\"block\"}");

            var result = TestClient.Run(TestClient.Create(transport).DetectBullyingAsync("text"));

            Assert.AreEqual(0.75f, result.RiskScore, 1e-6);
            Assert.AreEqual(ModerationAction.Block, result.Action);
        }

        [Test]
        public void DetectGrooming_SendsSenderAgeAndChildAge()
        {
            var transport = new FakeTransport();

            TestClient.Run(TestClient.Create(transport).DetectGroomingAsync(new DetectGroomingInput
            {
                Messages = new List<GroomingMessage>
                {
                    new GroomingMessage(MessageRole.Adult, "hello", senderAge: 34),
                    new GroomingMessage(MessageRole.Child, "hi")
                },
                ChildAge = 12,
                ParticipantAge = 34
            }));

            var messages = (List<object>)transport.LastBody["messages"];
            var first = (Dictionary<string, object>)messages[0];
            var second = (Dictionary<string, object>)messages[1];
            Assert.AreEqual(34L, first["sender_age"]);
            Assert.AreEqual("adult", first["sender_role"]);
            Assert.IsFalse(second.ContainsKey("sender_age"));
            var context = (Dictionary<string, object>)transport.LastBody["context"];
            Assert.AreEqual(12L, context["child_age"]);
            Assert.AreEqual(34L, context["participant_age"]);
        }

        [Test]
        public void DetectGrooming_ParsesMessageAnalysis()
        {
            var transport = new FakeTransport().Respond(200,
                "{\"grooming_risk\":\"high\",\"recommended_action\":\"flag_for_review\",\"message_analysis\":[{\"message_index\":0,\"risk_score\":0.8,\"flags\":[\"secrecy\"]}]}");

            var result = TestClient.Run(TestClient.Create(transport).DetectGroomingAsync(new DetectGroomingInput
            {
                Messages = new List<GroomingMessage> { new GroomingMessage(MessageRole.Adult, "hello") }
            }));

            Assert.AreEqual(ModerationAction.FlagForReview, result.Action);
            Assert.AreEqual(1, result.MessageAnalysis.Count);
            Assert.AreEqual("secrecy", result.MessageAnalysis[0].Flags[0]);
        }

        [Test]
        public void DetectDistressSignals_PostsToItsRouteAndParsesExtraFields()
        {
            var transport = new FakeTransport().Respond(200,
                "{\"detected\":true,\"recommended_action\":\"monitor\",\"distress_reading\":\"figurative\",\"support_suggested\":true}");

            var result = TestClient.Run(TestClient.Create(transport)
                .DetectDistressSignalsAsync(new DetectionInput { Content = "text" }));

            StringAssert.EndsWith("/api/v1/safety/distress-signals", transport.Last.Url);
            Assert.AreEqual("text", transport.LastBody["text"]);
            Assert.AreEqual("figurative", result.DistressReading);
            Assert.AreEqual(true, result.SupportSuggested);
            Assert.AreEqual(ModerationAction.Monitor, result.Action);
        }

        [Test]
        public void DetectNickname_SendsNameStrictnessAndContext()
        {
            var transport = new FakeTransport().Respond(200,
                "{\"signal\":\"review\",\"recommendation\":\"hold_for_review\",\"reading_complete\":true,\"parts\":[{\"text\":\"x\",\"reading\":\"concerning\",\"category\":null}]}");

            var result = TestClient.Run(TestClient.Create(transport).DetectNicknameAsync(new DetectNicknameInput
            {
                Name = "PlayerOne",
                Strictness = NicknameStrictness.Strict,
                AgeGroup = "11-13",
                PlatformReserved = new List<string> { "Support" }
            }));

            StringAssert.EndsWith("/api/v1/safety/nickname", transport.Last.Url);
            Assert.AreEqual("PlayerOne", transport.LastBody["name"]);
            Assert.AreEqual("strict", transport.LastBody["strictness"]);
            var context = (Dictionary<string, object>)transport.LastBody["context"];
            Assert.AreEqual("11-13", context["age_group"]);
            Assert.AreEqual("hold_for_review", result.Recommendation);
            Assert.IsTrue(result.ReadingComplete);
            Assert.AreEqual("concerning", result.Parts[0].Reading);
        }

        [Test]
        public void DetectNickname_TooLong_IsRejectedLocally()
        {
            var transport = new FakeTransport();
            Assert.Throws<ValidationException>(() => TestClient.Run(TestClient.Create(transport)
                .DetectNicknameAsync(new DetectNicknameInput { Name = new string('a', 65) })));
            Assert.AreEqual(0, transport.Requests.Count);
        }

        [Test]
        public void VerifyImageMatch_SendsBase64AndCheckNames()
        {
            var transport = new FakeTransport().Respond(200,
                "{\"checks_run\":[\"face_match\"],\"face_match\":{\"matched\":true,\"match_score\":0.9},\"age\":null}");

            var result = TestClient.Run(TestClient.Create(transport).VerifyImageMatchAsync(new VerifyImageMatchInput
            {
                Image = new byte[] { 1, 2, 3 },
                ReferenceImage = new byte[] { 4, 5, 6 },
                Checks = new List<ImageMatchCheck> { ImageMatchCheck.FaceMatch }
            }));

            StringAssert.EndsWith("/api/v1/verify-image-match", transport.Last.Url);
            Assert.AreEqual(Convert.ToBase64String(new byte[] { 1, 2, 3 }), transport.LastBody["image"]);
            Assert.AreEqual(Convert.ToBase64String(new byte[] { 4, 5, 6 }), transport.LastBody["reference_image"]);
            CollectionAssert.AreEqual(new object[] { "face_match" }, (List<object>)transport.LastBody["checks"]);
            Assert.IsTrue(result.FaceMatch.Matched);
            Assert.AreEqual(0.9, result.FaceMatch.MatchScore);
            Assert.IsNull(result.Age);
        }

        [Test]
        public void Redact_SendsTextAndMode()
        {
            var transport = new FakeTransport().Respond(200,
                "{\"text\":\"Call me on [PHONE_1]\",\"counts\":{\"phone\":1},\"entities_total\":1,\"mode\":\"precise\"}");

            var result = TestClient.Run(TestClient.Create(transport).RedactAsync(new RedactInput
            {
                Text = "Call me on 555 0100",
                Mode = RedactionMode.Precise
            }));

            StringAssert.EndsWith("/api/v1/privacy/redact", transport.Last.Url);
            Assert.AreEqual("precise", transport.LastBody["mode"]);
            Assert.IsFalse(transport.LastBody.ContainsKey("messages"));
            Assert.AreEqual("Call me on [PHONE_1]", result.Text);
            Assert.AreEqual(1, result.Counts["phone"]);
        }

        [Test]
        public void Redact_BothTextAndMessages_IsRejectedLocally()
        {
            var transport = new FakeTransport();
            Assert.Throws<ValidationException>(() => TestClient.Run(TestClient.Create(transport).RedactAsync(new RedactInput
            {
                Text = "a",
                Messages = new List<string> { "b" }
            })));
            Assert.AreEqual(0, transport.Requests.Count);
        }

        [Test]
        public void AnalyzeDocument_SendsMultipartForm()
        {
            var transport = new FakeTransport().Respond(200,
                "{\"file_id\":\"doc_1\",\"total_pages\":2,\"page_results\":[{\"page_number\":1,\"results\":[{\"endpoint\":\"unsafe\",\"recommended_action\":\"block\"}]}],\"flagged_pages\":[]}");

            var result = TestClient.Run(TestClient.Create(transport).AnalyzeDocumentAsync(new AnalyzeDocumentInput
            {
                File = new byte[] { 37, 80, 68, 70 },
                Filename = "notes.pdf",
                Endpoints = new List<string> { "unsafe" },
                SplitBySender = true
            }));

            StringAssert.EndsWith("/api/v1/safety/document", transport.Last.Url);
            Assert.IsNull(transport.Last.JsonBody);
            Assert.AreEqual("[\"unsafe\"]", transport.Last.Form.ValueOf("endpoints"));
            Assert.AreEqual("true", transport.Last.Form.ValueOf("split_by_sender"));
            Assert.AreEqual("Unity SDK", transport.Last.Form.ValueOf("platform"));
            Assert.AreEqual(2, result.TotalPages);
            Assert.AreEqual(ModerationAction.Block, result.PageResults[0].Results[0].Action);
        }

        [Test]
        public void AnalyzeImage_SendsChildAgeInvariant()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("sv-SE");
            var transport = new FakeTransport();

            TestClient.Run(TestClient.Create(transport).AnalyzeImageAsync(new byte[] { 1 }, "a.png", childAge: 12));

            Assert.AreEqual("12", transport.Last.Form.ValueOf("child_age"));
        }
    }

    public class VerificationSessionTests
    {
        [Test]
        public void CreateSession_SendsOnlyAcceptedFields()
        {
            var transport = new FakeTransport().Respond(200,
                "{\"session_id\":\"vs_1\",\"mobile_url\":\"https://verify.example/vs_1\",\"expires_at\":1790000000000,\"mode\":\"identity\",\"chip_policy\":\"required\",\"auto_capture\":false,\"verification_mode\":\"full\",\"recommended_image_width\":1920}");

            var session = TestClient.Run(TestClient.Create(transport).CreateVerificationSessionAsync(new CreateVerificationSessionInput
            {
                Mode = VerificationMode.Identity,
                DocumentType = VerificationDocumentType.NationalId,
                ChipPolicy = ChipPolicy.InsteadOfPhoto,
                AutoCapture = false
            }));

            Assert.AreEqual("POST", transport.Last.Method);
            StringAssert.EndsWith("/api/v1/verify/session", transport.Last.Url);
            var body = transport.LastBody;
            CollectionAssert.AreEquivalent(new[] { "mode", "document_type", "chip_policy", "auto_capture" }, body.Keys);
            Assert.AreEqual("identity", body["mode"]);
            Assert.AreEqual("national_id", body["document_type"]);
            Assert.AreEqual("instead_of_photo", body["chip_policy"]);
            Assert.AreEqual(false, body["auto_capture"]);

            Assert.AreEqual("vs_1", session.SessionId);
            Assert.AreEqual("https://verify.example/vs_1", session.MobileUrl);
            Assert.AreEqual(1790000000000L, session.ExpiresAt);
            Assert.AreEqual(1920, session.RecommendedImageWidth);
            Assert.AreEqual(false, session.AutoCapture);
        }

        [Test]
        public void CreateSession_DefaultsToAgeMode()
        {
            var transport = new FakeTransport();
            TestClient.Run(TestClient.Create(transport).CreateVerificationSessionAsync(new CreateVerificationSessionInput()));

            CollectionAssert.AreEquivalent(new[] { "mode" }, transport.LastBody.Keys);
            Assert.AreEqual("age", transport.LastBody["mode"]);
        }

        [Test]
        public void GetSession_ParsesResult()
        {
            var transport = new FakeTransport().Respond(200,
                "{\"session_id\":\"vs_1\",\"status\":\"completed\",\"verification_mode\":\"full\",\"result\":{\"status\":\"verified\",\"age\":16,\"is_minor\":true,\"face_match\":{\"matched\":true,\"confidence\":0.55,\"match_score\":0.93},\"liveness\":{\"valid\":true},\"document\":{\"ocr_confidence\":91}}}");

            var result = TestClient.Run(TestClient.Create(transport).GetVerificationSessionAsync("vs_1"));

            Assert.AreEqual("GET", transport.Last.Method);
            StringAssert.EndsWith("/api/v1/verify/session/vs_1", transport.Last.Url);
            Assert.AreEqual(VerificationSessionStatus.Completed, result.Status);
            Assert.AreEqual("verified", result.Result.Status);
            Assert.AreEqual(true, result.Result.IsMinor);
            Assert.AreEqual(0.93, result.Result.FaceMatch.MatchScore);
            Assert.IsTrue(result.Result.Liveness.Valid);
            Assert.AreEqual(91, result.Result.Document.OcrConfidence);
        }

        [TestCase("pending", VerificationSessionStatus.Pending)]
        [TestCase("in_progress", VerificationSessionStatus.InProgress)]
        [TestCase("failed", VerificationSessionStatus.Failed)]
        [TestCase("expired", VerificationSessionStatus.Expired)]
        [TestCase("document_required", VerificationSessionStatus.DocumentRequired)]
        [TestCase("something_new", VerificationSessionStatus.Unknown)]
        public void GetSession_MapsStatus(string raw, VerificationSessionStatus expected)
        {
            var transport = new FakeTransport().Respond(200, $"{{\"session_id\":\"vs_1\",\"status\":\"{raw}\"}}");

            var result = TestClient.Run(TestClient.Create(transport).GetVerificationSessionAsync("vs_1"));

            Assert.AreEqual(expected, result.Status);
            Assert.AreEqual(raw, result.RawStatus);
            Assert.IsNull(result.Result);
        }

        [Test]
        public void CancelSession_UsesDelete()
        {
            var transport = new FakeTransport().Respond(200, "{\"ok\":true}");

            var ok = TestClient.Run(TestClient.Create(transport).CancelVerificationSessionAsync("vs_1"));

            Assert.AreEqual("DELETE", transport.Last.Method);
            StringAssert.EndsWith("/api/v1/verify/session/vs_1", transport.Last.Url);
            Assert.IsNull(transport.Last.JsonBody);
            Assert.IsTrue(ok);
        }
    }

    public class IncidentRequestTests
    {
        [Test]
        public void ListIncidents_BuildsEncodedQuery()
        {
            var transport = new FakeTransport().Respond(200,
                "{\"incidents\":[{\"id\":\"inc_1\",\"risk_category\":\"bullying\",\"recommended_actions\":[\"flag_for_review\"]}],\"next_cursor\":\"c2\",\"total_returned\":1}");

            var result = TestClient.Run(TestClient.Create(transport).ListIncidentsAsync(new ListIncidentsInput
            {
                Category = "bullying",
                From = "2026-01-01T00:00:00Z",
                Limit = 50,
                IncludeSummary = true
            }));

            StringAssert.EndsWith("/api/v1/incidents?category=bullying&from=2026-01-01T00%3A00%3A00Z&limit=50&include_summary=true",
                transport.Last.Url);
            Assert.AreEqual("inc_1", result.Incidents[0].Id);
            Assert.AreEqual("c2", result.NextCursor);
        }

        [Test]
        public void ListIncidents_NoFilters_HasNoQuery()
        {
            var transport = new FakeTransport();
            TestClient.Run(TestClient.Create(transport).ListIncidentsAsync());

            StringAssert.EndsWith("/api/v1/incidents", transport.Last.Url);
        }

        [Test]
        public void IncidentsOverviewAndTrends_UseTheirRoutes()
        {
            var transport = new FakeTransport()
                .Respond(200, "{\"timeframe\":{\"from\":\"a\",\"to\":\"b\"},\"requires_review_count\":4,\"counts_by_severity\":{\"high\":3}}")
                .Respond(200, "{\"bucket_size\":\"week\",\"series\":[{\"bucket_start\":\"a\",\"total\":2}]}");
            var client = TestClient.Create(transport);

            var overview = TestClient.Run(client.GetIncidentsOverviewAsync());
            StringAssert.EndsWith("/api/v1/incidents/overview", transport.Last.Url);
            var trends = TestClient.Run(client.GetIncidentTrendsAsync(IncidentTrendBucket.Week));
            StringAssert.EndsWith("/api/v1/incidents/trends?bucket=week", transport.Last.Url);

            Assert.AreEqual(4, overview.RequiresReviewCount);
            Assert.AreEqual(3, overview.CountsBySeverity["high"]);
            Assert.AreEqual("a", overview.From);
            Assert.AreEqual(2, trends.Series[0].Total);
        }

        [Test]
        public void GetIncident_ParsesDetail()
        {
            var transport = new FakeTransport().Respond(200,
                "{\"id\":\"inc_1\",\"summary\":\"text\",\"message_analysis\":[{\"message_index\":2,\"risk_score\":0.6,\"flags\":[]}]}");

            var incident = TestClient.Run(TestClient.Create(transport).GetIncidentAsync("inc_1"));

            StringAssert.EndsWith("/api/v1/incidents/inc_1", transport.Last.Url);
            Assert.AreEqual("text", incident.Summary);
            Assert.AreEqual(2, incident.MessageAnalysis[0].MessageIndex);
        }

        [Test]
        public void ReviewIncident_SendsSnakeCaseValues()
        {
            var transport = new FakeTransport().Respond(200,
                "{\"incident_id\":\"inc_1\",\"original\":{\"risk_level\":\"high\"},\"revised\":{\"risk_level\":\"low\"},\"audit_receipt\":null}");

            var result = TestClient.Run(TestClient.Create(transport).ReviewIncidentAsync("inc_1", new ReviewIncidentInput
            {
                Action = ModeratorAction.Downgrade,
                ReasonCode = ModeratorReasonCode.FalsePositive,
                NewRiskLevel = "low",
                ModeratorExternalId = "moderator-7"
            }));

            StringAssert.EndsWith("/api/v1/incidents/inc_1/review", transport.Last.Url);
            Assert.AreEqual("downgrade", transport.LastBody["action"]);
            Assert.AreEqual("false_positive", transport.LastBody["reason_code"]);
            Assert.AreEqual("moderator-7", transport.LastBody["moderator_external_id"]);
            Assert.AreEqual("low", result.Revised.RiskLevel);
            Assert.IsNull(result.AuditReceipt);
        }

        [Test]
        public void ReviewIncident_WithoutModerator_IsRejectedLocally()
        {
            var transport = new FakeTransport();
            Assert.Throws<ValidationException>(() => TestClient.Run(TestClient.Create(transport)
                .ReviewIncidentAsync("inc_1", new ReviewIncidentInput { Action = ModeratorAction.Confirm })));
            Assert.AreEqual(0, transport.Requests.Count);
        }

        [Test]
        public void BatchReview_SendsIdsAndParsesOutcomes()
        {
            var transport = new FakeTransport().Respond(200,
                "{\"total\":2,\"succeeded\":1,\"failed\":1,\"results\":[{\"incident_id\":\"a\",\"ok\":true},{\"incident_id\":\"b\",\"ok\":false,\"error\":\"not found\"}]}");

            var result = TestClient.Run(TestClient.Create(transport).BatchReviewIncidentsAsync(new BatchReviewIncidentsInput
            {
                IncidentIds = new List<string> { "a", "b" },
                Action = ModeratorAction.Dismiss,
                ReasonCode = ModeratorReasonCode.FalsePositive,
                ModeratorExternalId = "moderator-7"
            }));

            StringAssert.EndsWith("/api/v1/incidents/batch-review", transport.Last.Url);
            CollectionAssert.AreEqual(new object[] { "a", "b" }, (List<object>)transport.LastBody["incident_ids"]);
            Assert.AreEqual(1, result.Failed);
            Assert.IsFalse(result.Results[1].Ok);
            Assert.AreEqual("not found", result.Results[1].Error);
        }

        [Test]
        public void BatchReview_MoreThan100_IsRejectedLocally()
        {
            var ids = new List<string>();
            for (int i = 0; i < 101; i++) ids.Add($"id{i}");
            var transport = new FakeTransport();

            Assert.Throws<ValidationException>(() => TestClient.Run(TestClient.Create(transport).BatchReviewIncidentsAsync(
                new BatchReviewIncidentsInput { IncidentIds = ids, ModeratorExternalId = "moderator-7" })));
            Assert.AreEqual(0, transport.Requests.Count);
        }
    }

    public class ActionPlanRequestTests
    {
        private const string Plan = "{\"audience\":\"parent\",\"steps\":[\"Stay calm.\"],\"tone\":\"supportive\"";

        private static Dictionary<string, object> BodyFor(GetActionPlanInput input)
        {
            var transport = new FakeTransport().Respond(200, Plan + "}");
            TestClient.Run(TestClient.Create(transport).GetActionPlanAsync(input));
            return transport.LastBody;
        }

        private static ActionPlanResult ResultFor(string response)
        {
            var transport = new FakeTransport().Respond(200, response);
            return TestClient.Run(TestClient.Create(transport)
                .GetActionPlanAsync(new GetActionPlanInput { Situation = "A worry" }));
        }

        [Test]
        public void GetActionPlan_SendsCountry()
        {
            var body = BodyFor(new GetActionPlanInput { Situation = "A worry", Country = "SE" });
            Assert.AreEqual("SE", body["country"]);
        }

        [Test]
        public void GetActionPlan_LeavesOutCountryWhenNotSet()
        {
            var body = BodyFor(new GetActionPlanInput { Situation = "A worry" });
            Assert.IsFalse(body.ContainsKey("country"));
        }

        [Test]
        public void GetActionPlan_ReadsGeneralGuidance()
        {
            Assert.IsTrue(ResultFor(Plan + ",\"general_guidance\":true}").GeneralGuidance);
        }

        [Test]
        public void GetActionPlan_TailoredPlanIsNotGeneralGuidance()
        {
            Assert.IsFalse(ResultFor(Plan + "}").GeneralGuidance);
        }
    }
}
