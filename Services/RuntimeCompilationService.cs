using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

using Discord.Addons.Hosting;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OriBot.Services
{
    public class WrappedAssembly(Assembly assembly, AssemblyLoadContext asl) : IDisposable
    {
        private Assembly _assembly = assembly;
        public Assembly? Assembly => disposedValue ? null : _assembly;

        private AssemblyLoadContext _asl = asl;
        public AssemblyLoadContext? ASL => disposedValue ? null : _asl;
        private bool disposedValue;

        public void Dispose()
        {
            if (!disposedValue)
            {
                ASL!.Unload();
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
            GC.SuppressFinalize(this);
        }

        public OriBot.Shared.IAssemblyEntryPoint Instantiate()
        {
            
            var type = Assembly!.GetTypes().Where(x => x.IsAssignableTo(typeof(OriBot.Shared.IAssemblyEntryPoint)) && !x.IsInterface && !x.IsAbstract).First();
            return (OriBot.Shared.IAssemblyEntryPoint)(Activator.CreateInstance(type)!);
        }
    }

    public class RuntimeCompilationService(ILogger<RuntimeCompilationService> logger, IOptions<RuntimeCompileOptions> options)
    {
        private RuntimeCompileOptions _options = options.Value;
        private string SDKPath => _options.DotnetBinPath ?? string.Empty;

        private string OutputPath => _options.CompiledPath ?? string.Empty;

        public async Task<WrappedAssembly> CompileProjectAsAssembly(string projectPath, string filename, string aslname = "defaultname")
        {
            var process = Process.Start(SDKPath, ["build",projectPath,"--output",OutputPath]);
            await process.WaitForExitAsync();
            using (var memory = new MemoryStream())
            {
                memory.Write(File.ReadAllBytes(Path.Combine(OutputPath, filename)));
                memory.Position = 0;
                var asl = new AssemblyLoadContext(aslname,true);
                
                var asm =  asl.LoadFromStream(memory);
                File.Delete(Path.Combine(OutputPath, filename));
                return new WrappedAssembly(asm, asl);
            }

        }

    }
}
