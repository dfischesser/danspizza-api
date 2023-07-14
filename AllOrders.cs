namespace Pizza
{
    public class AllOrders
    {
        public List<Order> activeOrders { get; set; } = new List<Order>();
        public List<Order> pastOrders { get; set; } = new List<Order>();
        public int OrderCount { get; set; } = 0;
    }
}
