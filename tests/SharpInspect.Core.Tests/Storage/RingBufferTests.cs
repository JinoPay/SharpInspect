using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using SharpInspect.Core.Storage;
using Xunit;

namespace SharpInspect.Core.Tests.Storage
{
    /// <summary>
    ///     RingBuffer 클래스의 단위 테스트.
    /// </summary>
    public class RingBufferTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidCapacity_SetsCapacity()
        {
            // Arrange & Act
            var buffer = new RingBuffer<int>(10);

            // Assert
            buffer.Capacity.Should().Be(10);
            buffer.Count.Should().Be(0);
        }

        [Fact]
        public void Constructor_WithZeroCapacity_ThrowsArgumentOutOfRangeException()
        {
            // Arrange & Act
            Action act = () => new RingBuffer<int>(0);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            // Arrange & Act
            Action act = () => new RingBuffer<int>(-1);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        #endregion

        #region Add Tests

        [Fact]
        public void Add_SingleItem_IncreasesCount()
        {
            // Arrange
            var buffer = new RingBuffer<int>(5);

            // Act
            buffer.Add(1);

            // Assert
            buffer.Count.Should().Be(1);
        }

        [Fact]
        public void Add_MultipleItems_IncreasesCount()
        {
            // Arrange
            var buffer = new RingBuffer<int>(10);

            // Act
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            // Assert
            buffer.Count.Should().Be(3);
        }

        [Fact]
        public void Add_ExceedsCapacity_OverwritesOldest()
        {
            // Arrange
            var buffer = new RingBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            // Act
            buffer.Add(4);

            // Assert
            buffer.Count.Should().Be(3);
            var items = buffer.GetAll();
            items.Should().BeEquivalentTo(new[] { 2, 3, 4 });
        }

        [Fact]
        public void Add_ExceedsCapacityTwice_MaintainsCorrectOrder()
        {
            // Arrange
            var buffer = new RingBuffer<int>(3);

            // Act
            for (var i = 1; i <= 9; i++)
                buffer.Add(i);

            // Assert
            buffer.Count.Should().Be(3);
            var items = buffer.GetAll();
            items.Should().BeEquivalentTo(new[] { 7, 8, 9 });
        }

        #endregion

        #region GetAll Tests

        [Fact]
        public void GetAll_EmptyBuffer_ReturnsEmptyArray()
        {
            // Arrange
            var buffer = new RingBuffer<int>(5);

            // Act
            var result = buffer.GetAll();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetAll_PartiallyFilled_ReturnsAllItems()
        {
            // Arrange
            var buffer = new RingBuffer<int>(10);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            // Act
            var result = buffer.GetAll();

            // Assert
            result.Should().HaveCount(3);
            result.Should().ContainInOrder(1, 2, 3);
        }

        [Fact]
        public void GetAll_FullBuffer_ReturnsAllItems()
        {
            // Arrange
            var buffer = new RingBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            // Act
            var result = buffer.GetAll();

            // Assert
            result.Should().HaveCount(3);
            result.Should().ContainInOrder(1, 2, 3);
        }

        [Fact]
        public void GetAll_OverflowedBuffer_ReturnsCorrectItems()
        {
            // Arrange
            var buffer = new RingBuffer<int>(3);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.Add(5);

            // Act
            var result = buffer.GetAll();

            // Assert
            result.Should().HaveCount(3);
            result.Should().ContainInOrder(3, 4, 5);
        }

        #endregion

        #region GetRange Tests

        [Fact]
        public void GetRange_ValidOffsetAndLimit_ReturnsSubset()
        {
            // Arrange
            var buffer = new RingBuffer<int>(10);
            for (var i = 1; i <= 5; i++)
                buffer.Add(i);

            // Act
            var result = buffer.GetRange(1, 2);

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainInOrder(2, 3);
        }

        [Fact]
        public void GetRange_OffsetExceedsCount_ReturnsEmptyArray()
        {
            // Arrange
            var buffer = new RingBuffer<int>(10);
            buffer.Add(1);
            buffer.Add(2);

            // Act
            var result = buffer.GetRange(5, 2);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetRange_LimitZero_ReturnsAllFromOffset()
        {
            // Arrange
            var buffer = new RingBuffer<int>(10);
            for (var i = 1; i <= 5; i++)
                buffer.Add(i);

            // Act
            var result = buffer.GetRange(2, 0);

            // Assert
            result.Should().HaveCount(3);
            result.Should().ContainInOrder(3, 4, 5);
        }

        [Fact]
        public void GetRange_LimitExceedsAvailable_ReturnsAvailable()
        {
            // Arrange
            var buffer = new RingBuffer<int>(10);
            for (var i = 1; i <= 5; i++)
                buffer.Add(i);

            // Act
            var result = buffer.GetRange(3, 100);

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainInOrder(4, 5);
        }

        [Fact]
        public void GetRange_AfterOverflow_ReturnsCorrectItems()
        {
            // Arrange
            var buffer = new RingBuffer<int>(5);
            for (var i = 1; i <= 8; i++)
                buffer.Add(i);

            // Act
            var result = buffer.GetRange(1, 3);

            // Assert
            result.Should().HaveCount(3);
            result.Should().ContainInOrder(5, 6, 7);
        }

        #endregion

        #region TryGetLatest Tests

        [Fact]
        public void TryGetLatest_EmptyBuffer_ReturnsFalse()
        {
            // Arrange
            var buffer = new RingBuffer<int>(5);

            // Act
            var result = buffer.TryGetLatest(out var item);

            // Assert
            result.Should().BeFalse();
            item.Should().Be(default(int));
        }

        [Fact]
        public void TryGetLatest_SingleItem_ReturnsItem()
        {
            // Arrange
            var buffer = new RingBuffer<int>(5);
            buffer.Add(42);

            // Act
            var result = buffer.TryGetLatest(out var item);

            // Assert
            result.Should().BeTrue();
            item.Should().Be(42);
        }

        [Fact]
        public void TryGetLatest_MultipleItems_ReturnsLastAdded()
        {
            // Arrange
            var buffer = new RingBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            // Act
            var result = buffer.TryGetLatest(out var item);

            // Assert
            result.Should().BeTrue();
            item.Should().Be(3);
        }

        [Fact]
        public void TryGetLatest_AfterOverflow_ReturnsLastAdded()
        {
            // Arrange
            var buffer = new RingBuffer<int>(3);
            for (var i = 1; i <= 10; i++)
                buffer.Add(i);

            // Act
            var result = buffer.TryGetLatest(out var item);

            // Assert
            result.Should().BeTrue();
            item.Should().Be(10);
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_EmptyBuffer_RemainsEmpty()
        {
            // Arrange
            var buffer = new RingBuffer<int>(5);

            // Act
            buffer.Clear();

            // Assert
            buffer.Count.Should().Be(0);
        }

        [Fact]
        public void Clear_FilledBuffer_ResetsToEmpty()
        {
            // Arrange
            var buffer = new RingBuffer<int>(5);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            // Act
            buffer.Clear();

            // Assert
            buffer.Count.Should().Be(0);
            buffer.GetAll().Should().BeEmpty();
        }

        [Fact]
        public void Clear_ReleasesReferences()
        {
            // Arrange
            var buffer = new RingBuffer<string>(3);
            buffer.Add("one");
            buffer.Add("two");
            buffer.Add("three");

            // Act
            buffer.Clear();

            // Assert
            buffer.Count.Should().Be(0);
            buffer.TryGetLatest(out var item).Should().BeFalse();
            item.Should().BeNull();
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public void ThreadSafety_ConcurrentAdds_NoDataCorruption()
        {
            // Arrange
            var buffer = new RingBuffer<int>(1000);
            var itemsPerThread = 100;
            var threadCount = 10;

            // Act
            Parallel.For(0, threadCount, threadIndex =>
            {
                for (var i = 0; i < itemsPerThread; i++)
                    buffer.Add(threadIndex * itemsPerThread + i);
            });

            // Assert
            buffer.Count.Should().Be(1000);
        }

        [Fact]
        public async Task ThreadSafety_ConcurrentReadsAndWrites_NoExceptions()
        {
            // Arrange
            var buffer = new RingBuffer<int>(100);
            var cts = new CancellationTokenSource();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Act
            var writeTask = Task.Run(async () =>
            {
                try
                {
                    for (var i = 0; i < 1000 && !cts.Token.IsCancellationRequested; i++)
                    {
                        buffer.Add(i);
                        await Task.Yield();
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            var readTask = Task.Run(async () =>
            {
                try
                {
                    for (var i = 0; i < 1000 && !cts.Token.IsCancellationRequested; i++)
                    {
                        _ = buffer.GetAll();
                        _ = buffer.Count;
                        buffer.TryGetLatest(out _);
                        await Task.Yield();
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            await Task.WhenAll(writeTask, readTask);
            cts.Cancel();

            // Assert
            exceptions.Should().BeEmpty();
        }

        #endregion
    }
}
