# Design Principles

## **Fabric Principals**

The **Fabric Deployment Hub** is designed to address the specific needs of **ISV teams** working with Microsoft Fabric. Below are the guiding principles and use cases for teams that can make effective use of this tool:


### **1. Workspace-Centric Approach**

- **Baseline**: In Fabric, workspaces are a foundational concept for organizing and isolating resources. ISV teams can adopt different workspace deployment patterns depending on their multi-tenancy strategy:
  - **Shared Workspace**: One workspace shared among all customers.
  - **Dedicated Workspace**: A separate workspace for each customer.
- **Principle**: The hub ensures flexibility in accommodating these patterns by supporting deployments tailored to each workspace type.

### **2. Alignment with Code Lifecycle Practices**

- **Baseline**: Fabric provides three code lifecycle approaches:
  1. **Deployment Pipelines**: Uses an existing workspace as the source of deployment.
  2. **Update from Git**: Uses a connected Git repository as the source of truth.
  3. **Granular REST API Calls**: Direct deployment of individual items via REST APIs.
- **Principle**: The hub aligns with established developer workflows by focusing on granular REST API calls, where:
  - Developers work in branches, connect a branch to a workspace for development and unit testing.
  - Once changes are ready, a pull request (PR) is submitted, and the hub handles **planning**, **validation**, and **deployment**.
  - **Source of Truth**: The Git repository remains the single source of truth, ensuring consistency with broader software development lifecycle practices.

### **3. Multi-Tiered ISV Offerings**

- **Baseline**: ISVs often provide multiple tiers of offerings (e.g., Free, Silver, Gold), each with a distinct set of Fabric components.
- **Principle**: The hub ensures deployment logic aligns with the tier of the workspace. For example:
  - A **Silver** workspace would deploy a specific set of components.
  - A **Gold** workspace would deploy an enhanced set of components.
  This ensures accurate and consistent deployments across tiers.

## **Deployment-Hub Design Principles**

### 1. Modularity and Separation of Concerns
- **What**: Each component has a well-defined responsibility and does not overlap with others.
- **Why**: Enhances readability, maintainability, and testability of the codebase.
- **How**:
  - Services like `FabricRestService`, `DeploymentProcessor`, and `BlobUtils` focus on specific concerns such as REST API communication, deployment orchestration, and blob storage utilities.
  - Controllers (`DeploymentsController`, `PlannerController`) act as orchestrators, delegating logic to services rather than handling it directly.

---

### 2. Extensibility
- **What**: The system is designed to easily accommodate new features or deployment item types.
- **Why**: Adapts to evolving requirements without significant refactoring.
- **How**:
  - Use of interfaces like `IDeploymentRequest` allows for implementing new deployment item types with minimal changes.
  - `BaseDeployRequest` provides a foundation for common functionality while allowing individual classes like `DeployNotebookRequest` to extend behavior.

---

### 3. Resilient Error Handling
- **What**: Comprehensive error handling ensures robustness and graceful degradation.
- **Why**: Prevents unexpected application crashes and provides actionable feedback to users.
- **How**:
  - Controllers catch and log exceptions with meaningful messages.
  - Services like `FabricRestService` provide detailed error logs, including HTTP response codes and relevant context.
  - Critical sections, such as API calls and blob operations, use try-catch blocks with fallback mechanisms.

---

### 4. Logging and Observability
- **What**: Comprehensive logging provides insights into the system's behavior.
- **Why**: Simplifies debugging and helps identify issues in real-time or during retrospectives.
- **How**:
  - Structured logs in all layers (e.g., controllers, services) include contextual details like workspace IDs, request URIs, and sanitized payloads.
  - Payload sanitization ensures sensitive information (e.g., Base64-encoded files) is redacted from logs.
  - Consistent log levels (`info`, `warn`, `error`) guide actionable insights.

---

### 5. Data Security
- **What**: Sensitive data is protected from unintended exposure.
- **Why**: Ensures compliance with privacy and security standards.
- **How**:
  - Payload sanitization removes sensitive fields (e.g., `payload` in deployment requests) before logging.
  - Authentication and authorization mechanisms (e.g., bearer tokens) are enforced for API calls.
  - Secure credential storage using environment variables or secure configurations.

---

### 6. Reusability
- **What**: Common functionality is extracted into reusable components or utilities.
- **Why**: Reduces code duplication and simplifies future enhancements.
- **How**:
  - Utilities like `BlobUtils` and `PayloadSanitizer` handle recurring tasks such as blob operations and logging sanitization.
  - The use of configuration services (e.g., `IFabricTenantStateService`) centralizes access to shared state and metadata.
