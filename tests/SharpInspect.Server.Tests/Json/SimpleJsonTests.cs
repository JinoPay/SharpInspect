using System;
using System.Collections.Generic;
using FluentAssertions;
using SharpInspect.Core.Models;
using SharpInspect.Server.Json;
using Xunit;

namespace SharpInspect.Server.Tests.Json
{
    /// <summary>
    ///     SimpleJson 클래스의 단위 테스트.
    /// </summary>
    public class SimpleJsonTests
    {
        #region Null Tests

        [Fact]
        public void Serialize_Null_ReturnsNullString()
        {
            // Act
            var result = SimpleJson.Serialize(null);

            // Assert
            result.Should().Be("null");
        }

        #endregion

        #region String Tests

        [Fact]
        public void Serialize_EmptyString_ReturnsEmptyQuotedString()
        {
            // Act
            var result = SimpleJson.Serialize("");

            // Assert
            result.Should().Be("\"\"");
        }

        [Fact]
        public void Serialize_SimpleString_ReturnsQuotedString()
        {
            // Act
            var result = SimpleJson.Serialize("hello");

            // Assert
            result.Should().Be("\"hello\"");
        }

        [Fact]
        public void Serialize_StringWithQuotes_EscapesQuotes()
        {
            // Act
            var result = SimpleJson.Serialize("hello \"world\"");

            // Assert
            result.Should().Be("\"hello \\\"world\\\"\"");
        }

        [Fact]
        public void Serialize_StringWithBackslash_EscapesBackslash()
        {
            // Act
            var result = SimpleJson.Serialize("path\\to\\file");

            // Assert
            result.Should().Be("\"path\\\\to\\\\file\"");
        }

        [Fact]
        public void Serialize_StringWithNewline_EscapesNewline()
        {
            // Act
            var result = SimpleJson.Serialize("line1\nline2");

            // Assert
            result.Should().Be("\"line1\\nline2\"");
        }

        [Fact]
        public void Serialize_StringWithTab_EscapesTab()
        {
            // Act
            var result = SimpleJson.Serialize("col1\tcol2");

            // Assert
            result.Should().Be("\"col1\\tcol2\"");
        }

        [Fact]
        public void Serialize_StringWithCarriageReturn_EscapesCarriageReturn()
        {
            // Act
            var result = SimpleJson.Serialize("line1\rline2");

            // Assert
            result.Should().Be("\"line1\\rline2\"");
        }

        [Fact]
        public void Serialize_StringWithControlChars_EscapesAsUnicode()
        {
            // Act
            var result = SimpleJson.Serialize("test\u0001char");

            // Assert
            result.Should().Be("\"test\\u0001char\"");
        }

        [Fact]
        public void Serialize_KoreanString_PreservesUnicode()
        {
            // Act
            var result = SimpleJson.Serialize("안녕하세요");

            // Assert
            result.Should().Be("\"안녕하세요\"");
        }

        #endregion

        #region Number Tests

        [Fact]
        public void Serialize_Int_ReturnsNumberString()
        {
            // Act
            var result = SimpleJson.Serialize(42);

            // Assert
            result.Should().Be("42");
        }

        [Fact]
        public void Serialize_Long_ReturnsNumberString()
        {
            // Act
            var result = SimpleJson.Serialize(9223372036854775807L);

            // Assert
            result.Should().Be("9223372036854775807");
        }

        [Fact]
        public void Serialize_Float_ReturnsInvariantCultureNumber()
        {
            // Act
            var result = SimpleJson.Serialize(3.14f);

            // Assert
            result.Should().Contain("3.14");
        }

        [Fact]
        public void Serialize_Double_ReturnsInvariantCultureNumber()
        {
            // Act
            var result = SimpleJson.Serialize(3.14159265358979);

            // Assert
            result.Should().Contain("3.14159265358979");
        }

        [Fact]
        public void Serialize_Decimal_ReturnsInvariantCultureNumber()
        {
            // Act
            var result = SimpleJson.Serialize(123.456m);

            // Assert
            result.Should().Be("123.456");
        }

        [Fact]
        public void Serialize_NegativeNumber_IncludesSign()
        {
            // Act
            var result = SimpleJson.Serialize(-42);

            // Assert
            result.Should().Be("-42");
        }

        [Fact]
        public void Serialize_Short_ReturnsNumberString()
        {
            // Act
            var result = SimpleJson.Serialize((short)123);

            // Assert
            result.Should().Be("123");
        }

        [Fact]
        public void Serialize_Byte_ReturnsNumberString()
        {
            // Act
            var result = SimpleJson.Serialize((byte)255);

            // Assert
            result.Should().Be("255");
        }

