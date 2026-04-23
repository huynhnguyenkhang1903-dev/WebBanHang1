using System;
using System.Collections.Generic;

namespace Websitebanhang.Models.ViewModels
{
    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }

    public class MonthlyRevenue
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Amount { get; set; }
    }

    public class RevenueViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int OrdersCount { get; set; }
        public int PaidOrdersCount { get; set; }
        public List<DailyRevenue> RevenueByDay { get; set; } = new List<DailyRevenue>();
        public List<MonthlyRevenue> RevenueByMonth { get; set; } = new List<MonthlyRevenue>();
    }
}
