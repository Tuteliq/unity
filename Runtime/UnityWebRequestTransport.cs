using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Tuteliq
{
    /// <summary>
    /// Sends requests with <c>UnityWebRequest</c>.
    /// </summary>
    internal sealed class UnityWebRequestTransport : ITransport
    {
        public async Task<TransportResponse> SendAsync(TransportRequest request)
        {
            using var webRequest = new UnityWebRequest(request.Url, request.Method);
            foreach (var header in request.Headers)
                webRequest.SetRequestHeader(header.Key, header.Value);

            if (request.Form != null)
            {
                var boundary = UnityWebRequest.GenerateBoundary();
                webRequest.uploadHandler = new UploadHandlerRaw(UnityWebRequest.SerializeFormSections(ToSections(request.Form), boundary));
                webRequest.SetRequestHeader("Content-Type", $"multipart/form-data; boundary={Encoding.UTF8.GetString(boundary)}");
            }
            else if (request.JsonBody != null)
            {
                webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(request.JsonBody));
                webRequest.SetRequestHeader("Content-Type", "application/json");
            }
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = request.TimeoutSeconds;

            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            return ToResponse(webRequest);
        }

        private static TransportResponse ToResponse(UnityWebRequest webRequest)
        {
            var response = new TransportResponse
            {
                StatusCode = (int)webRequest.responseCode,
                Body = webRequest.downloadHandler?.text
            };

            var headers = webRequest.GetResponseHeaders();
            if (headers != null)
            {
                foreach (var header in headers)
                    response.Headers[header.Key] = header.Value;
            }

            if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                webRequest.result == UnityWebRequest.Result.DataProcessingError)
            {
                response.TransportError = webRequest.error ?? "Network error";
                response.TimedOut = response.TransportError.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
                    || response.TransportError.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            return response;
        }

        private static List<IMultipartFormSection> ToSections(MultipartForm form)
        {
            var sections = new List<IMultipartFormSection>();
            foreach (var field in form.Fields)
            {
                if (field.FileData != null)
                    sections.Add(new MultipartFormFileSection(field.Name, field.FileData, field.FileName, field.ContentType));
                else
                    sections.Add(new MultipartFormDataSection(field.Name, field.Value));
            }
            return sections;
        }
    }
}
