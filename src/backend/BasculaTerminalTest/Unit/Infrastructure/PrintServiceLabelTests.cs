using Infrastructure.Service;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// Pins the two direction-dependent ticket labels from design.md Decision 7 — a discharge
    /// entry's ticket must never read as if the vehicle left heavier than it arrived. The
    /// helpers are <c>internal</c> on <see cref="PrintService"/> (InternalsVisibleTo) so this
    /// asserts the label choice directly instead of driving the full iText document build.
    /// </summary>
    public class PrintServiceLabelTests
    {
        [Theory]
        [InlineData(false, "BRUTO:")]
        [InlineData(true, "PESO FINAL (VACÍO):")]
        public void GetFinalWeightLabel_reflects_discharge_direction(bool isDischarge, string expected)
        {
            Assert.Equal(expected, PrintService.GetFinalWeightLabel(isDischarge));
        }

        [Theory]
        [InlineData(false, "TARA INICIAL:")]
        [InlineData(true, "PESO INICIAL (CARGADO):")]
        public void GetInitialWeightLabel_reflects_discharge_direction(bool isDischarge, string expected)
        {
            Assert.Equal(expected, PrintService.GetInitialWeightLabel(isDischarge));
        }
    }
}
