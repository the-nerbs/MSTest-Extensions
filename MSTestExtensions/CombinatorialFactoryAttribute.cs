﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTestExtensions
{
    /// <summary>
    /// Attribute used to describe a set of values to pass for an argument to a combinatorial test.
    /// This will retrieve the list of values from a static function.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class CombinatorialFactoryAttribute : BaseCombinatorialArgumentAttribute
    {
        private IReadOnlyList<object> _values = null;


        /// <summary>
        /// Gets the name of the factory method.
        /// </summary>
        public string FactoryMethodName { get; }

        /// <summary>
        /// Gets or sets the type that declares the factory method.
        /// </summary>
        public Type FactoryDeclaringType { get; set; }


        /// <summary>
        /// Initializes a new instance of <see cref="CombinatorialFactoryAttribute"/>.
        /// </summary>
        /// <param name="factoryMethodName">The name of the method used to generate values for this argument.</param>
        public CombinatorialFactoryAttribute(string factoryMethodName)
        {
            if (factoryMethodName == null)
                throw new ArgumentNullException(nameof(factoryMethodName));

            if (string.IsNullOrWhiteSpace(factoryMethodName))
                throw new ArgumentException("Invalid factory method name.", nameof(factoryMethodName));

            FactoryMethodName = factoryMethodName;
        }


        /// <inheritdoc />
        /// <see cref="BaseCombinatorialArgumentAttribute.GetValues"/>
        public override IReadOnlyList<object> GetValues(ITestMethod testMethod, ParameterInfo parameter)
        {
            if (_values == null)
            {
                // this can be arbitrarily complex, so cache the result.
                _values = RunFactory(testMethod);
            }

            return _values;
        }


        /// <summary>
        /// Resolves the factory method from the test class.
        /// </summary>
        /// <param name="testMethod">The test method being run.</param>
        /// <returns>The MethodInfo for the factory method, or null if it could not be resolved.</returns>
        /// <remarks>
        /// To customize how factory methods are resolved, you can inherit from this
        /// type and override this method to perform the necessary logic.
        /// </remarks>
        protected virtual MethodInfo ResolveFactory(ITestMethod testMethod)
        {
            const BindingFlags binding = BindingFlags.Public | BindingFlags.NonPublic
                                       | BindingFlags.Static;

            // Try to resolve the actual test class by its name.
            // It that fails, just use the test method's declaring type.
            Type testClassType = FactoryDeclaringType
                              ?? Type.GetType(testMethod.TestClassName)
                              ?? testMethod.MethodInfo.DeclaringType;

            if (testClassType == null)
            {
                // TODO: can this case ever be hit?
                throw new ArgumentException(
                    $"Failed to resolve type for test class {testMethod.TestClassName}."
                );
            }

            return testClassType.GetTypeInfo()
                                .GetMethod(FactoryMethodName, binding);
        }

        /// <summary>
        /// Runs the factory method.
        /// </summary>
        /// <param name="testMethod"></param>
        /// <returns>Gets the list of values returned by the factory method.</returns>
        private IReadOnlyList<object> RunFactory(ITestMethod testMethod)
        {
            MethodInfo factoryMethod = ResolveFactory(testMethod);

            if (factoryMethod == null)
            {
                throw new ArgumentException(
                    $"Unable to resolve factory method {FactoryMethodName}."
                );
            }
            else if (!factoryMethod.IsStatic)
            {
                throw new ArgumentException(
                    $"The factory method {FactoryMethodName} must be static."
                );
            }
            else if (factoryMethod.GetParameters().Length != 0 ||
                     !typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(factoryMethod.ReturnType))
            {
                throw new ArgumentException(
                    $"The factory method {FactoryMethodName} does not have the expected " +
                    $"signature. Factory methods must take 0 parameters and return a collection " +
                    $"that is assignable to IEnumerable."
                );
            }

            var values = (IEnumerable)factoryMethod.Invoke(null, null);

            if (values == null)
            {
                throw new ArgumentException(
                    $"The factory method {FactoryMethodName} returned a null collection."
                );
            }

            return values.Cast<object>().ToArray();
        }
    }
}
