using MarcusW.VncClient.Protocol;
using Xunit;

namespace MarcusW.VncClient.Tests.Protocol
{
    public class RfbProtocolVersionTests
    {
        [Theory]
        [InlineData("RFB 003.003", RfbProtocolVersion.RFB_3_3)]
        [InlineData("RFB 003.005", RfbProtocolVersion.RFB_3_3)]
        [InlineData("RFB 003.007", RfbProtocolVersion.RFB_3_7)]
        [InlineData("RFB 003.008", RfbProtocolVersion.RFB_3_8)]
        [InlineData("RFB 003.009", RfbProtocolVersion.RFB_3_9)]
        [InlineData("RFB 003.010", RfbProtocolVersion.Unknown)]
        [InlineData("Invalid", RfbProtocolVersion.Unknown)]
        public void GetFromStringRepresentation_ReturnsCorrectVersion(string input, RfbProtocolVersion expected)
        {
            // Act
            var result = RfbProtocolVersions.GetFromStringRepresentation(input);
            
            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(RfbProtocolVersion.RFB_3_3, "RFB 003.003")]
        [InlineData(RfbProtocolVersion.RFB_3_7, "RFB 003.007")]
        [InlineData(RfbProtocolVersion.RFB_3_8, "RFB 003.008")]
        [InlineData(RfbProtocolVersion.RFB_3_9, "RFB 003.009")]
        public void GetStringRepresentation_ReturnsCorrectString(RfbProtocolVersion version, string expected)
        {
            // Act
            var result = version.GetStringRepresentation();
            
            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(RfbProtocolVersion.RFB_3_3, "RFB 3.3")]
        [InlineData(RfbProtocolVersion.RFB_3_7, "RFB 3.7")]
        [InlineData(RfbProtocolVersion.RFB_3_8, "RFB 3.8")]
        [InlineData(RfbProtocolVersion.RFB_3_9, "RFB 3.9")]
        [InlineData(RfbProtocolVersion.Unknown, "Unknown")]
        public void ToReadableString_ReturnsCorrectString(RfbProtocolVersion version, string expected)
        {
            // Act
            var result = version.ToReadableString();
            
            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void LatestSupported_IsRfb38()
        {
            // Assert - keeping 3.8 as latest supported for stability
            Assert.Equal(RfbProtocolVersion.RFB_3_8, RfbProtocolVersions.LatestSupported);
        }
    }
}
