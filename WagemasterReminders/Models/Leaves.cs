namespace YourProjectName.Models
{
    public class LeaveBals
    {
        public string? Num { get; set; }
        public string? EmpName { get; set; }
        public decimal Annual_Bfwd { get; set; }
        public decimal Annual_Cfwd { get; set; }
        public decimal Maternity_Bfwd { get; set; }
        public decimal Maternity_Cfwd { get; set; }
        public decimal Paternity_Bfwd { get; set; }
        public decimal Paternity_Cfwd { get; set; }
        public decimal Full_Sick_Bal { get; set; }
        public decimal Half_Sick_Bal { get; set; }
        public decimal Earned { get; set; }
        public decimal Taken { get; set; }
        public decimal Sold { get; set; }
        public decimal Adjustment { get; set; }
        public decimal Absence { get; set; }
        public string? DatabasePath { get; set; }

    }
}
