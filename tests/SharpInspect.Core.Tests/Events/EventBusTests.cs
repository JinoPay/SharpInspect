using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using SharpInspect.Core.Events;
using Xunit;

// SharpInspect.Core.Events.EventHandler<T>와 System.EventHandler<T> 충돌 방지
using EventHandler = SharpInspect.Core.Events.EventHandler<SharpInspect.Core.Tests.TestEvent>;
using AnotherEventHandler = SharpInspect.Core.Events.EventHandler<SharpInspect.Core.Tests.AnotherTestEvent>;

namespace SharpInspect.Core.Tests.Events
{
    /// <summary>
    ///     EventBus 클래스의 단위 테스트.
    /// </summary>
    public class EventBusTests
    {
        #region Singleton Tests

        [Fact]
        public void Instance_ReturnsSingleton()
        {
            // Arrange & Act
            var instance1 = EventBus.Instance;
            var instance2 = EventBus.Instance;

            // Assert
            instance1.Should().BeSameAs(instance2);
        }

        [Fact]
        public void Constructor_CreatesNewInstance()
        {
            // Arrange & Act
            var bus1 = new EventBus();
            var bus2 = new EventBus();

            // Assert
            bus1.Should().NotBeSameAs(bus2);
        }

        #endregion

        #region Subscribe Tests

        [Fact]
        public void Subscribe_ValidHandler_ReturnsSubscription()
        {
            // Arrange
            var bus = new EventBus();
            EventHandler handler = evt => { };

            // Act
            var subscription = bus.Subscribe(handler);

            // Assert
            subscription.Should().NotBeNull();
            subscription.Should().BeAssignableTo<IDisposable>();
        }

        [Fact]
        public void Subscribe_NullHandler_ThrowsArgumentNullException()
        {
            // Arrange
            var bus = new EventBus();

            // Act
            Action act = () => bus.Subscribe<TestEvent>(null);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
        }

        [Fact]
        public void Subscribe_MultipleHandlers_AllRegistered()
        {
            // Arrange
            var bus = new EventBus();
            EventHandler handler1 = evt => { };
            EventHandler handler2 = evt => { };
            EventHandler handler3 = evt => { };

            // Act
            bus.Subscribe(handler1);
            bus.Subscribe(handler2);
            bus.Subscribe(handler3);

            // Assert
            bus.GetSubscriberCount<TestEvent>().Should().Be(3);
        }

        [Fact]
        public void Subscribe_SameHandlerTwice_BothRegistered()
        {
            // Arrange
            var bus = new EventBus();
            EventHandler handler = evt => { };

            // Act
            bus.Subscribe(handler);
            bus.Subscribe(handler);

            // Assert
            bus.GetSubscriberCount<TestEvent>().Should().Be(2);
        }

        #endregion

        #region Publish Tests

        [Fact]
        public void Publish_WithSubscribers_InvokesAllHandlers()
        {
            // Arrange
            var bus = new EventBus();
            var invokedMessages = new List<string>();

            bus.Subscribe<TestEvent>(evt => invokedMessages.Add($"Handler1: {evt.Message}"));
            bus.Subscribe<TestEvent>(evt => invokedMessages.Add($"Handler2: {evt.Message}"));

            // Act
            bus.Publish(new TestEvent("Hello"));

            // Assert
            invokedMessages.Should().HaveCount(2);
            invokedMessages.Should().Contain("Handler1: Hello");
            invokedMessages.Should().Contain("Handler2: Hello");
        }

