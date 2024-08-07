using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Utils;
using Moq;
using Moq.Protected;
using Xunit;

namespace MarcusW.VncClient.Tests.Utils;

public class BackgroundThreadTests
{
    [Fact]
    public Task Cancels_ThreadWorker()
    {
        var thread = new CancellableThread();
        thread.Start();

        // Stop thread, should not throw..
        return thread.StopAndWaitAsync();
    }

    [Fact]
    public void Raises_Event_On_Failure()
    {
        Mock<BackgroundThread> mock = new(MockBehavior.Strict, "Test Thread") { CallBase = true };

        // Setup thread worker that throws an exception.
        mock.Protected().Setup("ThreadWorker", ItExpr.IsAny<CancellationToken>()).Throws<Exception>();

        Assert.Raises<BackgroundThreadFailedEventArgs>(handler => mock.Object.Failed += handler,
            handler => mock.Object.Failed -= handler, () => {
                // Call start method.
                typeof(BackgroundThread).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(
                    mock.Object, []);

                // Ensure the thread has started.
                Thread.Sleep(1000);
            });
    }

    [Fact]
    public void Starts_ThreadWorker()
    {
        Mock<BackgroundThread> mock = new(MockBehavior.Strict, "Test Thread") { CallBase = true };

        // Call start method.
        typeof(BackgroundThread).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(mock.Object,
            []);

        // Ensure the thread has started.
        Thread.Sleep(1000);

        // Verify thread worker was called.
        mock.Protected().Verify("ThreadWorker", Times.Exactly(1), ItExpr.IsAny<CancellationToken>());
    }

    private class CancellableThread : BackgroundThread
    {
        public new void Start() => base.Start();

        public new Task StopAndWaitAsync() => base.StopAndWaitAsync();

        protected override async Task ThreadWorker(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
                await Task.Delay(10);
        }
    }
}
