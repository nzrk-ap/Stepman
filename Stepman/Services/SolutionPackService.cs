using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Stepman.Services
{
    public class SolutionPackService
    {
        public Task Pack(string solutionPath,  string zipPath, string map)
        {
            string script = $"dotnet tool install --global Microsoft.PowerApps.CLI.Tool;" +
                $"pac solution pack --zipfile {zipPath} --folder {solutionPath} --packagetype Managed --map {map}";

            using (PowerShell ps = PowerShell.Create())
            {
                // Add the script to execute
                ps.AddScript(script);

                // Execute the script and get the output
                Collection<PSObject> results = ps.Invoke();

                // Output the results
                foreach (PSObject result in results)
                {
                    Console.WriteLine(result.ToString());
                }
            }

            return Task.CompletedTask;
        }
    }
}
