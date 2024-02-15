using Npgsql;

namespace RinhaBackendCrebito.Codigo
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
