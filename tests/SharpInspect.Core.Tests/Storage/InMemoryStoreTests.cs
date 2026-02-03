using System;
using FluentAssertions;
using SharpInspect.Core.Models;
using SharpInspect.Core.Storage;
using Xunit;

namespace SharpInspect.Core.Tests.Storage
{
    /// <summary>
    ///     InMemoryStore 클래스의 단위 테스트.
    /// </summary>
    public class InMemoryStoreTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_Default_UsesDefaultCapacities()
        {
            // Arrange & Act
            var store = new InMemoryStore();

            // Assert
            store.ConsoleEntryCount.Should().Be(0);
            store.NetworkEntryCount.Should().Be(0);
            store.PerformanceEntryCount.Should().Be(0);
        }

        [Fact]
        public void Constructor_CustomCapacities_SetsCapacities()
        {
            // Arrange & Act
            var store = new InMemoryStore(100, 200, 300);

            // Assert
            store.ConsoleEntryCount.Should().Be(0);
            store.NetworkEntryCount.Should().Be(0);
            store.PerformanceEntryCount.Should().Be(0);
        }

        #endregion

        #region Console Entry Tests

        [Fact]
        public void AddConsoleEntry_ValidEntry_IncreasesCount()
        {
            // Arrange
            var store = new InMemoryStore();
            var entry = TestHelpers.CreateConsoleEntry();

            // Act
            store.AddConsoleEntry(entry);

            // Assert
            store.ConsoleEntryCount.Should().Be(1);
        }

        [Fact]
        public void AddConsoleEntry_NullEntry_ThrowsArgumentNullException()
        {
            // Arrange
            var store = new InMemoryStore();

            // Act
            Action act = () => store.AddConsoleEntry(null);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("entry");
        }

        [Fact]
        public void GetConsoleEntries_ReturnsAllEntries()
        {
            // Arrange
            var store = new InMemoryStore();
            store.AddConsoleEntry(TestHelpers.CreateConsoleEntry("Message 1"));
            store.AddConsoleEntry(TestHelpers.CreateConsoleEntry("Message 2"));
            store.AddConsoleEntry(TestHelpers.CreateConsoleEntry("Message 3"));

            // Act
            var result = store.GetConsoleEntries();

            // Assert
            result.Should().HaveCount(3);
            result[0].Message.Should().Be("Message 1");
            result[1].Message.Should().Be("Message 2");
            result[2].Message.Should().Be("Message 3");
        }

        [Fact]
        public void GetConsoleEntries_WithPagination_ReturnsSubset()
        {
            // Arrange
            var store = new InMemoryStore();
            for (var i = 1; i <= 10; i++)
                store.AddConsoleEntry(TestHelpers.CreateConsoleEntry($"Message {i}"));

            // Act
            var result = store.GetConsoleEntries(2, 3);

            // Assert
            result.Should().HaveCount(3);
            result[0].Message.Should().Be("Message 3");
            result[1].Message.Should().Be("Message 4");
            result[2].Message.Should().Be("Message 5");
        }

        [Fact]
        public void ClearConsoleEntries_RemovesAllEntries()
        {
            // Arrange
            var store = new InMemoryStore();
            store.AddConsoleEntry(TestHelpers.CreateConsoleEntry());
            store.AddConsoleEntry(TestHelpers.CreateConsoleEntry());

            // Act
            store.ClearConsoleEntries();

            // Assert
            store.ConsoleEntryCount.Should().Be(0);
            store.GetConsoleEntries().Should().BeEmpty();
        }

        #endregion

        #region Network Entry Tests

        [Fact]
        public void AddNetworkEntry_ValidEntry_IncreasesCount()
        {
            // Arrange
            var store = new InMemoryStore();
            var entry = TestHelpers.CreateNetworkEntry();

            // Act
            store.AddNetworkEntry(entry);

            // Assert
            store.NetworkEntryCount.Should().Be(1);
        }

        [Fact]
        public void AddNetworkEntry_NullEntry_ThrowsArgumentNullException()
        {
            // Arrange
            var store = new InMemoryStore();

            // Act
            Action act = () => store.AddNetworkEntry(null);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("entry");
        }

        [Fact]
        public void AddNetworkEntry_UpdatesIndex()
        {
            // Arrange
            var store = new InMemoryStore();
            var entry = TestHelpers.CreateNetworkEntry();

            // Act
            store.AddNetworkEntry(entry);

            // Assert
            var retrieved = store.GetNetworkEntry(entry.Id);
            retrieved.Should().NotBeNull();
            retrieved.Should().BeSameAs(entry);
        }

