using RaceDay.Api.Security;

namespace RaceDay.Tests;

public class PasswordServiceTests
{
    [Fact]
    public void Hash_IsNotThePlainTextPassword()
    {
        var hash = PasswordService.Hash("Password123!");
        Assert.NotEqual("Password123!", hash);
        Assert.DoesNotContain("Password123!", hash);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = PasswordService.Hash("Password123!");
        Assert.True(PasswordService.Verify("Password123!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = PasswordService.Hash("Password123!");
        Assert.False(PasswordService.Verify("WrongPassword!", hash));
    }

    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentHashes()
    {
        Assert.NotEqual(PasswordService.Hash("Password123!"), PasswordService.Hash("Password123!"));
    }

    [Theory]
    [InlineData("HASHED_PWD_1")]
    [InlineData("")]
    [InlineData("a.b.c")]
    public void Verify_MalformedStoredHash_ReturnsFalseInsteadOfThrowing(string stored)
    {
        Assert.False(PasswordService.Verify("Password123!", stored));
    }
}
