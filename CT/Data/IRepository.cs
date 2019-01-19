using System;
using System.Collections.Generic;
using System.Text;

namespace CT.Data
{
    public interface IRepository : IService
    {
        string ConnectionString { get; set; }
        IEnumerable<Composite> Load(string query);
        void Save(Composite composite);
        T Execute<T>(string statement);
        void UpdateSchema<T>() where T : Composite;
    }
}
