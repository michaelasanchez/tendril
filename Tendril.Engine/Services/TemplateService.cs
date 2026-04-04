using Fluid;
using Fluid.Values;
using System.Text.RegularExpressions;
using Tendril.Engine.Interfaces;

public class TemplateService : ITemplateService
{
    private static readonly FluidParser _parser = new();
    private readonly TemplateOptions _options;

    public TemplateService()
    {
        _options = new TemplateOptions();

        // Allow Fluid to read the dictionary keys naturally
        _options.MemberAccessStrategy.Register<Dictionary<string, string>>();
        _options.MemberAccessStrategy.Register<Dictionary<string, object>>();

        // ADD THIS: A generic Regex filter for "one-off" logic in the DB
        // Usage in DB: {{ Parent.Title | regex_replace: '.* Presents ', '' }}
        _options.Filters.AddFilter("regex_replace", (input, arguments, _) =>
        {
            var pattern = arguments.At(0).ToStringValue();
            var replacement = arguments.At(1).ToStringValue();
            var val = input.ToStringValue();

            return new StringValue(Regex.Replace(val, pattern, replacement, RegexOptions.IgnoreCase));
        });
    }

    public string Render(string template, Dictionary<string, object> context)
    {
        if (string.IsNullOrWhiteSpace(template)) return string.Empty;

        if (!_parser.TryParse(template, out var fluidTemplate, out var error))
            return template;

        var templateContext = new TemplateContext(context, _options);
        return fluidTemplate.Render(templateContext);
    }
}