        [Fact]
        public void GetNetworkEntry_ValidId_ReturnsEntry()
        {
            // Arrange
            var store = new InMemoryStore();
            var entry = TestHelpers.CreateNetworkEntry();
            store.AddNetworkEntry(entry);

            // Act
            var result = store.GetNetworkEntry(entry.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(entry.Id);
        }

        [Fact]
        public void GetNetworkEntry_InvalidId_ReturnsNull()
        {
            // Arrange
            var store = new InMemoryStore();
            store.AddNetworkEntry(TestHelpers.CreateNetworkEntry());

            // Act
            var result = store.GetNetworkEntry("nonexistent-id");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetNetworkEntry_NullOrEmpty_ReturnsNull()
        {
            // Arrange
            var store = new InMemoryStore();

            // Act & Assert
            store.GetNetworkEntry(null).Should().BeNull();
            store.GetNetworkEntry("").Should().BeNull();
        }

        [Fact]
        public void GetNetworkEntries_ReturnsAllEntries()
        {
            // Arrange
            var store = new InMemoryStore();
            store.AddNetworkEntry(TestHelpers.CreateNetworkEntry("https://example.com/1"));
            store.AddNetworkEntry(TestHelpers.CreateNetworkEntry("https://example.com/2"));
            store.AddNetworkEntry(TestHelpers.CreateNetworkEntry("https://example.com/3"));

            // Act
            var result = store.GetNetworkEntries();

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public void GetNetworkEntries_WithPagination_ReturnsSubset()
        {
            // Arrange
            var store = new InMemoryStore();
            for (var i = 1; i <= 10; i++)
                store.AddNetworkEntry(TestHelpers.CreateNetworkEntry($"https://example.com/{i}"));

            // Act
            var result = store.GetNetworkEntries(2, 3);

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public void ClearNetworkEntries_RemovesAllEntries()
        {
            // Arrange
            var store = new InMemoryStore();
            var entry = TestHelpers.CreateNetworkEntry();
            store.AddNetworkEntry(entry);

            // Act
            store.ClearNetworkEntries();

            // Assert
            store.NetworkEntryCount.Should().Be(0);
            store.GetNetworkEntry(entry.Id).Should().BeNull();
        }

        [Fact]
        public void NetworkIndex_RebuildOnOverflow_MaintainsIntegrity()
        {
            // Arrange
            var store = new InMemoryStore(5, 100, 100);

            // Add more entries than capacity * 2 to trigger index rebuild
            for (var i = 0; i < 15; i++)
                store.AddNetworkEntry(TestHelpers.CreateNetworkEntry($"https://example.com/{i}"));

            // Act
            var entries = store.GetNetworkEntries();

            // Assert
            store.NetworkEntryCount.Should().Be(5);
            foreach (var entry in entries)
            {
                var retrieved = store.GetNetworkEntry(entry.Id);
                retrieved.Should().NotBeNull();
                retrieved.Id.Should().Be(entry.Id);
            }
        }

        #endregion

        #region Performance Entry Tests

        [Fact]
        public void AddPerformanceEntry_ValidEntry_IncreasesCount()
        {
            // Arrange
            var store = new InMemoryStore();
            var entry = TestHelpers.CreatePerformanceEntry();

            // Act
            store.AddPerformanceEntry(entry);

            // Assert
            store.PerformanceEntryCount.Should().Be(1);
        }

        [Fact]
        public void AddPerformanceEntry_NullEntry_ThrowsArgumentNullException()
        {
            // Arrange
            var store = new InMemoryStore();

            // Act
            Action act = () => store.AddPerformanceEntry(null);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("entry");
        }

        [Fact]
        public void GetPerformanceEntries_ReturnsAllEntries()
        {
            // Arrange
            var store = new InMemoryStore();
            store.AddPerformanceEntry(TestHelpers.CreatePerformanceEntry(10.0));
            store.AddPerformanceEntry(TestHelpers.CreatePerformanceEntry(20.0));
            store.AddPerformanceEntry(TestHelpers.CreatePerformanceEntry(30.0));

            // Act
            var result = store.GetPerformanceEntries();

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public void GetPerformanceEntries_WithPagination_ReturnsSubset()
        {
            // Arrange
            var store = new InMemoryStore();
            for (var i = 0; i < 10; i++)
                store.AddPerformanceEntry(TestHelpers.CreatePerformanceEntry(i * 10.0));

            // Act
            var result = store.GetPerformanceEntries(2, 3);

            // Assert
            result.Should().HaveCount(3);
        }

        [Fact]
        public void ClearPerformanceEntries_RemovesAllEntries()
        {
            // Arrange
            var store = new InMemoryStore();
            store.AddPerformanceEntry(TestHelpers.CreatePerformanceEntry());
            store.AddPerformanceEntry(TestHelpers.CreatePerformanceEntry());

            // Act
            store.ClearPerformanceEntries();

            // Assert
            store.PerformanceEntryCount.Should().Be(0);
        }

        #endregion

        #region Application Info Tests

        [Fact]
        public void SetApplicationInfo_ValidInfo_StoresInfo()
        {
            // Arrange
            var store = new InMemoryStore();
            var info = TestHelpers.CreateApplicationInfo();

            // Act
            store.SetApplicationInfo(info);

            // Assert
            var result = store.GetApplicationInfo();
            result.Should().NotBeNull();
            result.Should().BeSameAs(info);
        }

        [Fact]
        public void GetApplicationInfo_NotSet_ReturnsNull()
        {
            // Arrange
            var store = new InMemoryStore();

            // Act
            var result = store.GetApplicationInfo();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void SetApplicationInfo_Overwrites_PreviousInfo()
        {
            // Arrange
            var store = new InMemoryStore();
            var info1 = TestHelpers.CreateApplicationInfo("App1");
            var info2 = TestHelpers.CreateApplicationInfo("App2");

            // Act
            store.SetApplicationInfo(info1);
            store.SetApplicationInfo(info2);

            // Assert
            var result = store.GetApplicationInfo();
            result.AssemblyName.Should().Be("App2");
        }

        #endregion

        #region ClearAll Tests

        [Fact]
        public void ClearAll_RemovesAllEntries()
        {
            // Arrange
            var store = new InMemoryStore();
            store.AddConsoleEntry(TestHelpers.CreateConsoleEntry());
            store.AddNetworkEntry(TestHelpers.CreateNetworkEntry());
            store.AddPerformanceEntry(TestHelpers.CreatePerformanceEntry());

            // Act
            store.ClearAll();

            // Assert
            store.ConsoleEntryCount.Should().Be(0);
            store.NetworkEntryCount.Should().Be(0);
            store.PerformanceEntryCount.Should().Be(0);
        }

        #endregion
    }
}