        #endregion

        #region Boolean Tests

        [Fact]
        public void Serialize_True_ReturnsTrueString()
        {
            // Act
            var result = SimpleJson.Serialize(true);

            // Assert
            result.Should().Be("true");
        }

        [Fact]
        public void Serialize_False_ReturnsFalseString()
        {
            // Act
            var result = SimpleJson.Serialize(false);

            // Assert
            result.Should().Be("false");
        }

        #endregion

        #region DateTime Tests

        [Fact]
        public void Serialize_DateTime_ReturnsIso8601Format()
        {
            // Arrange
            var dt = new DateTime(2024, 1, 15, 10, 30, 0);

            // Act
            var result = SimpleJson.Serialize(dt);

            // Assert
            result.Should().StartWith("\"");
            result.Should().EndWith("\"");
            result.Should().Contain("2024-01-15");
        }

        [Fact]
        public void Serialize_DateTimeUtc_PreservesUtcMarker()
        {
            // Arrange
            var dt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

            // Act
            var result = SimpleJson.Serialize(dt);

            // Assert
            result.Should().Contain("Z");
        }

        #endregion

        #region Enum Tests

        [Fact]
        public void Serialize_Enum_ReturnsStringValue()
        {
            // Act
            var result = SimpleJson.Serialize(SharpInspectLogLevel.Error);

            // Assert
            result.Should().Be("\"Error\"");
        }

        #endregion

        #region Array Tests

        [Fact]
        public void Serialize_EmptyArray_ReturnsEmptyBrackets()
        {
            // Act
            var result = SimpleJson.Serialize(new int[0]);

            // Assert
            result.Should().Be("[]");
        }

        [Fact]
        public void Serialize_IntArray_ReturnsBracketedNumbers()
        {
            // Act
            var result = SimpleJson.Serialize(new[] { 1, 2, 3 });

            // Assert
            result.Should().Be("[1,2,3]");
        }

        [Fact]
        public void Serialize_StringArray_ReturnsBracketedStrings()
        {
            // Act
            var result = SimpleJson.Serialize(new[] { "a", "b", "c" });

            // Assert
            result.Should().Be("[\"a\",\"b\",\"c\"]");
        }

        [Fact]
        public void Serialize_MixedList_ReturnsCorrectTypes()
        {
            // Arrange
            var list = new List<object> { 1, "two", true, null };

            // Act
            var result = SimpleJson.Serialize(list);

            // Assert
            result.Should().Be("[1,\"two\",true,null]");
        }

        #endregion

        #region Dictionary Tests

        [Fact]
        public void Serialize_EmptyDictionary_ReturnsEmptyBraces()
        {
            // Arrange
            var dict = new Dictionary<string, string>();

            // Act
            var result = SimpleJson.Serialize(dict);

            // Assert
            result.Should().Be("{}");
        }

        [Fact]
        public void Serialize_StringDictionary_ReturnsKeyValuePairs()
        {
            // Arrange
            var dict = new Dictionary<string, int>
            {
                { "one", 1 },
                { "two", 2 }
            };

            // Act
            var result = SimpleJson.Serialize(dict);

            // Assert
            result.Should().Contain("\"one\":1");
            result.Should().Contain("\"two\":2");
        }

        #endregion

        #region Object Tests

        [Fact]
        public void Serialize_SimpleObject_ReturnsCamelCaseProperties()
        {
            // Arrange
            var obj = new { FirstName = "John", LastName = "Doe" };

            // Act
            var result = SimpleJson.Serialize(obj);

            // Assert
            result.Should().Contain("\"firstName\":\"John\"");
            result.Should().Contain("\"lastName\":\"Doe\"");
        }

        [Fact]
        public void Serialize_ObjectWithNullProperty_IncludesNullValue()
        {
            // Arrange
            var obj = new { Name = "Test", Value = (string)null };

            // Act
            var result = SimpleJson.Serialize(obj);

            // Assert
            result.Should().Contain("\"name\":\"Test\"");
            result.Should().Contain("\"value\":null");
        }

        [Fact]
        public void Serialize_NestedObject_SerializesNested()
        {
            // Arrange
            var obj = new
            {
                Name = "Parent",
                Child = new { Name = "Child" }
            };

            // Act
            var result = SimpleJson.Serialize(obj);

            // Assert
            result.Should().Contain("\"name\":\"Parent\"");
            result.Should().Contain("\"child\":{");
            result.Should().Contain("\"name\":\"Child\"");
        }

        #endregion

        #region Model Tests

