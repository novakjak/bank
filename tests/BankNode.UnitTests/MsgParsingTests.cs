using Xunit;

public class MsgParsingTests
{
    [Fact]
    public void Parse_AC_Message()
    {
        var msg = "AC".MsgFromString();
        Assert.Equal(MsgType.AC, msg.GetMsgType());
    }

    [Fact]
    public void Parse_Invalid_Message_Throws()
    {
        Assert.Throws<ArgumentException>(() => "ZZ".MsgFromString());
    }
}
