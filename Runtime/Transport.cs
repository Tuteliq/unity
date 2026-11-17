using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tuteliq
{
    /// <summary>
    /// One field of a multipart form: text, or a file when <see cref="FileData"/> is set.
    /// </summary>
    internal sealed class FormField
    {
        public string Name;
        public string Value;
        public byte[] FileData;
        public string FileName;
        public string ContentType;
    }

    /// <summary>
    /// A multipart form, built without engine types so request building can be tested anywhere.
    /// </summary>
    internal sealed class MultipartForm
    {
        public readonly List<FormField> Fields = new List<FormField>();

        public MultipartForm AddFile(string name, byte[] data, string fileName, string contentType = "application/octet-stream")
        {
            Fields.Add(new FormField { Name = name, FileData = data, FileName = fileName, ContentType = contentType });
            return this;
        }

        public MultipartForm Add(string name, string value)
        {
            Fields.Add(new FormField { Name = name, Value = value });
            return this;
        }

        public MultipartForm AddIfSet(string name, string value)
        {
            return value == null ? this : Add(name, value);
        }

        public string ValueOf(string name)
        {
            foreach (var field in Fields)
                if (field.Name == name && field.FileData == null) return field.Value;
            return null;
        }
    }

    internal sealed class TransportRequest
    {
        public string Method;
        public string Url;
        public readonly Dictionary<string, string> Headers = new Dictionary<string, string>();
        /// <summary>JSON body, or null.</summary>
        public string JsonBody;
        /// <summary>Multipart body, or null.</summary>
        public MultipartForm Form;
        public int TimeoutSeconds;
    }

    internal sealed class TransportResponse
    {
        /// <summary>HTTP status, or 0 when no response arrived.</summary>
        public int StatusCode;
        public string Body;
        public Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Set when the request failed before a response arrived.</summary>
        public string TransportError;
        public bool TimedOut;

        public string Header(string name)
        {
            return Headers != null && Headers.TryGetValue(name, out var value) ? value : null;
        }
    }

    /// <summary>
    /// Sends one HTTP request. The SDK uses <c>UnityWebRequest</c>; tests use a fake.
    /// </summary>
    internal interface ITransport
    {
        Task<TransportResponse> SendAsync(TransportRequest request);
    }
}
