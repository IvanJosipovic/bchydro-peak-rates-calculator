namespace bchydro_peak_rates_calculator.Pages;

public partial class Index
{
    public class AccountData
    {
        //public string AccountHolder { get; set; }
        //public string AccountNumber { get; set; }
        public DateTime IntervalStartDateTime { get; set; }
        public decimal NetConsumption { get; set; }
        //public string Demand { get; set; }
        //public string PowerFactor { get; set; }
        //public string EstimatedUsage { get; set; }
        //public string ServiceAddress { get; set; }
        //public string City { get; set; }
        public decimal OldCost { get; set; }
        public decimal NewCost { get; set; }
    }
}