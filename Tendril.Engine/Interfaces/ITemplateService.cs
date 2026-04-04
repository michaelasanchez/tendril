namespace Tendril.Engine.Interfaces;

public interface ITemplateService
{
    public string Render(string template, Dictionary<string, object> context);
}
