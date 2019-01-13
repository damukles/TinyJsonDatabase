using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TinyJsonDatabase.Json
{
    public interface IJsonDocumentCollection<T>
    {
        Guid Insert(T json);
        void Update(T json);
        void Delete(Guid jsonId);
        T First(Expression<Func<T, object>> propertySelector, object value);
        IEnumerable<T> Find(Expression<Func<T, object>> propertySelector, object value);
    }
}

