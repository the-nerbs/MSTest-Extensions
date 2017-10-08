using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTestExtensions
{
    /// <summary>
    /// Attribute used to describe a set of values to pass for an argument to a combinatorial test.
    /// This uses a set of values that are passed to the constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class CombinatorialArgumentAttribute : BaseCombinatorialArgumentAttribute
    {
        /// <summary>
        /// Gets the list of values to pass for the argument.
        /// </summary>
        public IReadOnlyList<object> Values { get; }


        /// <summary>
        /// Initializes a new instance of <see cref="CombinatorialArgumentAttribute"/>.
        /// </summary>
        /// <param name="argIndex">The index of the argument this is for.</param>
        /// <param name="values">The set of values to pass for the argument.</param>
        public CombinatorialArgumentAttribute(int argIndex, params object[] values)
            : base(argIndex)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (values.Length == 0)
                throw new ArgumentException("expected values", nameof(values));

            Values = values;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CombinatorialArgumentAttribute"/>.
        /// </summary>
        /// <param name="argName">The name of the argument this is for.</param>
        /// <param name="values">The set of values to pass for the argument.</param>
        public CombinatorialArgumentAttribute(string argName, params object[] values)
            : base(argName)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (values.Length == 0)
                throw new ArgumentException("expected values", nameof(values));

            Values = values;
        }


        /// <inheritdoc />
        /// <see cref="BaseCombinatorialArgumentAttribute.GetValues(ITestMethod)"/>
        public override IReadOnlyList<object> GetValues(ITestMethod testMethod)
        {
            return Values;
        }
    }
}
