namespace bchydro_peak_rates_calculator.Pages;

public partial class Index
{
    public class AccountData
    {
        public DateTime IntervalStartDateTime { get; set; }
        public decimal NetConsumption { get; set; }
        public decimal OldCost { get; set; }
        public decimal NewCost { get; set; }
    }
}