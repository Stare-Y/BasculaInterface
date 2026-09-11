using Core.Application.DTOs;

namespace BasculaTerminalTest.Unit.Application
{
    /// <summary>
    /// <see cref="PedidoLineDto.Concluded"/> and <see cref="PedidoDto.Concluded"/> are computed
    /// properties — no stored header-level flag (design.md Decision 4). Both DTOs are plain
    /// data holders, so these are pure construct-and-assert tests: no mocking, no DB.
    /// </summary>
    public class PedidoDtoTests
    {
        private static PedidoLineDto Line(decimal required, decimal received, bool manuallyClosed = false) => new()
        {
            RequiredAmount = required,
            ReceivedAmount = received,
            ManuallyClosed = manuallyClosed,
        };

        // --- PedidoLineDto.Concluded ------------------------------------------------

        [Fact]
        public void Line_is_concluded_when_manually_closed_regardless_of_pending()
        {
            PedidoLineDto line = Line(required: 100m, received: 0m, manuallyClosed: true);

            Assert.True(line.Concluded);
            Assert.Equal(100m, line.PendingAmount); // still pending, but closed wins
        }

        [Fact]
        public void Line_is_concluded_once_received_reaches_required_exactly()
        {
            PedidoLineDto line = Line(required: 100m, received: 100m);

            Assert.True(line.Concluded);
            Assert.Equal(0m, line.PendingAmount);
        }

        [Fact]
        public void Line_is_concluded_once_received_exceeds_required()
        {
            // Shouldn't normally happen (ConvertLineToWeightAsync rejects overshoot), but the
            // computed property must not read "unconcluded" if it ever does.
            PedidoLineDto line = Line(required: 100m, received: 110m);

            Assert.True(line.Concluded);
            Assert.Equal(0m, line.PendingAmount); // floored, never negative
        }

        [Fact]
        public void Line_is_not_concluded_while_partially_received_and_open()
        {
            PedidoLineDto line = Line(required: 100m, received: 40m);

            Assert.False(line.Concluded);
            Assert.Equal(60m, line.PendingAmount);
        }

        // --- PedidoDto.Concluded -----------------------------------------------------

        [Fact]
        public void Pedido_with_no_lines_is_never_concluded()
        {
            var pedido = new PedidoDto { Lines = [] };

            Assert.False(pedido.Concluded);
        }

        [Fact]
        public void Pedido_is_not_concluded_while_any_line_is_open()
        {
            var pedido = new PedidoDto
            {
                Lines =
                [
                    Line(required: 100m, received: 100m), // concluded
                    Line(required: 50m, received: 10m),    // still pending
                ],
            };

            Assert.False(pedido.Concluded);
        }

        [Fact]
        public void Pedido_is_concluded_once_every_line_is_concluded_naturally_or_manually()
        {
            var pedido = new PedidoDto
            {
                Lines =
                [
                    Line(required: 100m, received: 100m),                    // naturally complete
                    Line(required: 50m, received: 5m, manuallyClosed: true), // force-closed short
                ],
            };

            Assert.True(pedido.Concluded);
        }
    }
}
