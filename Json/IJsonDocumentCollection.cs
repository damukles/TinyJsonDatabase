using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TinyJsonDatabase.Json
{
    public interface IJsonDocumentCollection<T>
    {
        void Insert(T json);
        void Update(T json);
        T First(Expression<Func<T, object>> propertySelector, object value);
        IEnumerable<T> Find(Expression<Func<T, object>> propertySelector, object value);
        void DeleteFirst(Expression<Func<T, object>> propertySelector, object value);
        void Delete(Expression<Func<T, object>> propertySelector, object value);
    }
}

