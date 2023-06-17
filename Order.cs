namespace Pizza
{
    public class Order
    {
        public int OrderID { get; set; }
        public int UserID { get; set; }
        public List<FoodItem> FoodItems { get; set; } = new List<FoodItem>();
        public int Active { get; set; }
    }
}
