using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Data;
using System.Data.Common;
using System.Net.Sockets;
using System.Security.Claims;

namespace Pizza.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        SqlTools sqlTools;
        private readonly IConfiguration _config;
        public UserController(IConfiguration config)
        {
            _config = config;
            sqlTools = new SqlTools(_config);
        }

        [HttpGet]
        [Route("Admins")]
        [Authorize]
        public IActionResult AdminEndPoint()
        {
            var test = HttpContext.User.Identity;
            var currentUser = sqlTools.GetCurrentUser(HttpContext.User);
            return Ok($"Hi you are an {currentUser.Role}");
        }

        [HttpGet]
        [Route("Account")]
        [Authorize]
        public IActionResult Account()
        {
            Account account = new Account();
            var currentUser = sqlTools.GetCurrentUser(HttpContext.User);
            try
            {
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();

                DataSet ds = new DataSet();
                var orderItems = new List<OrderItem>();
                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    var OrderIDs = new List<int>();
                    var customizeIDs = new List<int>();
                    var orders = new List<Order>();
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.GetUserById", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        SqlParameter param = new SqlParameter();
                        param.ParameterName = "@id";
                        param.Value = currentUser.UserID;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);
                        // TODO: Add Error Logging for Users
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (Convert.ToInt32(reader["id"]) == Convert.ToInt32(currentUser.UserID))
                                {
                                    account.UserID = Convert.ToInt32(reader["id"]);
                                    account.Role = currentUser.Role;
                                    account.FirstName = string.IsNullOrEmpty(reader["first_name"].ToString()) ? "" : reader["first_name"].ToString();
                                    account.LastName = string.IsNullOrEmpty(reader["last_name"].ToString()) ? "" : reader["last_name"].ToString();
                                    account.Email = string.IsNullOrEmpty(reader["email"].ToString()) ? "" : reader["email"].ToString();
                                    account.Phone = string.IsNullOrEmpty(reader["phone"].ToString()) ? "" : reader["phone"].ToString();
                                    account.Address1 = string.IsNullOrEmpty(reader["address1"].ToString()) ? "" : reader["address1"].ToString();
                                    account.Address2 = string.IsNullOrEmpty(reader["address2"].ToString()) ? "" : reader["address2"].ToString();
                                    account.City = string.IsNullOrEmpty(reader["city"].ToString()) ? "" : reader["city"].ToString();
                                    account.State = string.IsNullOrEmpty(reader["state"].ToString()) ? "" : reader["state"].ToString();
                                    account.Zip = string.IsNullOrEmpty(reader["zip"].ToString()) ? "" : reader["zip"].ToString();
                                }
                            }
                        }
                    }

                    using (SqlCommand command = new SqlCommand("dbo.GetUserOrders", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        SqlParameter param = new SqlParameter();
                        param.ParameterName = "@user_id";
                        param.Value = Convert.ToInt32(currentUser.UserID);
                        param.DbType = DbType.Int32;
                        command.Parameters.Add(param);
                        // TODO: Add Error Logging for Users
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var order = new Order();
                                order.Active = Convert.ToInt32(reader["active"]);
                                order.OrderID = Convert.ToInt32(reader["id"]);
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
                    account.PastOrders = orders.Where(order => order.Active == 0).ToList();
                    account.ActiveOrders = orders.Where(order => order.Active == 1).ToList();
                }
                sqlTools.Logamuffin("Account (GetUserByEmail)", "System", "Retrieved Account for " + account.Email + ". User ID: " + account.UserID);
                return Ok(account);
            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Account", "Error", "Error Getting Account for user " + currentUser.Email, ex.Message);
                return NotFound(account);
            }
        }

        [HttpPost]
        [Route("Create")]
        [AllowAnonymous]
        public IActionResult CreateUser(UserLogin userLogin)
        {
            SqlTools sqlTools = new SqlTools(_config);
            Boolean uniqueEmail = true;
            string result = "";
            try
            {
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
                DataSet ds = new DataSet();
                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.GetUserByEmail", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        SqlParameter param = new SqlParameter();
                        param.ParameterName = "@email";
                        param.Value = userLogin.Email;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);
                        // TODO: Add Error Logging for Users
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader["email"].ToString() == userLogin.Email)
                                {
                                    uniqueEmail = false;
                                }
                            }
                        }
                    }
                    if (uniqueEmail)
                    {
                        using (SqlCommand command = new SqlCommand("dbo.CreateUser", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            SqlParameter param = new SqlParameter();
                            param.ParameterName = "@email";
                            param.Value = userLogin.Email;
                            param.DbType = DbType.String;
                            command.Parameters.Add(param);

                            param = new SqlParameter();
                            param.ParameterName = "@password";
                            param.Value = userLogin.Password;
                            param.DbType = DbType.String;
                            command.Parameters.Add(param);

                            param = new SqlParameter();
                            param.ParameterName = "@salt";
                            param.Value = userLogin.Salt;
                            param.DbType = DbType.String;
                            command.Parameters.Add(param);

                            // TODO: Add Error Logging for Users
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    result = reader["added"].ToString();
                                }
                            }
                        }

                        Token token = new Token();

                        token = sqlTools.TryLogin(userLogin);

                        CookieOptions cookie = new()
                        {
                            Expires = DateTimeOffset.Now.AddDays(15),
                            MaxAge = TimeSpan.FromDays(15)
                        };
                        Response.Cookies.Append("token", token.UserToken, cookie);

                        return Ok(token);
                    }
                    else
                    {
                        sqlTools.Logamuffin("Create", "Error", "Email already exists for user " + userLogin.Email);
                        return new NotFoundObjectResult(new { message = "Email already exists" });
                    }
                }
            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Create", "Error", "Error Creating Account for user " + userLogin.Email, ex.Message);
                return new NotFoundObjectResult(new { message = "Error Creating Account for user " + userLogin.Email });
            }
        }

        [HttpPost]
        [Route("CreateStep2")]
        [Authorize]
        public IActionResult CreateStep2([FromBody] object step2details)
        {
            SqlTools sqlTools = new SqlTools(_config);
            var currentUser = sqlTools.GetCurrentUser(HttpContext.User);

            Account user = JsonConvert.DeserializeObject<Account>(step2details.ToString());
            string email = "";
            string password = "";
            try
            {
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
                DataSet ds = new DataSet();
                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.CreateUserStep2", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        SqlParameter param = new SqlParameter();
                        param.ParameterName = "@id";
                        param.Value = Convert.ToInt32(currentUser.UserID);
                        param.DbType = DbType.Int32;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@first_name";
                        param.Value = user.FirstName;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@last_name";
                        param.Value = user.LastName;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@phone";
                        param.Value = user.Phone;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@street_number";
                        param.Value = user.Address1;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@street_name";
                        param.Value = user.Address2;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@city";
                        param.Value = user.City;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@state";
                        param.Value = user.State;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        param = new SqlParameter();
                        param.ParameterName = "@zip";
                        param.Value = user.Zip;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);

                        // TODO: Add Error Logging for Users
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                email = reader["email"].ToString();
                                password = reader["password"].ToString();
                            }
                        }
                    }
                }
                Token token = new Token();

                token = sqlTools.TryLogin(new UserLogin() { Email = email, Password = password});

                CookieOptions cookie = new()
                {
                    Expires = DateTimeOffset.Now.AddDays(15),
                    MaxAge = TimeSpan.FromDays(15)
                };
                Response.Cookies.Append("token", token.UserToken, cookie);
                return Ok(token);

            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Create", "Error", "Error Creating Account Step 2 for user " + currentUser.UserFirstName, ex.Message);
                return new NotFoundObjectResult(new { message = "Error Creating Account Step 2 for user " + currentUser.UserFirstName });
            }
        }
    }
}

