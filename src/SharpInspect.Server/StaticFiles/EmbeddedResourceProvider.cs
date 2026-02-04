using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SharpInspect.Server.StaticFiles;

/// <summary>
///     임베디드 리소스에서 정적 파일 콘텐츠를 제공합니다.
/// </summary>
public class EmbeddedResourceProvider
{
    private readonly Assembly _assembly;
    private readonly Dictionary<string, byte[]> _cache;
    private readonly object _cacheLock = new();
    private readonly string _resourcePrefix;

    /// <summary>
    ///     새 EmbeddedResourceProvider를 생성합니다.
    /// </summary>
    public EmbeddedResourceProvider()
    {
        _cache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        _assembly = typeof(EmbeddedResourceProvider).Assembly;
        _resourcePrefix = "SharpInspect.Server.wwwroot";

        // 임베디드 리소스 사전 로드
        LoadEmbeddedResources();
    }

    /// <summary>
    ///     경로의 콘텐츠를 가져오거나, 찾을 수 없으면 null 반환.
    /// </summary>
    public byte[] GetContent(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        // 경로 정규화
        path = path.TrimStart('/').Replace('/', '.').Replace('\\', '.');

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(path, out var content)) return content;
        }

        // 캐시에 없으면 임베디드 리소스에서 로드 시도
        var resourceName = _resourcePrefix + "." + path;
        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            var content = ReadStream(stream);
            lock (_cacheLock)
            {
                _cache[path] = content;
            }

            return content;
        }

        return null;
    }

    /// <summary>
    ///     파일 경로의 콘텐츠 타입을 가져옵니다.
    /// </summary>
    public string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();

        switch (ext)
        {
            case ".html":
                return "text/html; charset=utf-8";
            case ".css":
                return "text/css; charset=utf-8";
            case ".js":
                return "application/javascript; charset=utf-8";
            case ".json":
                return "application/json; charset=utf-8";
            case ".png":
                return "image/png";
            case ".jpg":
            case ".jpeg":
                return "image/jpeg";
            case ".gif":
                return "image/gif";
            case ".svg":
                return "image/svg+xml";
            case ".ico":
                return "image/x-icon";
            case ".woff":
                return "font/woff";
            case ".woff2":
                return "font/woff2";
            case ".ttf":
                return "font/ttf";
            case ".eot":
                return "application/vnd.ms-fontobject";
            default:
                return "application/octet-stream";
        }
    }

    private byte[] ReadStream(Stream stream)
    {
        using (var ms = new MemoryStream())
        {
            var buffer = new byte[4096];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) ms.Write(buffer, 0, read);
            return ms.ToArray();
        }
    }

    private void LoadEmbeddedResources()
    {
        // 임베디드 리소스 이름 목록 가져오기
        var resourceNames = _assembly.GetManifestResourceNames();

        foreach (var resourceName in resourceNames)
        {
            // wwwroot 리소스만 처리
            if (!resourceName.StartsWith(_resourcePrefix + "."))
                continue;

            // 리소스 이름에서 경로 추출
            var path = resourceName.Substring(_resourcePrefix.Length + 1);

            using var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                var content = ReadStream(stream);
                lock (_cacheLock)
                {
                    _cache[path] = content;
                }
            }
        }
    }
}
