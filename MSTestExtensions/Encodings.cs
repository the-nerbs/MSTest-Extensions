using System;
using System.Collections.Generic;
using System.Text;

namespace MSTestExtensions
{
    /// <summary>
    /// Contains some commonly used encodings that are not directly provided by the framework.
    /// </summary>
    internal static class Encodings
    {
        /// <summary>
        /// UTF-8 encoding that does not include a byte order mark.
        /// </summary>
        /// <devdoc>
        /// Note: <see cref="Encoding.UTF8"/> includes the BOM on all GetBytes calls.
        /// </devdoc>
        public static readonly Encoding UTF8NoBom = new UTF8Encoding(false);
    }
}
