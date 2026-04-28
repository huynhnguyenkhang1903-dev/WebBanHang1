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

    public class YearlyRevenue
    {
        public int Year { get; set; }
        public decimal Amount { get; set; }
    }

    public class TopSellingProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RevenueViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int OrdersCount { get; set; }
        public int PaidOrdersCount { get; set; }
        public List<DailyRevenue> RevenueByDay { get; set; } = new List<DailyRevenue>();
        public List<MonthlyRevenue> RevenueByMonth { get; set; } = new List<MonthlyRevenue>();
        public List<YearlyRevenue> RevenueByYear { get; set; } = new List<YearlyRevenue>();
        public List<TopSellingProduct> TopProducts { get; set; } = new List<TopSellingProduct>();
    }
}
