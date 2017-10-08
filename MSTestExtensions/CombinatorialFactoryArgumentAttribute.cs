using System;
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
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class CombinatorialFactoryArgumentAttribute : BaseCombinatorialArgumentAttribute
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
        /// Initializes a new instance of <see cref="CombinatorialFactoryArgumentAttribute"/>.
        /// </summary>
        /// <param name="argIndex">The index of the argument this is for.</param>
        /// <param name="factoryMethodName">The name of the method used to generate values for this argument.</param>
        public CombinatorialFactoryArgumentAttribute(int argIndex, string factoryMethodName)
            : base(argIndex)
        {
            if (factoryMethodName == null)
                throw new ArgumentNullException(nameof(factoryMethodName));
            if (string.IsNullOrWhiteSpace(factoryMethodName))
                throw new ArgumentException("Invalid factory method name.", nameof(factoryMethodName));

            FactoryMethodName = factoryMethodName;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CombinatorialFactoryArgumentAttribute"/>.
        /// </summary>
        /// <param name="argName">The name of the argument this is for.</param>
        /// <param name="factoryMethodName">The name of the method used to generate values for this argument.</param>
        public CombinatorialFactoryArgumentAttribute(string argName, string factoryMethodName)
            : base(argName)
        {
            if (factoryMethodName == null)
                throw new ArgumentNullException(nameof(factoryMethodName));
            if (string.IsNullOrWhiteSpace(factoryMethodName))
                throw new ArgumentException("Invalid factory method name.", nameof(factoryMethodName));

            FactoryMethodName = factoryMethodName;
        }


        /// <inheritdoc />
        /// <see cref="BaseCombinatorialArgumentAttribute.GetValues(ITestMethod)"/>
        public override IReadOnlyList<object> GetValues(ITestMethod testMethod)
        {
            if (_values == null)
            {
                // this can be arbitrarily complex, so cache the result.
                _values = RunFactory(testMethod);
            }

            return _values;
        }


        /// <summary>
        /// Runs the factory method.
        /// </summary>
        /// <param name="testMethod"></param>
        /// <returns>Gets the list of values returned by the factory method.</returns>
        private IReadOnlyList<object> RunFactory(ITestMethod testMethod)
        {
            const BindingFlags binding = BindingFlags.Public | BindingFlags.NonPublic
                                       | BindingFlags.Static;

            // If we don't have a factory type, try to resolve the actual test class by its name
            // It that also fails, just use the test method's declaring type.
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

            MethodInfo factoryMethod = testClassType.GetTypeInfo()
                                                    .GetMethod(FactoryMethodName, binding);

            if (factoryMethod == null)
            {
                throw new ArgumentException(
                    $"The factory method {FactoryMethodName} does not exist on type " +
                    $"{testClassType.FullName}."
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
