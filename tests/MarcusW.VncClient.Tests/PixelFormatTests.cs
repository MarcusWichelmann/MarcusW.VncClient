using System.Collections.Generic;
using Xunit;

namespace MarcusW.VncClient.Tests
{
    public class PixelFormatTests
    {
        [Fact]
        public void Unknown_Format_Has_Zero_Values()
        {
            PixelFormat unknown = PixelFormat.Unknown;

            Assert.Equal("Unknown", unknown.Name);
            Assert.Equal(0, unknown.BitsPerPixel);
            Assert.Equal(0, unknown.Depth);
            Assert.False(unknown.BigEndian);
            Assert.False(unknown.TrueColor);
            Assert.False(unknown.HasAlpha);
            Assert.Equal(0, unknown.RedMax);
            Assert.Equal(0, unknown.GreenMax);
            Assert.Equal(0, unknown.BlueMax);
            Assert.Equal(0, unknown.AlphaMax);
        }

        [Fact]
        public void Plain_Format_Has_Expected_Values()
        {
            PixelFormat plain = PixelFormat.Plain;

            Assert.Equal("Plain RGBA", plain.Name);
            Assert.Equal(32, plain.BitsPerPixel);
            Assert.Equal(32, plain.Depth);
            Assert.False(plain.BigEndian);
            Assert.True(plain.TrueColor);
            Assert.True(plain.HasAlpha);
            Assert.Equal(255, plain.RedMax);
            Assert.Equal(255, plain.GreenMax);
            Assert.Equal(255, plain.BlueMax);
            Assert.Equal(255, plain.AlphaMax);
            Assert.Equal(24, plain.RedShift);
            Assert.Equal(16, plain.GreenShift);
            Assert.Equal(8, plain.BlueShift);
            Assert.Equal(0, plain.AlphaShift);
        }

        [Fact]
        public void BytesPerPixel_Is_Computed_Correctly()
        {
            PixelFormat format32 = PixelFormat.Plain;
            Assert.Equal(4, format32.BytesPerPixel);

            var format16 = new PixelFormat("16bit", 16, 16, false, true, false, 31, 63, 31, 0, 11, 5, 0, 0);
            Assert.Equal(2, format16.BytesPerPixel);

            var format8 = new PixelFormat("8bit", 8, 8, false, true, false, 7, 7, 3, 0, 5, 2, 0, 0);
            Assert.Equal(1, format8.BytesPerPixel);
        }

        [Fact]
        public void LittleEndian_Is_Inverse_Of_BigEndian()
        {
            var littleEndian = new PixelFormat("LE", 32, 32, false, true, false, 255, 255, 255, 0, 16, 8, 0, 0);
            Assert.True(littleEndian.LittleEndian);
            Assert.False(littleEndian.BigEndian);

            var bigEndian = new PixelFormat("BE", 32, 32, true, true, false, 255, 255, 255, 0, 16, 8, 0, 0);
            Assert.False(bigEndian.LittleEndian);
            Assert.True(bigEndian.BigEndian);
        }

        [Fact]
        public void Alpha_Values_Are_Zeroed_When_HasAlpha_Is_False()
        {
            var noAlpha = new PixelFormat("NoAlpha", 32, 32, false, true, false, 255, 255, 255, 128, 16, 8, 0, 24);
            Assert.Equal(0, noAlpha.AlphaMax);
            Assert.Equal(0, noAlpha.AlphaShift);
        }

        [Fact]
        public void Alpha_Values_Are_Preserved_When_HasAlpha_Is_True()
        {
            var withAlpha = new PixelFormat("WithAlpha", 32, 32, false, true, true, 255, 255, 255, 128, 16, 8, 0, 24);
            Assert.Equal(128, withAlpha.AlphaMax);
            Assert.Equal(24, withAlpha.AlphaShift);
        }

        [Fact]
        public void Equal_Formats_Are_Equal()
        {
            var format1 = new PixelFormat("Test", 32, 32, false, true, true, 255, 255, 255, 255, 24, 16, 8, 0);
            var format2 = new PixelFormat("Test", 32, 32, false, true, true, 255, 255, 255, 255, 24, 16, 8, 0);

            Assert.Equal(format1, format2);
            Assert.True(format1 == format2);
            Assert.False(format1 != format2);
            Assert.Equal(format1.GetHashCode(), format2.GetHashCode());
        }

        [Fact]
        public void Different_Formats_Are_Not_Equal()
        {
            var format1 = new PixelFormat("RGB32", 32, 32, false, true, false, 255, 255, 255, 0, 16, 8, 0, 0);
            var format2 = new PixelFormat("BGR32", 32, 32, false, true, false, 255, 255, 255, 0, 0, 8, 16, 0);

            Assert.NotEqual(format1, format2);
            Assert.True(format1 != format2);
            Assert.False(format1 == format2);
        }

