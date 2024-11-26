// GlobalUsings.cs

global using System;
global using System.IO;
global using System.Linq;
global using System.Collections.Generic;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Configuration;
global using YamlDotNet.Serialization;
global using YamlDotNet.Serialization.NamingConventions;
global using FabricDeploymentHub.Models;
global using FabricDeploymentHub.Models.Items;
global using FabricDeploymentHub.Models.Response;
global using FabricDeploymentHub.Models.State;
global using FabricDeploymentHub.Services;
global using FabricDeploymentHub.Services.Deployment;
global using FabricDeploymentHub.Controllers;
global using FabricDeploymentHub.Interfaces;

global using System.Threading.Tasks; // For Task and asynchronous programming
global using Microsoft.Fabric.Api; // For FabricClient and its methods
global using Microsoft.Fabric.Api.Utils;
// global using Microsoft.Fabric.Api.Admin.Models;
// global using Microsoft.Fabric.Api.Core.Models;
global using AdminModels = Microsoft.Fabric.Api.Admin.Models;
global using CoreModels = Microsoft.Fabric.Api.Core.Models;
global using Azure.Identity; // For DefaultAzureCredential
global using Microsoft.Identity.Client;
global using Azure.Storage.Blobs;
global using System.Text.Json.Serialization;
global using System.Text.Json;
