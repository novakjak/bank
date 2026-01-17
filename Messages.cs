using System.Reflection;
using System.Net;
using System.Net.Sockets;

public enum MsgType
{
    BC,
    BA,
    BN,
    AC,
    AD,
    AW,
    AB,
    AR,
    RP,
    ER,
}

public static class MsgTypeExtensions
{
    public static IBankMsg MsgFromString(this MsgType type, string str) => type switch
    {
        MsgType.BC => BankCode.FromString(ref str),
        MsgType.BA => BankAmount.FromString(ref str),
        MsgType.BN => BankNumber.FromString(ref str),
        MsgType.AC => AccountCreate.FromString(ref str),
        MsgType.AD => AccountDeposit.FromString(ref str),
        MsgType.AW => AccountWithdraw.FromString(ref str),
        MsgType.AB => AccountBalance.FromString(ref str),
        MsgType.AR => AccountRemove.FromString(ref str),
        MsgType.RP => RobberyPlan.FromString(ref str),
        MsgType.ER => BankCode.FromString(ref str),
        _ => throw new InvalidOperationException("Msg type is invalid"),
    };
    public static IBankMsg MsgFromString(this string str)
    {
        if (str.Length < 2)
            throw new ArgumentException("Unknown message");
        var typeStr = String.Join("", str.Take(2));
        MsgType type;
        try
        {
            type = (MsgType)Enum.Parse(typeof(MsgType), typeStr.ToUpper());
        }
        catch
        {
            throw new ArgumentException($"Unrecognized message {typeStr}");
        }
        return type.MsgFromString(str);
    }
}

public interface IBankMsg
{
    static abstract MsgType Type { get; }
    static abstract IBankMsg FromString(ref string str);
    // ToString in on object and does not need to be defined here
}
public interface IMsgWithDetails : IBankMsg
{
    public int Account { get; set; }
    public string Code { get; set; }
}
public interface IMsgWithAmount : IMsgWithDetails
{
    public int Amount { get; set; }
}

public abstract class BankMsg<T> : IBankMsg where T: IBankMsg, new()
{
    public static MsgType Type { get {
        var cls = typeof(T);
        var t = cls.GetProperty("Type", typeof(MsgType))!.GetValue(null, null)!;
        return (MsgType)t;
    }}
    public static IBankMsg FromString(ref string str)
    {
        BankMsg<T>.ConsumeType(ref str);
        if (str.Length > 0)
            throw new ArgumentException("Too many arguments specified");
        return new T();
    }

    private static void ConsumeType(ref string str)
    {
        str = str.Trim();
        if (str.Length != 2)
            throw new ArgumentException("Message type has incorrect length.");
        var typeStr = String.Join("", str.Take(2)).ToUpper();
        if (typeStr != T.Type.ToString())
            throw new ArgumentException($"Cannot parse message as {T.Type}");
        str = String.Empty;
    }

    public override string? ToString() => $"{T.Type.ToString()}";
}
public abstract class MsgWithDetails<T> : BankMsg<T>, IMsgWithDetails where T: IMsgWithDetails, new()
{
    public int Account { get; set; }
    public string Code { get; set; } = "";

    new public static IBankMsg FromString(ref string str)
    {
        str = str.Trim();
        var (number, addr) = MsgWithDetails<T>.ConsumeAccountDetails(ref str);
        IMsgWithDetails t = (BankMsg<T>.FromString(ref str) as IMsgWithDetails)!;
        t.Account = number;
        t.Code = addr.ToString();
        return t;
    }

    public static (int, IPAddress) ConsumeAccountDetails(ref string str)
    {
        char[] separators = {' ', '\t', '\v'};
        var parts = str.Split(separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Count() < 1)
            throw new ArgumentException("Message has wrong format.");
        var accountDetailsStr = parts.Last();
        str = str.Substring(0, str.Length - accountDetailsStr.Length);
        var accountDetails = accountDetailsStr.Split('/');
        if (accountDetails.Count() != 2)
            throw new ArgumentException("Account has wrong format.");
        int number;
        try
        {
            number = Int32.Parse(accountDetails[0]);
        }
        catch
        {
            throw new ArgumentException("Cannot parse account number");
        }
        IPAddress addr;
        try
        {
            addr = IPAddress.Parse(accountDetails[1]);
        }
        catch
        {
            throw new ArgumentException("Cannot parse IP address");
        }
        if (addr.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("IP Address is in an incorrect format");
        return (number, addr);
    }

    public override string? ToString() => $"{T.Type.ToString()} {Account}/{Code}";
}
public abstract class MsgWithAmount<T> : MsgWithDetails<T>, IMsgWithAmount where T: IMsgWithAmount, new()
{
    public int Amount { get; set; }

    new public static IBankMsg FromString(ref string str)
    {
        str = str.Trim();
        var amount = MsgWithAmount<T>.ConsumeAmount(ref str);
        IMsgWithAmount t = (MsgWithDetails<T>.FromString(ref str) as IMsgWithAmount)!;
        t.Amount = amount;
        return t;
    }

    public static int ConsumeAmount(ref string str)
    {
        char[] separators = {' ', '\t', '\v'};
        var parts = str.Split(separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Count() < 1)
            throw new ArgumentException("Message has wrong format.");
        var numStr = parts.Last();
        str = str.Substring(0, str.Length - numStr.Length);
        try
        {
            return Int32.Parse(numStr);
        }
        catch
        {
            throw new ArgumentException("Cannot parse account number");
        }
    }

    public override string? ToString() => $"{T.Type.ToString()} {Account}/{Code} {Amount}";
}

public sealed class BankCode : BankMsg<BankCode>
{
    new public static MsgType Type => MsgType.BC;
}
public sealed class BankAmount : BankMsg<BankAmount>
{
    new public static MsgType Type => MsgType.BA;
}
public sealed class BankNumber : BankMsg<BankNumber>
{
    new public static MsgType Type => MsgType.BN;
}

public sealed class AccountCreate : BankMsg<AccountCreate>
{
    new public static MsgType Type => MsgType.AC;
}
public sealed class AccountDeposit : MsgWithAmount<AccountDeposit>
{
    new public static MsgType Type => MsgType.AD;
}
public sealed class AccountWithdraw : MsgWithAmount<AccountWithdraw>
{
    new public static MsgType Type => MsgType.AW;
}
public sealed class AccountBalance : MsgWithDetails<AccountBalance>
{
    new public static MsgType Type => MsgType.AB;
}
public sealed class AccountRemove : MsgWithDetails<AccountRemove>
{
    new public static MsgType Type => MsgType.AR;
}

public sealed class RobberyPlan : BankMsg<RobberyPlan>
{
    new public static MsgType Type => MsgType.RP;
    public string Code { get; } = "";
    public override string? ToString() => $"{RobberyPlan.Type.ToString()} {Code}";
    new public static BankMsg<RobberyPlan> FromString(ref string str)
        => throw new Exception();
}
