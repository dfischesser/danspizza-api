namespace Pizza
{
    public class CustomizePizza
    {
        public int? CustomizePizzaID { get; set; }
        public string? Size { get; set; }
        public string? Style { get; set; }
        public List<Topping>? Toppings { get; set; }
    }

    public class Topping
    {
        public int? ToppingID { get; set; }
        public string? ToppingName { get; set; }
        public decimal? Price { get; set; }
    }
}
