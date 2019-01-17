using CT.Test.Model.Shops.Registers;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using CT.Test.Presentation.Shops.Registers;

namespace CT.Test.Presentation.Services.Shops.Registers
{
    public class RegisterService : IRegisterService
    {
        private RegisterService() { }

        public CompositeRoot CompositeRoot { get; set; }

        public IEnumerable<Register> LoadDefaultRegisters()
        {
            var registerDirectory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "Shops", "Registers"));
            foreach(var registerFile in registerDirectory.EnumerateFiles())
                yield return JsonConvert.DeserializeObject<Register>(File.ReadAllText(registerFile.FullName));
        }

        public void Save(Register register)
        {
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "Shops", "Registers", register.Id + ".json"), JsonConvert.SerializeObject(register));
        }
    }
}
