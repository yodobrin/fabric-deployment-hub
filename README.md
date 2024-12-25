# Fabric Deployment Hub

## Overview

The **Fabric Deployment Hub** is a .NET tool designed for managing deployment planning and execution in Microsoft Fabric. Hosted on Azure Container Apps (ACA), it provides APIs for scalable, multi-workspace deployments, making it ideal for CI/CD integrations and large-scale environments.

---

## Why

Microsoft Fabric environments often involve complex deployment scenarios across multiple workspaces. This project simplifies and automates these deployments, addressing challenges such as:
- **Scalability**: Handles deployments across hundreds or thousands of workspaces efficiently.
- **CI/CD Integration**: Seamlessly integrates with existing CI/CD pipelines.
- **Flexibility**: Supports dynamic configurations to adapt to varying deployment requirements.

---

## What

Here is a very high level diagram of the Fabric Deployment Hub together with a non simple fabric tenant topology with multiple workspaces and capacities:

![Deployment Hub](./images/deployment_hub_HL.png)

This tool provides:

- **REST APIs** for managing deployments programmatically.
- **Containerized Environment** for easy scalability and deployment.
- **Dynamic Configuration Management** to streamline multi-tenant deployments.
- **Azure Managed Identity Integration** for secure authentication and access control.

The security angle is detailed in [network-security-design](./network-security-design.md)

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

Below is a **Mermaid** diagram illustrating the main workflows.


```mermaid
graph TD

    %% --- Define shared styles for coloring ---
    classDef lightblue fill:#ADD8E6,stroke:#333,stroke-width:1px,color:#333
    classDef lightgreen fill:#90EE90,stroke:#333,stroke-width:1px,color:#333
    classDef yellow fill:#FFFF00,stroke:#333,stroke-width:1px,color:#333
    classDef pink fill:#FFC0CB,stroke:#333,stroke-width:1px,color:#333
    classDef lightgrey fill:#D3D3D3,stroke:#333,stroke-width:1px,color:#333
    classDef red fill:#FF0000,stroke:#333,stroke-width:1px,color:#fff

    %% -----------------------------------------
    %% Section 1
    %% -----------------------------------------
    A[Validate Tenant Request]:::lightblue --> B{Valid Request?}:::yellow
    B -- Yes --> C[Fetch Workspaces]:::lightgreen
    B -- No --> G[Log Error]:::red

    C --> D[Process Workspaces]:::yellow
    %% "Process Workspaces" is a rectangle, but we show a separate error path
    D --> E[Create Deployment Plan]:::pink
    D -->|Error| G[Log Error]:::red

    E --> F[Save Deployment Plan]:::lightgrey

    %% -----------------------------------------
    %% Section 2
    %% -----------------------------------------
    H[Validate Request]:::lightblue --> I{Valid Request?}:::yellow
    I -- Yes --> J[Load Deployment Plan]:::lightgreen
    I -- No --> G[Log Error]:::red

    %% "Load Deployment Plan" is a rectangle, can still fail
    J --> K[Validate Workspaces]:::yellow
    J -->|Error| G

    %% "Validate Workspaces" as a rectangle; can fail
    K --> L[Save Validated Plan]:::pink
    K -->|Error| G

    L --> M[Return Results]:::lightgrey

    %% -----------------------------------------
    %% Section 3
    %% -----------------------------------------
    N[Validate Request]:::lightblue --> O{Valid Request?}:::yellow
    O -- Yes --> P[Load Deployment Plan]:::lightgreen
    O -- No --> G[Log Error]:::red

    P --> Q[Process Workspaces]:::yellow
    P -->|Error| G

    Q --> R[Handle Deployment Requests]:::pink
    Q -->|Error| G

    R --> S[Handle Errors]:::red
    S --> T[Return Results]:::lightgrey
```
### Overview

The Fabric Deployment Hub streamlines tenant resource deployment by providing a fault-tolerant, multi-phase approach:

1. **Deployment Planning**:  
   - Validates the tenant request.
   - Fetches and processes workspaces.
   - Resolves dependencies and creates a deployment plan.

2. **Validate Deployment Plan**:  
   - Loads and verifies the deployment plan.
   - Checks each workspace and its items, marking them as create, update, or error.

3. **Deploy a Plan**:  
   - Executes each valid deployment request.
   - Logs or skips errors without halting the entire process.

4. **Error Handling**:  
   - Errors are isolated by item, workspace, or overall plan, ensuring partial failures don’t halt the entire deployment.

5. **Key Benefits**:  
   - **Fault-Tolerant Execution**: Issues in one workspace/item don’t disrupt others.  
   - **Granular Validation & Deployment**: Each request is validated independently.  
   - **Detailed Logging**: Comprehensive logs for each phase.  
   - **Customizable Actions**: Supports create, update, or more advanced operations.  
   - **Actionable Results**: Returns clear outcomes for each phase, including detailed errors.

>Note: For more detailed information about each phase, see [detailed-deployment-phases](detailed-deployment-phases.md).

---

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