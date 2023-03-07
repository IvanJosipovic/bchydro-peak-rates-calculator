using Microsoft.AspNetCore.Components.Forms;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;

namespace bchydro_peak_rates_calculator.Pages;

public partial class Index
{
    List<AccountData> accounDatas = new();

    List<AggregatedData> finalData = new();

    // Charge per each day
    public decimal basicCharge = 0.2090M;

    // Regional transit levy per day
    public decimal transitLevy = 0.0624M;

    // Step 1 charge per kWh
    public decimal step1Rate = 0.0950M;

    // Step 2 charge per kWh
    public decimal step2Rate = 0.1408M;

    // Step treshold
    public int stepTreshold = 1350;

    List<IGrouping<GroupClass,AccountData>> monthGrouppings;

    private async Task UploadFiles(IBrowserFile file)
    {
        accounDatas = new();
        finalData = new();

        using var memory = new MemoryStream();
        await file.OpenReadStream(long.MaxValue).CopyToAsync(memory);
        memory.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(memory);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture){};

        using (var csv = new CsvReader(reader, config))
        {
            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                var record = new AccountData
                {
                    IntervalStartDateTime = csv.GetField<DateTime>("Interval Start Date/Time"),
                    NetConsumption = csv.GetField("Net Consumption (kWh)") == "N/A" ? 0.0M : csv.GetField<decimal>("Net Consumption (kWh)")
                };
                accounDatas.Add(record);
            }
        }

        monthGrouppings = accounDatas.GroupBy(x => new GroupClass(x.IntervalStartDateTime.Year, x.IntervalStartDateTime.Month)).ToList();

        foreach (var record in accounDatas)
        {
            var step2 = IsStep2(record);
            record.OldCost = basicCharge + transitLevy + GetOldCost(step2);
            record.NewCost = basicCharge + transitLevy + GetNewCost(step2, record);
        }

        foreach (var group in monthGrouppings)
        {
            var item = new AggregatedData();
            item.IntervalStartDateTime = DateTime.Parse($"{group.Key.Month}/{group.Key.Year}");
            item.NetConsumption = group.Sum(x => x.NetConsumption);
            item.OldCost = group.Sum(x => x.OldCost);
            item.NewCost = group.Sum(x => x.NewCost);
            finalData.Add(item);
        }
    }

    public class GroupClass {
        public GroupClass(int year, int month) {
            Year = year;
            Month = month;
        }

        public int Year { get; set; }
        public int Month { get; set; }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 31 + Year.GetHashCode();
            hash = hash * 31 + Month.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            var groupObj = obj as GroupClass;
            if (groupObj == null) return false;
            return (Year == groupObj.Year && Month == groupObj.Month);
        }
    }

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

    public class AggregatedData
    {
        public DateTime IntervalStartDateTime { get; set; }
        public decimal NetConsumption { get; set; }
        public decimal OldCost { get; set; }
        public decimal NewCost { get; set; }
    }

    public bool IsStep2(AccountData data)
    {
        var months = monthGrouppings.Where(x =>
        (x.Key.Month == data.IntervalStartDateTime.Month - 1 || x.Key.Month == data.IntervalStartDateTime.Month - 2) && x.Key.Year == data.IntervalStartDateTime.Year)
        .SelectMany(x => x.ToList());

        if (!months.Any())
        {
            return false;
        }

        var average = months.Average(x => x.NetConsumption);

        if (average > stepTreshold) {
            return true;
        }

        return false;
    }

    public decimal GetOldCost(bool step2)
    {
        return step2 == false ? step1Rate : step2Rate;
    }

    public decimal GetNewCost(bool step2, AccountData data)
    {
        return 0.00M;
    }
}