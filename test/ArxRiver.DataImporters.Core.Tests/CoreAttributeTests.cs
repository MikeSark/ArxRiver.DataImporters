using System.Reflection;
using ArxRiver.DataImporters.Core.Attributes;

namespace ArxRiver.DataImporters.Core.Tests;

public class CoreAttributeTests
{
    [Fact]
    public void ValidatorAttribute_StoresType()
    {
        var attr = new ValidatorAttribute(typeof(string));

        Assert.Equal(typeof(string), attr.ValidatorType);
    }

    [Fact]
    public void ValidatorAttribute_RuleName_DefaultsToNull()
    {
        var attr = new ValidatorAttribute(typeof(string));

        Assert.Null(attr.RuleName);
    }

    [Fact]
    public void ValidatorAttribute_RuleName_CanBeSet()
    {
        var attr = new ValidatorAttribute(typeof(string)) { RuleName = "Custom" };

        Assert.Equal("Custom", attr.RuleName);
    }

    [Fact]
    public void InlineValidationAttribute_StoresExpression()
    {
        var attr = new InlineValidationAttribute("Row.Age > 0");

        Assert.Equal("Row.Age > 0", attr.Expression);
    }

    [Fact]
    public void InlineValidationAttribute_OptionalProperties_DefaultToNull()
    {
        var attr = new InlineValidationAttribute("Row.Age > 0");

        Assert.Null(attr.ErrorMessage);
        Assert.Null(attr.RuleName);
    }

    [Fact]
    public void InlineValidationAttribute_OptionalProperties_CanBeSet()
    {
        var attr = new InlineValidationAttribute("Row.Age > 0")
        {
            ErrorMessage = "Must be positive",
            RuleName = "PositiveAge"
        };

        Assert.Equal("Must be positive", attr.ErrorMessage);
        Assert.Equal("PositiveAge", attr.RuleName);
    }

    [Fact]
    public void ValidatorAttribute_AllowsMultipleOnClassAndProperty()
    {
        var usage = typeof(ValidatorAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        Assert.NotNull(usage);
        Assert.True(usage!.AllowMultiple);
        Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Class));
        Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Property));
    }

    [Fact]
    public void InlineValidationAttribute_AllowsMultipleOnClassAndProperty()
    {
        var usage = typeof(InlineValidationAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        Assert.NotNull(usage);
        Assert.True(usage!.AllowMultiple);
        Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Class));
        Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Property));
    }

    [Fact]
    public void ValidatorAttribute_ValidOn_IncludesPropertyAndClass()
    {
        var usage = typeof(ValidatorAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Property | AttributeTargets.Class, usage!.ValidOn);
    }
}
