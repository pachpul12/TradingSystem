public class Logger
{
    private static Logger _instance;

    public static Logger Instance => _instance ??= new Logger();

    private Logger() { }

    public void LogInfo(string message) => Console.WriteLine($"INFO: {message}");
}
