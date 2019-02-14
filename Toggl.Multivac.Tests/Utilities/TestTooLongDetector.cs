using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class TestTooLongDetector
{
    private readonly CancellationTokenSource source;
    private readonly CancellationToken token;
    private readonly Action reportTooLongAction;
    private readonly object accessLock = new object();

    private bool isDisposed = false;

    public static TestTooLongDetector StartDetection(TimeSpan reportAfter, Action callback)
        => new TestTooLongDetector(reportAfter, callback);

    public void CancelDetection()
    {
        lock (accessLock)
        {
            if (isDisposed)
                return;

            source.Cancel();
        }
    }

    private TestTooLongDetector(TimeSpan stuckTime, Action reportTooLongAction)
    {
        this.reportTooLongAction = reportTooLongAction;

        source = new CancellationTokenSource();
        token = source.Token;

        Task.Delay(stuckTime).ContinueWith(reportStuckTest, token);
    }

    private void reportStuckTest(Task task)
    {
        try
        {
            if (token.IsCancellationRequested)
                return;

            reportTooLongAction();
        }
        finally
        {
            lock (accessLock)
            {
                source.Dispose();
                isDisposed = true;
            }
        }
    }

}
