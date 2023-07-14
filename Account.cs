﻿namespace Pizza
{
    public class Account
    {
        public int? UserID { get; set; }
        public string? Role { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public int? OrderCount { get; set; }
        public List<Order>? ActiveOrders { get; set; }
        public List<Order>? PastOrders { get; set; }
    }
}
