using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Parsing;

namespace DefaultNamespace;

/// <summary>
/// An <see cref="ITextFormatter"/> that writes events in a compact JSON format.
/// </summary>
public class CompactJsonFormatter : ITextFormatter
{
    private readonly JsonValueFormatter _valueFormatter;

    /// <summary>
    /// Construct a <see cref="CompactJsonFormatter"/>, optionally supplying a formatter for
    /// <see cref="LogEventPropertyValue"/>s on the event.
    /// </summary>
    /// <param name="valueFormatter">A value formatter, or null.</param>
    public CompactJsonFormatter(JsonValueFormatter valueFormatter = null)
    {
        _valueFormatter = valueFormatter ?? new JsonValueFormatter(typeTagName: "$type");
    }

    /// <summary>
    /// Format the log event into the output. Subsequent events will be newline-delimited.
    /// </summary>
    /// <param name="logEvent">The event to format.</param>
    /// <param name="output">The output.</param>
    public void Format(LogEvent logEvent, TextWriter output)
    {
        FormatEvent(logEvent, output, _valueFormatter);
        output.WriteLine();
    }

    /// <summary>
    /// Format the log event into the output.
    /// </summary>
    /// <param name="logEvent">The event to format.</param>
    /// <param name="output">The output.</param>
    /// <param name="valueFormatter">A value formatter for <see cref="LogEventPropertyValue"/>s on the event.</param>
    public static void FormatEvent(LogEvent logEvent, TextWriter output, JsonValueFormatter valueFormatter)
    {
        if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
        if (output == null) throw new ArgumentNullException(nameof(output));
        if (valueFormatter == null) throw new ArgumentNullException(nameof(valueFormatter));

        output.Write("{\"Datetime\":\"");
        output.Write(DateTimeOffset.FromUnixTimeMilliseconds(logEvent.Timestamp.ToUnixTimeMilliseconds()).ToLocalTime()
            .ToString("F"));

        output.Write("\",\"Data\":");
        output.Write('{');

        output.Write("\"MessageTemplate\":");
        JsonValueFormatter.WriteQuotedJsonString(logEvent.MessageTemplate.Text, output);

        foreach (var property in logEvent.Properties)
        {
            var name = property.Key;
            if (name.Length > 0 && name[0] == '@')
            {
                // Escape first '@' by doubling
                name = '@' + name;
            }

            output.Write(',');
            JsonValueFormatter.WriteQuotedJsonString(name, output);
            output.Write(':');
            valueFormatter.Format(property.Value, output);
        }

        IEnumerable<PropertyToken> tokensWithFormat = logEvent.MessageTemplate.Tokens
            .OfType<PropertyToken>()
            .Where(propertyToken => propertyToken.Format != null);

        // Better not to allocate an array in the 99.9% of cases where this is false
        // ReSharper disable once PossibleMultipleEnumeration
        if (tokensWithFormat.Any())
        {
            output.Write(",\"@r\":[");
            var delim = "";
            foreach (var r in tokensWithFormat)
            {
                output.Write(delim);
                delim = ",";
                var space = new StringWriter();
                r.Render(logEvent.Properties, space);
                JsonValueFormatter.WriteQuotedJsonString(space.ToString(), output);
            }

            output.Write(']');
        }

        if (logEvent.Level != LogEventLevel.Information)
        {
            output.Write(",\"LogLevel\":\"");
            output.Write(logEvent.Level);
            output.Write('\"');
        }

        if (logEvent.Exception != null)
        {
            output.Write(",\"Exception\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
        }

        output.Write('}');
        output.Write('}');
        output.Write(',');
    }
}