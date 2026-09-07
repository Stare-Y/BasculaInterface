using Infrastructure.Service;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// Device turn-taking lock: only one device may "hold the scale" at a time, the holder
    /// can renew, and only the holder can release. The 10-second auto-expiry
    /// (<c>KeepTurnAliveAsync</c>) is intentionally not exercised here — a unit test should not
    /// sleep for real time; make <c>_turnTimeout</c> injectable if that path needs coverage.
    /// </summary>
    public class WeightLogisticServiceTests
    {
        private readonly WeightLogisticService _sut = new();

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task RequestWeight_rejects_blank_device_id(string? deviceId)
        {
            Assert.False(await _sut.RequestWeight(deviceId!));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task ReleaseWeight_rejects_blank_device_id(string? deviceId)
        {
            Assert.False(await _sut.ReleaseWeight(deviceId!));
        }

        [Fact]
        public async Task RequestWeight_grants_the_turn_to_a_free_scale()
        {
            Assert.True(await _sut.RequestWeight("device-A"));
        }

        [Fact]
        public async Task RequestWeight_blocks_a_second_device_while_the_first_holds_the_turn()
        {
            await _sut.RequestWeight("device-A");

            Assert.False(await _sut.RequestWeight("device-B"));
        }

        [Fact]
        public async Task RequestWeight_lets_the_holder_renew_its_own_turn()
        {
            await _sut.RequestWeight("device-A");

            Assert.True(await _sut.RequestWeight("device-A"));
        }

        [Fact]
        public async Task ReleaseWeight_refuses_a_device_that_does_not_hold_the_turn()
        {
            await _sut.RequestWeight("device-A");

            Assert.False(await _sut.ReleaseWeight("device-B"));
        }

        [Fact]
        public async Task ReleaseWeight_succeeds_for_the_holder_and_frees_the_scale_for_others()
        {
            await _sut.RequestWeight("device-A");

            Assert.True(await _sut.ReleaseWeight("device-A"));
            Assert.True(await _sut.RequestWeight("device-B"));
        }
    }
}
