using Microsoft.AspNetCore.Components.Forms;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            var item = new AggregatedData
            {
                IntervalStartDateTime = DateTime.Parse($"{group.Key.Month}/{group.Key.Year}"),
                NetConsumption = group.Sum(x => x.NetConsumption),
                OldCost = group.Sum(x => x.OldCost),
                NewCost = group.Sum(x => x.NewCost)
            };

            finalData.Add(item);
        }
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
        // Peak hour list
        int[] peakHours = new int[] { 16, 17, 18, 19, 20 };

        // Off peak hours
        int[] offPeak = new int[] { 23, 0, 1, 2, 3, 4, 5, 6 };

        // Peak surchage
        decimal peakSurchage = 0.05M;

        // Off peak discount
        decimal offPeakDiscount = 0.05M;

        if (peakHours.Contains(data.IntervalStartDateTime.Hour))
        {
            return GetOldCost(step2) + peakSurchage;
        }
        else if (offPeak.Contains(data.IntervalStartDateTime.Hour))
        {
            return GetOldCost(step2) - offPeakDiscount;
        }

        return GetOldCost(step2);
    }
}