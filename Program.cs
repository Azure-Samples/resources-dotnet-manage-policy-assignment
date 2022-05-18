// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;

namespace ManagePolicyAssignment
{
    public class Program
    {
        /**
        * Azure PolicyAssignment sample for managing policy assignments -
        * - Create a policy assignment
        * - Create another policy assignment
        * - List policy assignments
        * - Delete policy assignments.
        */
        public static async Task RunSample(ArmClient client)
        {
            var resourceGroupName = "rgRSMPA";
            var policyDefinitionName = "pdn";
            var policyAssignmentName1 = "pan1";
            var policyAssignmentName2 = "pan2";
            var policyRuleJson = "{\"if\":{\"not\":{\"field\":\"location\",\"in\":[\"northeurope\",\"westeurope\"]}},\"then\":{\"effect\":\"deny\"}}";

            SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();
            try
            {
                //=============================================================
                // Create resource group.
                Console.WriteLine($"Creating a resource group with name: {resourceGroupName}");

                var lro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, new ResourceGroupData(AzureLocation.WestUS));
                var resourceGroup = lro.Value;

                Console.WriteLine($"Resource group created: {resourceGroup.Id}");

                //=============================================================
                // Create policy definition.
                Console.WriteLine($"Creating a policy definition with name: {policyDefinitionName}");

                var policyDefinitionData = new PolicyDefinitionData
                {
                    PolicyRule = BinaryData.FromString(policyRuleJson),
                    PolicyType = PolicyType.Custom,
                };
                var pdLro = await subscription.GetSubscriptionPolicyDefinitions().CreateOrUpdateAsync(WaitUntil.Completed, policyDefinitionName, policyDefinitionData);
                SubscriptionPolicyDefinitionResource policyDefinition = pdLro.Value;

                Console.WriteLine($"Policy definition created: {policyDefinition.Id}");

                //=============================================================
                // Create policy assignment.

                Console.WriteLine($"Creating a policy assignment with name: {policyAssignmentName1}");

                var policyAssignmentData = new PolicyAssignmentData
                {
                    PolicyDefinitionId = policyDefinition.Id,
                    EnforcementMode = EnforcementMode.Enforced,
                };
                var policyAssignmentLro = await resourceGroup.GetPolicyAssignments().CreateOrUpdateAsync(WaitUntil.Completed, policyAssignmentName1, policyAssignmentData);
                var policyAssignment = policyAssignmentLro.Value;

                Console.WriteLine($"Policy assignment created: {policyAssignment.Id}");

                //=============================================================
                // Create another policy assignment.

                Console.WriteLine($"Creating a policy assignment with name: {policyAssignmentName2}");

                policyAssignmentData = new PolicyAssignmentData
                {
                    PolicyDefinitionId = policyDefinition.Id,
                    EnforcementMode = EnforcementMode.DoNotEnforce,
                };
                policyAssignmentLro = await resourceGroup.GetPolicyAssignments().CreateOrUpdateAsync(WaitUntil.Completed, policyAssignmentName2, policyAssignmentData);
                var policyAssignment2 = policyAssignmentLro.Value;

                Console.WriteLine($"Policy assignment created: {policyAssignment2.Id}");

                //=============================================================
                // List policy assignments.

                Console.WriteLine("Listing all policy assignments: ");

                foreach (var pAssignment in resourceGroup.GetPolicyAssignments())
                {
                    Console.WriteLine($"Policy assignment: {pAssignment.Id}");
                }

                //=============================================================
                // Delete policy assignments.

                Console.WriteLine($"Deleting policy assignment: {policyAssignmentName1}");

                await policyAssignment.DeleteAsync(WaitUntil.Completed);

                Console.WriteLine($"Deleted policy assignment: {policyAssignmentName1}");

                Console.WriteLine($"Deleting policy assignment: {policyAssignmentName2}");

                await policyAssignment2.DeleteAsync(WaitUntil.Completed);

                Console.WriteLine($"Deleted policy assignment: {policyAssignmentName2}");

                //=============================================================
                // Delete policy definition.

                Console.WriteLine($"Deleting policy definition: {policyDefinitionName}");

                await policyDefinition.DeleteAsync(WaitUntil.Completed);

                Console.WriteLine($"Deleted policy definition: {policyDefinitionName}");
            }
            finally
            {
                try
                {
                    Console.WriteLine($"Deleting Resource Group: {resourceGroupName}");

                    var resourceGroupId = ResourceGroupResource.CreateResourceIdentifier(subscription.Data.SubscriptionId, resourceGroupName);
                    await client.GetResourceGroupResource(resourceGroupId).DeleteAsync(WaitUntil.Completed);

                    Console.WriteLine($"Deleted Resource Group: {resourceGroupName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            
        }

        public static async Task Main(string[] args)
        {
            try
            {
                //=================================================================
                // Authenticate
                var credential = new DefaultAzureCredential();

                var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
                // you can also use `new ArmClient(credential)` here, and the default subscription will be the first subscription in your list of subscription
                var client = new ArmClient(credential, subscriptionId);

                await RunSample(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
