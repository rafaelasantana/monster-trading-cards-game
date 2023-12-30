namespace MTCG.Data.Repositories
{
    public interface IDbConnectionManager
    {
        System.Data.IDbConnection GetConnection();
        bool TestConnection();
    }
}