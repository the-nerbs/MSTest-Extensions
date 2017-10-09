using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTestExtensions
{
    /// <summary>
    /// Attribute used to describe a set of values to pass for an argument to a combinatorial test.
    /// This uses a set of values that are passed to the constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class CombinatorialAttribute : BaseCombinatorialArgumentAttribute
    {
        /// <summary>
        /// Gets the list of values to pass for the argument.
        /// </summary>
        public IReadOnlyList<object> Values { get; }


        /// <summary>
        /// Initializes a new instance of <see cref="CombinatorialAttribute"/>.
        /// </summary>
        /// <param name="values">The set of values to pass for the argument.</param>
        public CombinatorialAttribute(params object[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (values.Length == 0)
                throw new ArgumentException("expected values", nameof(values));

            Values = values;
        }


        /// <inheritdoc />
        /// <see cref="BaseCombinatorialArgumentAttribute.GetValues"/>
        public override IReadOnlyList<object> GetValues(ITestMethod testMethod, ParameterInfo parameter)
        {
            return Values;
        }
    }
}
