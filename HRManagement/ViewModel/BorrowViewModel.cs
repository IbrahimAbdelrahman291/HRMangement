﻿namespace HRManagement.ViewModel
{
    public class BorrowViewModel
    {
        public int Id { get; set; }
        public int MonthlyEmployeeDataId { get; set; }
        public int Amount { get; set; }
        public string Reason { get; set; }
        public string Notes { get; set; }
        public DateTime DateOfBorrow { get; set; }
    }
}
