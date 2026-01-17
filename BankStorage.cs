using System;
using System.IO;
using System.Threading.Tasks;

public class BankStorage
{
    private const int ACC_NUM_MIN = 10000;
    private const int ACC_NUM_MAX = 99999;

    public static BankStorage Storage { get => Get(); }
    
    private static BankStorage? _instance;
    private Dictionary<int, int> _accounts = new();
    private int _nextAccNumber = ACC_NUM_MIN;
    private ISet<int> _freeAccNumbers = new HashSet<int>();
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

    public int TotalBalance() => _accounts.Values.Sum();
    public int AccountCount() => _accounts.Count;

    public int OpenAccount()
    {
        lock (_accountsLock)
        {
            var accNum = -1;
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
    public void Remove(int accNum)
    {
        lock (_accountsLock)
        {
            ThrowOnNotExists(accNum);
            if (_accounts[accNum] > 0)
                throw new InvalidOperationException("Cannot delete account with funds in it");
            _accounts.Remove(accNum);
        }
    }

    public void Deposit(int accNum, int amount)
    {
        lock (_accountsLock)
        {
            ThrowOnNotExists(accNum);
            _accounts[accNum] += amount;
        }
    }
    public void Withdraw(int accNum, int amount)
    {
        lock (_accountsLock)
        {
            ThrowOnNotExists(accNum);
            if (_accounts[accNum] < amount)
                throw new ArgumentException("Cannot withdraw more than is in account");
            _accounts[accNum] -= amount;
        }
    }
    public int Balance(int accNum)
    {
        lock (_accountsLock)
        {
            ThrowOnNotExists(accNum);
            return _accounts[accNum];
        }
    }

    private void ThrowOnNotExists(int accNum)
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
