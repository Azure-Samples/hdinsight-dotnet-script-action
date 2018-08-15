using System;
using Microsoft.Azure;
using Microsoft.Azure.Management.HDInsight;
using Microsoft.Azure.Management.HDInsight.Models;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using Newtonsoft.Json;


using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace scriptaction
{
    class Program
    {
        // Provides management access to HDInsight
        private static HDInsightManagementClient _hdiManagementClient;

        // Replace with your AAD tenant ID if necessary
        private const string TenantId = UserTokenProvider.CommonTenantId;
        // This is the GUID for the PowerShell client. Used for interactive logins in this example.
        private const string ClientId = "1950a258-227b-4e31-a9cf-717495945fc2";
        // Replace the following with your Azure subscription ID
        private const string SubscriptionId = "Subscription ID GUID";
        // Replace the following with your HDInsight cluster ID
        private const string ClusterName = "Cluster Name";
        // Replace the following with the Azure resource group that contains the cluster
        private const string ResourceGroupName = "Resource Group Name";

        // Script information. In this example, the script to install Giraph on the cluster
        private const string GiraphScriptName = "Install Giraph";
        private const string GiraphScriptUri = "https://hdiconfigactions.blob.core.windows.net/linuxgiraphconfigactionv01/giraph-installer-v01.sh";

        // App Information. In this example, Datameer is installed on the cluster and script actions are run on edgenode
        private const string AppNameDatameer = "DatameerV2";
        private const string AppNameHue = "Hue";
        private const string AppScriptActionName = "Testwritehostname";
        private const string AppScriptActionUri = "https://hdiconfigactions.blob.core.windows.net/linuxsampleconfigaction/sample.sh";
        
        
        static void Main(string[] args)
        {
            // Create a new list to hold script information
            IList<RuntimeScriptAction> Scripts = new List<RuntimeScriptAction>();
            // Add a new script item for the Giraph script
            Scripts.Add(
                new RuntimeScriptAction(
                    GiraphScriptName, 
                    new Uri(GiraphScriptUri), 
                    new List<string>() { "headnode", "workernode" })); // The nodes the script will be used on
            // You could continue adding more scripts here by repeating the Scripts.Add process

            // Authenticate and get a token
            var authToken = Authenticate(TenantId, ClientId, SubscriptionId);
            // Flag subscription for HDInsight, if it isn't already.
            EnableHDInsight(authToken);
            // Get an HDInsight management client
            _hdiManagementClient = new HDInsightManagementClient(authToken);

            //Console.WriteLine("Applying scripts to the cluster, please wait...");
            // Run the script(s) on the cluster and, if they succeed, persist them.
            var scriptStatus = _hdiManagementClient.Clusters.ExecuteScriptActions(
                ResourceGroupName,
                ClusterName,
                Scripts,
                false); // Final parameter is whether the scripts should be persisted

            Console.WriteLine("Script status: {0}", scriptStatus.State);

            Console.WriteLine("The following is a history of the scripts applied to this cluster.");
            // Get a history of scripts ran on the cluster
            var scriptHistory = _hdiManagementClient.Clusters.ListScriptExecutionHistory(
                ResourceGroupName,
                ClusterName);
            // Loop through the results and display name and execution ID
            foreach (var script in scriptHistory)
            {
                Console.WriteLine("Script name: {0}, execution id: {1}", script.Name, script.ScriptExecutionId);
            }
            
            // If you wanted to promote a script to persisted, or demote (delete) a persisted script,
            // uncomment the following and provide an execution id.
            // NOTE: 'Deleting' a persisted script does not undo changes made by the script on the cluster
            // it only removes it from the list of persisted scripts, so it will not be applied to new worker nodes
            //
            // _hdiManagementClient.Clusters.PromoteScript(ResourceGroupName, ClusterName, <scriptexecutionid>);
            // _hdiManagementClient.Clusters.DeletePersistedScript(ResourceGroupName, ClusterName, "<scriptname>");

            //EdgeNode Customization
            //EdgeNodes can be customized by providing AppName in the script action payload.
            //If AppName is specified, script will be run on all edgenodes with the app installed. 
            //Constraints : "When application name is specified, only one script can be specified, persistOnSuccess has to be false, and roles must contain only 'edgenode'"
            
            // Edgenode customization when AppName is specified
            
            var appScriptWithApplicationName = new RuntimeScriptAction()
            {
                Name = AppScriptActionName,
                Uri = new Uri(AppScriptActionUri),
                Parameters = string.Empty,
                Roles = new List<string>() { "edgenode" },
                ApplicationName = AppNameHue
            };
            Scripts.Clear();
            Scripts.Add(appScriptWithApplicationName);
            Console.WriteLine("Applying script to the edgenode with App installed when application name is specified, please wait...");
            var appScriptWithApplicationNameStatus = _hdiManagementClient.Clusters.ExecuteScriptActions(
                ResourceGroupName,
                ClusterName,
                Scripts,
                false); // When app name is specified, Persist on success should be false.

            Console.WriteLine("Script status: {0}", appScriptWithApplicationNameStatus.State);

            // EdgeNode customization when AppName is not specified but roles list contains edgenode
            var appScriptWithoutApplicationName = new RuntimeScriptAction()
            {
                Name = AppScriptActionName,
                Uri = new Uri(AppScriptActionUri),
                Parameters = string.Empty,
                Roles = new List<string>() { "edgenode" }
            };
            Scripts.Clear();
            Scripts.Add(appScriptWithoutApplicationName);
            Console.WriteLine("Applying script to the edgenode with App installed when application name is not specified, please wait...");
            var appScriptWithoutApplicationNameStatus = _hdiManagementClient.Clusters.ExecuteScriptActions(
                ResourceGroupName,
                ClusterName,
                Scripts,
                false);

            Console.WriteLine("Script status: {0}", appScriptWithoutApplicationNameStatus.State);
            
            // When multiple apps are installed, Edgenode customization must run only on edge nodes with corresponding app installed
            var appScriptDatameer = new RuntimeScriptAction()
            {
                Name = AppScriptActionName,
                Uri = new Uri(AppScriptActionUri),
                Parameters = string.Empty,
                Roles = new List<string>() { "edgenode" },
                ApplicationName = AppNameDatameer
            };
            Scripts.Clear();
            Scripts.Add(appScriptDatameer);
            Console.WriteLine("Applying customization on Datameer app nodes. Should run script action only on Datameer app edgenodes, please wait...");
            var appScriptDatameerStatus = _hdiManagementClient.Clusters.ExecuteScriptActions(
                ResourceGroupName,
                ClusterName,
                Scripts,
                false);

            Console.WriteLine("Script status: {0}", appScriptDatameerStatus.State);
            
            // Check the apps run on corresponding edgenodes
            Console.WriteLine("Checking script actions are executed on edgenodes for corresponding apps...");
            var appScriptExecutionHistory = _hdiManagementClient.Clusters.ListScriptExecutionHistory(ResourceGroupName, ClusterName);

            foreach (var script in appScriptExecutionHistory) 
            {
                var scriptExecutionDetail = _hdiManagementClient.Clusters.GetScriptExecutionDetail(ResourceGroupName, ClusterName, script.ScriptExecutionId).RuntimeScriptActionDetail;
                JToken debugInfo = JToken.Parse(scriptExecutionDetail.DebugInformation);
                JArray tasks = (JArray)debugInfo.SelectToken("tasks");
                foreach (JToken token in tasks)
                {
                    JToken edgenode = token.SelectToken("Tasks.host_name");
                    if (!String.IsNullOrEmpty(script.ApplicationName))
                    {
                        Console.WriteLine("Edgenode for " + script.ApplicationName + " : " + edgenode);
                    }
                }  
            }
           

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// Authenticate to an Azure subscription and retrieve an authentication token
        /// </summary>
        /// <param name="TenantId">The AAD tenant ID</param>
        /// <param name="ClientId">The AAD client ID</param>
        /// <param name="SubscriptionId">The Azure subscription ID</param>
        /// <returns></returns>
        static TokenCloudCredentials Authenticate(string TenantId, string ClientId, string SubscriptionId)
        {
            var authContext = new AuthenticationContext("https://login.microsoftonline.com/" + TenantId);
            var tokenAuthResult = authContext.AcquireToken("https://management.core.windows.net/",
                ClientId,
                new Uri("urn:ietf:wg:oauth:2.0:oob"),
                PromptBehavior.Always,
                UserIdentifier.AnyUser);
            return new TokenCloudCredentials(SubscriptionId, tokenAuthResult.AccessToken);
        }
        /// <summary>
        /// Marks your subscription as one that can use HDInsight, if it has not already been marked as such.
        /// </summary>
        /// <remarks>This is essentially a one-time action; if you have already done something with HDInsight
        /// on your subscription, then this isn't needed at all and will do nothing.</remarks>
        /// <param name="authToken">An authentication token for your Azure subscription</param>
        static void EnableHDInsight(TokenCloudCredentials authToken)
        {
            // Create a client for the Resource manager and set the subscription ID
            var resourceManagementClient = new ResourceManagementClient(new TokenCredentials(authToken.Token));
            resourceManagementClient.SubscriptionId = SubscriptionId;
            // Register the HDInsight provider
            var rpResult = resourceManagementClient.Providers.Register("Microsoft.HDInsight");
        }
    }
}
