namespace LostFound.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalItems { get; set; }
        public int ActiveItems { get; set; }
        public int ResolvedItems { get; set; }
        public int PendingApproval { get; set; }
        public int TotalMessages { get; set; }
        public List<Item> RecentItems { get; set; } = new();
        public List<User> RecentUsers { get; set; } = new();
    }
}
