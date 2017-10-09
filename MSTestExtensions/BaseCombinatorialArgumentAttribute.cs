using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTestExtensions
{
    /// <summary>
    /// Base type for attributes used to describe a set of values to pass for an argument
    /// to a combinatorial test.
    /// </summary>
    public abstract class BaseCombinatorialArgumentAttribute : Attribute
    {
        /// <summary>
        /// Gets the set of values to pass for the argument.
        /// </summary>
        /// <param name="testMethod">The test method descriptor.</param>
        /// <param name="parameter">The parameter this attribute is applied to.</param>
        /// <returns>The list of values to pass for the argument.</returns>
        /// <remarks>
        /// If generating the values for the combinatorial argument can throw an exception,
        /// it should be done in this function.  If an exception is thrown from the attribute
        /// constructor, then it can cause the test this attribute is applied on to be skipped.
        /// </remarks>
        public abstract IReadOnlyList<object> GetValues(ITestMethod testMethod, ParameterInfo parameter);
    }
}