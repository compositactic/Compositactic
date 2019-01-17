using CT.Test.Model.Shops.Registers;
using System.Collections.Generic;

namespace CT.Test.Presentation.Shops.Registers
{
    public interface IRegisterService : IService
    {
        void Save(Register register);
        IEnumerable<Register> LoadDefaultRegisters();
    }
}
