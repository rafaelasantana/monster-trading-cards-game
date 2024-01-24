using Npgsql;
namespace MTCG.Data.Repositories
{
    public interface IDbConnectionManager
    {
        NpgsqlConnection GetConnection();
        bool TestConnection();
    }
}