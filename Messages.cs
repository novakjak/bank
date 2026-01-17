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
        MsgType.BC => BankCode.FromString(str),
        MsgType.BA => BankAmount.FromString(str),
        MsgType.BN => BankNumber.FromString(str),
        MsgType.AC => AccountCreate.FromString(str),
        MsgType.AD => AccountDeposit.FromString(str),
        MsgType.AW => AccountWithdraw.FromString(str),
        MsgType.AB => AccountBalance.FromString(str),
        MsgType.AR => AccountRemove.FromString(str),
        MsgType.RP => RobberyPlan.FromString(str),
        MsgType.ER => BankCode.FromString(str),
    };
    public static IBankMsg MsgFromString(this string str)
    {
        if (str.Length < 2)
            throw new ArgumentException("Unknown message");
        var typeStr = String.Join("", str.Take(2));
        var type = (MsgType)Enum.Parse(typeof(MsgType), typeStr.ToUpper());
        return type.MsgFromString(str);
    }
}

public abstract class IBankMsg
{
    public static MsgType Type { get; }
    public static IBankMsg FromString(string str)
        => throw new NotImplementedException($"From string was not implemented for msg type {Type}");

    public override string? ToString() => $"{Type.ToString()}";
}
public abstract class AccountMsg(int account, string code) : IBankMsg
{
    public int Account { get; } = account;
    public string Code { get; } = code;
    public override string? ToString() => $"{Type.ToString()} {Account}/{Code}";
}
public abstract class AccountMsgWithAmount : AccountMsg
{
    public int Amount { get; }
    public AccountMsgWithAmount(int account, string code, int amount) : base(account, code)
    {
        Amount = amount;
    }
    public override string? ToString() => $"{Type.ToString()} {Account}/{Code} {Amount}";
}

public class BankCode : IBankMsg
{
    new public static MsgType Type { get; } = MsgType.BC;
}
public class BankAmount : IBankMsg
{
    new public static MsgType Type { get; } = MsgType.BA;
}
public class BankNumber : IBankMsg
{
    new public static MsgType Type { get; } = MsgType.BN;
}

public class AccountCreate : IBankMsg
{
    new public static MsgType Type { get; } = MsgType.AC;
}
public class AccountDeposit : AccountMsgWithAmount
{
    new public static MsgType Type { get; } = MsgType.AD;
    public AccountDeposit(int account, string code, int amount) : base(account, code, amount) {}
}
public class AccountWithdraw : AccountMsgWithAmount
{
    new public static MsgType Type { get; } = MsgType.AW;
    public AccountWithdraw(int account, string code, int amount) : base(account, code, amount) {}
}
public class AccountBalance : AccountMsg
{
    new public static MsgType Type { get; } = MsgType.AB;
    public AccountBalance(int account, string code) : base(account, code) {}
}
public class AccountRemove : AccountMsg
{
    new public static MsgType Type { get; } = MsgType.AB;
    public AccountRemove(int account, string code) : base(account, code) {}
}

public class RobberyPlan(string code) : IBankMsg
{
    new public static MsgType Type { get; } = MsgType.RP;
    public string Code { get; } = code;
    public override string? ToString() => $"{Type.ToString()} {Code}";
}
