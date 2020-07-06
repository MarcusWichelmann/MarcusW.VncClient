using System.Collections.Generic;
using MarcusW.VncClient.Protocol;
using Xunit;

namespace MarcusW.VncClient.Tests
{
    public class PixelFormatTests
    {
        public static IEnumerable<object[]> CommonPixelFormats
            => new[] {
                new object[] { PixelFormat.RGB111, nameof(PixelFormat.RGB111) },
                new object[] { PixelFormat.BGR111, nameof(PixelFormat.BGR111) },
                new object[] { PixelFormat.RGB222, nameof(PixelFormat.RGB222) },
                new object[] { PixelFormat.BGR222, nameof(PixelFormat.BGR222) },
                new object[] { PixelFormat.RGB332, nameof(PixelFormat.RGB332) },
                new object[] { PixelFormat.BGR332, nameof(PixelFormat.BGR332) },
                new object[] { PixelFormat.RGB565, nameof(PixelFormat.RGB565) },
                new object[] { PixelFormat.BGR565, nameof(PixelFormat.BGR565) },
                new object[] { PixelFormat.RGB888, nameof(PixelFormat.RGB888) },
                new object[] { PixelFormat.BGR888, nameof(PixelFormat.BGR888) }
            };

        public static IEnumerable<object[]> InvalidPixelFormats
            => new[] {
                new object[] { PixelFormat.Unknown },
                new object[] { new PixelFormat(32, 24, false, true, 100, 0xFF, 0xFF, 16, 8, 0) },
                new object[] { new PixelFormat(32, 24, false, true, 0xFF, 100, 0xFF, 16, 8, 0) },
                new object[] { new PixelFormat(32, 24, false, true, 0xFF, 0xFF, 100, 16, 8, 0) },
                new object[] { new PixelFormat(32, 24, false, true, 0xFF, 0xFF, 0xFF, 8, 8, 0) },
                new object[] { new PixelFormat(32, 24, false, true, 0xFF, 0xFF, 0xFF, 0, 16, 8) },
            };

        [Theory]
        [MemberData(nameof(CommonPixelFormats))]
        public void ToShortString_Returns_Correct_Value(PixelFormat pixelFormat, string expectedString)
        {
            Assert.Equal(expectedString, pixelFormat.ToShortString(), true);
        }

        [Theory]
        [MemberData(nameof(InvalidPixelFormats))]
        public void ToShortString_Throws_For_Incorrect_PixelFormats(PixelFormat pixelFormat)
        {
            Assert.Throws<UnexpectedDataException>(pixelFormat.ToShortString);
        }
    }
}
