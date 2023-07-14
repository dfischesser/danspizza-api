using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
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

        [HttpPost]
        [Route("Hash")]
        public IActionResult GetHash([FromBody] string email)
        {
            try
            {
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
                string hash = "";
                string firstName = "";
                string role = "";
                int roleID = 0;
                int userID = 0;

                DataSet ds = new DataSet();
                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("dbo.GetHash", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        SqlParameter param = new SqlParameter();
                        param.ParameterName = "@email";
                        param.Value = email;
                        param.DbType = DbType.String;
                        command.Parameters.Add(param);
                        // TODO: Add Error Logging for Users
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                hash = reader["password"].ToString() ?? "";
                                firstName = reader["first_name"].ToString() ?? "";
                                roleID = Convert.ToInt32(reader["role_id"].ToString() ?? "");
                                userID = Convert.ToInt32(reader["id"].ToString() ?? "");
                            }
                        }
                    }
                }
                role = _config["Roles:" + roleID] ?? "";
                return Ok(new { hash, firstName, role, userID });
            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Account", "Error", "Error Getting Account for user " + email, ex.Message);
                return NotFound(new { message = "Error Getting Account for user " + email });
            }
        }

        [HttpPost]
        [Route("OrderPage")]
        [Authorize]
        public IActionResult OrderPage([FromBody] int page)
        {
            var currentUser = sqlTools.GetCurrentUser(HttpContext.User);
            try
            {
                var allOrders = new AllOrders();
                var orders = new List<Order>();
                SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();

                using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
                {
                    connection.Open();
                    GetUserOrders(allOrders, currentUser, connection, page);
                }
                return Ok(allOrders.pastOrders);
            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Account", "Error", "Error Getting Order Page " + page + " for user " + currentUser.Email, ex.Message);
                return NotFound(new { message = "Error Getting Order Page " + page + " for user " + currentUser.Email });
            }
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
                                account.UserID = Convert.ToInt32(currentUser.UserID);
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

                    AllOrders allOrders = new AllOrders();

                    GetUserOrders(allOrders, currentUser, connection, 1);

                    account.ActiveOrders = allOrders.activeOrders.OrderByDescending(x => x.Created).ToList();
                    account.PastOrders = allOrders.pastOrders.OrderByDescending(x => x.Created).ToList();
                    account.OrderCount = allOrders.OrderCount;

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

        public static void FillOrders(List<Order> orders, SqlConnection connection)
        {
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
                            food.Price = 0;
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
                                    item.totalPrice += Convert.ToDecimal(reader["price"]);
                                    foodItem.Price += Convert.ToDecimal(reader["price"]);
                                    option.OptionItems.Add(customizeItem);

                                }
                            }
                        }
                    }
                }
            }
        }

        private void GetUserOrders(AllOrders allOrders, UserModel currentUser, SqlConnection connection, int page)
        {

            var dsOrders = new DataSet();
            try
            {
                using (SqlCommand command = new SqlCommand("dbo.GetUserOrders", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@user_id";
                    param.Value = Convert.ToInt32(currentUser.UserID);
                    param.DbType = DbType.Int32;
                    command.Parameters.Add(param);

                    param = new SqlParameter();
                    param.ParameterName = "@page";
                    param.Value = page;
                    param.DbType = DbType.Int32;
                    command.Parameters.Add(param);

                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = command;
                    adapter.Fill(dsOrders);

                    if (dsOrders.Tables.Count > 2)
                    {
                        if (dsOrders.Tables[0].Rows.Count > 0)
                        {
                            foreach (DataRow row in dsOrders.Tables[0].Rows)
                            {
                                var order = new Order();
                                order.OrderID = Convert.ToInt32(row["id"]);
                                order.Created = Convert.ToDateTime(row["created_on"]);
                                order.totalPrice = 0;
                                allOrders.activeOrders.Add(order);
                            }
                        }

                        if (dsOrders.Tables[1].Rows.Count > 0)
                        {
                            foreach (DataRow row in dsOrders.Tables[1].Rows)
                            {
                                var order = new Order();
                                order.OrderID = Convert.ToInt32(row["id"]);
                                order.Created = Convert.ToDateTime(row["created_on"]);
                                order.totalPrice = 0;

                                allOrders.pastOrders.Add(order);
                            }
                        }

                        if (dsOrders.Tables[2].Rows.Count > 0)
                        {
                            foreach (DataRow row in dsOrders.Tables[2].Rows)
                            {
                                allOrders.OrderCount = Convert.ToInt32(row["past_order_count"]);
                            }
                        }
                    }
                }
                FillOrders(allOrders.activeOrders, connection);
                FillOrders(allOrders.pastOrders, connection);

            } 
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Account", "Error", "Error Getting Orders for user " + currentUser.Email, ex.Message);
            }
        }

        [HttpPost]
        [Route("Create")]
        [AllowAnonymous]
        public IActionResult CreateUser([FromBody] UserLogin userLogin)
        {
            SqlTools sqlTools = new SqlTools(_config);
            Boolean uniqueEmail = true;
            int userID = 0;
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
                        string hash = BCrypt.Net.BCrypt.HashPassword(userLogin.Password);
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
                            param.Value = hash;
                            param.DbType = DbType.String;
                            command.Parameters.Add(param);

                            // TODO: Add Error Logging for Users
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    userID = Convert.ToInt32(reader["user_id"]);
                                }
                            }
                        }

                        Token token = new Token();

                        token = sqlTools.TryLogin(userLogin);


                        if (token != null)
                        {
                            CookieOptions cookieOptions = new()
                            {
                                Expires = DateTimeOffset.Now.AddDays(15),
                                MaxAge = TimeSpan.FromDays(15),
                                HttpOnly = true
                            };
                            var response = new HttpResponseMessage();

                            HttpContext.Response.Cookies.Append("token", token.UserToken, cookieOptions);

                        }

                        return Ok(new { message = "Login Success" });
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
            string? email = "";
            string? role = "";

            Account user = JsonConvert.DeserializeObject<Account>(step2details.ToString());
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

                        var reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            email = reader["email"].ToString();
                            role = reader["role"].ToString();
                        }
                    }
                }

                Token token = new();

                token = sqlTools.Step2Token(new UserModel { UserID = currentUser.UserID, UserFirstName = user.FirstName, Role = role, Email = email});

                if (token != null)
                {
                    CookieOptions cookieOptions = new()
                    {
                        Expires = DateTimeOffset.Now.AddDays(15),
                        MaxAge = TimeSpan.FromDays(15),
                        HttpOnly = true
                    };
                    var response = new HttpResponseMessage();

                    HttpContext.Response.Cookies.Append("token", token.UserToken, cookieOptions);

                }

                return Ok(new { message = "Login Success" });


            }
            catch (Exception ex)
            {
                sqlTools.Logamuffin("Create", "Error", "Error Creating Account Step 2 for user " + currentUser.UserFirstName, ex.Message);
                return new NotFoundObjectResult(new { message = "Error Creating Account Step 2 for user " + currentUser.UserFirstName });
            }
        }
    }
}

