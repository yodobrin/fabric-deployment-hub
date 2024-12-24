# Fabric Deployment Hub

## Overview

The **Fabric Deployment Hub** is a .NET tool designed for managing deployment planning and execution in Microsoft Fabric. Hosted on Azure Container Apps (ACA), it provides APIs for scalable, multi-workspace deployments, making it ideal for CI/CD integrations and large-scale environments.

A non simple topology will include multiple workspaces and capacities as described below:

![Multi-tenant](./images/fabric-multi-tenant.png)

---

## Why

Microsoft Fabric environments often involve complex deployment scenarios across multiple workspaces. This project simplifies and automates these deployments, addressing challenges such as:
- **Scalability**: Handles deployments across hundreds or thousands of workspaces efficiently.
- **CI/CD Integration**: Seamlessly integrates with existing CI/CD pipelines.
- **Flexibility**: Supports dynamic configurations to adapt to varying deployment requirements.

---

## What

Here is a very high level diagram of the Fabric Deployment Hub together with the fabric tenant:

![Deployment Hub](./images/deployment_hub_HL.png)

This tool provides:

- **REST APIs** for managing deployments programmatically.
- **Containerized Environment** for easy scalability and deployment.
- **Dynamic Configuration Management** to streamline multi-tenant deployments.
- **Azure Managed Identity Integration** for secure authentication and access control.

### Additional Resources

For a detailed explanation of the **guiding principles** and **design guidelines** used in the Fabric Deployment Hub, refer to the [Design Principles](./design-principles.md) document.

---

### Key Features

- **Scalable Deployments**: Handles large-scale deployment needs.
- **Customizable Configurations**: Uses `appsettings.json` for tenant-specific setups.
- **Secure Authentication**: Integrates with Azure Managed Identities for secure and compliant operations.

![GitHub & DeploymentHub](./images/github-deploymenthub.png)
---

## How

### Deployment Planning

The **Fabric Deployment Planning Process** is designed to ensure that all resources within a tenant are properly evaluated and prepared for deployment. It follows a structured approach to validate configurations, resolve dependencies, and create deployment plans. The process is robust, handling errors gracefully to minimize the impact of issues on the overall planning.

---

#### Steps in the Planning Process

1. **Validate Tenant Request**  
   The process begins by validating the incoming tenant deployment request to ensure it meets the necessary criteria. Any validation errors are logged and surfaced as issues, halting further processing for invalid requests.

2. **Fetch Workspaces**  
   All workspaces associated with the tenant are retrieved to determine the scope of the deployment. This is done using an authenticated API call to the Fabric API.

3. **Plan Deployment for Each Workspace**  
   For each workspace:
   - Validate its configuration.
   - Process each item (e.g., notebooks, files) within the workspace, including:
     - Reading metadata.
     - Resolving dependencies (e.g., Lakehouse, Environment).
     - Replacing placeholders with resolved values. (settings, variables, etc.)
     - Injecting resolved metadata into the content.
     - Creating a deployment request for eligible items.
   - If any critical issues are encountered, the workspace is marked as invalid, and no deployment plan is created for it.

   A saved plan would be created on a new storage container. A plan contains all the information required to run the deployment. Some of the information during the planning phase would be updated during validation phase.

4. **Handle Dependencies**  
   Dependencies such as Lakehouse and Environment are resolved for each item:
   - If a dependency is missing or invalid, it is logged as an issue, and the item is skipped. The reason for failure to obtain dependencies is logged, but less critical, as for all reasons the item is skipped.
   - The process continues for other items in the workspace.

5. **Generate Deployment Plan**  
   For each valid workspace, a deployment plan is generated:
   - Includes all eligible and validated items.
   - Details any issues encountered during the planning process.

6. **Save Deployment Plan** (Optional)  
   If requested, the final deployment plan is saved to a designated storage location for validation and execution.

---

#### Error Handling

The planning process is designed to be fault-tolerant:
- Errors at the item level (e.g., missing metadata, invalid dependencies) are logged, and the specific item is skipped.
- Critical errors at the workspace level halt processing for that workspace, but other workspaces are processed independently.
- The overall tenant planning continues unless the request itself is invalid.

---

#### Key Benefits

- **Resilient:** Ensures issues in one workspace or item do not affect others.
- **Transparent:** Logs detailed messages and tracks issues for each folder, workspace, and item.
- **Customizable:** Supports saving and reviewing deployment plans for validation and execution. 

This approach ensures a reliable and efficient deployment planning process while highlighting and isolating any issues for further investigation.

### Prerequisites

- .NET 8.0 SDK
- Docker (for local containerization)
- Azure Subscription with Container Apps enabled
- Configuration values for:
  - `FABRIC_TENANT_CONFIG`
  - `AZURE_TENANT_ID`
  - `FABRIC_API_CLIENT_ID`
  - `FABRIC_API_CLIENT_SECRET`

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/your-repository/fabric-deployment-hub.git
2.	Navigate to the project directory:
    ```bash
    cd fabric-deployment-hub
    ```
3. Restore dependencies and build the project:
    ```bash
    dotnet restore
    dotnet build
    ```
### Running Locally

1. Update appsettings.json with your configuration values.
2. Start the application: `dotnet run`
3. Access the APIs at http://localhost:5000.

### Containerization

1. Build the Docker image: `docker build -t fabric-deployment-hub .`
2. Run the container: `docker run -p 5000:80 fabric-deployment-hub`

### Deployment to Azure Container Apps

1. Ensure containerapp-config.json is configured for your environment.
2. Deploy the app using Azure CLI: `az containerapp up --source . --name fabric-deployment-hub --resource-group <your-resource-group>`

## License

This project is licensed under the terms of the MIT License.