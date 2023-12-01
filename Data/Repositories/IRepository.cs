namespace mtcg;

/// <summary>
/// Repositories implement this interface
/// </summary>
public interface IRepository<T>
{
    /// <summary>
    /// Gets the Object by its ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Object</returns>
    public T? Get(string id);

    /// <summary>
    /// Gets all objects
    /// </summary>
    /// <returns>List of objects</returns>
    public IEnumerable<T> GetAll();

    /// <summary>
    /// Deletes an object
    /// </summary>
    /// <param name="obj"></param>
    public void Delete(string id);

    /// <summary>
    /// Saves an object
    /// </summary>
    /// <param name="obj"></param>
    public void Save(T obj);
}
