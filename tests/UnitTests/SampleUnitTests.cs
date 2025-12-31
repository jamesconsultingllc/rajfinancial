namespace RajFinancial.UnitTests;

public class SampleUnitTests
{
    [Fact]
    public void SampleTest_ShouldPass()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SampleCalculation_ShouldReturnCorrectResult()
    {
        // Arrange
        var a = 2;
        var b = 3;
        var expected = 5;

        // Act
        var actual = a + b;

        // Assert
        Assert.Equal(expected, actual);
    }
}