using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using BeethovenBusiness.NewFolder;
using BeethovenBusiness.Interfaces;
using BeethovenBusiness.GameStatistics;
using BeethovenBusiness.MidiFileLogica;
using System.Collections.Generic;


namespace Unittesten
{
    public class GameStatsTest
    {
        private readonly Mock<IData> _mockData;
        private readonly GameStatsService _service;

        public GameStatsTest()
        {
            _mockData = new Mock<IData>();

            // Dummy session en song instellen
            var dummySession = new Session { Duration = 30.5, SongID = 1 };
            var dummySong = new Song { SongID = 1, Title = "Test Song" };

            _mockData.Setup(d => d.getSessionDetails()).Returns(dummySession);
            _mockData.Setup(d => d.GetSongDetails(1)).Returns(dummySong);
            _mockData.Setup(d => d.GetTotalAmountOfSongs()).Returns(10);
            _mockData.Setup(d => d.GetAmountOfSongsthisMont()).Returns(3);
            _mockData.Setup(d => d.GetScoresBySongId(1)).Returns(new List<int> { 80, 90 });

            _service = new GameStatsService(_mockData.Object);
        }

        [Fact]
        public void GetTotalAmountOfSongs_ReturnsCorrectValue()
        {
            Assert.Equals(10, _service.AmountOfSongs);
        }

        [Fact]
        public void GetAmountOfSongsThisMonth_ReturnsCorrectValue()
        {
            Assert.Equals(3, _service.AmountOfSongsThisMonth);
        }

        [Fact]
        public void GetLastPlayedSong_ReturnsCorrectTitle()
        {
            Assert.Equals("Test Song", _service.LastPlayedSong);
        }

        [Fact]
        public void GetSessionDetails_ReturnsCorrectDuration()
        {
            var session = _service.GetSessionDetails();
            Assert.Equals(30.5, session.Duration);
        }

        [Fact]
        public void GetScoreBoardData_ReturnsListOfScores()
        {
            var scores = _service.GetScoreBoardData();
            Assert.Contains(80, scores);
            Assert.Contains(90, scores);
        }
    }
}
