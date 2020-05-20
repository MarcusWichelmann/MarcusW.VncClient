using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Utils;
using Moq;
using Moq.Protected;
using Xunit;

namespace MarcusW.VncClient.Tests.Utils
{
    public class BackgroundThreadTests
    {
        [Fact]
        public void Starts_ThreadWorker()
        {
            var mock = new Mock<BackgroundThread>(MockBehavior.Strict, "Test Thread") { CallBase = true };

            // Call start method.
            typeof(BackgroundThread).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(
                mock.Object, new object[] { CancellationToken.None });

            // Ensure the thread has started.
            Thread.Sleep(1000);

            // Verify thread worker was called.
            mock.Protected().Verify("ThreadWorker", Times.Exactly(1), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public void Raises_Event_On_Failure()
        {
            var mock = new Mock<BackgroundThread>(MockBehavior.Strict, "Test Thread") { CallBase = true };

            // Setup thread worker that throws an exception.
            mock.Protected().Setup("ThreadWorker", ItExpr.IsAny<CancellationToken>()).Throws<Exception>();

            Assert.Raises<BackgroundThreadFailedEventArgs>(handler => mock.Object.Failed += handler,
                handler => mock.Object.Failed -= handler, () => {
                    // Call start method.
                    typeof(BackgroundThread).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(
                        mock.Object, new object[] { CancellationToken.None });

                    // Ensure the thread has started.
                    Thread.Sleep(1000);
                });
        }

        [Fact]
        public Task Cancels_ThreadWorker()
        {
            var thread = new CancellableThread();
            thread.Start();

            // Stop thread, should not throw..
            return thread.StopAndWaitAsync();
        }

        private class CancellableThread : BackgroundThread
        {
            public CancellableThread() : base("Cancellable Thread") { }

            public new void Start(CancellationToken cancellationToken = default) => base.Start();

            public new Task StopAndWaitAsync() => base.StopAndWaitAsync();

            protected override void ThreadWorker(CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                    Thread.Sleep(10);
            }
        }
    }
}
