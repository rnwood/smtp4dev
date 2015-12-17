using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev
{
    public class AssemblyResolver
    {
        private IApplicationEnvironment _appEnvironment;

        public AssemblyResolver(IApplicationEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public string GetAssemblyLocation(Assembly assembly)
        {
            string assemblyFileName = assembly.GetName().Name + ".dll";
            return Directory.EnumerateFiles(Path.Combine(_appEnvironment.ApplicationBasePath, ".."), assemblyFileName, SearchOption.AllDirectories).FirstOrDefault();
        }
    }
}