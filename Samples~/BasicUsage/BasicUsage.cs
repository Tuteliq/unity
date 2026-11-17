using Tuteliq;
using UnityEngine;

/// <summary>
/// Screens one chat message and logs what to do with it.
/// </summary>
/// <remarks>
/// The API key grants full access to your account. Run this from a server you
/// control, or set <c>baseUrl</c> to your own proxy and pass the token it accepts.
/// Do not ship the key in a game build.
/// </remarks>
public class BasicUsage : MonoBehaviour
{
    [SerializeField] private string apiKey = "";
    [SerializeField] private string baseUrl = "https://api.tuteliq.ai";
    [SerializeField] private string message = "Nobody likes you, just leave";

    private async void Start()
    {
        var client = new TuteliqClient(apiKey, baseUrl: baseUrl);

        try
        {
            var result = await client.DetectBullyingAsync(message);

            if (ModerationActions.ShouldWithhold(result.Action))
                Debug.Log("Do not deliver this message.");
            else if (ModerationActions.NeedsReview(result.Action))
                Debug.Log($"Send to your moderation queue: {result.Rationale}");
            else
                Debug.Log("No action needed.");
        }
        catch (TuteliqException e)
        {
            Debug.LogError($"Tuteliq request failed ({e.Code}, request {e.RequestId}): {e.Message}");
        }
    }
}
