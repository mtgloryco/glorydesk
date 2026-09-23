using System;
using System.Collections.Generic;
using System.Linq;

namespace InventoryManagementSystem.Domain
{
    public static class UnitConverter
    {
        public static double Convert(double value, string sourceUnitName, string targetUnitName, List<ProductUnit> units)
        {
            if (string.IsNullOrWhiteSpace(sourceUnitName) || string.IsNullOrWhiteSpace(targetUnitName))
                return value;

            if (string.Equals(sourceUnitName, targetUnitName, StringComparison.OrdinalIgnoreCase))
                return value;

            var sourceUnit = units.FirstOrDefault(u => string.Equals(u.Name, sourceUnitName, StringComparison.OrdinalIgnoreCase));
            var targetUnit = units.FirstOrDefault(u => string.Equals(u.Name, targetUnitName, StringComparison.OrdinalIgnoreCase));

            if (sourceUnit == null || targetUnit == null)
                return value;

            // Resolve to base units and compute cumulative multipliers
            var sourceBase = GetBaseUnitAndMultiplier(sourceUnit, units, out double sourceMultiplier);
            var targetBase = GetBaseUnitAndMultiplier(targetUnit, units, out double targetMultiplier);

            // If they resolve to the same base category, perform conversion
            if (string.Equals(sourceBase, targetBase, StringComparison.OrdinalIgnoreCase))
            {
                return (value * sourceMultiplier) / targetMultiplier;
            }

            return value; // Incompatible UoM categories, default to original value
        }

        private static string GetBaseUnitAndMultiplier(ProductUnit unit, List<ProductUnit> units, out double multiplier)
        {
            multiplier = unit.Quantity;
            var currentUnit = unit;
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (!string.IsNullOrWhiteSpace(currentUnit.ReferenceUnit))
            {
                if (!visited.Add(currentUnit.Name))
                    break; // Prevent circular references

                var parent = units.FirstOrDefault(u => string.Equals(u.Name, currentUnit.ReferenceUnit, StringComparison.OrdinalIgnoreCase));
                if (parent == null)
                    break;

                multiplier *= parent.Quantity;
                currentUnit = parent;
            }

            return currentUnit.Name;
        }
    }
}
