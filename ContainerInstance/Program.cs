using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ContainerInstance;
using Microsoft.Azure.Management.ContainerInstance.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Configuration;

namespace ContainerInstance
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }
        static async Task MainAsync()
        {
            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);
            var client = new ContainerInstanceManagementClient(credentials);
            client.SubscriptionId = subsctiptionId;

            // Creating 1000 containers
            // We can only 20 instances at the same time. Then we need to wait for 1.5 min. 
            // Microsoft.Rest.Azure.CloudException: 'Number of requests for action 'ContainerGroupCreation' exceeded the limit of '20' for time interval '00:05:00'. Please try again after '1.25' minutes.'
            // Microsoft.Rest.Azure.CloudException: 'Resource type 'Microsoft.ContainerInstance/containerGroups' container group quota '20' exceeded in region 'East US'.'
            // I send a support request for change the quota. https://docs.microsoft.com/en-us/azure/container-instances/container-instances-quotas
            var loop = Enumerable.Range(0, 3);
            var locations = new string[] { "EastUS", "WestUS", "WestEurope" }; // Currently available only three resion. 
            var innerLoop = Enumerable.Range(0, 20);

            foreach (var y in loop)
            {
                
                Parallel.ForEach(innerLoop, async x =>
                {
                    await Create10Instances(client, $"containerGroup{y}-{x}", locations[y]);
                });

                await Task.Delay(TimeSpan.FromMinutes(5));
            }
            Console.WriteLine("Done. Press button");
            Console.ReadLine();

            // 1000 concurrent request now for 1 min
            await Task.Delay(TimeSpan.FromMinutes(1));
            // Delete 1000 containers
            foreach (var y in loop)
            {
                Parallel.ForEach(innerLoop, async x =>
                {
                    await DeleteInstance(client, $"containerGroup{y}-{x}");
                });
                await Task.Delay(TimeSpan.FromMinutes(2));
            }

            Console.WriteLine("Done. Deleted");
            Console.ReadLine();
        }

        private static async Task DeleteInstance(ContainerInstanceManagementClient client, string containerGroup)
        {
            await client.ContainerGroups.DeleteAsync(resourceGroup, containerGroup);
        }

        private static async Task Create10Instances(ContainerInstanceManagementClient client, string containerGroup, string location)
        {
            var resources = new ResourceRequirements();
            var request = new ResourceRequests();
            request.Cpu = 0.1;
            request.MemoryInGB = 0.1;

            resources.Requests = request;
            var container = new Container("nginx", "nginx", resources);
            var list = new List<Container>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(new Container($"nginx{i}", "nginx", resources));
            }
            await client.ContainerGroups.CreateOrUpdateAsync(resourceGroup, containerGroup,
                new ContainerGroup()
                {
                    Containers = list,
                    Location = location,
                    OsType = "Linux"

                }
                );
            Console.WriteLine($"Done for {containerGroup}");
        }

        private static string resourceGroup = ConfigurationManager.AppSettings.Get("resourceGroup");
        private static string containerGroup = ConfigurationManager.AppSettings.Get("containerGroup");
        private static string subsctiptionId = ConfigurationManager.AppSettings.Get("subsctiptionId");
        private static string clientId = ConfigurationManager.AppSettings.Get("clientId");
        private static string clientSecret = ConfigurationManager.AppSettings.Get("clientSecret");
        private static string tenantId = ConfigurationManager.AppSettings.Get("tenantId");


    }
}
