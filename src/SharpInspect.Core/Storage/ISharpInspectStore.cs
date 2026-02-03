using SharpInspect.Core.Models;

namespace SharpInspect.Core.Storage
{
    /// <summary>
    ///     캡처된 데이터를 저장하고 조회하기 위한 인터페이스.
    /// </summary>
    public interface ISharpInspectStore
    {
        /// <summary>
        ///     콘솔 엔트리 수를 가져옵니다.
        /// </summary>
        int ConsoleEntryCount { get; }

        /// <summary>
        ///     네트워크 엔트리 수를 가져옵니다.
        /// </summary>
        int NetworkEntryCount { get; }

        /// <summary>
        ///     성능 엔트리 수를 가져옵니다.
        /// </summary>
        int PerformanceEntryCount { get; }

        /// <summary>
        ///     모든 콘솔 엔트리를 가져옵니다.
        /// </summary>
        ConsoleEntry[] GetConsoleEntries();

        /// <summary>
        ///     페이지네이션으로 콘솔 엔트리를 가져옵니다.
        /// </summary>
        ConsoleEntry[] GetConsoleEntries(int offset, int limit);

        /// <summary>
        ///     ID로 특정 네트워크 엔트리를 가져옵니다.
        /// </summary>
        NetworkEntry GetNetworkEntry(string id);

        /// <summary>
        ///     모든 네트워크 엔트리를 가져옵니다.
        /// </summary>
        NetworkEntry[] GetNetworkEntries();

        /// <summary>
        ///     페이지네이션으로 네트워크 엔트리를 가져옵니다.
        /// </summary>
        NetworkEntry[] GetNetworkEntries(int offset, int limit);

        /// <summary>
        ///     모든 성능 엔트리를 가져옵니다.
        /// </summary>
        PerformanceEntry[] GetPerformanceEntries();

        /// <summary>
        ///     페이지네이션으로 성능 엔트리를 가져옵니다.
        /// </summary>
        PerformanceEntry[] GetPerformanceEntries(int offset, int limit);

        /// <summary>
        ///     스토어에 콘솔 엔트리를 추가합니다.
        /// </summary>
        void AddConsoleEntry(ConsoleEntry entry);

        /// <summary>
        ///     스토어에 네트워크 엔트리를 추가합니다.
        /// </summary>
        void AddNetworkEntry(NetworkEntry entry);

        /// <summary>
        ///     스토어에 성능 엔트리를 추가합니다.
        /// </summary>
        void AddPerformanceEntry(PerformanceEntry entry);

        /// <summary>
        ///     현재 애플리케이션 정보 스냅샷을 가져옵니다.
        /// </summary>
        ApplicationInfo GetApplicationInfo();

        /// <summary>
        ///     애플리케이션 정보 스냅샷을 설정합니다.
        /// </summary>
        void SetApplicationInfo(ApplicationInfo info);

        /// <summary>
        ///     저장된 모든 데이터를 지웁니다.
        /// </summary>
        void ClearAll();

        /// <summary>
        ///     모든 콘솔 엔트리를 지웁니다.
        /// </summary>
        void ClearConsoleEntries();

        /// <summary>
        ///     모든 네트워크 엔트리를 지웁니다.
        /// </summary>
        void ClearNetworkEntries();

        /// <summary>
        ///     모든 성능 엔트리를 지웁니다.
        /// </summary>
        void ClearPerformanceEntries();
    }
}
