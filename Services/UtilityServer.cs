using LoanApp.IServices;

namespace LoanApp.Services
{
    public class UtilityServer : IUtilityServer
    {
        public bool CheckDBtest(string dbName = "NORA178")
        {
            string connectionString = string.Empty;

            var builder1 = new ConfigurationBuilder().AddUserSecrets<Program>();
            var Configuration = builder1.Build();
            int configCount = Configuration.AsEnumerable().Count();

            if (configCount == 0)
            {
                IConfiguration builder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.Docker.json")
                    .Build();

                connectionString = builder.GetConnectionString("DefaultConnection") ?? throw new NullReferenceException(nameof(connectionString));
            }
            else
            {
                connectionString = Configuration["service_name"]!;
            }

            return connectionString.Contains(dbName);
        }
    }
}
