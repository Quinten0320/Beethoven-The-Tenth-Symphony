using BeethovenBusiness.Interfaces;
using BeethovenBusiness.PreviewLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;

namespace Unittesten
{
    public class PreviewLogicTests
    {
        [Test]
        public void GetDuration_ShouldReturnFormattedTime_WhenDataReturnsSeconds()
        {
            // Arrange
            var mockData = new Mock<IData>();
            mockData.Setup(d => d.SelectedSongDuration(It.IsAny<string>())).Returns(125); // 125 seconden = 2:05
            var logic = new PreviewLogic(mockData.Object);

            // Act
            var result = logic.GetDuration("test-song");

            // Assert
            Assert.AreEqual("2:05", result);
        }

        [Test]
        public void GetDuration_ShouldHandleZeroSeconds()
        {
            var mockData = new Mock<IData>();
            mockData.Setup(d => d.SelectedSongDuration(It.IsAny<string>())).Returns(0);
            var logic = new PreviewLogic(mockData.Object);

            var result = logic.GetDuration("test-song");

            Assert.AreEqual("0:00", result);
        }
    }
}
