using System;
using System.Collections.Generic;
using SharpInspect.Core.Models;

namespace SharpInspect.Server.Har
{
    /// <summary>
    ///     NetworkEntry 배열을 HAR 형식으로 변환합니다.
    /// </summary>
    public static class HarConverter
    {
        private const string HarVersion = "1.2";
        private const string CreatorName = "SharpInspect";
        private const string CreatorVersion = "1.0.0";

        /// <summary>
        ///     NetworkEntry 배열을 HarRoot 객체로 변환합니다.
        /// </summary>
        /// <param name="entries">변환할 NetworkEntry 배열.</param>
        /// <returns>HAR 형식의 루트 객체.</returns>
        public static HarRoot Convert(NetworkEntry[] entries)
        {
            if (entries == null)
                entries = new NetworkEntry[0];

            var harEntries = new HarEntry[entries.Length];
            for (var i = 0; i < entries.Length; i++)
            {
                harEntries[i] = ConvertEntry(entries[i]);
            }

            return new HarRoot
            {
                Log = new HarLog
                {
                    Version = HarVersion,
                    Creator = new HarCreator
                    {
                        Name = CreatorName,
                        Version = CreatorVersion
                    },
                    Entries = harEntries
                }
            };
        }

        /// <summary>
        ///     단일 NetworkEntry를 HarEntry로 변환합니다.
        /// </summary>
        private static HarEntry ConvertEntry(NetworkEntry entry)
        {
            var harEntry = new HarEntry
            {
                StartedDateTime = ToIso8601(entry.Timestamp),
                Time = (int)entry.TotalMs,
                Request = ConvertRequest(entry),
                Response = ConvertResponse(entry),
                Cache = new HarCache(),
                Timings = ConvertTimings(entry)
            };

            // 오류가 있으면 주석에 추가
            if (entry.IsError && !string.IsNullOrEmpty(entry.ErrorMessage))
            {
                harEntry.Comment = entry.ErrorMessage;
            }

            return harEntry;
        }

        /// <summary>
        ///     NetworkEntry에서 HarRequest를 생성합니다.
        /// </summary>
        private static HarRequest ConvertRequest(NetworkEntry entry)
        {
            var request = new HarRequest
            {
                Method = entry.Method ?? "GET",
                Url = entry.Url ?? "",
                HttpVersion = entry.Protocol ?? "HTTP/1.1",
                Cookies = ParseCookies(GetHeaderValue(entry.RequestHeaders, "Cookie")),
                Headers = ConvertHeaders(entry.RequestHeaders),
                QueryString = ParseQueryString(entry.QueryString),
                HeadersSize = -1,
                BodySize = entry.RequestContentLength > 0 ? (int)entry.RequestContentLength : -1
            };

            // POST 데이터가 있으면 추가
            if (!string.IsNullOrEmpty(entry.RequestBody))
            {
                request.PostData = new HarPostData
                {
                    MimeType = entry.RequestContentType ?? "application/octet-stream",
                    Text = entry.RequestBody
                };
            }

            return request;
        }

        /// <summary>
        ///     NetworkEntry에서 HarResponse를 생성합니다.
        /// </summary>
        private static HarResponse ConvertResponse(NetworkEntry entry)
        {
            return new HarResponse
            {
                Status = entry.StatusCode,
                StatusText = entry.StatusText ?? "",
                HttpVersion = entry.Protocol ?? "HTTP/1.1",
                Cookies = ParseCookies(GetHeaderValue(entry.ResponseHeaders, "Set-Cookie")),
                Headers = ConvertHeaders(entry.ResponseHeaders),
                Content = new HarContent
                {
                    Size = entry.ResponseContentLength,
                    MimeType = entry.ResponseContentType ?? "application/octet-stream",
                    Text = entry.ResponseBody
                },
                RedirectURL = GetHeaderValue(entry.ResponseHeaders, "Location") ?? "",
                HeadersSize = -1,
                BodySize = entry.ResponseContentLength > 0 ? (int)entry.ResponseContentLength : -1
            };
        }

