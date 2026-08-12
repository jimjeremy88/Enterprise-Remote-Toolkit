using System.Collections.Generic;
using RemoteITToolkit.Core.Entities;

namespace RemoteITToolkit.Core.Interfaces
{
    public interface ISettingsRepository
    {
        IEnumerable<Setting> GetAll();
        void Add(Setting entity);
        void Update(Setting entity);
    }
}