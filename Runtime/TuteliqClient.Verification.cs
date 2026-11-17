using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tuteliq
{
    public partial class TuteliqClient
    {
        // =====================================================================
        // Verification Sessions
        // =====================================================================

        /// <summary>
        /// Create an age or identity verification session. Open
        /// <see cref="VerificationSession.MobileUrl"/> for the user (browser or web view);
        /// the hosted page handles capture. Then poll <see cref="GetVerificationSessionAsync"/>
        /// or subscribe to the verification.completed webhook.
        /// Not retried automatically: a repeat would create a second session.
        /// </summary>
        public async Task<VerificationSession> CreateVerificationSessionAsync(CreateVerificationSessionInput input)
        {
            var data = await SendJsonOnceAsync("POST", "/api/v1/verify/session", BuildVerificationSessionBody(input));
            return ParseVerificationSession(data);
        }

        /// <summary>
        /// Get the status and result of a verification session. The extracted document fields,
        /// date of birth and failure reasons are released only on the first poll after the session
        /// completes or fails: store them on that read, or use the webhook.
        /// </summary>
        public async Task<VerificationSessionResult> GetVerificationSessionAsync(string sessionId)
        {
            RequireId(sessionId, "Session ID");
            var data = await RequestAsync("GET", $"/api/v1/verify/session/{Uri.EscapeDataString(sessionId)}");
            return ParseVerificationSessionResult(data);
        }

        /// <summary>
        /// Cancel a verification session. Returns true when the API confirmed it.
        /// </summary>
        public async Task<bool> CancelVerificationSessionAsync(string sessionId)
        {
            RequireId(sessionId, "Session ID");
            var data = await RequestAsync("DELETE", $"/api/v1/verify/session/{Uri.EscapeDataString(sessionId)}");
            return GetBool(data, "ok");
        }

        private static Dictionary<string, object> BuildVerificationSessionBody(CreateVerificationSessionInput input)
        {
            var body = new Dictionary<string, object> { { "mode", input.Mode.ToApiString() } };
            if (input.DocumentType.HasValue) body["document_type"] = input.DocumentType.Value.ToApiString();
            if (input.ChipPolicy.HasValue) body["chip_policy"] = input.ChipPolicy.Value.ToApiString();
            if (input.AutoCapture.HasValue) body["auto_capture"] = input.AutoCapture.Value;
            return body;
        }

        private static void RequireId(string id, string name)
        {
            if (string.IsNullOrEmpty(id))
                throw new ValidationException($"{name} is required");
        }

        // =====================================================================
        // Parsing
        // =====================================================================

        private static VerificationSession ParseVerificationSession(Dictionary<string, object> data)
        {
            return new VerificationSession
            {
                SessionId = GetString(data, "session_id"),
                MobileUrl = GetString(data, "mobile_url"),
                ExpiresAt = GetNullableLong(data, "expires_at") ?? 0,
                Mode = GetString(data, "mode"),
                ChipPolicy = GetString(data, "chip_policy"),
                AutoCapture = GetNullableBool(data, "auto_capture"),
                VerificationMode = GetString(data, "verification_mode"),
                RecommendedImageWidth = GetNullableInt(data, "recommended_image_width")
            };
        }

        private static VerificationSessionResult ParseVerificationSessionResult(Dictionary<string, object> data)
        {
            var rawStatus = GetString(data, "status");
            return new VerificationSessionResult
            {
                SessionId = GetString(data, "session_id"),
                Status = EnumExtensions.ParseVerificationSessionStatus(rawStatus),
                RawStatus = rawStatus,
                Result = GetObject(data, "result", ParseVerificationResult),
                VerificationMode = GetString(data, "verification_mode"),
                DocumentEvidence = GetString(data, "document_evidence"),
                StepProgress = GetString(data, "step_progress"),
                ChipPolicy = GetString(data, "chip_policy"),
                CreatedAt = GetNullableLong(data, "created_at"),
                ExpiresAt = GetNullableLong(data, "expires_at")
            };
        }

        private static VerificationResult ParseVerificationResult(Dictionary<string, object> data)
        {
            return new VerificationResult
            {
                Status = GetString(data, "status"),
                Age = GetNullableDouble(data, "age"),
                AgeRange = GetObject(data, "age_range", range => new VerificationAgeRange
                {
                    Low = GetDouble(range, "low"),
                    High = GetDouble(range, "high")
                }),
                IsMinor = GetNullableBool(data, "is_minor"),
                DateOfBirth = GetString(data, "date_of_birth"),
                DocumentEvidence = GetString(data, "document_evidence"),
                Document = GetObject(data, "document", ParseVerificationDocument),
                FaceMatch = GetObject(data, "face_match", face => new VerificationFaceMatch
                {
                    Matched = GetBool(face, "matched"),
                    Confidence = GetDouble(face, "confidence"),
                    MatchScore = GetNullableDouble(face, "match_score")
                }),
                Liveness = GetObject(data, "liveness", liveness => new VerificationLiveness
                {
                    Valid = GetBool(liveness, "valid")
                }),
                FailureReasons = data.ContainsKey("failure_reasons") && data["failure_reasons"] != null
                    ? GetStringList(data, "failure_reasons")
                    : null
            };
        }

        private static VerificationDocument ParseVerificationDocument(Dictionary<string, object> data)
        {
            return new VerificationDocument
            {
                OcrConfidence = GetDouble(data, "ocr_confidence"),
                NameExtracted = GetString(data, "name_extracted"),
                FirstName = GetString(data, "first_name"),
                LastName = GetString(data, "last_name"),
                DobExtracted = GetString(data, "dob_extracted"),
                DocumentNumber = GetString(data, "document_number"),
                DocumentNumberValid = GetNullableBool(data, "document_number_valid"),
                Sex = GetString(data, "sex"),
                CountryCode = GetString(data, "country_code"),
                CountryName = GetString(data, "country_name"),
                DocumentType = GetString(data, "document_type"),
                ExpirationDate = GetString(data, "expiration_date"),
                Expired = GetNullableBool(data, "expired"),
                IssueDate = GetString(data, "issue_date"),
                HeightCm = GetNullableDouble(data, "height_cm"),
                PlaceOfBirth = GetString(data, "place_of_birth"),
                Nationality = GetString(data, "nationality"),
                IssuingAuthority = GetString(data, "issuing_authority"),
                ChipVerified = GetNullableBool(data, "chip_verified"),
                ChipIssuerTrusted = GetNullableBool(data, "chip_issuer_trusted"),
                NationalId = GetObject(data, "national_id", id => new VerificationNationalId
                {
                    Value = GetString(id, "value"),
                    Valid = GetBool(id, "valid"),
                    Type = GetString(id, "type"),
                    CountryCode = GetString(id, "country_code")
                })
            };
        }
    }
}