        [Fact]
        public void Publish_NoSubscribers_DoesNotThrow()
        {
            // Arrange
            var bus = new EventBus();

            // Act
            Action act = () => bus.Publish(new TestEvent("Hello"));

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Publish_NullEvent_DoesNotThrow()
        {
            // Arrange
            var bus = new EventBus();
            var invoked = false;
            bus.Subscribe<TestEvent>(evt => invoked = true);

            // Act
            Action act = () => bus.Publish<TestEvent>(null);

            // Assert
            act.Should().NotThrow();
            invoked.Should().BeFalse();
        }

        [Fact]
        public void Publish_HandlerThrowsException_ContinuesToOtherHandlers()
        {
            // Arrange
            var bus = new EventBus();
            var handler2Invoked = false;
            var handler3Invoked = false;

            bus.Subscribe<TestEvent>(evt => throw new InvalidOperationException("Test exception"));
            bus.Subscribe<TestEvent>(evt => handler2Invoked = true);
            bus.Subscribe<TestEvent>(evt => handler3Invoked = true);

            // Act
            bus.Publish(new TestEvent("Hello"));

            // Assert
            handler2Invoked.Should().BeTrue();
            handler3Invoked.Should().BeTrue();
        }

        [Fact]
        public void Publish_DifferentEventTypes_OnlyMatchingHandlersInvoked()
        {
            // Arrange
            var bus = new EventBus();
            var testEventInvoked = false;
            var anotherEventInvoked = false;

            bus.Subscribe<TestEvent>(evt => testEventInvoked = true);
            bus.Subscribe<AnotherTestEvent>(evt => anotherEventInvoked = true);

            // Act
            bus.Publish(new TestEvent("Hello"));

            // Assert
            testEventInvoked.Should().BeTrue();
            anotherEventInvoked.Should().BeFalse();
        }

        #endregion

        #region Unsubscribe Tests

        [Fact]
        public void Unsubscribe_ValidHandler_RemovesHandler()
        {
            // Arrange
            var bus = new EventBus();
            EventHandler handler = evt => { };
            bus.Subscribe(handler);

            // Act
            bus.Unsubscribe(handler);

            // Assert
            bus.GetSubscriberCount<TestEvent>().Should().Be(0);
        }

        [Fact]
        public void Unsubscribe_NullHandler_DoesNotThrow()
        {
            // Arrange
            var bus = new EventBus();

            // Act
            Action act = () => bus.Unsubscribe<TestEvent>(null);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Unsubscribe_NotSubscribedHandler_DoesNotThrow()
        {
            // Arrange
            var bus = new EventBus();
            EventHandler handler = evt => { };

            // Act
            Action act = () => bus.Unsubscribe(handler);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region Subscription Dispose Tests

        [Fact]
        public void SubscriptionDispose_RemovesHandler()
        {
            // Arrange
            var bus = new EventBus();
            var invoked = false;
            EventHandler handler = evt => invoked = true;
            var subscription = bus.Subscribe(handler);

            // Act
            subscription.Dispose();
            bus.Publish(new TestEvent("Hello"));

            // Assert
            invoked.Should().BeFalse();
            bus.GetSubscriberCount<TestEvent>().Should().Be(0);
        }

        [Fact]
        public void SubscriptionDispose_MultipleTimes_SafeToCall()
        {
            // Arrange
            var bus = new EventBus();
            EventHandler handler = evt => { };
            var subscription = bus.Subscribe(handler);

            // Act
            Action act = () =>
            {
                subscription.Dispose();
                subscription.Dispose();
                subscription.Dispose();
            };

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region GetSubscriberCount Tests

        [Fact]
        public void GetSubscriberCount_NoSubscribers_ReturnsZero()
        {
            // Arrange
            var bus = new EventBus();

            // Act
            var count = bus.GetSubscriberCount<TestEvent>();

            // Assert
            count.Should().Be(0);
        }

        [Fact]
        public void GetSubscriberCount_WithSubscribers_ReturnsCorrectCount()
        {
            // Arrange
            var bus = new EventBus();
            bus.Subscribe<TestEvent>(evt => { });
            bus.Subscribe<TestEvent>(evt => { });
            bus.Subscribe<AnotherTestEvent>(evt => { });

            // Act & Assert
            bus.GetSubscriberCount<TestEvent>().Should().Be(2);
            bus.GetSubscriberCount<AnotherTestEvent>().Should().Be(1);
        }

        #endregion

        #region ClearAll Tests

        [Fact]
        public void ClearAll_RemovesAllSubscriptions()
        {
            // Arrange
            var bus = new EventBus();
            bus.Subscribe<TestEvent>(evt => { });
            bus.Subscribe<TestEvent>(evt => { });
            bus.Subscribe<AnotherTestEvent>(evt => { });

            // Act
            bus.ClearAll();

            // Assert
            bus.GetSubscriberCount<TestEvent>().Should().Be(0);
            bus.GetSubscriberCount<AnotherTestEvent>().Should().Be(0);
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public async Task ThreadSafety_ConcurrentSubscribeUnsubscribe_NoException()
        {
            // Arrange
            var bus = new EventBus();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var handlers = new List<EventHandler>();

            for (var i = 0; i < 100; i++)
            {
                var index = i;
                handlers.Add(evt => { var _ = index; });
            }

            // Act
            var tasks = new List<Task>();
            foreach (var handler in handlers)
            {
                var h = handler;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var subscription = bus.Subscribe(h);
                        await Task.Delay(1);
                        subscription.Dispose();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            exceptions.Should().BeEmpty();
        }

        [Fact]
        public async Task ThreadSafety_PublishDuringSubscribe_NoException()
        {
            // Arrange
            var bus = new EventBus();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var cts = new CancellationTokenSource();

            // Act
            var subscribeTask = Task.Run(async () =>
            {
                try
                {
                    for (var i = 0; i < 100 && !cts.Token.IsCancellationRequested; i++)
                    {
                        var subscription = bus.Subscribe<TestEvent>(evt => { });
                        await Task.Delay(1);
                        subscription.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            var publishTask = Task.Run(async () =>
            {
                try
                {
                    for (var i = 0; i < 100 && !cts.Token.IsCancellationRequested; i++)
                    {
                        bus.Publish(new TestEvent($"Message {i}"));
                        await Task.Delay(1);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            await Task.WhenAll(subscribeTask, publishTask);
            cts.Cancel();

            // Assert
            exceptions.Should().BeEmpty();
        }

        #endregion
    }
}
