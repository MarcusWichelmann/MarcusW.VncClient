using System.Reactive.Subjects;

namespace WpfVncClient.Logging;

public class ReactiveLog
{
    public Subject<string> Subject { get; set; } = new();

    public void Log(string msg)
    {
        Subject.OnNext(msg);
    }
}
