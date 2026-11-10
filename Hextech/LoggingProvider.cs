using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Hextech;

public static class LoggingProvider
{
    private static ILoggerFactory _factory;

    static LoggingProvider()
    {
        _factory = LoggerFactory.Create(builder => builder
#if DEBUG
            .SetMinimumLevel(LogLevel.Trace)
#endif
            .AddConsole());
    }
    
    public static ILogger<T> NewLogger<T>()
    {
        return _factory.CreateLogger<T>();
    }
    
    public static ILogger NewLogger(string name)
    {
        return _factory.CreateLogger(name);
    }
    public static string GetLogFor(object target)
    {
        var properties =
            from property in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            select new
            {
                Name = property.Name,
                Value = property.GetValue(target, null)
            };

        var builder = new StringBuilder();

        foreach(var property in properties)
        {
            var val = property.Value;

            builder
                .Append(property.Name)
                .Append(" = ")
                .Append(val)
                .AppendLine();
        }

        return builder.ToString();
    }
}