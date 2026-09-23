# xUnit v3 Basics

## Patterns

### xUnit v3 Basics

```csharp
public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrder_WithValidItems_ReturnsSuccessResult()
    {
        // Arrange
        var db = CreateDb();
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero));
        var service = new OrderService(db, clock);
        var request = new CreateOrderRequest("customer-1", [new("product-1", 2)]);

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.CreatedAt.ShouldBe(clock.GetUtcNow());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateOrder_WithInvalidCustomerId_ReturnsFailure(string? customerId)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CreateAsync(new CreateOrderRequest(customerId!, []));

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }
}
```

### Test Naming Convention

Use the pattern: `MethodName_StateUnderTest_ExpectedBehavior`

```csharp
[Fact] public async Task CreateOrder_WithValidItems_ReturnsSuccessResult() { }
[Fact] public async Task CreateOrder_WithEmptyItems_ReturnsValidationError() { }
[Fact] public async Task GetOrder_WithNonExistentId_ReturnsNotFound() { }
[Fact] public async Task CancelOrder_WhenAlreadyShipped_ReturnsConflict() { }
```
