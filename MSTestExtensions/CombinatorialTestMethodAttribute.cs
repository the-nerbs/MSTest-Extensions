using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTestExtensions
{
    /// <summary>
    /// Attribute for a combinatorial data test method.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Test methods marked with this attribute will be called with all combinations
    ///     of the values given by each of the combinatorial argument attributes.
    ///   </para>
    ///   <para>
    ///     To customize the values that tests are run with, you can extend the
    ///     <see cref="BaseCombinatorialArgumentAttribute"/> class.  A set of standard
    ///     combinatorial arguments attributes are already defined:
    ///   </para>
    ///   <list type="table">
    ///     <listheader>
    ///       <term>Attribute</term>
    ///       <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///       <term><see cref="CombinatorialArgumentAttribute"/></term>
    ///       <description>Utilizes a set of given values for the argument.</description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="CombinatorialEnumArgument"/></term>
    ///       <description>Utilizes all defined values of an enumeration for the argument.</description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="CombinatorialFactoryArgumentAttribute"/></term>
    ///       <description>Retrieves the argument's values from a static method on the test class.</description>
    ///     </item>
    ///   </list>
    /// </remarks>
    /// <seealso cref="BaseCombinatorialArgumentAttribute"/>
    /// <seealso cref="CombinatorialArgumentAttribute"/>
    /// <seealso cref="CombinatorialEnumArgument"/>
    /// <seealso cref="CombinatorialFactoryArgumentAttribute"/>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CombinatorialTestMethodAttribute : TestMethodAttribute
    {
        /// <summary>
        /// Executes a combinatorial test method.
        /// </summary>
        /// <param name="testMethod">The test method to execute.</param>
        /// <returns>An array of TestResult objects that represent the outcomes of the test.</returns>
        public override TestResult[] Execute(ITestMethod testMethod)
        {
            try
            {
                var attrs = testMethod.GetAttributes<BaseCombinatorialArgumentAttribute>(inherit: true);

                ValidateAttributes(testMethod, attrs);

                object[][] args = GetArgumentsInOrder(testMethod, attrs);

                long totalTests = 1;

                checked
                {
                    foreach (object[] list in args)
                    {
                        totalTests *= list.Length;
                    }
                }

                int totalTestsDigits = (int)Math.Ceiling(Math.Log10(totalTests));

                ValidateArguments(testMethod, args);

                var indexes = new int[args.Length];
                bool done;

                var results = new List<TestResult>();
                do
                {
                    // run the test with the current values.
                    var argsToPass = Enumerable.Range(0, args.Length)
                                               .Select(i => args[i][indexes[i]])
                                               .ToArray();

                    var result = testMethod.Invoke(argsToPass);

                    // In case the results are alphabetized (I'm looking at you
                    // VS test explorer), prepend the test # padded with 0s.
                    string number = (results.Count + 1).ToString().PadLeft(totalTestsDigits, '0');
                    result.DisplayName = $"#{number}: {GetTestDisplayName(testMethod, argsToPass)}";
                    results.Add(result);

                    // update the current indexes.
                    int updateIdx = indexes.Length - 1;

                    for (; updateIdx >= 0; updateIdx--)
                    {
                        indexes[updateIdx]++;

                        if (indexes[updateIdx] < args[updateIdx].Length)
                        {
                            // still values left for this parameter.
                            break;
                        }

                        // carry to the next slot
                        indexes[updateIdx] = 0;
                    }

                    // If we carried off the end of the array, we're done
                    done = (updateIdx < 0);

                } while (!done);

                return results.ToArray();
            }
            catch (Exception ex)
            {
                // something went wrong - fail the test with the exception info.
                // note: I believe the MSTest executor will catch any exceptions that escape
                // tests, and return a failed TestResult (if it wasn't an ExpectedException).
                return new[]
                {
                    new TestResult
                    {
                        Outcome = UnitTestOutcome.Error,
                        TestFailureException = ex
                    }
                };
            }
        }


        /// <summary>
        /// Validates that each of the test method parameters have a combinatorial argument attribute.
        /// </summary>
        /// <param name="testMethod">The test method.</param>
        /// <param name="attributes">The list of combinatorial argument attributes.</param>
        /// <exception cref="Exception">An error is found with the attributes.</exception>
        /// <devdoc>
        /// The MSTest executor seems to do some special handling for base Exception type, where it
        /// will omit the exception type.
        /// </devdoc>
        private static void ValidateAttributes(ITestMethod testMethod, BaseCombinatorialArgumentAttribute[] attributes)
        {
            ParameterInfo[] parameters = testMethod.MethodInfo.GetParameters();

            if (parameters.Length != attributes.Length)
            {
                throw new Exception(
                    $"Count of {nameof(BaseCombinatorialArgumentAttribute)}s does not match number of parameters."
                );
            }

            var matched = new bool[parameters.Length];
            foreach (var attr in attributes)
            {
                int index;

                if (attr.ArgumentIndex != null)
                {
                    index = attr.ArgumentIndex.Value;
                    if (index < 0 || index >= parameters.Length)
                    {
                        throw new Exception(
                            $"{index} is not a valid parameter index."
                        );
                    }
                }
                else
                {
                    index = Array.FindIndex(parameters, p => p.Name == attr.ArgumentName);
                    if (index == -1)
                    {
                        throw new Exception(
                            $"Test method does not have an argument named {attr.ArgumentName}."
                        );
                    }
                }

                if (matched[index])
                {
                    throw new Exception(
                        $"Argument {index} has duplicate combinatorial data."
                    );
                }

                matched[index] = true;
            }

            for (int i = 0; i < matched.Length; i++)
            {
                if (!matched[i])
                {
                    throw new Exception(
                        $"Argument {parameters[i].Name} is missing a {nameof(BaseCombinatorialArgumentAttribute)}."
                    );
                }
            }
        }

        /// <summary>
        /// Gets all the values for each parameter in the order they need to be passed to the function.
        /// </summary>
        /// <param name="testMethod">The test method.</param>
        /// <param name="attributes">The list of combinatorial argument attributes.</param>
        /// <returns>
        /// A list of all parameters' values.  Each sub-array corresponds to a parameter, so
        /// <c>argArrays[0]</c> will yield the values to pass to the first parameter. Additionally,
        /// <c>argArrays[0][0]</c> will yield the first value to pass to the first parameter.
        /// </returns>
        private static object[][] GetArgumentsInOrder(ITestMethod testMethod, BaseCombinatorialArgumentAttribute[] attributes)
        {
            var argsAndOrder = new Tuple<int, object[]>[attributes.Length];

            ParameterInfo[] parameters = testMethod.MethodInfo.GetParameters();

            for (int i = 0; i < attributes.Length; i++)
            {
                int index = attributes[i].ArgumentIndex
                         ?? Array.FindIndex(parameters, p => p.Name == attributes[i].ArgumentName);

                argsAndOrder[i] = Tuple.Create(index, attributes[i].GetValues(testMethod).ToArray());
            }

            return argsAndOrder
                .OrderBy(tpl => tpl.Item1)
                .Select(tpl => tpl.Item2)
                .ToArray();
        }

        /// <summary>
        /// Validates the values being passed to each parameter.
        /// </summary>
        /// <param name="testMethod">The test method.</param>
        /// <param name="arguments">The set of values for each parameter, as returned by <see cref="GetArgumentsInOrder"/>.</param>
        private static void ValidateArguments(ITestMethod testMethod, object[][] arguments)
        {
            ParameterInfo[] allParameters = testMethod.MethodInfo.GetParameters();

            for (int i = 0; i < arguments.Length; i++)
            {
                object[] values = arguments[i];
                ParameterInfo parameter = allParameters[i];

                for (int j = 0; j < values.Length; j++)
                {
                    object v = values[i];

                    if (!parameter.ParameterType.GetTypeInfo().IsAssignableFrom(v.GetType()))
                    {
                        throw new ArgumentException(
                            $"Argument value ({GetValueString(v)}) has a type that is incompatible with parameter {parameter.Name}."
                        );
                    }
                }
            }
        }


        /// <summary>
        /// Creates a name for the test as run using the given arguments.
        /// </summary>
        /// <param name="method">The test method.</param>
        /// <param name="arguments">The argument values.</param>
        /// <returns>A name for the test as run with the given arguments.</returns>
        private static string GetTestDisplayName(ITestMethod method, object[] arguments)
        {
            StringBuilder name = new StringBuilder(method.TestMethodName);
            name.Append('(');

            bool first = true;

            foreach (var value in arguments)
            {
                if (!first)
                {
                    name.Append(", ");
                }

                name.Append(GetValueString(value));

                first = false;
            }

            name.Append(')');
            return name.ToString();
        }

        /// <summary>
        /// Converts the given value to a string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The string form of the given value.</returns>
        private static string GetValueString(object value)
        {
            switch (value)
            {
                case null:
                    return "null";

                case string str:
                    return "\"" + str + "\"";

                case object enumVal
                when enumVal.GetType().GetTypeInfo().IsEnum:
                    return enumVal.GetType().Name + "." + value;

                default:
                    return value.ToString();
            }
        }
    }
}
