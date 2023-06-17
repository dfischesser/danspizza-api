using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using static System.Net.Mime.MediaTypeNames;

namespace Pizza.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        SqlTools sqlTools = new SqlTools();

        [HttpPost]
        [Authorize]
        [Route("Post")]
        public IActionResult AddOrder([FromBody] Object cartItems)
        {
            var currentUser = sqlTools.GetCurrentUser(HttpContext.User);
            IList<Food> items = JsonConvert.DeserializeObject<IList<Food>>(cartItems.ToString());
            var orderItems = new List<OrderItem>();
            int orderID = 0;
            try
            {
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
                var order = new Order();

                // Load Data from DB
                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();

                    try
                    {
                        // Load Menu Categories from DB
                        using (SqlCommand command = new SqlCommand("dbo.CreateOrder", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            SqlParameter param = new SqlParameter();
                            param.ParameterName = "@user_id";
                            param.Value = Convert.ToInt32(currentUser.UserID);
                            param.DbType = DbType.Int32;
                            command.Parameters.Add(param);

                            var reader = command.ExecuteReader();
                            while (reader.Read()) 
                            {
                                orderID = Convert.ToInt32(reader["order_id"]);
                            }
                        }

                        int orderItemID = 0;
                        foreach (var item in items)
                        {
                            // Load Menu Categories from DB
                            using (SqlCommand command = new SqlCommand("dbo.CreateOrderItem", connection))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                SqlParameter param = new SqlParameter();
                                param.ParameterName = "@order_id";
                                param.Value = orderID;
                                param.DbType = DbType.Int32;
                                command.Parameters.Add(param);

                                command.CommandType = CommandType.StoredProcedure;
                                param = new SqlParameter();
                                param.ParameterName = "@food_id";
                                param.Value = Convert.ToInt32(item.FoodID);
                                param.DbType = DbType.Int32;
                                command.Parameters.Add(param);

                                param = new SqlParameter();
                                param.ParameterName = "@food_name";
                                param.Value = item.FoodName;
                                param.DbType = DbType.String;
                                command.Parameters.Add(param);

                                var reader = command.ExecuteReader();
                                while (reader.Read())
                                {
                                    orderItemID = Convert.ToInt32(reader["order_item_id"]);
                                }
                            }

                            int orderItemOptionID = 0;
                            foreach (var option in item.CustomizeOptions)
                            {
                                try
                                {
                                    using (SqlCommand command = new SqlCommand("dbo.CreateOrderItemOption", connection))
                                    {
                                        command.CommandType = CommandType.StoredProcedure;
                                        SqlParameter param = new SqlParameter();
                                        param.ParameterName = "@order_item_id";
                                        param.Value = orderItemID;
                                        param.DbType = DbType.Int32;
                                        command.Parameters.Add(param);

                                        param = new SqlParameter();
                                        param.ParameterName = "@option_name";
                                        param.Value = option.OptionName;
                                        param.DbType = DbType.String;
                                        command.Parameters.Add(param);

                                        var reader = command.ExecuteReader();
                                        while (reader.Read())
                                        {
                                            orderItemOptionID = Convert.ToInt32(reader["order_item_option_id"]);
                                        }
                                    }
                                    foreach (var optionItem in option.OptionItems)
                                    {
                                        using (SqlCommand command = new SqlCommand("dbo.CreateOrderItemOptionItem", connection))
                                        {
                                            command.CommandType = CommandType.StoredProcedure;
                                            SqlParameter param = new SqlParameter();
                                            param.ParameterName = "@order_item_option_id";
                                            param.Value = orderItemOptionID;
                                            param.DbType = DbType.Int32;
                                            command.Parameters.Add(param);

                                            param = new SqlParameter();
                                            param.ParameterName = "@order_item_option_item_name";
                                            param.Value = optionItem.CustomizeOptionItem;
                                            param.DbType = DbType.String;
                                            command.Parameters.Add(param);

                                            param = new SqlParameter();
                                            param.ParameterName = "@price";
                                            param.Value = optionItem.Price;
                                            param.DbType = DbType.Decimal;
                                            command.Parameters.Add(param);

                                            command.ExecuteNonQuery();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    sqlTools.Logamuffin("Post Order (Create Order Item Customize)", "Error", "Error Creating Order Item Customization", ex.Message);
                                    return NotFound("Order item customization failed to create");
                                }
                            }
                        }
                        return Ok("{\"result\": \"Order Created\"}");
                    }
                    catch (Exception ex)
                    {
                        sqlTools.Logamuffin("Post Order (Create Order)", "Error", "Error Creating Order", ex.Message);
                        return NotFound("Order failed to create");
                    }
                }
            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Post Order", "Error", "Error Creating Order", ex.Message);
                return NotFound("{\"error\": \"Order Post Failed\"}");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        [Route("Fulfill")]
        public IActionResult FulfillOrder([FromBody] Object orderID)
        {
            var currentUser = sqlTools.GetCurrentUser(HttpContext.User);
            var rows = 0;
            try
            {
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();

                DataSet ds = new DataSet();
                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("dbo.FulfillOrder", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        SqlParameter param = new SqlParameter();
                        param.ParameterName = "@order_id";
                        param.Value = Convert.ToInt32(orderID.ToString());
                        param.DbType = DbType.Int32;
                        command.Parameters.Add(param);

                        // TODO: Add Error Logging for Users
                        command.ExecuteNonQuery();
                    }
                }
            } catch (Exception ex)
            {
                sqlTools.Logamuffin("Fulfill Order (FullfillOrder)", "Error", "Error Fulfilling order for: " + currentUser.Email, ex.Message);
                return NotFound("Order failed to fulfill");
            }

            try
            {
                var orders = GetLatestOrders();
                sqlTools.Logamuffin("Manage Orders (GetLatestOrders)", "System", "Retrieved Orders for " + currentUser.UserID);
                return Ok(orders);
                
            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Fulfill Order (FullfillOrder)", "Error", "Error Fulfilling order for: " + currentUser.Email, ex.Message);
                return NotFound("Order failed to fulfill");
            }

        }

        [HttpGet]
        [Authorize(Roles = "Employee")]
        [Route("Latest")]
        public IActionResult GetOrders()
        {
            var currentUser = sqlTools.GetCurrentUser(HttpContext.User);
            try
            {
                var orders = GetLatestOrders();
                sqlTools.Logamuffin("Manage Orders (GetLatestOrders)", "System", "Retrieved Orders for " + currentUser.UserID);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Manage Orders (GetLatestOrders)", "Error", "Error retrieving Orders for " + currentUser.UserID, ex.Message);
                return NotFound("Could not retrieve orders");
            }
        }
        
        private List<Order> GetLatestOrders()
        {
            SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();

            DataSet ds = new DataSet();
            var orders = new List<Order>();
            using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
            {
                var OrderIDs = new List<int>();
                var customizeIDs = new List<int>();
                connection.Open();

                using (SqlCommand command = new SqlCommand("dbo.GetLatestOrders", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    // TODO: Add Error Logging for Users
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var order = new Order();
                            order.OrderID = Convert.ToInt32(reader["id"]);
                            order.UserID = Convert.ToInt32(reader["user_id"]);
                            orders.Add(order);
                        }
                    }
                }

                foreach (var item in orders)
                {
                    using (SqlCommand command = new SqlCommand("dbo.GetOrderItems", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        SqlParameter param = new SqlParameter();
                        param.ParameterName = "@order_id";
                        param.Value = item.OrderID;
                        param.DbType = DbType.Int32;
                        command.Parameters.Add(param);
                        // TODO: Add Error Logging for Users
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var food = new FoodItem();
                                food.FoodID = Convert.ToInt32(reader["food_id"]);
                                food.FoodName = reader["food_name"].ToString();
                                food.OrderItemID = Convert.ToInt32(reader["id"]);
                                item.FoodItems.Add(food);
                            }
                        }
                    }

                    foreach (var foodItem in item.FoodItems)
                    {
                        using (SqlCommand command = new SqlCommand("dbo.GetOrderItemOptions", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            SqlParameter param = new SqlParameter();
                            param.ParameterName = "@order_item_id";
                            param.Value = foodItem.OrderItemID;
                            param.DbType = DbType.Int32;
                            command.Parameters.Add(param);
                            // TODO: Add Error Logging for Users
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var option = new CustomizeOptions();
                                    option.OptionName = reader["option_name"].ToString();
                                    option.OrderItemOptionID = Convert.ToInt32(reader["id"]);
                                    foodItem.CustomizeOptions.Add(option);
                                }
                            }
                        }

                        foreach (var option in foodItem.CustomizeOptions)
                        {
                            using (SqlCommand command = new SqlCommand("dbo.GetOrderItemOptionItems", connection))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                SqlParameter param = new SqlParameter();
                                param.ParameterName = "@order_item_option_id";
                                param.Value = option.OrderItemOptionID;
                                param.DbType = DbType.Int32;
                                command.Parameters.Add(param);
                                // TODO: Add Error Logging for Users
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var customizeItem = new CustomizeItem();
                                        customizeItem.CustomizeOptionItem = reader["order_item_option_item_name"].ToString();
                                        customizeItem.Price = Convert.ToDecimal(reader["price"]);
                                        option.OptionItems.Add(customizeItem);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return orders;
        }
    }

    
}


