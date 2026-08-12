using System;
using System.Collections.Generic;
using RemoteITToolkit.Core.Entities;

namespace RemoteITToolkit.Core.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        T GetById(Guid id);
        IEnumerable<T> GetAll();
        void Add(T entity);
        void Update(T entity);
        void Delete(Guid id);
    }
}