        [Fact]
        public void Serialize_ConsoleEntry_ProducesValidJson()
        {
            // Arrange
            var entry = new ConsoleEntry("Test message", SharpInspectLogLevel.Warning)
            {
                Category = "TestCategory"
            };

            // Act
            var result = SimpleJson.Serialize(entry);

            // Assert
            result.Should().StartWith("{");
            result.Should().EndWith("}");
            result.Should().Contain("\"message\":\"Test message\"");
            result.Should().Contain("\"level\":\"Warning\"");
            result.Should().Contain("\"category\":\"TestCategory\"");
            result.Should().Contain("\"id\":");
        }

        [Fact]
        public void Serialize_NetworkEntry_ProducesValidJson()
        {
            // Arrange
            var entry = new NetworkEntry
            {
                Url = "https://example.com/api/test",
                Method = "GET",
                StatusCode = 200,
                TotalMs = 150.5
            };

            // Act
            var result = SimpleJson.Serialize(entry);

            // Assert
            result.Should().StartWith("{");
            result.Should().EndWith("}");
            result.Should().Contain("\"url\":\"https://example.com/api/test\"");
            result.Should().Contain("\"method\":\"GET\"");
            result.Should().Contain("\"statusCode\":200");
            result.Should().Contain("\"totalMs\":150.5");
        }

        [Fact]
        public void Serialize_PerformanceEntry_ProducesValidJson()
        {
            // Arrange
            var entry = new PerformanceEntry
            {
                CpuUsagePercent = 45.5,
                TotalMemoryBytes = 1024 * 1024 * 512,
                Gen0Collections = 100,
                Gen1Collections = 20,
                Gen2Collections = 5
            };

            // Act
            var result = SimpleJson.Serialize(entry);

            // Assert
            result.Should().StartWith("{");
            result.Should().EndWith("}");
            result.Should().Contain("\"cpuUsagePercent\":45.5");
            result.Should().Contain("\"totalMemoryBytes\":");
            result.Should().Contain("\"gen0Collections\":100");
        }

        [Fact]
        public void Serialize_ApplicationInfo_ProducesValidJson()
        {
            // Arrange
            var entry = new ApplicationInfo
            {
                AssemblyName = "TestApp",
                RuntimeVersion = "8.0.0",
                ProcessId = 12345,
                ProcessorCount = 8
            };
            entry.EnvironmentVariables["PATH"] = "/usr/bin";
            entry.LoadedAssemblies.Add(new AssemblyInfo
            {
                Name = "System.Core",
                Version = "8.0.0.0"
            });

            // Act
            var result = SimpleJson.Serialize(entry);

            // Assert
            result.Should().StartWith("{");
            result.Should().EndWith("}");
            result.Should().Contain("\"assemblyName\":\"TestApp\"");
            result.Should().Contain("\"runtimeVersion\":\"8.0.0\"");
            result.Should().Contain("\"processId\":12345");
            result.Should().Contain("\"environmentVariables\":");
            result.Should().Contain("\"loadedAssemblies\":");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Serialize_ObjectWithAllEscapeCharacters_ProducesValidJson()
        {
            // Arrange
            var obj = new { Text = "quotes\"backslash\\tab\tnewline\ncarriage\rbackspace\bformfeed\f" };

            // Act
            var result = SimpleJson.Serialize(obj);

            // Assert
            result.Should().Contain("\\\"");
            result.Should().Contain("\\\\");
            result.Should().Contain("\\t");
            result.Should().Contain("\\n");
            result.Should().Contain("\\r");
            result.Should().Contain("\\b");
            result.Should().Contain("\\f");
        }

        [Fact]
        public void Serialize_DeeplyNestedObject_SerializesCorrectly()
        {
            // Arrange
            var obj = new
            {
                Level1 = new
                {
                    Level2 = new
                    {
                        Level3 = new
                        {
                            Value = "Deep"
                        }
                    }
                }
            };

            // Act
            var result = SimpleJson.Serialize(obj);

            // Assert
            result.Should().Contain("\"value\":\"Deep\"");
        }

        [Fact]
        public void Serialize_ArrayOfObjects_SerializesCorrectly()
        {
            // Arrange
            var arr = new[]
            {
                new { Id = 1, Name = "First" },
                new { Id = 2, Name = "Second" }
            };

            // Act
            var result = SimpleJson.Serialize(arr);

            // Assert
            result.Should().StartWith("[");
            result.Should().EndWith("]");
            result.Should().Contain("\"id\":1");
            result.Should().Contain("\"id\":2");
            result.Should().Contain("\"name\":\"First\"");
            result.Should().Contain("\"name\":\"Second\"");
        }

        #endregion
    }
}
