using PixHub.Accounts.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace PixHub.Accounts.Domain.Tests.ValueObjects;

public class CpfTests
{
    [Theory]
    [InlineData("111.444.777-35")]
    [InlineData("11144477735")]
    [InlineData("529.982.247-25")]
    public void Create_WithValidCpf_Succeeds(string input)
    {
        var result = Cpf.Create(input);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.Length.ShouldBe(11);
    }

    [Fact]
    public void Create_NormalizesPunctuation()
    {
        Cpf.Create("111.444.777-35").Value.Value.ShouldBe("11144477735");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyInput_FailsWithEmpty(string? input)
    {
        Cpf.Create(input).Error.ShouldBe(CpfErrors.Empty);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("111444777356")]
    public void Create_WithWrongLength_FailsWithInvalidLength(string input)
    {
        Cpf.Create(input).Error.ShouldBe(CpfErrors.InvalidLength);
    }

    [Theory]
    [InlineData("111.444.777-00")] // wrong check digits
    [InlineData("111.111.111-11")] // repeated digits
    [InlineData("000.000.000-00")]
    public void Create_WithInvalidCheckDigits_Fails(string input)
    {
        Cpf.Create(input).Error.ShouldBe(CpfErrors.InvalidCheckDigits);
    }

    [Fact]
    public void Formatted_ReturnsPunctuatedForm()
    {
        Cpf.Create("11144477735").Value.Formatted.ShouldBe("111.444.777-35");
    }

    [Fact]
    public void Masked_HidesMiddleDigitsForPii()
    {
        Cpf.Create("11144477735").Value.Masked.ShouldBe("111.***.***-35");
    }
}
