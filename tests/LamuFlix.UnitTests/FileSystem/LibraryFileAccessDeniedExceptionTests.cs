using System;
using System.Reflection;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.FileSystem;

public sealed class LibraryFileAccessDeniedExceptionTests
{
    private const string DefaultMessage = "Access to the library file was denied.";

    [Fact]
    public void Constructor_Parameterless_ExposesTheDefaultPolicyDenialMessage()
    {
        // act
        var exception = new LibraryFileAccessDeniedException();

        // assert
        exception.Message.ShouldBe(DefaultMessage);
    }

    [Fact]
    public void Constructor_MessageOnly_KeepsTheSuppliedMessage()
    {
        // act
        var exception = new LibraryFileAccessDeniedException("custom denial");

        // assert
        exception.Message.ShouldBe("custom denial");
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_MessageAndInnerException_PreservesTheInnerException()
    {
        // arrange
        var inner = new InvalidOperationException("inner");

        // act
        var exception = new LibraryFileAccessDeniedException("custom denial", inner);

        // assert
        exception.Message.ShouldBe("custom denial");
        exception.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void PolicyDenial_IsDistinctFromTheFeatureDisabledAndNotFoundFamilies()
    {
        // act
        var type = typeof(LibraryFileAccessDeniedException);

        // assert
        type.Namespace.ShouldBe("LamuFlix.Core.Ports");
        type.IsSealed.ShouldBeTrue();
        type.BaseType.ShouldBe(typeof(Exception));
        type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length.ShouldBe(3);
    }
}
