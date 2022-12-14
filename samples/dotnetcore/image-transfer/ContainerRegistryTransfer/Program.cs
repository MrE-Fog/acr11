using ContainerRegistryTransfer.Clients;
using ContainerRegistryTransfer.Helpers;
using ContainerRegistryTransfer.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace ContainerRegistryTransfer
{
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                string appSettingsFile = args.Length > 0
                    ? args[0]
                    : Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "appsettings.json");
                var options = LoadOptions(appSettingsFile);
                options.Validate();

                // Use ACR Transfer to move artifacts between two registries
                await TransferRegistryArtifacts(options).ConfigureAwait(false);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"Failed with the following error:");
                Console.WriteLine(ex);
                return -1;
            }
        }

        private static async Task TransferRegistryArtifacts(Options options)
        {
            var exportOptionsDisplay = options.ExportPipeline.Options != null ? string.Join(", ", options.ExportPipeline.Options) : "";
            var importOptionsDisplay = options.ImportPipeline.Options != null ? string.Join(", ", options.ImportPipeline.Options) : "";

            Console.WriteLine($"Starting ContainerRegistryTransfer...");
            Console.WriteLine();
            Console.WriteLine($"Azure Environment properties:");
            Console.WriteLine($"  MIClientId: {options.MIClientId}");
            Console.WriteLine($"  SPClientId: {options.SPClientId}");
            Console.WriteLine($"  AzureEnvironment: {options.AzureEnvironment.Name}");
            Console.WriteLine($"  SubscriptionId: {options.SubscriptionId}");
            Console.WriteLine($"======================================================================");
            Console.WriteLine($"ExportPipeline properties:");
            Console.WriteLine($"  ResourceGroupName: {options.ExportPipeline.ResourceGroupName}");
            Console.WriteLine($"  RegistryName: {options.ExportPipeline.RegistryName}");
            Console.WriteLine($"  ExportPipelineName: {options.ExportPipeline.PipelineName}");
            Console.WriteLine($"  UserAssignedIdentity: {options.ExportPipeline.UserAssignedIdentity}");
            Console.WriteLine($"  StorageUri: {options.ExportPipeline.ContainerUri}");
            Console.WriteLine($"  KeyVaultSecretUri: {options.ExportPipeline.KeyVaultUri}");
            Console.WriteLine($"  Options: {exportOptionsDisplay}");
            Console.WriteLine($"======================================================================");
            Console.WriteLine($"ImportPipeline properties:");
            Console.WriteLine($"  ResourceGroupName: {options.ImportPipeline.ResourceGroupName}");
            Console.WriteLine($"  RegistryName: {options.ImportPipeline.RegistryName}");
            Console.WriteLine($"  ImportPipelineName: {options.ImportPipeline.PipelineName}");
            Console.WriteLine($"  UserAssignedIdentity: {options.ImportPipeline.UserAssignedIdentity}");
            Console.WriteLine($"  StorageUri: {options.ImportPipeline.ContainerUri}");
            Console.WriteLine($"  KeyVaultSecretUri: {options.ImportPipeline.KeyVaultUri}");
            Console.WriteLine($"  Options: {importOptionsDisplay}");
            Console.WriteLine($"======================================================================");
            Console.WriteLine();

            var registryClient = AzureHelper.GetContainerRegistryManagementClient(options);
            var keyVaultClient = AzureHelper.GetKeyVaultManagementClient(options);

            var exportClient = new ExportClient(registryClient, keyVaultClient, options);
            var exportPipeline = await exportClient.CreateExportPipelineAsync().ConfigureAwait(false);

            var importClient = new ImportClient(registryClient, keyVaultClient, options);
            var importPipeline = await importClient.CreateImportPipelineAsync().ConfigureAwait(false);

            Console.WriteLine();
            Console.WriteLine($"======================================================================");
            Console.WriteLine($"Your importPipeline '{importPipeline.Name}' will run automatically.");
            Console.WriteLine($"Would you like to run your exportPipeline '{options.ExportPipeline.PipelineName}'? [Y/N]");
            var response = Console.ReadLine();

            if (string.Equals("Y", response, StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("Validating pipelineRun configurations for export.");
                options.ExportPipelineRun.Validate();
                await exportClient.ExportImagesAsync(exportPipeline).ConfigureAwait(false);
            }

            Console.WriteLine("ContainerRegistryTransfer completed. Goodbye!");
        }

        private static Options LoadOptions(string appSettingsFile)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile(appSettingsFile, optional: true)
                .AddEnvironmentVariables();

            var options = new Options();

            builder.Build().Bind(options);

            return options;
        }
    }
}
