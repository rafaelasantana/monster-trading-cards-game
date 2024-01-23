using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MTCG.Data.Services
{
    public class DataMapperService
    {
        public static T MapToObject<T>(Npgsql.NpgsqlDataReader reader) where T : new()
        {
            T obj = new T();
            foreach (var prop in typeof(T).GetProperties())
            {
                try
                {
                    var ordinal = reader.GetOrdinal(prop.Name);
                    if (!reader.IsDBNull(ordinal))
                    {
                        Type convertTo = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        prop.SetValue(obj, Convert.ChangeType(reader.GetValue(ordinal), convertTo));
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    // Column not found in the data reader
                    Console.WriteLine("Column not found in the data reader");
                }
            }
            return obj;
        }
    }
}