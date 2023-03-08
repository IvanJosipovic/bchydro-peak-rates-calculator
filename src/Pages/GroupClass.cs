namespace bchydro_peak_rates_calculator.Pages;

public partial class Index
{
    public class GroupClass {
        public GroupClass(int year, int month) {
            Year = year;
            Month = month;
        }

        public int Year { get; set; }
        public int Month { get; set; }

        public override int GetHashCode()
        {
            return HashCode.Combine(Year, Month);
        }

        public override bool Equals(object obj)
        {
            var groupObj = obj as GroupClass;
            if (groupObj == null) return false;
            return (Year == groupObj.Year && Month == groupObj.Month);
        }
    }
}