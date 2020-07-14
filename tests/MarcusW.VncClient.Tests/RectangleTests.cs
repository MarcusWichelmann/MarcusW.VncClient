using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace MarcusW.VncClient.Tests
{
    public class RectangleTests
    {
        public static IEnumerable<object[]> CroppingTests
            => new[] {
                new object[] { new Rectangle(10, 10, 50, 50), new Rectangle(0, 0, 100, 100), new Rectangle(10, 10, 50, 50) },
                new object[] { new Rectangle(0, 0, 100, 100), new Rectangle(50, 50, 10, 10), new Rectangle(50, 50, 10, 10) },
                new object[] { new Rectangle(0, 0, 100, 100), new Rectangle(0, 0, 200, 200), new Rectangle(0, 0, 100, 100) },
                new object[] { new Rectangle(-100, -100, 200, 200), new Rectangle(0, 0, 200, 200), new Rectangle(0, 0, 100, 100) },
                new object[] { new Rectangle(200, 200, 10, 10), new Rectangle(0, 0, 100, 100), new Rectangle(100, 100, 0, 0) },
                new object[] { new Rectangle(-100, -100, 50, 50), new Rectangle(-70, -70, 100, 100), new Rectangle(-70, -70, 20, 20) },
                new object[] { new Rectangle(-100, -100, 50, 50), new Rectangle(-10, -10, 100, 100), new Rectangle(-10, -10, 0, 0) },
                new object[] { new Rectangle(-100, 10, 50, 50), new Rectangle(0, 0, 100, 100), new Rectangle(0, 10, 0, 50) }
            };

        [Theory]
        [MemberData(nameof(CroppingTests))]
        public void Rectangles_Are_Cropped_Correctly(Rectangle rectangle, Rectangle area, Rectangle expectedResult)
        {
            Assert.Equal(expectedResult, rectangle.CroppedTo(area));
        }
    }
}
