using Dapper;

namespace mtcg;

public abstract class Repository<T> : IRepositoryT<T>
{
    /// <summary>
    /// Database table name
    /// </summary>
    protected string _Table = "";

    /// <summary>
    /// Table fields list (comma-separated)
    /// </summary>
    protected string _Fields = "";

    protected readonly DbConnectionManager _dbConnectionManager;

    /// <summary>
    /// Creates a new Repository with Table and Field properties
    /// </summary>
    /// <param name="dbConnectionManager"></param>
    protected Repository(DbConnectionManager dbConnectionManager)
    {
        _dbConnectionManager = dbConnectionManager;
        InitializeTableAndFields();
    }

    /// <summary>
    /// sets the _Table and _Fields properties based on the entity's type
    /// </summary>
    protected void InitializeTableAndFields() {

        Type entityType = typeof(T);

        // Use the entity type's name as the table name
        _Table = entityType.Name;

        // Get the fields (columns) from the entity's properties
        var propertyNames = entityType.GetProperties().Select(property => property.Name);
        _Fields = string.Join(", ", propertyNames);
    }


    /// <summary>
    /// Gets the Object by its ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Object</returns>
    public T? Get(string id) {
        // open connection
        using var connection = _dbConnectionManager.GetConnection();
        connection.Open();

        // build query
        string query = $"SELECT {_Fields} FROM {_Table} Where Id = @Id";

        // execute query and retrieve result
        var result = connection.QueryFirstOrDefault<T>(query, new {Id = id});

        return result;
    }

    /// <summary>
    /// Gets all objects
    /// </summary>
    /// <returns>List of objects</returns>
    public IEnumerable<T>? GetAll() {
        // open connection
        using var connection = _dbConnectionManager.GetConnection();
        connection.Open();

        // query all
        var results = connection.Query<T>($"SELECT * FROM {_Table}");

        return results;
    }

    /// <summary>
    /// deletes an object by its Id
    /// </summary>
    /// <param name="id"></param>
    public void Delete(string id) {
        // open connection
        using var connection = _dbConnectionManager.GetConnection();
        connection.Open();

        // execute delete
        connection.Execute($"DELETE FROM {_Table} Where Id = @Id", new {Id = id});
    }

    /// <summary>
    /// Saves an object
    /// </summary>
    /// <param name="obj"></param>
    public void Save(T obj) {
        // TODO implement in corresponding class
    }
}
