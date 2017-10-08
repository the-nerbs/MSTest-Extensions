using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTestExtensions
{
    /// <summary>
    /// Base type for attributes used to describe a set of values to pass for an argument
    /// to a combinatorial test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public abstract class BaseCombinatorialArgumentAttribute : Attribute
    {
        /// <summary>
        /// Gets the index of the argument this is for. If null, the argument is specified by name.
        /// </summary>
        public int? ArgumentIndex { get; }

        /// <summary>
        /// Gets the name of the argument this is for. If null, the argument is specified by index.
        /// </summary>
        public string ArgumentName { get; }


        /// <summary>
        /// Initializes a new instance of <see cref="BaseCombinatorialArgumentAttribute"/>.
        /// </summary>
        /// <param name="argIndex">The index of the argument this is for.</param>
        protected BaseCombinatorialArgumentAttribute(int argIndex)
        {
            ArgumentIndex = argIndex;
            ArgumentName = null;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BaseCombinatorialArgumentAttribute"/>.
        /// </summary>
        /// <param name="argName">The name of the argument this is for.</param>
        protected BaseCombinatorialArgumentAttribute(string argName)
        {
            if (argName == null)
                throw new ArgumentNullException(nameof(argName));

            ArgumentIndex = null;
            ArgumentName = argName;
        }


        /// <summary>
        /// Gets the set of values to pass for the argument.
        /// </summary>
        /// <param name="testMethod">The test method descriptor.</param>
        /// <returns>The list of values to pass for the argument.</returns>
        /// <remarks>
        /// If generating the values for the combinatorial argument can throw an exception,
        /// it should be done in this function.  If an exception is thrown from the attribute
        /// constructor, then it can cause the test this attribute is applied on to be skipped.
        /// </remarks>
        public abstract IReadOnlyList<object> GetValues(ITestMethod testMethod);
    }
}