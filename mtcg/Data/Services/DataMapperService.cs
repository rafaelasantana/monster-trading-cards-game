using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MTCG.Data.Services
{
    public class DataMapperService
    {
        public static T MapToObject<T>(SqlDataReader reader) where T : new()
        {
            T obj = new T();
            foreach (var prop in typeof(T).GetProperties())
            {
                if (!reader.IsDBNull(reader.GetOrdinal(prop.Name)))
                {
                    Type convertTo = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    prop.SetValue(obj, Convert.ChangeType(reader[prop.Name], convertTo));
                }
            }
            return obj;
        }
    }
}