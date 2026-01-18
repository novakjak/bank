using System;
using System.IO;
using System.Threading.Tasks;

public class BankStorage
{
    private const long ACC_NUM_MIN = 10000;
    private const long ACC_NUM_MAX = 99999;

    public static BankStorage Storage { get => Get(); }
    
    private static BankStorage? _instance;
    private Dictionary<long, long> _accounts = new();
    private long _nextAccNumber = ACC_NUM_MIN;
    private ISet<long> _freeAccNumbers = new HashSet<long>();
    private Lock _accountsLock = new();

    private BankStorage()
    {
        Load();
    }

    public static BankStorage Get()
    {
        if (_instance is null)
            _instance = new BankStorage();
        return _instance;
    }

    public long TotalBalance() => _accounts.Values.Sum();
    public long AccountCount() => _accounts.Count;

    public long OpenAccount()
    {
        lock (_accountsLock)
        {
            long accNum = -1;
            if (_freeAccNumbers.Count > 0)
            {
                accNum = _freeAccNumbers.First();
                _freeAccNumbers.Remove(accNum);
            }
            else if (_nextAccNumber <= ACC_NUM_MAX)
            {
                accNum = _nextAccNumber;
                _nextAccNumber += 1;
            }
            else
            {
                throw new InvalidOperationException("No more accounts can be created");
            }

            _accounts.Add(accNum, 0);
            return accNum;
        }
    }
    public void Remove(long accNum)
    {
        lock (_accountsLock)
        {
            ThrowOnNotExists(accNum);
            if (_accounts[accNum] > 0)
                throw new InvalidOperationException("Cannot delete account with funds in it");
            _accounts.Remove(accNum);
        }
    }

    public void Deposit(long accNum, long amount)
    {
        lock (_accountsLock)
        {
            ThrowOnNotExists(accNum);
            _accounts[accNum] += amount;
        }
    }
    public void Withdraw(long accNum, long amount)
    {
        lock (_accountsLock)
        {
            ThrowOnNotExists(accNum);
            if (_accounts[accNum] < amount)
                throw new ArgumentException("Cannot withdraw more than is in account");
            _accounts[accNum] -= amount;
        }
    }
    public long Balance(long accNum)
    {
        lock (_accountsLock)
        {
            ThrowOnNotExists(accNum);
            return _accounts[accNum];
        }
    }

    private void ThrowOnNotExists(long accNum)
    {
        if (!_accounts.ContainsKey(accNum))
            throw new KeyNotFoundException($"Account {accNum} does not exist");
    }

    private void Load()
    {
        // TODO: Load data from disk.
    }
    private void Save()
    {
        // TODO: Store data to disk.
    }

    ~BankStorage()
    {
        Save();
    }
}
