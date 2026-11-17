// Compile-only stand-ins for the UnityEngine types the Runtime scripts use,
// so CI can build them with the .NET SDK. Signatures follow Unity 2021.3.
// Nothing here sends a request: tests use a fake transport.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class AsyncOperation
    {
        public bool isDone => true;
    }
}

namespace UnityEngine.Networking
{
    public interface IMultipartFormSection
    {
    }

    public class MultipartFormDataSection : IMultipartFormSection
    {
        public MultipartFormDataSection(string name, string data) { }
    }

    public class MultipartFormFileSection : IMultipartFormSection
    {
        public MultipartFormFileSection(string name, byte[] data, string fileName, string contentType) { }
    }

    public class UploadHandler : IDisposable
    {
        public void Dispose() { }
    }

    public sealed class UploadHandlerRaw : UploadHandler
    {
        public UploadHandlerRaw(byte[] data) { }
    }

    public class DownloadHandler : IDisposable
    {
        public string text => null;
        public void Dispose() { }
    }

    public sealed class DownloadHandlerBuffer : DownloadHandler
    {
    }

    public class UnityWebRequestAsyncOperation : AsyncOperation
    {
    }

    public class UnityWebRequest : IDisposable
    {
        public enum Result
        {
            InProgress,
            Success,
            ConnectionError,
            ProtocolError,
            DataProcessingError
        }

        public UnityWebRequest(string url, string method) { }

        public UploadHandler uploadHandler { get; set; }
        public DownloadHandler downloadHandler { get; set; }
        public int timeout { get; set; }
        public Result result => Result.InProgress;
        public string error => null;
        public long responseCode => 0;

        public void SetRequestHeader(string name, string value) { }
        public string GetResponseHeader(string name) => null;
        public Dictionary<string, string> GetResponseHeaders() => null;

        public UnityWebRequestAsyncOperation SendWebRequest()
        {
            throw new NotSupportedException("Compile-only stub: no network in CI.");
        }

        public static byte[] GenerateBoundary() => new byte[0];

        public static byte[] SerializeFormSections(List<IMultipartFormSection> multipartFormSections, byte[] boundary) => new byte[0];

        public void Dispose() { }
    }
}
