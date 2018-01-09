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
            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);
            var client = new ContainerInstanceManagementClient(credentials);
            client.SubscriptionId = subsctiptionId;
            var resources = new ResourceRequirements();
            var request = new ResourceRequests();
            request.Cpu = 1.0;
            request.MemoryInGB = 1.5;

            resources.Requests = request;
            var container = new Container("nginx", "nginx", resources);
            client.ContainerGroups.CreateOrUpdate(resourceGroup, containerGroup,
                new ContainerGroup() {
                    Containers = new Container[] { container },
                    Location = "EastUS",
                    OsType = "Linux"
                     
                   
                       
                }
                );
            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static string resourceGroup = ConfigurationManager.AppSettings.Get("resourceGroup");
        private static string containerGroup = ConfigurationManager.AppSettings.Get("containerGroup");
        private static string subsctiptionId = ConfigurationManager.AppSettings.Get("subsctiptionId");
        private static string clientId = ConfigurationManager.AppSettings.Get("clientId");
        private static string clientSecret = ConfigurationManager.AppSettings.Get("clientSecret");
        private static string tenantId = ConfigurationManager.AppSettings.Get("tenantId");


    }
}
