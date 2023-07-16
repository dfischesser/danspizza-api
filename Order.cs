namespace Pizza
{
    public class Order
    {
        public int OrderID { get; set; }
        public int UserID { get; set; }
        public List<FoodItem> FoodItems { get; set; } = new List<FoodItem>();
        public decimal totalPrice { get; set; }
        public int Active { get; set; }
        public DateTime? Created { get; set; }
        public Account? Account { get; set; } = new Account();
    }
}
