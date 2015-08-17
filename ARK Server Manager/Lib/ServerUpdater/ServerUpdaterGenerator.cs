using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.IO;
using System.Net;
using System.Reflection;

namespace ARK_Server_Manager.Lib
{
    public class ServerUpdaterGenerator
    {
        public static void GenerateUpdaterExe()
        {
            var rootSrcPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var pathToSource = Path.Combine(rootSrcPath, "Lib", "ServerUpdater", "ServerUpdater.cs");
            var outputLocation = "E:\\Test";
            var codeProvider = new CSharpCodeProvider();


            var parameters = new CompilerParameters();
            parameters.GenerateInMemory = false;
            parameters.GenerateExecutable = true;
            parameters.OutputAssembly = Path.Combine(outputLocation, "ServerUpdater.exe");
            parameters.Win32Resource = null;
            parameters.ReferencedAssemblies.Add(Path.Combine(rootSrcPath, "QueryMaster.dll"));
            parameters.ReferencedAssemblies.Add(System.Reflection.Assembly.GetAssembly(typeof(IPAddress)).Location);            

            var results = codeProvider.CompileAssemblyFromFile(parameters, pathToSource);
            if (!results.Errors.HasErrors)
            {
                foreach(string referenceAssembly in parameters.ReferencedAssemblies)
                {
                    var destinationFile = Path.Combine(outputLocation, Path.GetFileName(referenceAssembly));
                    if (File.Exists(destinationFile))
                    {
                        File.Delete(destinationFile);
                    }

                    File.Copy(referenceAssembly, destinationFile);
                }
            }
            else
            {
                foreach (CompilerError error in results.Errors)
                {
                    DebugUtils.WriteFormatThreadSafeAsync($"Compiler error: {error}").DoNotWait();
                }
            }
        }
    }
}
