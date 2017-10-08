using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTestExtensions
{
    /// <summary>
    /// Attribute used to describe a set of values to pass for an argument to a combinatorial test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class CombinatorialEnumArgument : BaseCombinatorialArgumentAttribute
    {
        /// <summary>
        /// Gets the enumeration type from which the values are generated.
        /// </summary>
        public Type EnumType { get; }


        /// <summary>
        /// Constructs a new instance of <see cref="CombinatorialEnumArgument"/>.
        /// </summary>
        /// <param name="argIndex">The index of the argument this is for.</param>
        /// <param name="enumType">The enumeration type to generate values from.</param>
        public CombinatorialEnumArgument(int argIndex, Type enumType)
            : base(argIndex)
        {
            EnumType = enumType;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="CombinatorialEnumArgument"/>.
        /// </summary>
        /// <param name="argName">The name of the argument this is for.</param>
        /// <param name="enumType">The enumeration type to generate values from.</param>
        public CombinatorialEnumArgument(string argName, Type enumType)
            : base(argName)
        {
            EnumType = enumType;
        }


        /// <inheritdoc />
        /// <see cref="BaseCombinatorialArgumentAttribute.GetValues(ITestMethod)"/>
        public override IReadOnlyList<object> GetValues(ITestMethod testMethod)
        {
            if (!EnumType.GetTypeInfo().IsEnum)
            {
                throw new ArgumentException("Type is not an enumeration.", nameof(EnumType));
            }

            return Enum.GetValues(EnumType)
                       .Cast<object>()
                       .ToArray();
        }
    }
}
