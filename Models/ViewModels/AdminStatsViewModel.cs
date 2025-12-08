using System;
using System.Collections.Generic;

namespace MasterServicePlatform.Web.Models.ViewModels
{
    public class AdminStatsViewModel
    {
        // Top counters
        public int UserCount { get; set; }
        public int MasterCount { get; set; }
        public int OrderCount { get; set; }
        public int CompletedCount { get; set; }

        // For charts
        public int PendingCount { get; set; }
        public int AcceptedCount { get; set; }
        public int CompletedChartCount { get; set; }

        // Orders per day
        public List<OrdersByDayItem> OrdersByDay { get; set; } = new();

        // Top masters
        public List<TopMasterItem> TopMasters { get; set; } = new();

        // Recent orders
        public List<RecentOrderItem> RecentOrders { get; set; } = new();
    }

    public class OrdersByDayItem
    {
        public string Day { get; set; }
        public int Count { get; set; }
    }

    public class TopMasterItem
    {
        public string Name { get; set; }
        public int Completed { get; set; }
        public double Rating { get; set; }

        public int Id { get; set; }

    }

    public class RecentOrderItem
    {
        public int Id { get; set; }
        public string User { get; set; }
        public string Master { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
