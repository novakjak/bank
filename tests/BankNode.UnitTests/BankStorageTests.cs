using Xunit;

public class BankStorageTests
{
    private const string BANK = "TEST";

    [Fact]
    public void OpenAccount_ReturnsUniqueAccounts()
    {
        var s = new InMemoryAccountStorage();

        var a1 = s.OpenAccount(BANK);
        var a2 = s.OpenAccount(BANK);

        Assert.NotEqual(a1, a2);
    }

    [Fact]
    public void Deposit_IncreasesBalance()
    {
        var s = new InMemoryAccountStorage();
        var acc = s.OpenAccount(BANK);

        s.Deposit(acc, BANK, 100);

        Assert.Equal(100, s.Balance(acc, BANK));
    }

    [Fact]
    public void Withdraw_DecreasesBalance()
    {
        var s = new InMemoryAccountStorage();
        var acc = s.OpenAccount(BANK);

        s.Deposit(acc, BANK, 200);
        s.Withdraw(acc, BANK, 50);

        Assert.Equal(150, s.Balance(acc, BANK));
    }
}
