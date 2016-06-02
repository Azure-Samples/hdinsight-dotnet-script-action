using System;
using System.Security;
using Microsoft.Azure;
using Microsoft.Azure.Common.Authentication;
using Microsoft.Azure.Common.Authentication.Factories;
using Microsoft.Azure.Common.Authentication.Models;
using Microsoft.Azure.Management.HDInsight;
using Microsoft.Azure.Management.HDInsight.Models;
using Newtonsoft.Json;


using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace scriptaction
{
    class Program
    {
        // Provides management access to HDInsight
        private static HDInsightManagementClient _hdiManagementClient;

        // Replace the following with your Azure subscription ID
        private static Guid SubscriptionId = new Guid("Subscription ID GUID");
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
        private const string AppScriptActionUri = "https://dulestorage.blob.core.windows.net/publichumboldtscriptactions/write-hostname-to-storage.sh";
        
        
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

            // Get tokens and credentials
            var tokenCreds = GetTokenCloudCredentials();
            var subCloudCredentials = GetSubscriptionCloudCredentials(tokenCreds, SubscriptionId);
            // Connect to HDInsight management
            _hdiManagementClient = new HDInsightManagementClient(subCloudCredentials);
            
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
        /// Authenticate to Azure
        /// </summary>
        /// <param name="username">The Azure login</param>
        /// <param name="password">The password for the Azure login</param>
        /// <returns>An access token used to authenticate access to Azure</returns>
        public static TokenCloudCredentials GetTokenCloudCredentials(string username = null, SecureString password = null)
        {
            var authFactory = new AuthenticationFactory();

            var account = new AzureAccount { Type = AzureAccount.AccountType.User };

            if (username != null && password != null)
                account.Id = username;

            var env = AzureEnvironment.PublicEnvironments[EnvironmentName.AzureCloud];

            var accessToken =
                authFactory.Authenticate(account, env, AuthenticationFactory.CommonAdTenant, password, ShowDialog.Auto)
                    .AccessToken;

            return new TokenCloudCredentials(accessToken);
        }

        /// <summary>
        /// Get a token that provides access to a specific subscription
        /// </summary>
        /// <param name="creds">Azure access credentials</param>
        /// <param name="subId">The subscription ID you wish to access</param>
        /// <returns>Credentials used to authenticate requests to resources in the subscription</returns>
        public static SubscriptionCloudCredentials GetSubscriptionCloudCredentials(TokenCloudCredentials creds, Guid subId)
        {
            return new TokenCloudCredentials(subId.ToString(), creds.Token);

        }
    }
}
