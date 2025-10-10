using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Air.Cloud.Core.Modules.AppAssembly.Model
{
    public  class AssemblyLoadContextInformation
    {
    
        public string Name { get; set; }

        public string AssemblyPath { get; set; }

        public AssemblyName AssemblyName { get; set; }

        public Assembly Assembly{ get; set; }

        public AssemblyLoadContext Context { get; set; }

        public DateTime LoadTime { get; set; }



    }
}
