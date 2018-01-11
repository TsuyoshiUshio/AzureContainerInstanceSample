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
            var loop = Enumerable.Range(0, 2); // upto 3
            var locations = new string[] { "EastUS", "WestUS", "WestEurope" }; // Currently available only three resion. 


            var containerGroupNumberRange = Enumerable.Range(0, 10); // upto 20


            //  Create clients
            foreach (var i in containerGroupNumberRange)
            {
                await Create10Instances(client, $"containerGroup0-{i}", "EastUS", 0, $"container-{1}");
            }

            // wait for the all Client has been created. 
            await Task.Delay(TimeSpan.FromMinutes(1));


            foreach (var i in containerGroupNumberRange) { 
                await Create10Instances(client, $"containerGroup1-{i}", "EastUS", 1, $"container-{i}");
            }

            Console.WriteLine("Done. Press button");
            Console.ReadLine(); // Please click after finish the test.

            // 1000 concurrent request now for 1 min
           // await Task.Delay(TimeSpan.FromMinutes(1));
            // Delete 1000 containers
            foreach (var y in loop)
            {
                foreach(var x in containerGroupNumberRange)
                {
                    await DeleteInstance(client, $"containerGroup{y}-{x}");
                }

            }

            Console.WriteLine("Done. Deleted");
            Console.ReadLine();
        }

        private static async Task DeleteInstance(ContainerInstanceManagementClient client, string containerGroup)
        {
            await client.ContainerGroups.DeleteAsync(resourceGroup, containerGroup);
        }

        private static async Task Create10Instances(ContainerInstanceManagementClient client, string containerGroup, string location, int flag, string guid)
        {
            var resources = new ResourceRequirements();
            var request = new ResourceRequests();
            request.Cpu = 0.2;
            request.MemoryInGB = 0.3; // once I tried 0.1 it cause a long execution time. 

            resources.Requests = request;
            
            var list = new List<Container>();
            for (int i = 0; i < 5; i++)
            {
                if (flag == 0)
                {
                    list.Add(CreateClientContainer("efitnesstest/client", i, resources, $"{guid}-{i}"));
                }
                else
                {
                    list.Add(CreateSpamerContainer("efitnesstest/spamer", i, resources, $"{guid}-{i}"));
                }
            }


            await client.ContainerGroups.CreateOrUpdateAsync(resourceGroup, containerGroup,
                new ContainerGroup()
                {
                    Containers = list,
                    Location = location,
                    OsType = "Linux",
                    RestartPolicy = "Never"   // try not to emit the message by restarting
                }
                );
            Console.WriteLine($"Done for {containerGroup}");
        }

        private static  Container CreateSpamerContainer(string name, int i, ResourceRequirements resources, string guid)
        {
            Console.WriteLine($"Create Container: spamer{i}, {name}, guid: {guid}, queName: que1, messageCount: 10 Connection:{ConfigurationManager.AppSettings.Get("StorageConnectionString")}");
            var container = new Container($"spamer{i}", name, resources);
            var list = new List<EnvironmentVariable>();
            list.Add(new EnvironmentVariable("StorageConnectionString", ConfigurationManager.AppSettings.Get("StorageConnectionString")));
            list.Add(new EnvironmentVariable("guid", guid));
            list.Add(new EnvironmentVariable("queName", "que1"));
            list.Add(new EnvironmentVariable("messagesCount", "2")); // original 10 

            container.EnvironmentVariables = list;
            return container;

        }

        private static Container CreateClientContainer(string name, int i, ResourceRequirements resources, string guid)
        {
            Console.WriteLine($"Create Container: client{i}, {name}, DeviceID: {guid}, Connection:{ConfigurationManager.AppSettings.Get("StorageConnectionString")}");

            var container = new Container($"client{i}", name, resources);
            var list = new List<EnvironmentVariable>();
            list.Add(new EnvironmentVariable("StorageConnectionString", ConfigurationManager.AppSettings.Get("StorageConnectionString")));
            list.Add(new EnvironmentVariable("DeviceID", guid));

            container.EnvironmentVariables = list;
            return container;

        }

        private static string resourceGroup = ConfigurationManager.AppSettings.Get("resourceGroup");
        private static string containerGroup = ConfigurationManager.AppSettings.Get("containerGroup");
        private static string subsctiptionId = ConfigurationManager.AppSettings.Get("subsctiptionId");
        private static string clientId = ConfigurationManager.AppSettings.Get("clientId");
        private static string clientSecret = ConfigurationManager.AppSettings.Get("clientSecret");
        private static string tenantId = ConfigurationManager.AppSettings.Get("tenantId");


    }
}
