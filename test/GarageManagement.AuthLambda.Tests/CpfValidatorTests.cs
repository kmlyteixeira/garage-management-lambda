using GarageManagement.AuthLambda;
using Xunit;

namespace GarageManagement.AuthLambda.Tests;

public class CpfValidatorTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("11144477735", "11144477735")]
    public void NormalizeAndValidate_returns_digits_for_valid_cpf(string input, string expected)
    {
        var result = CpfValidator.NormalizeAndValidate(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("111.111.111-11")]
    [InlineData("529.982.247-26")]
    public void NormalizeAndValidate_returns_null_for_invalid_cpf(string? input)
    {
        var result = CpfValidator.NormalizeAndValidate(input);

        Assert.Null(result);
    }
}