        /// <summary>
        ///     NetworkEntry에서 HarTimings를 생성합니다.
        /// </summary>
        private static HarTimings ConvertTimings(NetworkEntry entry)
        {
            return new HarTimings
            {
                Blocked = -1, // SharpInspect에서 추적하지 않음
                Dns = entry.DnsLookupMs > 0 ? (int)entry.DnsLookupMs : -1,
                Connect = entry.TcpConnectMs > 0 ? (int)entry.TcpConnectMs : -1,
                Ssl = entry.TlsHandshakeMs > 0 ? (int)entry.TlsHandshakeMs : -1,
                Send = entry.RequestSentMs > 0 ? (int)entry.RequestSentMs : -1,
                Wait = entry.WaitingMs > 0 ? (int)entry.WaitingMs : -1,
                Receive = entry.ContentDownloadMs > 0 ? (int)entry.ContentDownloadMs : -1
            };
        }

        /// <summary>
        ///     Dictionary 헤더를 HarNameValuePair 배열로 변환합니다.
        /// </summary>
        private static HarNameValuePair[] ConvertHeaders(Dictionary<string, string> headers)
        {
            if (headers == null || headers.Count == 0)
                return new HarNameValuePair[0];

            var result = new List<HarNameValuePair>();
            foreach (var kvp in headers)
            {
                result.Add(new HarNameValuePair
                {
                    Name = kvp.Key,
                    Value = kvp.Value ?? ""
                });
            }

            return result.ToArray();
        }

        /// <summary>
        ///     쿼리스트링을 파싱하여 HarNameValuePair 배열로 변환합니다.
        /// </summary>
        private static HarNameValuePair[] ParseQueryString(string queryString)
        {
            if (string.IsNullOrEmpty(queryString))
                return new HarNameValuePair[0];

            // 앞에 ?가 있으면 제거
            if (queryString.StartsWith("?"))
                queryString = queryString.Substring(1);

            var result = new List<HarNameValuePair>();
            var pairs = queryString.Split('&');

            foreach (var pair in pairs)
            {
                if (string.IsNullOrEmpty(pair))
                    continue;

                var idx = pair.IndexOf('=');
                if (idx > 0)
                {
                    result.Add(new HarNameValuePair
                    {
                        Name = UrlDecode(pair.Substring(0, idx)),
                        Value = UrlDecode(pair.Substring(idx + 1))
                    });
                }
                else
                {
                    result.Add(new HarNameValuePair
                    {
                        Name = UrlDecode(pair),
                        Value = ""
                    });
                }
            }

            return result.ToArray();
        }

        /// <summary>
        ///     Cookie 헤더를 파싱하여 HarCookie 배열로 변환합니다.
        /// </summary>
        private static HarCookie[] ParseCookies(string cookieHeader)
        {
            if (string.IsNullOrEmpty(cookieHeader))
                return new HarCookie[0];

            var result = new List<HarCookie>();
            var cookies = cookieHeader.Split(';');

            foreach (var cookie in cookies)
            {
                var trimmed = cookie.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                var idx = trimmed.IndexOf('=');
                if (idx > 0)
                {
                    result.Add(new HarCookie
                    {
                        Name = trimmed.Substring(0, idx).Trim(),
                        Value = trimmed.Substring(idx + 1).Trim()
                    });
                }
            }

            return result.ToArray();
        }

        /// <summary>
        ///     DateTime을 ISO 8601 형식 문자열로 변환합니다.
        /// </summary>
        private static string ToIso8601(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        /// <summary>
        ///     헤더 Dictionary에서 특정 키의 값을 가져옵니다.
        /// </summary>
        private static string GetHeaderValue(Dictionary<string, string> headers, string key)
        {
            if (headers == null)
                return null;

            // 대소문자 구분 없이 검색
            foreach (var kvp in headers)
            {
                if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            return null;
        }

        /// <summary>
        ///     URL 인코딩된 문자열을 디코딩합니다.
        /// </summary>
        private static string UrlDecode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

#if NET35 || LEGACY
            return System.Web.HttpUtility.UrlDecode(value);
#else
            return Uri.UnescapeDataString(value.Replace('+', ' '));
#endif
        }
    }
}
