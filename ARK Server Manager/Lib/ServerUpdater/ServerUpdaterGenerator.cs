using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace ARK_Server_Manager.Lib
{
    public class ServerUpdaterGenerator
    {
        public static string GenerateUpdaterExe(string outputLocation, string serverIP, int rconPort, string adminPass, string installDirectory, string steamCmdPath)
        {
            var rootSrcPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var constantsFile = Path.Combine(Path.GetTempFileName() + ".cs");
            var sources = new[]
            {
                Path.Combine(rootSrcPath, "Lib", "ServerUpdater", "ServerUpdater.cs"),
                Path.Combine(rootSrcPath, "Lib", "ServerUpdater", "ServerUpdaterApp.cs"),
                constantsFile
            };

            var constantsContent = string.Format(@"
namespace ARK_Server_Manager.Lib
{
    static partial class ServerUpdaterApp
    {
        string OutputLocation = {0};
        string ServerIP = {1};
        int    RCONPort = {2};
        string AdminPass = {3};
        string InstallDirectory = {4};
        string SteamCmdPath = {5};
    }
}
", outputLocation, serverIP, rconPort, adminPass, installDirectory, steamCmdPath);

            File.WriteAllText(constantsFile, constantsContent);

            var codeProvider = new CSharpCodeProvider();
            var parameters = new CompilerParameters();
            parameters.GenerateInMemory = false;
            parameters.GenerateExecutable = true;
            parameters.OutputAssembly = Path.Combine(outputLocation, "ServerUpdater.exe");
            parameters.Win32Resource = null;
            parameters.ReferencedAssemblies.Add(Path.Combine(rootSrcPath, "QueryMaster.dll"));
            parameters.ReferencedAssemblies.Add(System.Reflection.Assembly.GetAssembly(typeof(IPAddress)).Location);            

            var results = codeProvider.CompileAssemblyFromFile(parameters, constantsFile);

            if (!results.Errors.HasErrors)
            {
                //
                // Copy the referenced assemblies for runtime
                //
                foreach(string referenceAssembly in parameters.ReferencedAssemblies)
                {
                    var destinationFile = Path.Combine(outputLocation, Path.GetFileName(referenceAssembly));
                    if (File.Exists(destinationFile))
                    {
                        File.Delete(destinationFile);
                    }

                    File.Copy(referenceAssembly, destinationFile);
                }

                return parameters.OutputAssembly;
            }
            else
            {
                var exceptionMessage = new StringBuilder();
                exceptionMessage.AppendLine("Failed to generate updater exe.  Compiler output:");
                foreach (CompilerError error in results.Errors)
                {
                    exceptionMessage.AppendLine(error.ToString());
                    DebugUtils.WriteFormatThreadSafeAsync($"Compiler error: {error}").DoNotWait();
                }

                throw new System.Exception(exceptionMessage.ToString());
            }
        }
    }
}
