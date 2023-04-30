namespace Pizza
{
    public class Menu
    {
        public List<MenuCategoryItem>? MenuCategoryList { get; set; }
    }

    public class MenuCategoryItem
    {
        public int? MenuCategoryID { get; set; }
        public string? FoodType { get; set; }
        public List<FoodItem>? FoodList { get; set; }
    }
}
