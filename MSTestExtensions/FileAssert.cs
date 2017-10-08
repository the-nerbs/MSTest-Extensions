using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

namespace MSTestExtensions
{
#if !NO_FILESYSTEM

    /// <summary>
    /// Contains assertions related to files and directories.
    /// </summary>
    public static class FileAssert
    {
        /// <summary>
        /// Asserts that a file or directory exists.
        /// </summary>
        /// <param name="assert"><see cref="Assert.That"/></param>
        /// <param name="path">The path to the file or directory.</param>
        public static void FileOrDirectoryExists(this Assert assert, string path)
        {
            Assert.IsFalse(string.IsNullOrEmpty(path), "Attempted to assert null or empty path exists.");

            var file = new FileInfo(path);
            if (!file.Exists)
            {
                var directory = new DirectoryInfo(path);
                if (!directory.Exists)
                {
                    throw new AssertFailedException($"File or directory \"{path}\" does not exist.");
                }
            }
        }

        /// <summary>
        /// Asserts that a file or directory does not exist.
        /// </summary>
        /// <param name="assert"><see cref="Assert.That"/></param>
        /// <param name="path">The path to the file or directory.</param>
        public static void FileOrDirectoryDoesNotExist(this Assert assert, string path)
        {
            Assert.IsFalse(string.IsNullOrEmpty(path), "Attempted to assert null or empty path does not exist.");

            var file = new FileInfo(path);
            if (file.Exists)
            {
                throw new AssertFailedException($"File or directory \"{path}\" exists.");
            }

            var directory = new DirectoryInfo(path);
            if (!directory.Exists)
            {
                throw new AssertFailedException($"File or directory \"{path}\" exists.");
            }
        }


        /// <summary>
        /// Asserts that a file exists.
        /// </summary>
        /// <param name="assert"><see cref="Assert.That"/></param>
        /// <param name="path">The path to the file .</param>
        public static void FileExists(this Assert assert, string path)
        {
            Assert.IsFalse(string.IsNullOrEmpty(path), "Attempted to assert null or empty path exists.");

            var info = new FileInfo(path);
            if (!info.Exists)
            {
                throw new AssertFailedException($"File \"{path}\" does not exist.");
            }
        }

        /// <summary>
        /// Asserts that a file does not exist.
        /// </summary>
        /// <param name="assert"><see cref="Assert.That"/></param>
        /// <param name="path">The path to the file .</param>
        public static void FileDoesNotExist(this Assert assert, string path)
        {
            Assert.IsFalse(string.IsNullOrEmpty(path), "Attempted to assert null or empty path does not exist.");

            var info = new FileInfo(path);
            if (info.Exists)
            {
                throw new AssertFailedException($"File \"{path}\" exists.");
            }
        }


        /// <summary>
        /// Asserts that a directory exists.
        /// </summary>
        /// <param name="assert"><see cref="Assert.That"/></param>
        /// <param name="path">The path to the directory.</param>
        public static void DirectoryExists(this Assert assert, string path)
        {
            Assert.IsFalse(string.IsNullOrEmpty(path), "Attempted to assert null or empty path exists.");

            var info = new DirectoryInfo(path);
            if (!info.Exists)
            {
                throw new AssertFailedException($"Directory \"{path}\" does not exist.");
            }
        }

        /// <summary>
        /// Asserts that a directory exists.
        /// </summary>
        /// <param name="assert"><see cref="Assert.That"/></param>
        /// <param name="path">The path to the directory.</param>
        public static void DirectoryDoesNotExists(this Assert assert, string path)
        {
            Assert.IsFalse(string.IsNullOrEmpty(path), "Attempted to assert null or empty path does not exist.");

            var info = new DirectoryInfo(path);
            if (info.Exists)
            {
                throw new AssertFailedException($"Directory \"{path}\" exists.");
            }
        }


        /// <summary>
        /// Asserts that all bytes in the file are within the ASCII range.
        /// </summary>
        /// <param name="assert"><see cref="Assert.That"/></param>
        /// <param name="path">The path to the file.</param>
        public static void FileIsAsciiOnly(this Assert assert, string path)
        {
            FileExists(assert, path);

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                if (stream.ReadByte() > 0x7F)
                {
                    throw new AssertFailedException($"Invalid character at byte {stream.Position - 1}");
                }
            }
        }

