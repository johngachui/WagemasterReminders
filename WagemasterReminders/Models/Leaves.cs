namespace YourProjectName.Models
{
    public class LeaveBals
    {
       
        public string? Num { get; set; }
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
        public string? CompanyKey { get; set; }
        public string? DivisionKey { get; set; }

    }

    public class LeaveDays
    {
        public string? Num { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? StopDate { get; set; }
        public string? LeaveType { get; set; }
        public decimal Days { get; set; }
        public bool Approved { get; set; }
        public bool NotApproved { get; set; }
        public bool Notified { get; set; }
        public bool Taken { get; set; }
        public bool RecalcNeeded { get; set; }
        public string? DatabasePath { get; set; }

    }

    
}
