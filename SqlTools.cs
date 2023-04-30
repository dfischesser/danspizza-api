using Microsoft.Data.SqlClient;

namespace Pizza
{
    public class SqlTools
    {
        
        private string source;
        private string uname;
        private string pass;
        private string db;
        private SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder();

        public SqlTools()
        {
            source = "FUSER";
            uname = "fuser";
            pass = "goverbose";
            db = "PizzaDB";
        }

        public SqlTools(string Name, string Uname, string Pass, string Db)
        {
            source = Name;
            uname = Uname;
            pass = Pass;
            db = Db;
        }

        public SqlConnectionStringBuilder CreateConnectionString()
        {
            sqlBuilder.DataSource = source;
            sqlBuilder.UserID = uname;
            sqlBuilder.Password = pass;
            sqlBuilder.InitialCatalog = db;
            sqlBuilder.Encrypt = false;

            return sqlBuilder;
        }
    }
}
