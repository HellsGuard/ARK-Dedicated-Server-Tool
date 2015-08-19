using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.IO;
using System.Management;
using System.Net;
using System.Reflection;
using System.Text;

namespace ARK_Server_Manager.Lib
{
    public class ServerUpdaterGenerator
    {
        public static string GenerateUpdaterExe(string serverCacheDir, string serverIP, int rconPort, string adminPass, string installDirectory, string steamCmdPath)
        {
            var rootSrcPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);            

            var constantsContent = new StringBuilder();
            constantsContent.AppendLine(@"
namespace ARK_Server_Manager.Lib
{
    partial class ServerUpdaterApp
    {
");
            constantsContent.AppendLine($"static string ServerCacheDir = @\"{serverCacheDir}\";");
            constantsContent.AppendLine($"static string ServerIP = \"{serverIP}\";");
            constantsContent.AppendLine($"static int    RCONPort = {rconPort};");
            constantsContent.AppendLine($"static string AdminPass = \"{adminPass}\";");
            constantsContent.AppendLine($"static string InstallDirectory = @\"{installDirectory}\";");
            constantsContent.AppendLine($"static string SteamCmdPath = @\"{steamCmdPath}\";");
            constantsContent.AppendLine(@"
    }
}");
            string constantsFile = null;

            try
            {
                constantsFile = Path.Combine(Path.GetTempFileName() + ".cs");
                File.WriteAllText(constantsFile, constantsContent.ToString());
                
                var sources = new[]
                {
                    Path.Combine(rootSrcPath, "Lib", "ServerUpdater", "ServerUpdater.cs"),
                    Path.Combine(rootSrcPath, "Lib", "ServerUpdater", "ServerUpdaterApp.cs"),
                    constantsFile
                };

                var codeProvider = new CSharpCodeProvider();
                var parameters = new CompilerParameters();
                parameters.GenerateInMemory = false;
                parameters.GenerateExecutable = true;
                parameters.OutputAssembly = Path.Combine(serverCacheDir, "ServerUpdater.exe");
                parameters.Win32Resource = null;
                parameters.MainClass = "ARK_Server_Manager.Lib.ServerUpdaterApp";
                parameters.ReferencedAssemblies.Add(Path.Combine(rootSrcPath, "QueryMaster.dll"));
                parameters.ReferencedAssemblies.Add(System.Reflection.Assembly.GetAssembly(typeof(IPAddress)).Location);
                parameters.ReferencedAssemblies.Add(System.Reflection.Assembly.GetAssembly(typeof(ManagementObjectSearcher)).Location);

                var results = codeProvider.CompileAssemblyFromFile(parameters, sources);

                if (!results.Errors.HasErrors)
                {
                    //
                    // Copy the referenced assemblies for runtime
                    //
                    foreach (string referenceAssembly in parameters.ReferencedAssemblies)
                    {
                        var destinationFile = Path.Combine(serverCacheDir, Path.GetFileName(referenceAssembly));
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
            finally
            {
                File.Delete(constantsFile);
            }
        }
    }
}
