using FoxMq.Serialization;

namespace FoxMq.Tests.Serialization;

public class MessageDeserializationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var exception = new MessageDeserializationException("Test error");

        Assert.Equal("Test error", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        var inner = new InvalidOperationException("Inner error");
        var exception = new MessageDeserializationException("Outer error", inner);

        Assert.Equal("Outer error", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void Exception_InheritsFromException()
    {
        var exception = new MessageDeserializationException("Test");

        Assert.IsAssignableFrom<Exception>(exception);
    }
}
