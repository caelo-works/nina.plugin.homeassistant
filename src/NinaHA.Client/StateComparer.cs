using System;
using System.Globalization;

namespace NinaHA.Client {

    /// <summary>
    /// Compares a Home Assistant entity state string against a target value. Ordering operators use a
    /// numeric comparison; equality operators compare numerically when both sides are numbers, otherwise
    /// as trimmed, case-insensitive strings.
    /// </summary>
    public static class StateComparer {

        public static bool Matches(string? currentState, string? target, ComparisonOperator op) {
            var current = (currentState ?? string.Empty).Trim();
            var goal = (target ?? string.Empty).Trim();

            var currentIsNum = double.TryParse(current, NumberStyles.Any, CultureInfo.InvariantCulture, out var c);
            var goalIsNum = double.TryParse(goal, NumberStyles.Any, CultureInfo.InvariantCulture, out var g);
            var bothNumeric = currentIsNum && goalIsNum;

            switch (op) {
                case ComparisonOperator.Equals:
                    return bothNumeric ? c == g : string.Equals(current, goal, StringComparison.OrdinalIgnoreCase);
                case ComparisonOperator.NotEquals:
                    return bothNumeric ? c != g : !string.Equals(current, goal, StringComparison.OrdinalIgnoreCase);
                case ComparisonOperator.GreaterThan:
                    return bothNumeric && c > g;
                case ComparisonOperator.GreaterOrEqual:
                    return bothNumeric && c >= g;
                case ComparisonOperator.LessThan:
                    return bothNumeric && c < g;
                case ComparisonOperator.LessOrEqual:
                    return bothNumeric && c <= g;
                default:
                    return false;
            }
        }
    }
}
