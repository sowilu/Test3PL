using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace _3PL1_DiscordBot
{
    class MySqlClient
    {
        public static MySqlConnection Connection;

        static string GenerateConnectionString()
        {
            var builder = new MySqlConnectionStringBuilder()
            {
                UserID = "root",
                Password = "",
                Server = "localhost",
                Port = 3306,
                Database = "PirateBay"
            };

            return builder.ToString();
        }

        public static void Connect()
        {

            MySqlConnection connection = new MySqlConnection();
            connection.ConnectionString = GenerateConnectionString();
            connection.Open();

            Connection = connection;

        }

        /// <summary>
        /// Method executes non queries like INSERT, DELETE, UPDATE etc
        /// </summary>
        /// <param name="query">mySql command</param>
        public static void ExecuteCommand(string query)
        {
            var cmd = new MySqlCommand(query, Connection);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Error executing program: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Returns result as Reader type
        /// </summary>
        /// <param name="query">mySql command</param>
        /// <returns></returns>
        public static MySqlDataReader GetDataReader(string query)
        {
            var cmd = new MySqlCommand(query, Connection);

            try
            {
                return cmd.ExecuteReader();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Error getting reader: {ex.Message}\n{ex.StackTrace}");
            }

            return null;
        }

        /// <summary>
        /// Returns result as adapter type
        /// </summary>
        /// <param name="query">mySql command</param>
        /// <returns></returns>
        public static MySqlDataAdapter GetDataAdapter(string query)
        {
            var cmd = new MySqlCommand(query, Connection);

            try
            {
                return new MySqlDataAdapter(cmd);
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Error getting adapter: {ex.Message}\n{ex.StackTrace}");
            }
            return null;
        }

        /// <summary>
        /// Returns one item as object
        /// </summary>
        /// <param name="query">mySql command</param>
        /// <returns></returns>
        public static object GetResult(string query)
        {
            var cmd = new MySqlCommand(query, Connection);

            try
            {
                return cmd.ExecuteScalar();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Error getting results: {ex.Message}\n{ex.StackTrace}");
            }
            return null;
        }
    }
}
