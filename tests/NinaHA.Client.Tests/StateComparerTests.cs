using NinaHA.Client;
using Xunit;

namespace NinaHA.Client.Tests {

    public class StateComparerTests {

        [Theory]
        [InlineData("on", "on", ComparisonOperator.Equals, true)]
        [InlineData("on", "off", ComparisonOperator.Equals, false)]
        [InlineData("ON", "on", ComparisonOperator.Equals, true)]
        [InlineData("on", "off", ComparisonOperator.NotEquals, true)]
        [InlineData("21.5", "21.5", ComparisonOperator.Equals, true)]
        [InlineData("21.50", "21.5", ComparisonOperator.Equals, true)]
        public void Equality(string current, string target, ComparisonOperator op, bool expected) {
            Assert.Equal(expected, StateComparer.Matches(current, target, op));
        }

        [Theory]
        [InlineData("22", "20", ComparisonOperator.GreaterThan, true)]
        [InlineData("18", "20", ComparisonOperator.GreaterThan, false)]
        [InlineData("20", "20", ComparisonOperator.GreaterOrEqual, true)]
        [InlineData("19.9", "20", ComparisonOperator.LessThan, true)]
        [InlineData("20", "20", ComparisonOperator.LessOrEqual, true)]
        public void Ordering(string current, string target, ComparisonOperator op, bool expected) {
            Assert.Equal(expected, StateComparer.Matches(current, target, op));
        }

        [Fact]
        public void Ordering_on_non_numeric_is_false() {
            Assert.False(StateComparer.Matches("on", "off", ComparisonOperator.GreaterThan));
        }
    }
}
