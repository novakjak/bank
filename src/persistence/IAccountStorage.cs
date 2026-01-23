public interface IAccountStorage
{
    bool IsAvailable();
    void Load();
    void Save();

    long OpenAccount(string bankCode);
    void Remove(long account, string bankCode);

    void Deposit(long account, string bankCode, long amount);
    void Withdraw(long account, string bankCode, long amount);

    long Balance(long account, string bankCode);
    long TotalBalance();
    long AccountCount();

    string StrategyName { get; }

    IEnumerable<AccountRecord> Dump();
    void Restore(IEnumerable<AccountRecord> data);
}
