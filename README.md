---
page_type: sample
description: "Script Actions can be used to customize HDInsight clusters by performing custom configuration or installing additional components."
languages:
- csharp
products:
- dotnet
- azure
- azure-hdinsight
urlFragment: apply-script-action-linux-hdinsight
---

# Apply a Script Action against a running Linux-based HDInsight cluster

Script Actions can be used to customize HDInsight clusters by performing custom configuration or installing additional software components. Until March 2016, you could only use Script Actions when creating a cluster. Now, you can apply Script Actions both during cluster creation and to a running Linux-based cluster (Windows-based clusters are still limited to only using Script Actions during cluster creation.)

This example shows how to work with Script Actions on a running cluster using the .NET SDK for HDInsight.

## How to use

1. [Create a Linux-based HDInsight cluster](https://docs.microsoft.com/azure/hdinsight/hdinsight-hadoop-provision-linux-clusters).
2. Copy this project locally and open in Visual Studio 2015.
3. Modify the following lines to add your *Azure subscription ID*, *HDInsight cluster name*, and the *Azure Resource Group* that the cluster was created in.
```csharp
// Replace the following with your Azure subscription ID    
private static Guid SubscriptionId = new Guid("Subscription ID GUID");
// Replace the following with your HDInsight cluster ID
private const string ClusterName = "Cluster Name";
// Replace the following with the Azure resource group that contains the cluster
private const string ResourceGroupName = "Resource Group Name";
```
4. Run the project. It will install Giraph on the cluster using a Script Action, then display a history of the Script actions ran on the cluster.

5. Optionally, you can uncomment the following line to promote a Script Action to *persist* it. If you scale your cluster to add new nodes, any persisted Script Action that targets worker nodes will be applied to the new worker nodes.
```csharp
// _hdiManagementClient.Clusters.PromoteScript(ResourceGroupName, ClusterName, <scriptexecutionid>);
```
If you no longer want to apply a script when adding nodes to the cluster, you can demote it using `DeletePersistedScript`.
```csharp
// _hdiManagementClient.Clusters.DeletePersistedScript(ResourceGroupName, ClusterName, "<scriptname>");
```
For more information on using Script Actions, see [Customize HDInsight clusters using Script Actions](https://azure.microsoft.com/documentation/articles/hdinsight-hadoop-customize-cluster-linux/).

## Project code of conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
