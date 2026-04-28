using System;
using System.Collections.Generic;

namespace Websitebanhang.Models.ViewModels
{
    public class UserDetailsViewModel
    {
        public UserViewModel User { get; set; } = new UserViewModel();
        public List<UserAddress> Addresses { get; set; } = new List<UserAddress>();
        public List<Order> RecentOrders { get; set; } = new List<Order>();
    }
}