        /// <summary>
        /// Asserts that a file can be read using a specific encoding.
        /// </summary>
        /// <param name="assert"><see cref="Assert.That"/></param>
        /// <param name="path">The path to the file.</param>
        /// <param name="encoding">The encoding to check the file against.</param>
        public static void FileUsesEncoding(this Assert assert, string path, Encoding encoding)
        {
            FileExists(assert, path);
            Assert.IsNotNull(encoding, $"Attempted to call {nameof(FileAssert)}.{nameof(FileUsesEncoding)} with null encoding.");

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                // get a copy of the encoding that throws on un-mappable characters.
                var throwingEncoding = Encoding.GetEncoding(encoding.CodePage,
                    EncoderFallback.ExceptionFallback,
                    DecoderFallback.ExceptionFallback
                );

                try
                {
                    using (var reader = new StreamReader(stream, throwingEncoding, detectEncodingFromByteOrderMarks: false))
                    {
                        // just read the lines - if there's no exception, then we're good.
                        string line = reader.ReadLine();
                        while (line != null)
                        {
                            line = reader.ReadLine();
                        }
                    }
                }
                catch (DecoderFallbackException ex)
                {
                    throw new AssertFailedException(
                        $"File is not in the encoding {encoding.EncodingName}",
                        ex
                    );
                }
            }
        }


        /// <summary>
        /// Asserts that a file contains a UTF-8 encoded string.
        /// </summary>
        /// <param name="assert"><see cref="Assert.That"/></param>
        /// <param name="path">The path to the file to check.</param>
        /// <param name="expected">The expected string.</param>
        public static void FileContainsString(this Assert assert, string path, string expected)
        {
            FileContainsString(assert, path, expected, new UTF8Encoding(false));
        }

        /// <summary>
        /// Asserts that a file contains a string.
        /// </summary>
        /// <param name="assert"><see cref="Assert.That"/></param>
        /// <param name="path">The path to the file to check.</param>
        /// <param name="expected">The expected string.</param>
        /// <param name="encoding">The encoding of the string to search for.</param>
        public static void FileContainsString(this Assert assert, string path, string expected, Encoding encoding)
        {
            Assert.IsNotNull(expected, "Attempted to assert that file contains null string");
            Assert.IsNotNull(encoding, $"Attempted to call {nameof(FileAssert)}.{nameof(FileContainsString)} with a null encoding.");

            byte[] textBytes = encoding.GetBytes(expected);

            if (!InternalContainsBytes(path, textBytes))
            {
                throw new AssertFailedException("File does not contain the expected string, \"\".");
            }
        }


        /// <summary>
        /// Asserts that a file does not contain a UTF-8 encoded string.
        /// </summary>
        /// <param name="assert"><see cref="Assert.That"/></param>
        /// <param name="path">The path to the file to check.</param>
        /// <param name="notExpected">The string that is not expected.</param>
        public static void FileDoesNotContainString(this Assert assert, string path, string notExpected)
        {
            FileDoesNotContainString(assert, path, notExpected, new UTF8Encoding(false));
        }

        /// <summary>
        /// Asserts that a file does not contain a string.
        /// </summary>
        /// <param name="assert"><see cref="Assert.That"/></param>
        /// <param name="path">The path to the file to check.</param>
        /// <param name="notExpected">The string that is not expected.</param>
        /// <param name="encoding">The encoding of the string to search for.</param>
        public static void FileDoesNotContainString(this Assert assert, string path, string notExpected, Encoding encoding)
        {
            Assert.IsNotNull(notExpected, "Attempted to assert that file does not contain null string");
            Assert.AreNotEqual(0, notExpected.Length, "Attempted to assert that file does not contain null string");
            Assert.IsNotNull(encoding, $"Attempted to call {nameof(FileAssert)}.{nameof(FileContainsString)} with a null encoding.");

            byte[] textBytes = encoding.GetBytes(notExpected);

            if (InternalContainsBytes(path, textBytes))
            {
                throw new AssertFailedException("File does contains the expected string, \"\".");
            }
        }


        /// <summary>
        /// Asserts that a file contains a byte sequence.
        /// </summary>
        /// <param name="assert"><see cref="Assert.That"/></param>
        /// <param name="path">The path to the file to check.</param>
        /// <param name="expectedSequence">The expected byte sequence.</param>
        public static void FileContainsBytes(this Assert assert, string path, byte[] expectedSequence)
        {
            if (!InternalContainsBytes(path, expectedSequence))
            {
                throw new AssertFailedException("File does not contain the expected byte sequence.");
            }
        }

        /// <summary>
        /// Asserts that a file does not contain a byte sequence.
        /// </summary>
        /// <param name="assert"><see cref="Assert.That"/></param>
        /// <param name="path">The path to the file to check.</param>
        /// <param name="notExpectedSequence">The byte sequence that is not expected.</param>
        public static void FileDoesNotContainBytes(this Assert assert, string path, byte[] notExpectedSequence)
        {
            if (InternalContainsBytes(path, notExpectedSequence))
            {
                throw new AssertFailedException("File does not contain the expected byte sequence.");
            }
        }


        private static bool InternalContainsBytes(string path, byte[] searchBytes)
        {
            Assert.That.FileExists(path);

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                int idx = 0;

                int b = stream.ReadByte();

                while (idx < searchBytes.Length &&
                       b == searchBytes[idx])
                {
                    b = stream.ReadByte();
                }

                if (idx >= searchBytes.Length)
                {
                    // we found the whole string.
                    return true;
                }
            }

            return false;
        }
    }

#endif // NO_FILESYSTEM
}
