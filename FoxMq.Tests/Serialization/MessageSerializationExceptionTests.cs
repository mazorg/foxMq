using FoxMq.Serialization;

namespace FoxMq.Tests.Serialization;

public class MessageSerializationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var exception = new MessageSerializationException("Test error");

        Assert.Equal("Test error", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        var inner = new InvalidOperationException("Inner error");
        var exception = new MessageSerializationException("Outer error", inner);

        Assert.Equal("Outer error", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void Exception_InheritsFromException()
    {
        var exception = new MessageSerializationException("Test");

        Assert.IsAssignableFrom<Exception>(exception);
    }
}