        [Fact]
        public void Different_Name_Same_Layout_Are_Not_Equal()
        {
            var format1 = new PixelFormat("Name1", 32, 32, false, true, false, 255, 255, 255, 0, 16, 8, 0, 0);
            var format2 = new PixelFormat("Name2", 32, 32, false, true, false, 255, 255, 255, 0, 16, 8, 0, 0);

            // Equals considers Name, so different names means not equal
            Assert.NotEqual(format1, format2);
        }

        [Fact]
        public void Equals_With_Object_Returns_False_For_Non_PixelFormat()
        {
            PixelFormat format = PixelFormat.Plain;

            Assert.False(format.Equals(null));
            Assert.False(format.Equals("not a pixel format"));
            Assert.False(format.Equals(42));
        }

        [Fact]
        public void IsBinaryCompatibleTo_Same_Format_Returns_True()
        {
            PixelFormat plain = PixelFormat.Plain;
            Assert.True(plain.IsBinaryCompatibleTo(plain));
        }

        [Fact]
        public void IsBinaryCompatibleTo_Different_BitsPerPixel_Returns_False()
        {
            var format32 = new PixelFormat("32", 32, 32, false, true, false, 255, 255, 255, 0, 16, 8, 0, 0);
            var format16 = new PixelFormat("16", 16, 16, false, true, false, 31, 63, 31, 0, 11, 5, 0, 0);

            Assert.False(format32.IsBinaryCompatibleTo(format16));
        }

        [Fact]
        public void IsBinaryCompatibleTo_Different_Endianness_Returns_False()
        {
            var le = new PixelFormat("LE", 32, 32, false, true, false, 255, 255, 255, 0, 16, 8, 0, 0);
            var be = new PixelFormat("BE", 32, 32, true, true, false, 255, 255, 255, 0, 16, 8, 0, 0);

            Assert.False(le.IsBinaryCompatibleTo(be));
        }

        [Fact]
        public void IsBinaryCompatibleTo_Different_ColorShifts_Returns_False()
        {
            var rgb = new PixelFormat("RGB", 32, 32, false, true, false, 255, 255, 255, 0, 16, 8, 0, 0);
            var bgr = new PixelFormat("BGR", 32, 32, false, true, false, 255, 255, 255, 0, 0, 8, 16, 0);

            Assert.False(rgb.IsBinaryCompatibleTo(bgr));
        }

        [Fact]
        public void IsBinaryCompatibleTo_IgnoreAlpha_Ignores_Alpha_Differences()
        {
            var noAlpha = new PixelFormat("NoAlpha", 32, 32, false, true, false, 255, 255, 255, 0, 16, 8, 0, 0);
            var withAlpha = new PixelFormat("WithAlpha", 32, 32, false, true, true, 255, 255, 255, 255, 16, 8, 0, 24);

            Assert.False(noAlpha.IsBinaryCompatibleTo(withAlpha, ignoreAlpha: false));
            Assert.True(noAlpha.IsBinaryCompatibleTo(withAlpha, ignoreAlpha: true));
        }

        [Fact]
        public void ToString_Unknown_Returns_Unknown_String()
        {
            Assert.Equal("Unknown", PixelFormat.Unknown.ToString());
        }

        [Fact]
        public void ToString_Contains_Format_Details()
        {
            string result = PixelFormat.Plain.ToString();

            Assert.Contains("Plain RGBA", result);
            Assert.Contains("32bpp", result);
            Assert.Contains("LE", result);
            Assert.Contains("True-Color", result);
        }

        [Fact]
        public void ToString_BigEndian_Format_Shows_BE()
        {
            var be = new PixelFormat("BigEndian", 32, 32, true, true, false, 255, 255, 255, 0, 16, 8, 0, 0);
            string result = be.ToString();

            Assert.Contains("BE", result);
            Assert.DoesNotContain("LE", result);
        }

        [Fact]
        public void ToString_ColorMap_Format_Shows_Color_Map()
        {
            var colorMap = new PixelFormat("Mapped", 8, 8, false, false, false, 0, 0, 0, 0, 0, 0, 0, 0);
            string result = colorMap.ToString();

            Assert.Contains("Color-Map", result);
        }

        [Fact]
        public void ToString_Alpha_Format_Shows_Alpha_Info()
        {
            string result = PixelFormat.Plain.ToString();

            // Plain format has alpha, so alpha info should be present
            Assert.Contains("A:", result);
        }
    }
}
