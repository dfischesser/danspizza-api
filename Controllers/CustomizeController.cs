using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Security.AccessControl;

namespace Pizza.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CustomizeController : ControllerBase
    {
        private CustomizePizza customizePizza = new CustomizePizza();
        private List<Topping> toppings = new List<Topping>();
        private Topping? topping;

        [HttpGet("Get")]
        public CustomizePizza Get()
        {

            SqlTools sqlTools = new SqlTools();
            SqlConnectionStringBuilder sqlBuilder = sqlTools.CreateConnectionString();
            //DataSet dsCustomize = new DataSet();
            DataSet dsToppings = new DataSet();

            // Load Data from DB
            using (SqlConnection connection = new SqlConnection(sqlBuilder.ConnectionString))
            {
                connection.Open();

                // Load Menu Categories from DB
                using (SqlCommand command = new SqlCommand("dbo.GetToppings", connection))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter())
                    {
                        da.SelectCommand = command;
                        da.Fill(dsToppings);
                    }
                }

                if (dsToppings.Tables.Count > 0 && dsToppings.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow dr in dsToppings.Tables[0].Rows)
                    {
                        topping = new Topping();
                        topping.ToppingID = Convert.ToInt32(dr["id"]);
                        topping.ToppingName = string.IsNullOrEmpty(dr["topping"].ToString()) ? "" : dr["topping"].ToString();
                        toppings.Add(topping);
                    }
                }

                customizePizza.Toppings = toppings;

                return customizePizza;
            }
        }
    }
}
