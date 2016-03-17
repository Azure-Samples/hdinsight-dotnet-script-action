---
services: hdinsight
platforms: dotnet
author: blackmist
---
# Apply a Script Action against a running Linux-based HDInsight cluster

Script Actions can be used to customize HDInsight clusters by performing custom configuration or installing additional software components. Until March 2016, you could only use Script Actions when creating a cluster. Now, you can apply Script Actions both during cluster creation and to a running Linux-based cluster (Windows-based clusters are still limited to only using Script Actions during cluster creation.)

This example shows how to work with Script Actions on a running cluster using the .NET SDK for HDInsight.

##How to use

1. [Create a Linux-based HDInsight cluster](https://azure.microsoft.com/documentation/articles/hdinsight-hadoop-provision-linux-clusters/).
2. Copy this project locally and open in Visual Studio 2015.
3. Modify the following lines to add your __Azure subscription ID__, __HDInsight cluster name__, and the __Azure Resource Group__ that the cluster was created in.

        // Replace the following with your Azure subscription ID    
        private static Guid SubscriptionId = new Guid("Subscription ID GUID");
        // Replace the following with your HDInsight cluster ID
        private const string ClusterName = "Cluster Name";
        // Replace the following with the Azure resource group that contains the cluster
        private const string ResourceGroupName = "Resource Group Name";

4. Run the project. It will install Giraph on the cluster using a Script Action, then display a history of the Script actions ran on the cluster.

5. Optionally, you can uncomment the following line to promote a Script Action to __persist__ it. If you scale your cluster to add new nodes, any persisted Script Action that targets worker nodes will be applied to the new worker nodes.

        // _hdiManagementClient.Clusters.PromoteScript(ResourceGroupName, ClusterName, <scriptexecutionid>);

    If you no longer want to apply a script when adding nodes to the cluster, you can demote it using `DeletePersistedScript`.
    
        // _hdiManagementClient.Clusters.DeletePersistedScript(ResourceGroupName, ClusterName, "<scriptname>");

For more information on using Script Actions, see [Customize HDInsight clusters using Script Actions](https://azure.microsoft.com/documentation/articles/hdinsight-hadoop-customize-cluster-linux/).