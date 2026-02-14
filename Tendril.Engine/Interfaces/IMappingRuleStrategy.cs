using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;

namespace Tendril.Engine.Abstractions;

public interface IMappingRuleStrategy
{
    TransformType StrategyType { get; }

    object? Apply(ScraperMappingRule config, object? primary, object? secondary);
}