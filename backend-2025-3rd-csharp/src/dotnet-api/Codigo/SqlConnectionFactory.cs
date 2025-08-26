using Npgsql;

namespace RinhaBackend2025.Codigo
{
    public class SqlConnectionFactory(string connectionString)
    {
        private readonly string _connectionString = connectionString;

        public NpgsqlConnection Create()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
