using EmsTcpServer.Services;
using NUnit.Framework;

namespace EmsTcpServer.Tests;

[TestFixture]
public class MarkingCodeGeneratorTests
{
    private MarkingCodeGenerator _generator = null!;

    [SetUp]
    public void SetUp()
    {
        _generator = new MarkingCodeGenerator();
    }

    [Test]
    public void Generate_ShouldReturnExpectedLength()
    {
        var code = _generator.Generate();
        Assert.That(code, Has.Length.EqualTo(28));
    }

    [Test]
    public void Generate_ShouldStartWithGtinApplicationIdentifier()
    {
        var code = _generator.Generate();
        Assert.That(code, Does.StartWith("01"));
    }

    [Test]
    public void Generate_ShouldContainSerialApplicationIdentifier()
    {
        var code = _generator.Generate();

        Assert.That(code.Substring(16, 2), Is.EqualTo("21"));
    }

    [Test]
    public void Generate_ShouldGenerateDifferentCodes()
    {
        var codes = Enumerable
            .Range(0, 100)
            .Select(_ => _generator.Generate())
            .ToList();

        Assert.That(
            codes.Distinct().Count(),
            Is.EqualTo(100));
    }
}
