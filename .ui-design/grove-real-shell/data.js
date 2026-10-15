/* Immutable evidence from the running FIPS2022 Flight host. Unknown values remain null. */
window.GROVE_DATA = {
  "hostUrl": "http://localhost:5294",
  "capturedAt": "2026-10-04T16:01:43.859Z",
  "system": {
    "capturedAtUtc": "2026-10-04T16:01:26Z",
    "application": {
      "name": "航班服务",
      "id": "dev-service-flight-api",
      "version": "v1.0.3",
      "environmentName": "Development"
    },
    "process": {
      "startedAtUtc": "2026-10-04T15:45:23Z",
      "runtimeVersion": ".NET 10.0.12",
      "architecture": "64-bit process",
      "processId": 34056,
      "workingSetBytes": 558710784,
      "is64BitOperatingSystem": null,
      "is64BitProcess": true,
      "isPrivileged": false,
      "pageSizeBytes": 4096,
      "isInteractive": true,
      "processPath": "D:\\Repositories\\WorkTree2\\FIPS2022\\src\\Services\\Flight\\FlightService.API\\bin\\Debug\\net10.0\\FlightService.API.exe"
    },
    "host": {
      "machineName": "DESKTOP-H210SVV",
      "userName": "mo",
      "userDomainName": "DESKTOP-H210SVV",
      "operatingSystem": "Microsoft Windows 10.0.26100",
      "currentDirectory": "D:\\Repositories\\WorkTree2\\FIPS2022\\src\\Services\\Flight\\FlightService.API",
      "timeZone": {
        "id": "China Standard Time",
        "displayName": "China Standard Time (UTC+08:00)",
        "utcOffset": "08:00:00"
      }
    },
    "artifact": {
      "productName": "FlightService.API",
      "productVersion": "1.0.0+e2b1c46d76102346757ff834cfa715eec53e86a5",
      "fileVersion": "1.0.0.0",
      "companyName": "FlightService.API",
      "legalCopyright": null,
      "artifactTimestampUtc": "2026-10-04T15:45:22Z"
    },
    "startup": {
      "durationMs": 6243.1855,
      "readyAtUtc": "2026-10-04T15:45:30Z",
      "startedAtUtc": null
    },
    "endpoints": [
      {
        "address": "http://localhost:5294",
        "scheme": "http"
      }
    ],
    "links": [
      {
        "label": "接口管理",
        "description": "查看和测试 API 接口",
        "path": "/swagger",
        "external": true,
        "target": "New tab",
        "icon": "code"
      },
      {
        "label": "服务发现",
        "description": "查看服务注册与发现状态",
        "path": "/service-discovery-manage",
        "external": false,
        "target": "Current tab",
        "icon": "server"
      },
      {
        "label": "项目单元",
        "description": "浏览项目单元与架构图",
        "path": "/project-units",
        "external": false,
        "target": "Current tab",
        "icon": "module"
      },
      {
        "label": "Observable Instance",
        "description": "查看实例状态与异常记录",
        "path": "/observable-instance-monitor",
        "external": false,
        "target": "Current tab",
        "icon": "pulse"
      },
      {
        "label": "EventBus",
        "description": "检查订阅、状态和统计信息",
        "path": "/event-bus-monitor",
        "external": false,
        "target": "Current tab",
        "icon": "server"
      },
      {
        "label": "Execution Timing",
        "description": "接口性能监控",
        "path": "/execution-timing",
        "external": false,
        "target": "Current tab",
        "icon": "clock"
      },
      {
        "label": "Job Scheduler",
        "description": "作业调度面板",
        "path": "/job-scheduler",
        "external": false,
        "target": "Current tab",
        "icon": "clock"
      }
    ],
    "timestampPrecision": "Dossier display timestamps have second precision; startup-marker UTC time was not exposed in the captured UI."
  },
  "navigation": [
    {
      "id": "ai",
      "label": "AI",
      "icon": "ai",
      "items": [
        {
          "id": "/ai-chat",
          "base": "/ai-chat",
          "label": "AI Assistant",
          "path": "/ai-chat"
        },
        {
          "id": "/ai-providers",
          "base": "/ai-providers",
          "label": "AI Provider Management",
          "path": "/ai-providers"
        },
        {
          "id": "/ai/capabilities",
          "base": "/ai/capabilities",
          "label": "AI Capabilities",
          "path": "/ai/capabilities"
        }
      ]
    },
    {
      "id": "knowledge",
      "label": "Knowledge & Retrieval",
      "icon": "book",
      "items": [
        {
          "id": "/ai/knowledge-bases",
          "base": "/ai/knowledge-bases",
          "label": "Knowledge Bases",
          "path": "/ai/knowledge-bases"
        },
        {
          "id": "/ai/rag/manage",
          "base": "/ai/rag/manage",
          "label": "RAG Indexing",
          "path": "/ai/rag/manage"
        },
        {
          "id": "/ai/rag/debug",
          "base": "/ai/rag/debug",
          "label": "RAG Debug",
          "path": "/ai/rag/debug"
        },
        {
          "id": "/ai/rag/chunkers",
          "base": "/ai/rag/chunkers",
          "label": "RAG Chunker Management",
          "path": "/ai/rag/chunkers"
        }
      ]
    },
    {
      "id": "documentation",
      "label": "Documentation",
      "icon": "book",
      "items": [
        {
          "id": "/markdown-docs",
          "base": "/markdown-docs",
          "label": "Markdown Library",
          "path": "/markdown-docs"
        }
      ]
    },
    {
      "id": "configuration",
      "label": "Configuration",
      "icon": "settings",
      "items": [
        {
          "id": "/configuration/state",
          "base": "/configuration/state",
          "label": "Configuration State",
          "path": "/configuration/state"
        },
        {
          "id": "/configuration/history",
          "base": "/configuration/history",
          "label": "Configuration History",
          "path": "/configuration/history"
        },
        {
          "id": "/configuration/versions",
          "base": "/configuration/versions",
          "label": "Configuration Versions",
          "path": "/configuration/versions"
        },
        {
          "id": "/configuration/debug",
          "base": "/configuration/debug",
          "label": "Configuration Debug",
          "path": "/configuration/debug"
        },
        {
          "id": "/configuration/storage",
          "base": "/configuration/storage",
          "label": "Configuration Storage",
          "path": "/configuration/storage"
        }
      ]
    },
    {
      "id": "infrastructure",
      "label": "Infrastructure",
      "icon": "infrastructure",
      "items": [
        {
          "id": "/dependency-injection",
          "base": "/dependency-injection",
          "label": "Dependency Injection",
          "path": "/dependency-injection"
        },
        {
          "id": "/execution-pipeline",
          "base": "/execution-pipeline",
          "label": "Execution Pipeline",
          "path": "/execution-pipeline"
        },
        {
          "id": "/ui/k8s",
          "base": "/ui/k8s",
          "label": "K8S Operations",
          "path": "/ui/k8s"
        },
        {
          "id": "/ui/file-ops",
          "base": "/ui/file-ops",
          "label": "FileOps Workspace",
          "path": "/ui/file-ops"
        },
        {
          "id": "/eventbus-kafka",
          "base": "/eventbus-kafka",
          "label": "Kafka EventBus",
          "path": "/eventbus-kafka"
        },
        {
          "id": "/ui/utilities",
          "base": "/ui/utilities",
          "label": "Utilities Toolbox",
          "path": "/ui/utilities"
        },
        {
          "id": "/git-repositories",
          "base": "/git-repositories",
          "label": "Git Repositories",
          "path": "/git-repositories"
        },
        {
          "id": "/ui/terminal",
          "base": "/ui/terminal",
          "label": "Terminal",
          "path": "/ui/terminal"
        }
      ]
    },
    {
      "id": "module",
      "label": "Module",
      "icon": "modules",
      "items": [
        {
          "id": "module-workbench",
          "label": "Module workbench",
          "path": "/module-system-dashboard",
          "children": [
            {
              "id": "/module-system-dashboard/overview",
              "base": "/module-system-dashboard/overview",
              "label": "Overview",
              "path": "/module-system-dashboard/overview"
            },
            {
              "id": "/module-system-dashboard/performance",
              "base": "/module-system-dashboard/performance",
              "label": "Performance",
              "path": "/module-system-dashboard/performance"
            },
            {
              "id": "/module-system-dashboard/modules",
              "base": "/module-system-dashboard/modules",
              "label": "Modules",
              "path": "/module-system-dashboard/modules"
            },
            {
              "id": "/module-system-dashboard/dependencies",
              "base": "/module-system-dashboard/dependencies",
              "label": "Dependencies",
              "path": "/module-system-dashboard/dependencies"
            },
            {
              "id": "/module-system-dashboard/discovery",
              "base": "/module-system-dashboard/discovery",
              "label": "Discovery",
              "path": "/module-system-dashboard/discovery"
            }
          ],
          "sourceLabel": "Module diagnostics",
          "projection": "Existing page-owned sections are projected into this nested shell menu."
        }
      ]
    },
    {
      "id": "task-scheduling",
      "label": "Task Scheduling",
      "icon": "task",
      "items": [
        {
          "id": "/job-scheduler",
          "base": "/job-scheduler",
          "label": "Scheduler overview",
          "path": "/job-scheduler"
        },
        {
          "id": "/job-scheduler/catalog",
          "base": "/job-scheduler/catalog",
          "label": "Job catalog",
          "path": "/job-scheduler/catalog"
        },
        {
          "id": "/job-scheduler/executions",
          "base": "/job-scheduler/executions",
          "label": "Execution ledger",
          "path": "/job-scheduler/executions"
        },
        {
          "id": "/job-scheduler/statistics",
          "base": "/job-scheduler/statistics",
          "label": "Scheduler analytics",
          "path": "/job-scheduler/statistics"
        },
        {
          "id": "/job-scheduler/runtime",
          "base": "/job-scheduler/runtime",
          "label": "Scheduler runtime",
          "path": "/job-scheduler/runtime"
        }
      ]
    },
    {
      "id": "monitor",
      "label": "Monitor",
      "icon": "pulse",
      "items": [
        {
          "id": "/health-checks",
          "base": "/health-checks",
          "label": "Health checks",
          "path": "/health-checks"
        },
        {
          "id": "/seeders",
          "base": "/seeders",
          "label": "Seeders",
          "path": "/seeders"
        },
        {
          "id": "/project-units",
          "base": "/project-units",
          "label": "Project Units",
          "path": "/project-units"
        },
        {
          "id": "/execution-timing",
          "base": "/execution-timing",
          "label": "Execution Timing",
          "path": "/execution-timing"
        },
        {
          "id": "/logging-monitor",
          "base": "/logging-monitor",
          "label": "Logging Monitor",
          "path": "/logging-monitor"
        },
        {
          "id": "/repository-dashboard",
          "base": "/repository-dashboard",
          "label": "Repository Diagnostics",
          "path": "/repository-dashboard"
        },
        {
          "id": "/event-bus-monitor",
          "base": "/event-bus-monitor",
          "label": "Event Bus Monitor",
          "path": "/event-bus-monitor"
        },
        {
          "id": "/service-discovery-manage",
          "base": "/service-discovery-manage",
          "label": "Service Discovery",
          "path": "/service-discovery-manage"
        },
        {
          "id": "/system-info",
          "base": "/system-info",
          "label": "System Information",
          "path": "/system-info"
        }
      ]
    },
    {
      "id": "debug",
      "label": "Debug",
      "icon": "debug",
      "items": [
        {
          "id": "/mapper-debug",
          "base": "/mapper-debug",
          "label": "Mapper Debug",
          "path": "/mapper-debug"
        },
        {
          "id": "/state-store-dashboard",
          "base": "/state-store-dashboard",
          "label": "State Store Management",
          "path": "/state-store-dashboard"
        },
        {
          "id": "/observable-instance-monitor",
          "base": "/observable-instance-monitor",
          "label": "Observable Instance",
          "path": "/observable-instance-monitor"
        },
        {
          "id": "/diff-highlight-debug",
          "base": "/diff-highlight-debug",
          "label": "Text Diff Comparison",
          "path": "/diff-highlight-debug"
        }
      ]
    }
  ],
  "modules": [
    {
      "id": "Monica.Configuration::Monica.Modules.ModuleConfiguration",
      "name": "ModuleConfiguration",
      "fullTypeName": "Monica.Modules.ModuleConfiguration",
      "assembly": "Monica.Configuration",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 100,
      "isUI": false,
      "isWeb": true,
      "costMs": 743.4217000000004,
      "serialCallbackMs": 734.9858000000005,
      "startupWorkMs": 8.4359,
      "requires": [],
      "dependents": [
        "Monica.Configuration.EfCore::Monica.Modules.ModuleConfigurationEfCore",
        "Monica.Configuration.EventBus::Monica.Configuration.EventBus.Modules.ModuleConfigurationEventBus",
        "Monica.Configuration.UI::Monica.Modules.ModuleConfigurationUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.6752999999999929,
          "startMs": 323.0138,
          "spanId": "module-phase-000003",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 2.120400000000018,
          "startMs": 338.4489,
          "spanId": "module-phase-000098",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.29850000000004684,
          "startMs": 464.4548,
          "spanId": "module-phase-000193",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 1.3579000000000292,
          "startMs": 466.6866,
          "spanId": "module-phase-000288",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 1.5307000000000244,
          "startMs": 1781.7799,
          "spanId": "module-phase-000386",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder / Registration contribution",
          "durationMs": 1.113299999999981,
          "startMs": 1783.3275,
          "spanId": "module-phase-000387",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure builder / Registration contribution",
          "durationMs": 0.06640000000015789,
          "startMs": 1784.4469,
          "spanId": "module-phase-000388",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services",
          "durationMs": 0.7786999999998443,
          "startMs": 1986.036,
          "spanId": "module-phase-000482",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.1531999999999698,
          "startMs": 1986.8216,
          "spanId": "module-phase-000483",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Declare type discovery / Type discovery commit",
          "durationMs": 2.3284999999996217,
          "startMs": 2146.8778,
          "spanId": "module-phase-000607",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Type discovery commit"
        },
        {
          "name": "Startup work",
          "durationMs": 8.435899999999947,
          "startMs": 2149.6044,
          "spanId": "startup-work-000000",
          "kind": "Startup work",
          "modulePhase": "Declare type discovery",
          "callbackKind": null
        },
        {
          "name": "Declare type discovery / Startup work commit",
          "durationMs": 1.8056000000001404,
          "startMs": 2184.6739,
          "spanId": "module-phase-000620",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Startup work commit"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.058800000000246655,
          "startMs": 2186.6182,
          "spanId": "module-phase-000622",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 722.6403,
          "startMs": 2433.2574,
          "spanId": "module-phase-000720",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.05820000000039727,
          "startMs": 3206.0478,
          "spanId": "module-phase-000755",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Logging::Monica.Modules.ModuleLogging",
      "name": "ModuleLogging",
      "fullTypeName": "Monica.Modules.ModuleLogging",
      "assembly": "Monica.Logging",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 110,
      "isUI": false,
      "isWeb": true,
      "costMs": 217.59879999999993,
      "serialCallbackMs": 217.59879999999993,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.Framework.UI::Monica.Modules.ModuleLoggingUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0003000000000383807,
          "startMs": 324.6375,
          "spanId": "module-phase-000007",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 20.35129999999998,
          "startMs": 340.5837,
          "spanId": "module-phase-000099",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.004700000000013915,
          "startMs": 464.7595,
          "spanId": "module-phase-000194",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.044699999999977535,
          "startMs": 468.0513,
          "spanId": "module-phase-000289",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 197.1449,
          "startMs": 1784.5888,
          "spanId": "module-phase-000389",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.023899999999912325,
          "startMs": 1987.0235,
          "spanId": "module-phase-000484",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0007999999997991836,
          "startMs": 2186.684,
          "spanId": "module-phase-000623",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.027399999999943248,
          "startMs": 3155.9222,
          "spanId": "module-phase-000721",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.0008000000002539309,
          "startMs": 3206.1148,
          "spanId": "module-phase-000756",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.HealthCheck::Monica.Modules.ModuleHealthCheck",
      "name": "ModuleHealthCheck",
      "fullTypeName": "Monica.Modules.ModuleHealthCheck",
      "assembly": "Monica.HealthCheck",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 120,
      "isUI": false,
      "isWeb": true,
      "costMs": 5.402299999999457,
      "serialCallbackMs": 5.402299999999457,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.Dapr::Monica.Modules.ModuleDaprClient",
        "Monica.Framework::Monica.Modules.ModuleSeeder",
        "Monica.HealthCheck.UI::Monica.Modules.ModuleHealthCheckUI",
        "Monica.JobScheduler::Monica.Modules.ModuleJobScheduler"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0002000000000066393,
          "startMs": 324.6629,
          "spanId": "module-phase-000009",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.30239999999997735,
          "startMs": 360.9461,
          "spanId": "module-phase-000100",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0020000000000095497,
          "startMs": 464.7666,
          "spanId": "module-phase-000195",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.008400000000051477,
          "startMs": 468.0986,
          "spanId": "module-phase-000290",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.04479999999989559,
          "startMs": 1981.7581,
          "spanId": "module-phase-000390",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 1.3097999999999956,
          "startMs": 1987.0531,
          "spanId": "module-phase-000485",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0009999999997489795,
          "startMs": 2186.688,
          "spanId": "module-phase-000624",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.01209999999991851,
          "startMs": 3155.9741,
          "spanId": "module-phase-000722",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 3.721599999999853,
          "startMs": 3206.1196,
          "spanId": "module-phase-000757",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.DependencyInjection::Monica.Modules.ModuleDependencyInjection",
      "name": "ModuleDependencyInjection",
      "fullTypeName": "Monica.Modules.ModuleDependencyInjection",
      "assembly": "Monica.DependencyInjection",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 130,
      "isUI": false,
      "isWeb": false,
      "costMs": 15.895199999999704,
      "serialCallbackMs": 15.895199999999704,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.Framework.UI::Monica.Modules.ModuleDependencyInjectionUI",
        "Monica.Repository::Monica.Modules.ModuleUnitOfWork",
        "Monica.WebApi::Monica.Modules.ModuleRpcClient",
        "Monica.WebApi::Monica.Modules.ModuleWebApi"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0002000000000066393,
          "startMs": 324.694,
          "spanId": "module-phase-000011",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.07009999999996808,
          "startMs": 361.2545,
          "spanId": "module-phase-000101",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.002100000000041291,
          "startMs": 464.7716,
          "spanId": "module-phase-000196",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.7091000000000349,
          "startMs": 468.1088,
          "spanId": "module-phase-000291",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0040999999998803105,
          "startMs": 1981.8105,
          "spanId": "module-phase-000391",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.3148999999998523,
          "startMs": 1988.3748,
          "spanId": "module-phase-000486",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery / Type discovery commit",
          "durationMs": 14.794100000000071,
          "startMs": 2149.2142,
          "spanId": "module-phase-000608",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Type discovery commit"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005999999998493877,
          "startMs": 2186.692,
          "spanId": "module-phase-000625",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Repository::Monica.Modules.ModuleSnowflake",
      "name": "ModuleSnowflake",
      "fullTypeName": "Monica.Modules.ModuleSnowflake",
      "assembly": "Monica.Repository",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 140,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.4766000000001327,
      "serialCallbackMs": 0.4766000000001327,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0002000000000066393,
          "startMs": 324.6966,
          "spanId": "module-phase-000012",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.11189999999999145,
          "startMs": 361.3288,
          "spanId": "module-phase-000102",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001599999999996271,
          "startMs": 464.7761,
          "spanId": "module-phase-000197",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.007400000000018281,
          "startMs": 468.8241,
          "spanId": "module-phase-000292",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.003200000000106229,
          "startMs": 1981.8191,
          "spanId": "module-phase-000392",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.35170000000016444,
          "startMs": 1988.7015,
          "spanId": "module-phase-000487",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005999999998493877,
          "startMs": 2186.6952,
          "spanId": "module-phase-000626",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Core::Monica.Modules.ModuleObjectMapping",
      "name": "ModuleObjectMapping",
      "fullTypeName": "Monica.Modules.ModuleObjectMapping",
      "assembly": "Monica.Core",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 150,
      "isUI": false,
      "isWeb": true,
      "costMs": 743.9734000000001,
      "serialCallbackMs": 75.3961000000001,
      "startupWorkMs": 668.5773,
      "requires": [],
      "dependents": [
        "Monica.Framework.UI::Monica.Modules.ModuleMapperUI",
        "Monica.Repository::Monica.Modules.ModuleRepository",
        "Monica.WebApi::Monica.Modules.ModuleWebApi"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.00010000000003174137,
          "startMs": 324.698,
          "spanId": "module-phase-000013",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.07730000000003656,
          "startMs": 361.4491,
          "spanId": "module-phase-000103",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0015000000000213731,
          "startMs": 464.784,
          "spanId": "module-phase-000198",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.07459999999997535,
          "startMs": 468.8328,
          "spanId": "module-phase-000293",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0022999999998774,
          "startMs": 1981.8248,
          "spanId": "module-phase-000393",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.15330000000017208,
          "startMs": 1989.0619,
          "spanId": "module-phase-000488",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery / Type discovery commit",
          "durationMs": 0.22029999999995198,
          "startMs": 2164.0132,
          "spanId": "module-phase-000609",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Type discovery commit"
        },
        {
          "name": "Post-configure services",
          "durationMs": 74.65029999999979,
          "startMs": 2186.6975,
          "spanId": "module-phase-000627",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Startup work",
          "durationMs": 668.5772999999999,
          "startMs": 2261.3547,
          "spanId": "startup-work-000001",
          "kind": "Startup work",
          "modulePhase": "Post-configure services",
          "callbackKind": null
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.004200000000309956,
          "startMs": 3156.0022,
          "spanId": "module-phase-000723",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.21219999999993888,
          "startMs": 3209.8556,
          "spanId": "module-phase-000758",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.AutoModel::Monica.Modules.ModuleAutoModel",
      "name": "ModuleAutoModel",
      "fullTypeName": "Monica.Modules.ModuleAutoModel",
      "assembly": "Monica.AutoModel",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 160,
      "isUI": false,
      "isWeb": true,
      "costMs": 0.3100999999997498,
      "serialCallbackMs": 0.3100999999997498,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.WebApi::Monica.Modules.ModuleAutoControllers",
        "Monica.WebApi::Monica.Modules.ModuleWebApi"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.00029999999998153726,
          "startMs": 324.8962,
          "spanId": "module-phase-000017",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.07749999999998636,
          "startMs": 361.5297,
          "spanId": "module-phase-000104",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0014000000000464752,
          "startMs": 464.7879,
          "spanId": "module-phase-000199",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.006100000000003547,
          "startMs": 468.9104,
          "spanId": "module-phase-000294",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0029999999999290594,
          "startMs": 1981.8307,
          "spanId": "module-phase-000394",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.17039999999997235,
          "startMs": 1989.2252,
          "spanId": "module-phase-000489",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0032999999998537533,
          "startMs": 2261.3626,
          "spanId": "module-phase-000628",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.0025000000000545697,
          "startMs": 3156.0114,
          "spanId": "module-phase-000724",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.04559999999992215,
          "startMs": 3210.0808,
          "spanId": "module-phase-000759",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.StateStore::Monica.Modules.ModuleStateStore",
      "name": "ModuleStateStore",
      "fullTypeName": "Monica.Modules.ModuleStateStore",
      "assembly": "Monica.StateStore",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 170,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.299699999999973,
      "serialCallbackMs": 0.299699999999973,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.Dapr::Monica.Modules.ModuleDaprStateStore",
        "Monica.ServiceDiscovery::Monica.Modules.ModuleServiceDiscovery",
        "Monica.StateStore.UI::Monica.Modules.ModuleStateStoreUI",
        "Monica.StateStore::Monica.Modules.ModuleTaskProgress"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0033000000000242835,
          "startMs": 325.2797,
          "spanId": "module-phase-000022",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.04529999999999745,
          "startMs": 361.6102,
          "spanId": "module-phase-000105",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0019000000000346517,
          "startMs": 464.7915,
          "spanId": "module-phase-000200",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005399999999951888,
          "startMs": 468.9177,
          "spanId": "module-phase-000295",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0017000000000280124,
          "startMs": 1981.8373,
          "spanId": "module-phase-000395",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.19590000000016516,
          "startMs": 1989.4033,
          "spanId": "module-phase-000490",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.04539999999997235,
          "startMs": 1989.6029,
          "spanId": "module-phase-000491",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0007999999997991836,
          "startMs": 2261.3705,
          "spanId": "module-phase-000629",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Core::Monica.Modules.ModuleClock",
      "name": "ModuleClock",
      "fullTypeName": "Monica.Modules.ModuleClock",
      "assembly": "Monica.Core",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 180,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.08779999999978827,
      "serialCallbackMs": 0.08779999999978827,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.Repository::Monica.Modules.ModuleRepository"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0004000000000132786,
          "startMs": 325.7153,
          "spanId": "module-phase-000030",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.07339999999999236,
          "startMs": 361.6574,
          "spanId": "module-phase-000106",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013999999999896318,
          "startMs": 464.7948,
          "spanId": "module-phase-000201",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005300000000033833,
          "startMs": 468.924,
          "spanId": "module-phase-000296",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0019999999999527063,
          "startMs": 1981.8413,
          "spanId": "module-phase-000396",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.0048000000001593435,
          "startMs": 1989.6542,
          "spanId": "module-phase-000492",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0004999999996471161,
          "startMs": 2261.3733,
          "spanId": "module-phase-000630",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Core::Monica.Modules.ModuleJsonSerialization",
      "name": "ModuleJsonSerialization",
      "fullTypeName": "Monica.Modules.ModuleJsonSerialization",
      "assembly": "Monica.Core",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 190,
      "isUI": false,
      "isWeb": false,
      "costMs": 3.2915999999996757,
      "serialCallbackMs": 3.2915999999996757,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.Core::Monica.Modules.ModuleResultEnvelope",
        "Monica.Dapr::Monica.Modules.ModuleDaprClient",
        "Monica.EventBus.Kafka::Monica.Modules.ModuleEventBusKafka",
        "Monica.EventBus::Monica.Modules.ModuleEventBus",
        "Monica.Framework.UI::Monica.Modules.ModuleEventBusUI",
        "Monica.Framework::Monica.Modules.ModuleChainTracing",
        "Monica.ProjectUnits::Monica.Modules.ModuleProjectUnits"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0004999999999881766,
          "startMs": 325.9572,
          "spanId": "module-phase-000034",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.41399999999998727,
          "startMs": 361.7368,
          "spanId": "module-phase-000107",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001699999999971169,
          "startMs": 464.7973,
          "spanId": "module-phase-000202",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005699999999990268,
          "startMs": 468.9301,
          "spanId": "module-phase-000297",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0026000000000294676,
          "startMs": 1981.8455,
          "spanId": "module-phase-000397",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 2.86649999999986,
          "startMs": 1989.6617,
          "spanId": "module-phase-000493",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005999999998493877,
          "startMs": 2261.3757,
          "spanId": "module-phase-000631",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Core::Monica.Modules.ModuleResultEnvelope",
      "name": "ModuleResultEnvelope",
      "fullTypeName": "Monica.Modules.ModuleResultEnvelope",
      "assembly": "Monica.Core",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 200,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.8635999999995647,
      "serialCallbackMs": 0.8635999999995647,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleJsonSerialization"
      ],
      "dependents": [
        "Monica.Core::Monica.Modules.ModuleExceptionHandling",
        "Monica.Framework.UI::Monica.Modules.ModuleLoggingUI",
        "Monica.WebApi::Monica.Modules.ModuleRpcClient",
        "Monica.WebApi::Monica.Modules.ModuleSwagger"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.017899999999997362,
          "startMs": 324.6424,
          "spanId": "module-phase-000008",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.14660000000003492,
          "startMs": 362.1559,
          "spanId": "module-phase-000108",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.6544999999999845,
          "startMs": 464.8,
          "spanId": "module-phase-000203",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004799999999988813,
          "startMs": 468.9366,
          "spanId": "module-phase-000298",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.003200000000106229,
          "startMs": 1981.8498,
          "spanId": "module-phase-000398",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.03609999999980573,
          "startMs": 1992.5385,
          "spanId": "module-phase-000494",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0004999999996471161,
          "startMs": 2261.378,
          "spanId": "module-phase-000632",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Core::Monica.Modules.ModuleExceptionHandling",
      "name": "ModuleExceptionHandling",
      "fullTypeName": "Monica.Modules.ModuleExceptionHandling",
      "assembly": "Monica.Core",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 210,
      "isUI": false,
      "isWeb": true,
      "costMs": 10.806500000000142,
      "serialCallbackMs": 10.806500000000142,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleResultEnvelope"
      ],
      "dependents": [
        "Monica.Authority::Monica.Modules.ModuleAuthorization",
        "Monica.Framework::Monica.Modules.ModuleChainTracing",
        "Monica.WebApi::Monica.Modules.ModuleAutoControllers",
        "Monica.WebApi::Monica.Modules.ModuleWebApi"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.017899999999997362,
          "startMs": 324.8751,
          "spanId": "module-phase-000016",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.17029999999999745,
          "startMs": 362.3059,
          "spanId": "module-phase-000109",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.005200000000002092,
          "startMs": 465.4612,
          "spanId": "module-phase-000204",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004800000000045657,
          "startMs": 468.9421,
          "spanId": "module-phase-000299",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002100000000154978,
          "startMs": 1981.8563,
          "spanId": "module-phase-000399",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.7751999999998134,
          "startMs": 1992.5785,
          "spanId": "module-phase-000495",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005999999998493877,
          "startMs": 2261.3814,
          "spanId": "module-phase-000633",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 9.829099999999926,
          "startMs": 3156.0183,
          "spanId": "module-phase-000725",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.0013000000003557943,
          "startMs": 3210.1339,
          "spanId": "module-phase-000760",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.WebApi::Monica.Modules.ModuleSwagger",
      "name": "ModuleSwagger",
      "fullTypeName": "Monica.Modules.ModuleSwagger",
      "assembly": "Monica.WebApi",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 220,
      "isUI": false,
      "isWeb": true,
      "costMs": 48.67289999999957,
      "serialCallbackMs": 48.67289999999957,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleResultEnvelope"
      ],
      "dependents": [
        "Monica.Framework.UI::Monica.Modules.ModuleSwaggerUI",
        "Monica.WebApi::Monica.Modules.ModuleWebApi"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.015600000000006276,
          "startMs": 325.3562,
          "spanId": "module-phase-000026",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 32.0985,
          "startMs": 362.4817,
          "spanId": "module-phase-000110",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0019999999999527063,
          "startMs": 465.4691,
          "spanId": "module-phase-000205",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004500000000007276,
          "startMs": 468.9482,
          "spanId": "module-phase-000300",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.005099999999856664,
          "startMs": 1981.8619,
          "spanId": "module-phase-000400",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 11.61590000000001,
          "startMs": 1993.3651,
          "spanId": "module-phase-000496",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0007000000000516593,
          "startMs": 2261.3853,
          "spanId": "module-phase-000634",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 4.929699999999684,
          "startMs": 3165.8625,
          "spanId": "module-phase-000726",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.0009000000000014552,
          "startMs": 3210.1398,
          "spanId": "module-phase-000761",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Core::Monica.Modules.ModuleExecutionPipeline",
      "name": "ModuleExecutionPipeline",
      "fullTypeName": "Monica.Modules.ModuleExecutionPipeline",
      "assembly": "Monica.Core",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 230,
      "isUI": false,
      "isWeb": false,
      "costMs": 2.4314999999999714,
      "serialCallbackMs": 2.4314999999999714,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.Authority::Monica.Modules.ModuleAuthorization",
        "Monica.Core::Monica.Modules.ModuleControllers",
        "Monica.Core::Monica.Modules.ModuleHostedService",
        "Monica.Core::Monica.Modules.ModuleMediator",
        "Monica.EventBus::Monica.Modules.ModuleEventBus",
        "Monica.Framework.UI::Monica.Modules.ModuleExecutionPipelineUI",
        "Monica.Framework::Monica.Modules.ModuleChainTracing",
        "Monica.Profiling::Monica.Modules.ModuleExecutionTiming",
        "Monica.Repository::Monica.Modules.ModuleRepository",
        "Monica.Repository::Monica.Modules.ModuleUnitOfWork"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0003000000000383807,
          "startMs": 325.9992,
          "spanId": "module-phase-000037",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 1.3531999999999584,
          "startMs": 394.5946,
          "spanId": "module-phase-000111",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0014999999999645297,
          "startMs": 465.4723,
          "spanId": "module-phase-000206",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.0049999999999954525,
          "startMs": 468.9535,
          "spanId": "module-phase-000301",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0019999999999527063,
          "startMs": 1981.8743,
          "spanId": "module-phase-000401",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.5153000000000247,
          "startMs": 2004.993,
          "spanId": "module-phase-000497",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.5542000000000371,
          "startMs": 2261.3879,
          "spanId": "module-phase-000635",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Repository::Monica.Modules.ModuleUnitOfWork",
      "name": "ModuleUnitOfWork",
      "fullTypeName": "Monica.Modules.ModuleUnitOfWork",
      "assembly": "Monica.Repository",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 240,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.3657999999998083,
      "serialCallbackMs": 0.3657999999998083,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleExecutionPipeline",
        "Monica.DependencyInjection::Monica.Modules.ModuleDependencyInjection"
      ],
      "dependents": [
        "Monica.Repository::Monica.Modules.ModuleRepository"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.09070000000002665,
          "startMs": 324.7808,
          "spanId": "module-phase-000015",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.04149999999998499,
          "startMs": 395.9543,
          "spanId": "module-phase-000112",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001599999999996271,
          "startMs": 465.4753,
          "spanId": "module-phase-000207",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.0056000000000153705,
          "startMs": 468.9595,
          "spanId": "module-phase-000302",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0019999999999527063,
          "startMs": 1981.8784,
          "spanId": "module-phase-000402",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.09300000000007458,
          "startMs": 2005.5208,
          "spanId": "module-phase-000498",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.13000000000010914,
          "startMs": 2005.6242,
          "spanId": "module-phase-000499",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0013999999996485712,
          "startMs": 2261.954,
          "spanId": "module-phase-000636",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.EventBus::Monica.Modules.ModuleEventBus",
      "name": "ModuleEventBus",
      "fullTypeName": "Monica.Modules.ModuleEventBus",
      "assembly": "Monica.EventBus",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 250,
      "isUI": false,
      "isWeb": false,
      "costMs": 1.5849000000002889,
      "serialCallbackMs": 1.5849000000002889,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleExecutionPipeline",
        "Monica.Core::Monica.Modules.ModuleJsonSerialization"
      ],
      "dependents": [
        "Monica.Configuration.EventBus::Monica.Configuration.EventBus.Modules.ModuleConfigurationEventBus",
        "Monica.Dapr::Monica.Modules.ModuleDaprEventBus",
        "Monica.DevOps::Monica.Modules.ModuleGit",
        "Monica.EventBus.Kafka::Monica.Modules.ModuleEventBusKafka",
        "Monica.Framework.UI::Monica.Modules.ModuleEventBusUI",
        "Monica.ProjectUnits::Monica.Modules.ModuleProjectUnits",
        "Monica.Repository::Monica.Modules.ModuleRepository"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.01959999999996853,
          "startMs": 325.3142,
          "spanId": "module-phase-000024",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.025500000000022283,
          "startMs": 395.9998,
          "spanId": "module-phase-000113",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013999999999896318,
          "startMs": 465.4821,
          "spanId": "module-phase-000208",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.13290000000000646,
          "startMs": 468.9661,
          "spanId": "module-phase-000303",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018999999999778083,
          "startMs": 1981.8834,
          "spanId": "module-phase-000403",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.19329999999990832,
          "startMs": 2005.7688,
          "spanId": "module-phase-000500",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.12920000000008258,
          "startMs": 2005.9664,
          "spanId": "module-phase-000501",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Declare type discovery / Type discovery commit",
          "durationMs": 1.0796000000000276,
          "startMs": 2164.2394,
          "spanId": "module-phase-000610",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Type discovery commit"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0015000000003055902,
          "startMs": 2261.959,
          "spanId": "module-phase-000637",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Configuration.EventBus::Monica.Configuration.EventBus.Modules.ModuleConfigurationEventBus",
      "name": "ModuleConfigurationEventBus",
      "fullTypeName": "Monica.Configuration.EventBus.Modules.ModuleConfigurationEventBus",
      "assembly": "Monica.Configuration.EventBus",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 260,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.3296999999996615,
      "serialCallbackMs": 0.3296999999996615,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Configuration::Monica.Modules.ModuleConfiguration",
        "Monica.EventBus::Monica.Modules.ModuleEventBus"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.026599999999973534,
          "startMs": 324.6073,
          "spanId": "module-phase-000006",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.02600000000001046,
          "startMs": 396.0286,
          "spanId": "module-phase-000114",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.19890000000003738,
          "startMs": 465.4856,
          "spanId": "module-phase-000209",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.006100000000003547,
          "startMs": 469.1022,
          "spanId": "module-phase-000304",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0019999999999527063,
          "startMs": 1981.8888,
          "spanId": "module-phase-000404",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.06949999999983447,
          "startMs": 2006.1111,
          "spanId": "module-phase-000502",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005999999998493877,
          "startMs": 2261.965,
          "spanId": "module-phase-000638",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Profiling::Monica.Modules.ModuleExecutionTiming",
      "name": "ModuleExecutionTiming",
      "fullTypeName": "Monica.Modules.ModuleExecutionTiming",
      "assembly": "Monica.Profiling",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 270,
      "isUI": false,
      "isWeb": true,
      "costMs": 0.41939999999993915,
      "serialCallbackMs": 0.41939999999993915,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleExecutionPipeline"
      ],
      "dependents": [
        "Monica.Profiling::Monica.Modules.ModuleExecutionTimingUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.12130000000001928,
          "startMs": 325.59,
          "spanId": "module-phase-000029",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.04040000000003374,
          "startMs": 396.0567,
          "spanId": "module-phase-000115",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0056000000000153705,
          "startMs": 465.6889,
          "spanId": "module-phase-000210",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.00529999999997699,
          "startMs": 469.1095,
          "spanId": "module-phase-000305",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0020999999999276042,
          "startMs": 1981.8941,
          "spanId": "module-phase-000405",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.20129999999994652,
          "startMs": 2006.189,
          "spanId": "module-phase-000503",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0007000000000516593,
          "startMs": 2261.9686,
          "spanId": "module-phase-000639",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.0072000000000116415,
          "startMs": 3170.8094,
          "spanId": "module-phase-000727",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.035499999999956344,
          "startMs": 3210.1447,
          "spanId": "module-phase-000762",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Core::Monica.Modules.ModuleControllers",
      "name": "ModuleControllers",
      "fullTypeName": "Monica.Modules.ModuleControllers",
      "assembly": "Monica.Core",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 280,
      "isUI": false,
      "isWeb": false,
      "costMs": 8.76740000000035,
      "serialCallbackMs": 8.76740000000035,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleExecutionPipeline"
      ],
      "dependents": [
        "Monica.Framework::Monica.Modules.ModuleChainTracing",
        "Monica.WebApi::Monica.Modules.ModuleAutoControllers"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.013500000000021828,
          "startMs": 325.9804,
          "spanId": "module-phase-000036",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.17690000000004602,
          "startMs": 396.102,
          "spanId": "module-phase-000116",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0021999999999593456,
          "startMs": 465.696,
          "spanId": "module-phase-000211",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005400000000008731,
          "startMs": 469.1158,
          "spanId": "module-phase-000306",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0032999999998537533,
          "startMs": 1981.8982,
          "spanId": "module-phase-000406",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.03330000000005384,
          "startMs": 2006.4021,
          "spanId": "module-phase-000504",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 8.532800000000407,
          "startMs": 2261.9718,
          "spanId": "module-phase-000640",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.WebApi::Monica.Modules.ModuleAutoControllers",
      "name": "ModuleAutoControllers",
      "fullTypeName": "Monica.Modules.ModuleAutoControllers",
      "assembly": "Monica.WebApi",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 290,
      "isUI": false,
      "isWeb": true,
      "costMs": 47.26949999999988,
      "serialCallbackMs": 47.26949999999988,
      "startupWorkMs": 0,
      "requires": [
        "Monica.AutoModel::Monica.Modules.ModuleAutoModel",
        "Monica.Core::Monica.Modules.ModuleControllers",
        "Monica.Core::Monica.Modules.ModuleExceptionHandling"
      ],
      "dependents": [
        "Monica.WebApi::Monica.Modules.ModuleWebApi"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.030700000000024374,
          "startMs": 325.7482,
          "spanId": "module-phase-000032",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 6.587499999999977,
          "startMs": 396.2841,
          "spanId": "module-phase-000117",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0017000000000280124,
          "startMs": 465.6998,
          "spanId": "module-phase-000212",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.1490000000000009,
          "startMs": 469.1223,
          "spanId": "module-phase-000307",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018999999999778083,
          "startMs": 1981.9032,
          "spanId": "module-phase-000407",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 2.634699999999839,
          "startMs": 2006.44,
          "spanId": "module-phase-000505",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery / Type discovery commit",
          "durationMs": 0.3786000000000058,
          "startMs": 2165.3251,
          "spanId": "module-phase-000611",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Type discovery commit"
        },
        {
          "name": "Post-configure services",
          "durationMs": 1.3366000000000895,
          "startMs": 2270.5203,
          "spanId": "module-phase-000641",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.0030999999999039574,
          "startMs": 3170.8218,
          "spanId": "module-phase-000728",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 36.14570000000003,
          "startMs": 3210.185,
          "spanId": "module-phase-000763",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.ProjectUnits::Monica.Modules.ModuleProjectUnits",
      "name": "ModuleProjectUnits",
      "fullTypeName": "Monica.Modules.ModuleProjectUnits",
      "assembly": "Monica.ProjectUnits",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 300,
      "isUI": false,
      "isWeb": true,
      "costMs": 52.43879999999979,
      "serialCallbackMs": 52.43879999999979,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleJsonSerialization",
        "Monica.EventBus::Monica.Modules.ModuleEventBus"
      ],
      "dependents": [
        "Monica.Framework.UI::Monica.Modules.ModuleProjectUnitsUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.07310000000001082,
          "startMs": 324.7048,
          "spanId": "module-phase-000014",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.39449999999999363,
          "startMs": 402.8807,
          "spanId": "module-phase-000118",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0014999999999645297,
          "startMs": 465.7037,
          "spanId": "module-phase-000213",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.045000000000015916,
          "startMs": 469.2749,
          "spanId": "module-phase-000308",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0024000000000796717,
          "startMs": 1981.9089,
          "spanId": "module-phase-000408",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.21849999999994907,
          "startMs": 2009.0888,
          "spanId": "module-phase-000506",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery / Type discovery commit",
          "durationMs": 0.4675999999999476,
          "startMs": 2165.7102,
          "spanId": "module-phase-000612",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Type discovery commit"
        },
        {
          "name": "Post-configure services",
          "durationMs": 51.09990000000016,
          "startMs": 2271.8698,
          "spanId": "module-phase-000642",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.05349999999998545,
          "startMs": 3170.829,
          "spanId": "module-phase-000729",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.08279999999967913,
          "startMs": 3246.3501,
          "spanId": "module-phase-000764",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Framework::Monica.Modules.ModuleChainTracing",
      "name": "ModuleChainTracing",
      "fullTypeName": "Monica.Modules.ModuleChainTracing",
      "assembly": "Monica.Framework",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 310,
      "isUI": false,
      "isWeb": true,
      "costMs": 0.21120000000041728,
      "serialCallbackMs": 0.21120000000041728,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleControllers",
        "Monica.Core::Monica.Modules.ModuleExceptionHandling",
        "Monica.Core::Monica.Modules.ModuleExecutionPipeline",
        "Monica.Core::Monica.Modules.ModuleJsonSerialization"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.019399999999961892,
          "startMs": 325.9594,
          "spanId": "module-phase-000035",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.07040000000000646,
          "startMs": 403.2821,
          "spanId": "module-phase-000119",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0017000000000280124,
          "startMs": 465.7075,
          "spanId": "module-phase-000214",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005300000000033833,
          "startMs": 469.3225,
          "spanId": "module-phase-000309",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018999999999778083,
          "startMs": 1981.9147,
          "spanId": "module-phase-000409",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.03690000000005966,
          "startMs": 2009.3169,
          "spanId": "module-phase-000507",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.03999999999996362,
          "startMs": 2009.3559,
          "spanId": "module-phase-000508",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.02980000000002292,
          "startMs": 2009.3976,
          "spanId": "module-phase-000509",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.001900000000205182,
          "startMs": 2322.9957,
          "spanId": "module-phase-000643",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.0028999999999541615,
          "startMs": 3170.8876,
          "spanId": "module-phase-000730",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.0010000000002037268,
          "startMs": 3246.442,
          "spanId": "module-phase-000765",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Locker::Monica.Modules.ModuleLocker",
      "name": "ModuleLocker",
      "fullTypeName": "Monica.Modules.ModuleLocker",
      "assembly": "Monica.Locker",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 320,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.1750000000001819,
      "serialCallbackMs": 0.1750000000001819,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.Dapr::Monica.Modules.ModuleDaprLocker"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.015399999999999636,
          "startMs": 326.0016,
          "spanId": "module-phase-000038",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.043299999999987904,
          "startMs": 403.3562,
          "spanId": "module-phase-000120",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001599999999996271,
          "startMs": 465.7111,
          "spanId": "module-phase-000215",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004900000000020555,
          "startMs": 469.3357,
          "spanId": "module-phase-000310",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018000000000029104,
          "startMs": 1981.9196,
          "spanId": "module-phase-000410",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.06650000000013279,
          "startMs": 2009.4322,
          "spanId": "module-phase-000510",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.04099999999993997,
          "startMs": 2009.5006,
          "spanId": "module-phase-000511",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005000000001018634,
          "startMs": 2323.0024,
          "spanId": "module-phase-000644",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Dapr::Monica.Modules.ModuleDaprLocker",
      "name": "ModuleDaprLocker",
      "fullTypeName": "Monica.Modules.ModuleDaprLocker",
      "assembly": "Monica.Dapr",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 330,
      "isUI": false,
      "isWeb": false,
      "costMs": 2.289300000000196,
      "serialCallbackMs": 2.289300000000196,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Locker::Monica.Modules.ModuleLocker"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.01630000000000109,
          "startMs": 326.0189,
          "spanId": "module-phase-000039",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.2189999999999941,
          "startMs": 403.4026,
          "spanId": "module-phase-000121",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0012999999999578904,
          "startMs": 465.7146,
          "spanId": "module-phase-000216",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.006599999999991724,
          "startMs": 469.3421,
          "spanId": "module-phase-000311",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0019999999999527063,
          "startMs": 1981.9248,
          "spanId": "module-phase-000411",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 2.0434999999999945,
          "startMs": 2009.5472,
          "spanId": "module-phase-000512",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.000600000000304135,
          "startMs": 2323.0062,
          "spanId": "module-phase-000645",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Authority::Monica.Modules.ModuleCors",
      "name": "ModuleCors",
      "fullTypeName": "Monica.Modules.ModuleCors",
      "assembly": "Monica.Authority",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 340,
      "isUI": false,
      "isWeb": true,
      "costMs": 0.28029999999967004,
      "serialCallbackMs": 0.28029999999967004,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0002000000000066393,
          "startMs": 326.0374,
          "spanId": "module-phase-000040",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.047100000000000364,
          "startMs": 403.6299,
          "spanId": "module-phase-000122",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013000000000147338,
          "startMs": 465.7179,
          "spanId": "module-phase-000217",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004799999999988813,
          "startMs": 469.3501,
          "spanId": "module-phase-000312",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0015000000000782165,
          "startMs": 1981.9314,
          "spanId": "module-phase-000412",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.0024000000000796717,
          "startMs": 2011.6036,
          "spanId": "module-phase-000513",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.1570000000001528,
          "startMs": 2011.6072,
          "spanId": "module-phase-000514",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0018999999997504347,
          "startMs": 2323.01,
          "spanId": "module-phase-000646",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.06289999999989959,
          "startMs": 3171.3045,
          "spanId": "module-phase-000748",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.0011999999996987754,
          "startMs": 3246.4479,
          "spanId": "module-phase-000766",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Core::Monica.Modules.ModuleLocalization",
      "name": "ModuleLocalization",
      "fullTypeName": "Monica.Modules.ModuleLocalization",
      "assembly": "Monica.Core",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 350,
      "isUI": false,
      "isWeb": true,
      "costMs": 4.102999999999213,
      "serialCallbackMs": 4.102999999999213,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.AI.UI::Monica.Modules.ModuleAIUI",
        "Monica.AI.UI::Monica.Modules.ModuleKnowledgeBaseUI",
        "Monica.AI.UI::Monica.Modules.ModuleRAGUI",
        "Monica.Authority::Monica.Modules.ModuleAuthentication",
        "Monica.Authority::Monica.Modules.ModuleAuthorization",
        "Monica.Configuration.UI::Monica.Modules.ModuleConfigurationUI",
        "Monica.DevOps::Monica.Modules.ModuleFileOps",
        "Monica.DevOps::Monica.Modules.ModuleFileOpsUI",
        "Monica.DevOps::Monica.Modules.ModuleGitUI",
        "Monica.DevOps::Monica.Modules.ModuleK8S",
        "Monica.DevOps::Monica.Modules.ModuleK8SUI",
        "Monica.DevOps::Monica.Modules.ModuleTerminalUI",
        "Monica.EventBus.Kafka::Monica.Modules.ModuleEventBusKafka",
        "Monica.EventBus.Kafka::Monica.Modules.ModuleEventBusKafkaUI",
        "Monica.Framework.UI::Monica.Modules.ModuleDependencyInjectionUI",
        "Monica.Framework.UI::Monica.Modules.ModuleEventBusUI",
        "Monica.Framework.UI::Monica.Modules.ModuleExecutionPipelineUI",
        "Monica.Framework.UI::Monica.Modules.ModuleLoggingUI",
        "Monica.Framework.UI::Monica.Modules.ModuleMapperUI",
        "Monica.Framework.UI::Monica.Modules.ModuleObservableInstanceUI",
        "Monica.Framework.UI::Monica.Modules.ModuleProjectUnitsUI",
        "Monica.Framework.UI::Monica.Modules.ModuleSeederUI",
        "Monica.HealthCheck.UI::Monica.Modules.ModuleHealthCheckUI",
        "Monica.JobScheduler.UI::Monica.Modules.ModuleJobSchedulerUI",
        "Monica.Markdown::Monica.Modules.ModuleMarkdown",
        "Monica.Markdown::Monica.Modules.ModuleMarkdownUI",
        "Monica.Profiling::Monica.Modules.ModuleExecutionTimingUI",
        "Monica.Repository.UI::Monica.Modules.ModuleRepositoryUI",
        "Monica.ServiceDiscovery::Monica.Modules.ModuleServiceDiscovery",
        "Monica.ServiceDiscovery::Monica.Modules.ModuleServiceDiscoveryUI",
        "Monica.StateStore.UI::Monica.Modules.ModuleStateStoreUI",
        "Monica.UI::Monica.Modules.ModuleDiffHighlightUI",
        "Monica.UI::Monica.Modules.ModuleShellUI",
        "Monica.UI::Monica.Modules.ModuleSystemInfoUI",
        "Monica.UI::Monica.Modules.ModuleSystemUI",
        "Monica.Utilities::Monica.Modules.ModuleUtilities",
        "Monica.Utilities::Monica.Modules.ModuleUtilitiesUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.00029999999998153726,
          "startMs": 326.2124,
          "spanId": "module-phase-000046",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 2.126699999999971,
          "startMs": 403.6804,
          "spanId": "module-phase-000123",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013999999999896318,
          "startMs": 465.7233,
          "spanId": "module-phase-000218",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.04050000000000864,
          "startMs": 469.356,
          "spanId": "module-phase-000313",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0019999999999527063,
          "startMs": 1981.9355,
          "spanId": "module-phase-000413",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.7576999999998861,
          "startMs": 2011.7714,
          "spanId": "module-phase-000515",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery / Type discovery commit",
          "durationMs": 0.12479999999959546,
          "startMs": 2166.184,
          "spanId": "module-phase-000613",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Type discovery commit"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.9744000000000597,
          "startMs": 2323.0151,
          "spanId": "module-phase-000647",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.07150000000001455,
          "startMs": 3170.9028,
          "spanId": "module-phase-000731",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.003699999999753345,
          "startMs": 3246.4524,
          "spanId": "module-phase-000767",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Authority::Monica.Modules.ModuleAuthentication",
      "name": "ModuleAuthentication",
      "fullTypeName": "Monica.Modules.ModuleAuthentication",
      "assembly": "Monica.Authority",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 360,
      "isUI": false,
      "isWeb": true,
      "costMs": 23.048099999999977,
      "serialCallbackMs": 23.048099999999977,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization"
      ],
      "dependents": [
        "Monica.Authority::Monica.Modules.ModuleAuthorization",
        "Monica.WebApi::Monica.Modules.ModuleRpcClient",
        "Monica.WebApi::Monica.Modules.ModuleWebApi"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.08949999999998681,
          "startMs": 325.3747,
          "spanId": "module-phase-000027",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.24649999999996908,
          "startMs": 405.8136,
          "spanId": "module-phase-000124",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.004099999999993997,
          "startMs": 465.7256,
          "spanId": "module-phase-000219",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.008999999999957708,
          "startMs": 469.398,
          "spanId": "module-phase-000314",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0015000000000782165,
          "startMs": 1981.9396,
          "spanId": "module-phase-000414",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 22.549400000000105,
          "startMs": 2012.54,
          "spanId": "module-phase-000516",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.08019999999987704,
          "startMs": 2035.0993,
          "spanId": "module-phase-000517",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0028999999999541615,
          "startMs": 2324.0173,
          "spanId": "module-phase-000648",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.06419999999980064,
          "startMs": 3171.3737,
          "spanId": "module-phase-000749",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.0008000000002539309,
          "startMs": 3246.459,
          "spanId": "module-phase-000768",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Authority::Monica.Modules.ModuleAuthorization",
      "name": "ModuleAuthorization",
      "fullTypeName": "Monica.Modules.ModuleAuthorization",
      "assembly": "Monica.Authority",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 370,
      "isUI": false,
      "isWeb": true,
      "costMs": 1.1530999999997107,
      "serialCallbackMs": 1.1530999999997107,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Authority::Monica.Modules.ModuleAuthentication",
        "Monica.Core::Monica.Modules.ModuleExceptionHandling",
        "Monica.Core::Monica.Modules.ModuleExecutionPipeline",
        "Monica.Core::Monica.Modules.ModuleLocalization"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.1180999999999699,
          "startMs": 325.4673,
          "spanId": "module-phase-000028",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.034199999999998454,
          "startMs": 406.0643,
          "spanId": "module-phase-000125",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0012999999999578904,
          "startMs": 465.7311,
          "spanId": "module-phase-000220",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.0049999999999954525,
          "startMs": 469.408,
          "spanId": "module-phase-000315",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018999999999778083,
          "startMs": 1981.9432,
          "spanId": "module-phase-000415",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.4782999999999902,
          "startMs": 2035.1941,
          "spanId": "module-phase-000518",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.17639999999983047,
          "startMs": 2035.6789,
          "spanId": "module-phase-000519",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.045900000000074215,
          "startMs": 2035.8596,
          "spanId": "module-phase-000520",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.10969999999997526,
          "startMs": 2035.9079,
          "spanId": "module-phase-000521",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.10950000000002547,
          "startMs": 2036.0214,
          "spanId": "module-phase-000522",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0007000000000516593,
          "startMs": 2324.0232,
          "spanId": "module-phase-000649",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.07139999999981228,
          "startMs": 3171.443,
          "spanId": "module-phase-000750",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.0007000000000516593,
          "startMs": 3246.4625,
          "spanId": "module-phase-000769",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.WebApi::Monica.Modules.ModuleRpcClient",
      "name": "ModuleRpcClient",
      "fullTypeName": "Monica.Modules.ModuleRpcClient",
      "assembly": "Monica.WebApi",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 380,
      "isUI": false,
      "isWeb": false,
      "costMs": 5.499500000000069,
      "serialCallbackMs": 5.499500000000069,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Authority::Monica.Modules.ModuleAuthentication",
        "Monica.Core::Monica.Modules.ModuleResultEnvelope",
        "Monica.DependencyInjection::Monica.Modules.ModuleDependencyInjection"
      ],
      "dependents": [
        "Monica.Dapr::Monica.Modules.ModuleDaprRpcClient"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.024099999999975807,
          "startMs": 326.0391,
          "spanId": "module-phase-000041",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.32159999999998945,
          "startMs": 406.1008,
          "spanId": "module-phase-000126",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0019000000000346517,
          "startMs": 465.7335,
          "spanId": "module-phase-000221",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.1449000000000069,
          "startMs": 469.4139,
          "spanId": "module-phase-000316",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.003200000000106229,
          "startMs": 1981.9495,
          "spanId": "module-phase-000416",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.2395000000001346,
          "startMs": 2036.1407,
          "spanId": "module-phase-000523",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery / Type discovery commit",
          "durationMs": 0.04489999999987049,
          "startMs": 2166.313,
          "spanId": "module-phase-000614",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Type discovery commit"
        },
        {
          "name": "Post-configure services",
          "durationMs": 4.7193999999999505,
          "startMs": 2324.0285,
          "spanId": "module-phase-000650",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Markdown::Monica.Modules.ModuleMarkdown",
      "name": "ModuleMarkdown",
      "fullTypeName": "Monica.Modules.ModuleMarkdown",
      "assembly": "Monica.Markdown",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 390,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.34479999999939537,
      "serialCallbackMs": 0.34479999999939537,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization"
      ],
      "dependents": [
        "Monica.AI::Monica.Modules.ModuleKnowledgeBase",
        "Monica.AI::Monica.Modules.ModuleRAG",
        "Monica.Markdown::Monica.Modules.ModuleMarkdownUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.016799999999989268,
          "startMs": 326.1566,
          "spanId": "module-phase-000043",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.2588000000000079,
          "startMs": 406.428,
          "spanId": "module-phase-000127",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0020000000000095497,
          "startMs": 465.7374,
          "spanId": "module-phase-000222",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.00580000000002201,
          "startMs": 469.5628,
          "spanId": "module-phase-000317",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002199999999902502,
          "startMs": 1981.9608,
          "spanId": "module-phase-000417",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.05759999999986576,
          "startMs": 2036.3902,
          "spanId": "module-phase-000524",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0015999999995983671,
          "startMs": 2328.7609,
          "spanId": "module-phase-000651",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.DevOps::Monica.Modules.ModuleK8S",
      "name": "ModuleK8S",
      "fullTypeName": "Monica.Modules.ModuleK8S",
      "assembly": "Monica.DevOps",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 400,
      "isUI": false,
      "isWeb": true,
      "costMs": 1.129299999999546,
      "serialCallbackMs": 1.129299999999546,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization"
      ],
      "dependents": [
        "Monica.DevOps::Monica.Modules.ModuleK8SUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.012999999999976808,
          "startMs": 326.1979,
          "spanId": "module-phase-000045",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.9927999999999884,
          "startMs": 406.6969,
          "spanId": "module-phase-000128",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0018000000000029104,
          "startMs": 465.7413,
          "spanId": "module-phase-000223",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.0050999999999703505,
          "startMs": 469.5708,
          "spanId": "module-phase-000318",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0041999999998552084,
          "startMs": 1981.967,
          "spanId": "module-phase-000418",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.050700000000006185,
          "startMs": 2036.4531,
          "spanId": "module-phase-000525",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.001099999999951251,
          "startMs": 2328.7675,
          "spanId": "module-phase-000652",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.003699999999753345,
          "startMs": 3170.9836,
          "spanId": "module-phase-000732",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.05690000000004147,
          "startMs": 3246.4677,
          "spanId": "module-phase-000770",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.DevOps::Monica.Modules.ModuleFileOps",
      "name": "ModuleFileOps",
      "fullTypeName": "Monica.Modules.ModuleFileOps",
      "assembly": "Monica.DevOps",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 410,
      "isUI": false,
      "isWeb": true,
      "costMs": 0.29030000000005884,
      "serialCallbackMs": 0.29030000000005884,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization"
      ],
      "dependents": [
        "Monica.DevOps::Monica.Modules.ModuleFileOpsUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.015300000000024738,
          "startMs": 326.2138,
          "spanId": "module-phase-000047",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.18200000000001637,
          "startMs": 407.6961,
          "spanId": "module-phase-000129",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001599999999996271,
          "startMs": 465.7441,
          "spanId": "module-phase-000224",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005300000000033833,
          "startMs": 469.5805,
          "spanId": "module-phase-000319",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0026000000000294676,
          "startMs": 1981.9754,
          "spanId": "module-phase-000419",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.04369999999994434,
          "startMs": 2036.507,
          "spanId": "module-phase-000526",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.000600000000304135,
          "startMs": 2328.7706,
          "spanId": "module-phase-000653",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.0019999999999527063,
          "startMs": 3170.9902,
          "spanId": "module-phase-000733",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.03719999999975698,
          "startMs": 3246.5288,
          "spanId": "module-phase-000771",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Utilities::Monica.Modules.ModuleUtilities",
      "name": "ModuleUtilities",
      "fullTypeName": "Monica.Modules.ModuleUtilities",
      "assembly": "Monica.Utilities",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 420,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.18730000000010705,
      "serialCallbackMs": 0.18730000000010705,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization"
      ],
      "dependents": [
        "Monica.Utilities::Monica.Modules.ModuleUtilitiesUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.09949999999997772,
          "startMs": 326.2312,
          "spanId": "module-phase-000048",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.043400000000019645,
          "startMs": 407.8864,
          "spanId": "module-phase-000130",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0012999999999578904,
          "startMs": 465.7479,
          "spanId": "module-phase-000225",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.006400000000041928,
          "startMs": 469.5875,
          "spanId": "module-phase-000320",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0017000000000280124,
          "startMs": 1981.9811,
          "spanId": "module-phase-000420",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.03449999999997999,
          "startMs": 2036.5544,
          "spanId": "module-phase-000527",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005000000001018634,
          "startMs": 2328.7743,
          "spanId": "module-phase-000654",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.UI::Monica.Modules.ModuleShellUI",
      "name": "ModuleShellUI",
      "fullTypeName": "Monica.Modules.ModuleShellUI",
      "assembly": "Monica.UI",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 430,
      "isUI": true,
      "isWeb": true,
      "costMs": 71.15869999999978,
      "serialCallbackMs": 71.15869999999978,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization"
      ],
      "dependents": [
        "Monica.AI.UI::Monica.Modules.ModuleAIUI",
        "Monica.AI.UI::Monica.Modules.ModuleKnowledgeBaseUI",
        "Monica.AI.UI::Monica.Modules.ModuleRAGUI",
        "Monica.Configuration.UI::Monica.Modules.ModuleConfigurationUI",
        "Monica.DevOps::Monica.Modules.ModuleFileOpsUI",
        "Monica.DevOps::Monica.Modules.ModuleGitUI",
        "Monica.DevOps::Monica.Modules.ModuleK8SUI",
        "Monica.DevOps::Monica.Modules.ModuleTerminalUI",
        "Monica.EventBus.Kafka::Monica.Modules.ModuleEventBusKafkaUI",
        "Monica.Framework.UI::Monica.Modules.ModuleDependencyInjectionUI",
        "Monica.Framework.UI::Monica.Modules.ModuleEventBusUI",
        "Monica.Framework.UI::Monica.Modules.ModuleExecutionPipelineUI",
        "Monica.Framework.UI::Monica.Modules.ModuleLoggingUI",
        "Monica.Framework.UI::Monica.Modules.ModuleMapperUI",
        "Monica.Framework.UI::Monica.Modules.ModuleObservableInstanceUI",
        "Monica.Framework.UI::Monica.Modules.ModuleProjectUnitsUI",
        "Monica.Framework.UI::Monica.Modules.ModuleSeederUI",
        "Monica.HealthCheck.UI::Monica.Modules.ModuleHealthCheckUI",
        "Monica.JobScheduler.UI::Monica.Modules.ModuleJobSchedulerUI",
        "Monica.Markdown::Monica.Modules.ModuleMarkdownUI",
        "Monica.Profiling::Monica.Modules.ModuleExecutionTimingUI",
        "Monica.Repository.UI::Monica.Modules.ModuleRepositoryUI",
        "Monica.ServiceDiscovery::Monica.Modules.ModuleServiceDiscoveryUI",
        "Monica.StateStore.UI::Monica.Modules.ModuleStateStoreUI",
        "Monica.UI::Monica.Modules.ModuleDiffHighlightUI",
        "Monica.UI::Monica.Modules.ModuleSystemInfoUI",
        "Monica.UI::Monica.Modules.ModuleSystemUI",
        "Monica.Utilities::Monica.Modules.ModuleUtilitiesUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.06869999999997844,
          "startMs": 327.6155,
          "spanId": "module-phase-000056",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 1.4981000000000222,
          "startMs": 407.9333,
          "spanId": "module-phase-000131",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001699999999971169,
          "startMs": 465.7511,
          "spanId": "module-phase-000226",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005200000000002092,
          "startMs": 469.5956,
          "spanId": "module-phase-000321",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 3.6983999999999924,
          "startMs": 1981.9864,
          "spanId": "module-phase-000421",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 29.848099999999704,
          "startMs": 2036.5929,
          "spanId": "module-phase-000528",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005999999998493877,
          "startMs": 2328.7779,
          "spanId": "module-phase-000655",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 33.728200000000015,
          "startMs": 3171.5245,
          "spanId": "module-phase-000751",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 2.309700000000248,
          "startMs": 3246.5739,
          "spanId": "module-phase-000772",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.UI::Monica.Modules.ModuleSystemInfoUI",
      "name": "ModuleSystemInfoUI",
      "fullTypeName": "Monica.Modules.ModuleSystemInfoUI",
      "assembly": "Monica.UI",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 440,
      "isUI": true,
      "isWeb": true,
      "costMs": 0.54039999999992,
      "serialCallbackMs": 0.54039999999992,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.20300000000003138,
          "startMs": 326.806,
          "spanId": "module-phase-000051",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.18819999999999482,
          "startMs": 409.4382,
          "spanId": "module-phase-000132",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0012999999999578904,
          "startMs": 465.754,
          "spanId": "module-phase-000227",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005400000000008731,
          "startMs": 469.6019,
          "spanId": "module-phase-000322",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.010600000000067666,
          "startMs": 1985.7028,
          "spanId": "module-phase-000422",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.07339999999976499,
          "startMs": 2066.463,
          "spanId": "module-phase-000529",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005999999998493877,
          "startMs": 2328.7804,
          "spanId": "module-phase-000656",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.0024000000003070454,
          "startMs": 3170.9979,
          "spanId": "module-phase-000734",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.055499999999938154,
          "startMs": 3248.906,
          "spanId": "module-phase-000773",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Profiling::Monica.Modules.ModuleExecutionTimingUI",
      "name": "ModuleExecutionTimingUI",
      "fullTypeName": "Monica.Modules.ModuleExecutionTimingUI",
      "assembly": "Monica.Profiling",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 450,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.20279999999928577,
      "serialCallbackMs": 0.20279999999928577,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.Profiling::Monica.Modules.ModuleExecutionTiming",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.12550000000004502,
          "startMs": 327.0198,
          "spanId": "module-phase-000052",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.028400000000033288,
          "startMs": 409.6314,
          "spanId": "module-phase-000133",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0015000000000213731,
          "startMs": 465.7593,
          "spanId": "module-phase-000228",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004300000000000637,
          "startMs": 469.6085,
          "spanId": "module-phase-000323",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002499999999827196,
          "startMs": 1985.7197,
          "spanId": "module-phase-000423",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.040099999999711144,
          "startMs": 2066.5448,
          "spanId": "module-phase-000530",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0004999999996471161,
          "startMs": 2328.7847,
          "spanId": "module-phase-000657",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Framework.UI::Monica.Modules.ModuleExecutionPipelineUI",
      "name": "ModuleExecutionPipelineUI",
      "fullTypeName": "Monica.Modules.ModuleExecutionPipelineUI",
      "assembly": "Monica.Framework.UI",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 460,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.16269999999985885,
      "serialCallbackMs": 0.16269999999985885,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleExecutionPipeline",
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.09869999999995116,
          "startMs": 327.4134,
          "spanId": "module-phase-000054",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.020899999999983265,
          "startMs": 409.6623,
          "spanId": "module-phase-000134",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0027000000000043656,
          "startMs": 465.7729,
          "spanId": "module-phase-000229",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004700000000013915,
          "startMs": 469.6142,
          "spanId": "module-phase-000324",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002100000000154978,
          "startMs": 1985.7269,
          "spanId": "module-phase-000424",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.03330000000005384,
          "startMs": 2066.5899,
          "spanId": "module-phase-000531",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.00029999999969732016,
          "startMs": 2328.7883,
          "spanId": "module-phase-000658",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Framework.UI::Monica.Modules.ModuleProjectUnitsUI",
      "name": "ModuleProjectUnitsUI",
      "fullTypeName": "Monica.Modules.ModuleProjectUnitsUI",
      "assembly": "Monica.Framework.UI",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 470,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.15260000000046148,
      "serialCallbackMs": 0.15260000000046148,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.ProjectUnits::Monica.Modules.ModuleProjectUnits",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.09379999999998745,
          "startMs": 327.5163,
          "spanId": "module-phase-000055",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.017499999999984084,
          "startMs": 409.6846,
          "spanId": "module-phase-000135",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0015000000000213731,
          "startMs": 465.7772,
          "spanId": "module-phase-000230",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005400000000008731,
          "startMs": 469.6199,
          "spanId": "module-phase-000325",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002199999999902502,
          "startMs": 1985.7309,
          "spanId": "module-phase-000425",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.031800000000203,
          "startMs": 2066.6277,
          "spanId": "module-phase-000532",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.00040000000035433914,
          "startMs": 2328.7904,
          "spanId": "module-phase-000659",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Framework.UI::Monica.Modules.ModuleMapperUI",
      "name": "ModuleMapperUI",
      "fullTypeName": "Monica.Modules.ModuleMapperUI",
      "assembly": "Monica.Framework.UI",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 480,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.11349999999993088,
      "serialCallbackMs": 0.11349999999993088,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.Core::Monica.Modules.ModuleObjectMapping",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.08629999999999427,
          "startMs": 327.6872,
          "spanId": "module-phase-000057",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.016899999999964166,
          "startMs": 409.7035,
          "spanId": "module-phase-000136",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0011999999999829924,
          "startMs": 465.7796,
          "spanId": "module-phase-000231",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004599999999982174,
          "startMs": 469.6263,
          "spanId": "module-phase-000326",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018000000000029104,
          "startMs": 1985.735,
          "spanId": "module-phase-000426",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.002300000000104774,
          "startMs": 2066.6641,
          "spanId": "module-phase-000533",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0003999999998995918,
          "startMs": 2328.7927,
          "spanId": "module-phase-000660",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.HealthCheck.UI::Monica.Modules.ModuleHealthCheckUI",
      "name": "ModuleHealthCheckUI",
      "fullTypeName": "Monica.Modules.ModuleHealthCheckUI",
      "assembly": "Monica.HealthCheck.UI",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 490,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.20759999999972933,
      "serialCallbackMs": 0.20759999999972933,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.HealthCheck::Monica.Modules.ModuleHealthCheck",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.10939999999999372,
          "startMs": 328.0578,
          "spanId": "module-phase-000060",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.019299999999986994,
          "startMs": 409.7223,
          "spanId": "module-phase-000137",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0015000000000213731,
          "startMs": 465.7829,
          "spanId": "module-phase-000232",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.007499999999993179,
          "startMs": 469.6325,
          "spanId": "module-phase-000327",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.003599999999778447,
          "startMs": 1985.7405,
          "spanId": "module-phase-000427",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.06589999999960128,
          "startMs": 2066.6703,
          "spanId": "module-phase-000534",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.00040000000035433914,
          "startMs": 2328.796,
          "spanId": "module-phase-000661",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Framework.UI::Monica.Modules.ModuleLoggingUI",
      "name": "ModuleLoggingUI",
      "fullTypeName": "Monica.Modules.ModuleLoggingUI",
      "assembly": "Monica.Framework.UI",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 500,
      "isUI": true,
      "isWeb": true,
      "costMs": 2.491799999999955,
      "serialCallbackMs": 2.491799999999955,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.Core::Monica.Modules.ModuleResultEnvelope",
        "Monica.Logging::Monica.Modules.ModuleLogging",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.10199999999997544,
          "startMs": 328.2687,
          "spanId": "module-phase-000062",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 2.0301000000000045,
          "startMs": 409.7429,
          "spanId": "module-phase-000138",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001599999999996271,
          "startMs": 465.7854,
          "spanId": "module-phase-000233",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004700000000013915,
          "startMs": 469.6408,
          "spanId": "module-phase-000328",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002199999999902502,
          "startMs": 1985.7464,
          "spanId": "module-phase-000428",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.03890000000001237,
          "startMs": 2066.7396,
          "spanId": "module-phase-000535",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0007000000000516593,
          "startMs": 2328.7989,
          "spanId": "module-phase-000662",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.002399999999852298,
          "startMs": 3171.0055,
          "spanId": "module-phase-000735",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.30920000000014625,
          "startMs": 3248.9759,
          "spanId": "module-phase-000774",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Framework.UI::Monica.Modules.ModuleEventBusUI",
      "name": "ModuleEventBusUI",
      "fullTypeName": "Monica.Modules.ModuleEventBusUI",
      "assembly": "Monica.Framework.UI",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 510,
      "isUI": true,
      "isWeb": true,
      "costMs": 2.654799999999682,
      "serialCallbackMs": 2.654799999999682,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleJsonSerialization",
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.EventBus::Monica.Modules.ModuleEventBus",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.3120000000000118,
          "startMs": 328.5408,
          "spanId": "module-phase-000065",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.04860000000002174,
          "startMs": 411.7801,
          "spanId": "module-phase-000139",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013999999999896318,
          "startMs": 465.7883,
          "spanId": "module-phase-000234",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005200000000002092,
          "startMs": 469.6483,
          "spanId": "module-phase-000329",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0019999999999527063,
          "startMs": 1985.7508,
          "spanId": "module-phase-000429",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.03429999999980282,
          "startMs": 2066.7815,
          "spanId": "module-phase-000536",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0007999999997991836,
          "startMs": 2328.8013,
          "spanId": "module-phase-000663",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.0016000000000531145,
          "startMs": 3171.0104,
          "spanId": "module-phase-000736",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 2.2489000000000487,
          "startMs": 3249.2994,
          "spanId": "module-phase-000775",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.EventBus.Kafka::Monica.Modules.ModuleEventBusKafka",
      "name": "ModuleEventBusKafka",
      "fullTypeName": "Monica.Modules.ModuleEventBusKafka",
      "assembly": "Monica.EventBus.Kafka",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 520,
      "isUI": false,
      "isWeb": true,
      "costMs": 1.4589999999994916,
      "serialCallbackMs": 1.4589999999994916,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleJsonSerialization",
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.EventBus::Monica.Modules.ModuleEventBus"
      ],
      "dependents": [
        "Monica.EventBus.Kafka::Monica.Modules.ModuleEventBusKafkaUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.028399999999976444,
          "startMs": 328.8603,
          "spanId": "module-phase-000066",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.2697000000000003,
          "startMs": 411.8324,
          "spanId": "module-phase-000140",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0019999999999527063,
          "startMs": 465.7918,
          "spanId": "module-phase-000235",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.0049999999999954525,
          "startMs": 469.6552,
          "spanId": "module-phase-000330",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0026000000000294676,
          "startMs": 1985.7565,
          "spanId": "module-phase-000430",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.15779999999995198,
          "startMs": 2066.8199,
          "spanId": "module-phase-000537",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005999999998493877,
          "startMs": 2328.8051,
          "spanId": "module-phase-000664",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.0018000000000029104,
          "startMs": 3171.016,
          "spanId": "module-phase-000737",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.991099999999733,
          "startMs": 3251.5646,
          "spanId": "module-phase-000776",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.EventBus.Kafka::Monica.Modules.ModuleEventBusKafkaUI",
      "name": "ModuleEventBusKafkaUI",
      "fullTypeName": "Monica.Modules.ModuleEventBusKafkaUI",
      "assembly": "Monica.EventBus.Kafka",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 530,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.1942000000005919,
      "serialCallbackMs": 0.1942000000005919,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.EventBus.Kafka::Monica.Modules.ModuleEventBusKafka",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.11440000000004602,
          "startMs": 328.8914,
          "spanId": "module-phase-000067",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.03750000000002274,
          "startMs": 412.1065,
          "spanId": "module-phase-000141",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001599999999996271,
          "startMs": 465.7948,
          "spanId": "module-phase-000236",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004600000000039017,
          "startMs": 469.6611,
          "spanId": "module-phase-000331",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002200000000129876,
          "startMs": 1985.7612,
          "spanId": "module-phase-000431",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.033400000000256114,
          "startMs": 2066.9815,
          "spanId": "module-phase-000538",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005000000001018634,
          "startMs": 2328.8076,
          "spanId": "module-phase-000665",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Markdown::Monica.Modules.ModuleMarkdownUI",
      "name": "ModuleMarkdownUI",
      "fullTypeName": "Monica.Modules.ModuleMarkdownUI",
      "assembly": "Monica.Markdown",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 540,
      "isUI": true,
      "isWeb": true,
      "costMs": 0.3121999999999048,
      "serialCallbackMs": 0.3121999999999048,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.Markdown::Monica.Modules.ModuleMarkdown",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.09459999999995716,
          "startMs": 329.1292,
          "spanId": "module-phase-000069",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.05329999999997881,
          "startMs": 412.1469,
          "spanId": "module-phase-000142",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013999999999896318,
          "startMs": 465.7975,
          "spanId": "module-phase-000237",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004399999999975535,
          "startMs": 469.6666,
          "spanId": "module-phase-000332",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0019999999999527063,
          "startMs": 1985.7686,
          "spanId": "module-phase-000432",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.048699999999826105,
          "startMs": 2067.0189,
          "spanId": "module-phase-000539",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005000000001018634,
          "startMs": 2328.8105,
          "spanId": "module-phase-000666",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.0018000000000029104,
          "startMs": 3171.0217,
          "spanId": "module-phase-000738",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.10550000000012005,
          "startMs": 3252.571,
          "spanId": "module-phase-000777",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Framework.UI::Monica.Modules.ModuleSwaggerUI",
      "name": "ModuleSwaggerUI",
      "fullTypeName": "Monica.Modules.ModuleSwaggerUI",
      "assembly": "Monica.Framework.UI",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 550,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.5142000000001303,
      "serialCallbackMs": 0.5142000000001303,
      "startupWorkMs": 0,
      "requires": [
        "Monica.WebApi::Monica.Modules.ModuleSwagger"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.02970000000004802,
          "startMs": 329.5035,
          "spanId": "module-phase-000072",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.20639999999997372,
          "startMs": 412.2026,
          "spanId": "module-phase-000143",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001599999999996271,
          "startMs": 465.8024,
          "spanId": "module-phase-000238",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004399999999975535,
          "startMs": 469.6721,
          "spanId": "module-phase-000333",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0019999999999527063,
          "startMs": 1985.7727,
          "spanId": "module-phase-000433",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.2696000000000822,
          "startMs": 2067.0704,
          "spanId": "module-phase-000540",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005000000001018634,
          "startMs": 2328.8125,
          "spanId": "module-phase-000667",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.StateStore.UI::Monica.Modules.ModuleStateStoreUI",
      "name": "ModuleStateStoreUI",
      "fullTypeName": "Monica.Modules.ModuleStateStoreUI",
      "assembly": "Monica.StateStore.UI",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 560,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.3195999999999799,
      "serialCallbackMs": 0.3195999999999799,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.StateStore::Monica.Modules.ModuleStateStore",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.10759999999999081,
          "startMs": 329.5398,
          "spanId": "module-phase-000073",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.154200000000003,
          "startMs": 412.4148,
          "spanId": "module-phase-000144",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0018000000000029104,
          "startMs": 465.8059,
          "spanId": "module-phase-000239",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004300000000000637,
          "startMs": 469.6782,
          "spanId": "module-phase-000334",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0019999999999527063,
          "startMs": 1985.7778,
          "spanId": "module-phase-000434",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.04919999999992797,
          "startMs": 2067.3487,
          "spanId": "module-phase-000541",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005000000001018634,
          "startMs": 2328.816,
          "spanId": "module-phase-000668",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.DevOps::Monica.Modules.ModuleK8SUI",
      "name": "ModuleK8SUI",
      "fullTypeName": "Monica.Modules.ModuleK8SUI",
      "assembly": "Monica.DevOps",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 570,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.13049999999958573,
      "serialCallbackMs": 0.13049999999958573,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.DevOps::Monica.Modules.ModuleK8S",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.08690000000001419,
          "startMs": 329.6511,
          "spanId": "module-phase-000074",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.03289999999998372,
          "startMs": 412.5744,
          "spanId": "module-phase-000145",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0011999999999829924,
          "startMs": 465.8089,
          "spanId": "module-phase-000240",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004200000000025739,
          "startMs": 469.6835,
          "spanId": "module-phase-000335",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0015999999998257408,
          "startMs": 1985.7832,
          "spanId": "module-phase-000435",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.003400000000056025,
          "startMs": 2067.4025,
          "spanId": "module-phase-000542",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.00029999999969732016,
          "startMs": 2328.8184,
          "spanId": "module-phase-000669",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.DevOps::Monica.Modules.ModuleFileOpsUI",
      "name": "ModuleFileOpsUI",
      "fullTypeName": "Monica.Modules.ModuleFileOpsUI",
      "assembly": "Monica.DevOps",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 580,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.12079999999957636,
      "serialCallbackMs": 0.12079999999957636,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.DevOps::Monica.Modules.ModuleFileOps",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.09039999999998827,
          "startMs": 329.7412,
          "spanId": "module-phase-000075",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.019700000000000273,
          "startMs": 412.6092,
          "spanId": "module-phase-000146",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0027000000000043656,
          "startMs": 465.811,
          "spanId": "module-phase-000241",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004500000000007276,
          "startMs": 469.6885,
          "spanId": "module-phase-000336",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0017000000000280124,
          "startMs": 1985.7868,
          "spanId": "module-phase-000436",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.0014999999998508429,
          "startMs": 2067.4079,
          "spanId": "module-phase-000543",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.00029999999969732016,
          "startMs": 2328.8202,
          "spanId": "module-phase-000670",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Utilities::Monica.Modules.ModuleUtilitiesUI",
      "name": "ModuleUtilitiesUI",
      "fullTypeName": "Monica.Modules.ModuleUtilitiesUI",
      "assembly": "Monica.Utilities",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 590,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.14809999999971524,
      "serialCallbackMs": 0.14809999999971524,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.UI::Monica.Modules.ModuleShellUI",
        "Monica.Utilities::Monica.Modules.ModuleUtilities"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.09129999999998972,
          "startMs": 329.8353,
          "spanId": "module-phase-000076",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.017500000000040927,
          "startMs": 412.6312,
          "spanId": "module-phase-000147",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0011999999999829924,
          "startMs": 465.8148,
          "spanId": "module-phase-000242",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004300000000000637,
          "startMs": 469.6938,
          "spanId": "module-phase-000337",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002199999999902502,
          "startMs": 1985.7923,
          "spanId": "module-phase-000437",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.031199999999898864,
          "startMs": 2067.4124,
          "spanId": "module-phase-000544",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0003999999998995918,
          "startMs": 2328.8227,
          "spanId": "module-phase-000671",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.AI::Monica.Modules.ModuleAI",
      "name": "ModuleAI",
      "fullTypeName": "Monica.Modules.ModuleAI",
      "assembly": "Monica.AI",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 600,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.6162999999995691,
      "serialCallbackMs": 0.6162999999995691,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.AI.UI::Monica.Modules.ModuleAIUI",
        "Monica.AI::Monica.Modules.ModuleAIEndpoints",
        "Monica.AI::Monica.Modules.ModuleMcp",
        "Monica.AI::Monica.Modules.ModuleRAG",
        "Monica.AI::Monica.Modules.ModuleSkillSystem"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0004000000000132786,
          "startMs": 329.9303,
          "spanId": "module-phase-000077",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.12350000000003547,
          "startMs": 412.6516,
          "spanId": "module-phase-000148",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013000000000147338,
          "startMs": 465.8177,
          "spanId": "module-phase-000243",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004500000000007276,
          "startMs": 469.6996,
          "spanId": "module-phase-000338",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0020999999999276042,
          "startMs": 1985.7979,
          "spanId": "module-phase-000438",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.36249999999972715,
          "startMs": 2067.4484,
          "spanId": "module-phase-000545",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.07359999999971478,
          "startMs": 2067.8164,
          "spanId": "module-phase-000546",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.04790000000002692,
          "startMs": 2067.8927,
          "spanId": "module-phase-000547",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005000000001018634,
          "startMs": 2328.826,
          "spanId": "module-phase-000672",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.AI::Monica.Modules.ModuleAIEndpoints",
      "name": "ModuleAIEndpoints",
      "fullTypeName": "Monica.Modules.ModuleAIEndpoints",
      "assembly": "Monica.AI",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 610,
      "isUI": false,
      "isWeb": true,
      "costMs": 0.7856999999995082,
      "serialCallbackMs": 0.7856999999995082,
      "startupWorkMs": 0,
      "requires": [
        "Monica.AI::Monica.Modules.ModuleAI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.02240000000000464,
          "startMs": 329.935,
          "spanId": "module-phase-000078",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.10360000000002856,
          "startMs": 412.7779,
          "spanId": "module-phase-000149",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013000000000147338,
          "startMs": 465.82,
          "spanId": "module-phase-000244",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005899999999996908,
          "startMs": 469.7089,
          "spanId": "module-phase-000339",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.006399999999985084,
          "startMs": 1985.8016,
          "spanId": "module-phase-000439",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.008499999999912689,
          "startMs": 2067.9476,
          "spanId": "module-phase-000548",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0007999999997991836,
          "startMs": 2328.8281,
          "spanId": "module-phase-000673",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.0018999999997504347,
          "startMs": 3171.0295,
          "spanId": "module-phase-000739",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.634900000000016,
          "startMs": 3252.6875,
          "spanId": "module-phase-000778",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Framework.UI::Monica.Modules.ModuleDependencyInjectionUI",
      "name": "ModuleDependencyInjectionUI",
      "fullTypeName": "Monica.Modules.ModuleDependencyInjectionUI",
      "assembly": "Monica.Framework.UI",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 620,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.17089999999984684,
      "serialCallbackMs": 0.17089999999984684,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.DependencyInjection::Monica.Modules.ModuleDependencyInjection",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.12229999999999563,
          "startMs": 330.5197,
          "spanId": "module-phase-000085",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.022699999999986176,
          "startMs": 412.8843,
          "spanId": "module-phase-000150",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001099999999951251,
          "startMs": 465.8226,
          "spanId": "module-phase-000245",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004500000000007276,
          "startMs": 469.7158,
          "spanId": "module-phase-000340",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0017000000000280124,
          "startMs": 1985.8107,
          "spanId": "module-phase-000440",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.018100000000231375,
          "startMs": 2067.9595,
          "spanId": "module-phase-000549",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0004999999996471161,
          "startMs": 2328.8308,
          "spanId": "module-phase-000674",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.StateStore::Monica.Modules.ModuleCancellationManager",
      "name": "ModuleCancellationManager",
      "fullTypeName": "Monica.Modules.ModuleCancellationManager",
      "assembly": "Monica.StateStore",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 630,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.3762000000001535,
      "serialCallbackMs": 0.3762000000001535,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.StateStore::Monica.Modules.ModuleTaskProgress"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0004000000000132786,
          "startMs": 330.7083,
          "spanId": "module-phase-000087",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.24650000000002592,
          "startMs": 412.9097,
          "spanId": "module-phase-000151",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.04230000000001155,
          "startMs": 465.8252,
          "spanId": "module-phase-000246",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004900000000020555,
          "startMs": 469.7213,
          "spanId": "module-phase-000341",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018000000000029104,
          "startMs": 1985.8167,
          "spanId": "module-phase-000441",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.07999999999992724,
          "startMs": 2067.9809,
          "spanId": "module-phase-000550",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0003000000001520675,
          "startMs": 2328.8351,
          "spanId": "module-phase-000675",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.StateStore::Monica.Modules.ModuleTaskProgress",
      "name": "ModuleTaskProgress",
      "fullTypeName": "Monica.Modules.ModuleTaskProgress",
      "assembly": "Monica.StateStore",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 640,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.5683999999993716,
      "serialCallbackMs": 0.5683999999993716,
      "startupWorkMs": 0,
      "requires": [
        "Monica.StateStore::Monica.Modules.ModuleCancellationManager",
        "Monica.StateStore::Monica.Modules.ModuleStateStore"
      ],
      "dependents": [
        "Monica.Office::Monica.Modules.ModuleExcel"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.12489999999996826,
          "startMs": 324.9612,
          "spanId": "module-phase-000020",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.234800000000007,
          "startMs": 413.1619,
          "spanId": "module-phase-000152",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.040300000000002,
          "startMs": 465.8693,
          "spanId": "module-phase-000247",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004700000000013915,
          "startMs": 469.7271,
          "spanId": "module-phase-000342",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0017000000000280124,
          "startMs": 1985.8206,
          "spanId": "module-phase-000442",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.16169999999965512,
          "startMs": 2068.0646,
          "spanId": "module-phase-000551",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.00029999999969732016,
          "startMs": 2328.8373,
          "spanId": "module-phase-000676",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Office::Monica.Modules.ModuleExcel",
      "name": "ModuleExcel",
      "fullTypeName": "Monica.Modules.ModuleExcel",
      "assembly": "Monica.Office",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 650,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.17649999999991905,
      "serialCallbackMs": 0.17649999999991905,
      "startupWorkMs": 0,
      "requires": [
        "Monica.StateStore::Monica.Modules.ModuleTaskProgress"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.024400000000014188,
          "startMs": 324.6662,
          "spanId": "module-phase-000010",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.0665000000000191,
          "startMs": 413.4018,
          "spanId": "module-phase-000153",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0018999999999778083,
          "startMs": 465.9144,
          "spanId": "module-phase-000248",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004500000000007276,
          "startMs": 469.7331,
          "spanId": "module-phase-000343",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.001399999999875945,
          "startMs": 1985.8258,
          "spanId": "module-phase-000443",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.0014999999998508429,
          "startMs": 2068.2329,
          "spanId": "module-phase-000552",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.07580000000007203,
          "startMs": 2068.2354,
          "spanId": "module-phase-000553",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005000000001018634,
          "startMs": 2328.841,
          "spanId": "module-phase-000677",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Core::Monica.Modules.ModuleResilience",
      "name": "ModuleResilience",
      "fullTypeName": "Monica.Modules.ModuleResilience",
      "assembly": "Monica.Core",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 660,
      "isUI": false,
      "isWeb": false,
      "costMs": 1.5106000000004087,
      "serialCallbackMs": 1.5106000000004087,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.ServiceDiscovery::Monica.Modules.ModuleServiceDiscovery"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0003000000000383807,
          "startMs": 330.7102,
          "spanId": "module-phase-000088",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.39990000000000236,
          "startMs": 413.4755,
          "spanId": "module-phase-000154",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0015000000000213731,
          "startMs": 465.9176,
          "spanId": "module-phase-000249",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004300000000000637,
          "startMs": 469.7387,
          "spanId": "module-phase-000344",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018999999999778083,
          "startMs": 1985.8297,
          "spanId": "module-phase-000444",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 1.1022000000002663,
          "startMs": 2068.316,
          "spanId": "module-phase-000554",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005000000001018634,
          "startMs": 2328.8441,
          "spanId": "module-phase-000678",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Core::Monica.Modules.ModuleMediator",
      "name": "ModuleMediator",
      "fullTypeName": "Monica.Modules.ModuleMediator",
      "assembly": "Monica.Core",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 670,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.8512999999999806,
      "serialCallbackMs": 0.8512999999999806,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleExecutionPipeline"
      ],
      "dependents": [
        "Monica.WebApi::Monica.Modules.ModuleWebApi"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.018000000000029104,
          "startMs": 330.7116,
          "spanId": "module-phase-000089",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.030600000000049477,
          "startMs": 413.8819,
          "spanId": "module-phase-000155",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0014999999999645297,
          "startMs": 465.92,
          "spanId": "module-phase-000250",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.3191999999999666,
          "startMs": 469.744,
          "spanId": "module-phase-000345",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018000000000029104,
          "startMs": 1985.8331,
          "spanId": "module-phase-000445",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.045499999999719876,
          "startMs": 2069.4297,
          "spanId": "module-phase-000555",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery / Type discovery commit",
          "durationMs": 0.43400000000019645,
          "startMs": 2166.3603,
          "spanId": "module-phase-000615",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Type discovery commit"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0007000000000516593,
          "startMs": 2328.8463,
          "spanId": "module-phase-000679",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.UI::Monica.Modules.ModuleDiffHighlight",
      "name": "ModuleDiffHighlight",
      "fullTypeName": "Monica.Modules.ModuleDiffHighlight",
      "assembly": "Monica.UI",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 680,
      "isUI": false,
      "isWeb": true,
      "costMs": 0.3032000000006292,
      "serialCallbackMs": 0.3032000000006292,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.Configuration.UI::Monica.Modules.ModuleConfigurationUI",
        "Monica.UI::Monica.Modules.ModuleDiffHighlightUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0002000000000066393,
          "startMs": 330.7314,
          "spanId": "module-phase-000090",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.0957000000000221,
          "startMs": 413.9149,
          "spanId": "module-phase-000156",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001599999999996271,
          "startMs": 465.9227,
          "spanId": "module-phase-000251",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.0070999999999799,
          "startMs": 470.0686,
          "spanId": "module-phase-000346",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0036000000000058208,
          "startMs": 1985.8375,
          "spanId": "module-phase-000446",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.14229999999997744,
          "startMs": 2069.4797,
          "spanId": "module-phase-000556",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.000600000000304135,
          "startMs": 2328.8493,
          "spanId": "module-phase-000680",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.002199999999902502,
          "startMs": 3171.0353,
          "spanId": "module-phase-000740",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.049900000000434375,
          "startMs": 3253.336,
          "spanId": "module-phase-000779",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Configuration.UI::Monica.Modules.ModuleConfigurationUI",
      "name": "ModuleConfigurationUI",
      "fullTypeName": "Monica.Modules.ModuleConfigurationUI",
      "assembly": "Monica.Configuration.UI",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 690,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.4569999999999368,
      "serialCallbackMs": 0.4569999999999368,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Configuration::Monica.Modules.ModuleConfiguration",
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.UI::Monica.Modules.ModuleDiffHighlight",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.3462999999999852,
          "startMs": 326.4508,
          "spanId": "module-phase-000050",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.05750000000000455,
          "startMs": 414.0141,
          "spanId": "module-phase-000157",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001699999999971169,
          "startMs": 465.9263,
          "spanId": "module-phase-000252",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005200000000002092,
          "startMs": 470.0774,
          "spanId": "module-phase-000347",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0019999999999527063,
          "startMs": 1985.8466,
          "spanId": "module-phase-000447",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.04379999999991924,
          "startMs": 2069.6278,
          "spanId": "module-phase-000557",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005000000001018634,
          "startMs": 2328.8531,
          "spanId": "module-phase-000681",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.UI::Monica.Modules.ModuleDiffHighlightUI",
      "name": "ModuleDiffHighlightUI",
      "fullTypeName": "Monica.Modules.ModuleDiffHighlightUI",
      "assembly": "Monica.UI",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 700,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.126599999999371,
      "serialCallbackMs": 0.126599999999371,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.UI::Monica.Modules.ModuleDiffHighlight",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.08019999999999072,
          "startMs": 327.7767,
          "spanId": "module-phase-000058",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.020600000000001728,
          "startMs": 414.078,
          "spanId": "module-phase-000158",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001699999999971169,
          "startMs": 465.9291,
          "spanId": "module-phase-000253",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004599999999982174,
          "startMs": 470.0837,
          "spanId": "module-phase-000348",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002199999999902502,
          "startMs": 1985.8506,
          "spanId": "module-phase-000448",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.016499999999723514,
          "startMs": 2069.675,
          "spanId": "module-phase-000558",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0007999999997991836,
          "startMs": 2328.8555,
          "spanId": "module-phase-000682",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Core::Monica.Modules.ModuleObservableInstance",
      "name": "ModuleObservableInstance",
      "fullTypeName": "Monica.Modules.ModuleObservableInstance",
      "assembly": "Monica.Core",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 710,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.06439999999997781,
      "serialCallbackMs": 0.06439999999997781,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.Core::Monica.Modules.ModuleHostedService",
        "Monica.Framework.UI::Monica.Modules.ModuleObservableInstanceUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0002000000000066393,
          "startMs": 330.7326,
          "spanId": "module-phase-000091",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.0221000000000231,
          "startMs": 414.1004,
          "spanId": "module-phase-000159",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0011000000000080945,
          "startMs": 465.9319,
          "spanId": "module-phase-000254",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004599999999982174,
          "startMs": 470.096,
          "spanId": "module-phase-000349",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0028999999999541615,
          "startMs": 1985.8549,
          "spanId": "module-phase-000449",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.032999999999901775,
          "startMs": 2069.694,
          "spanId": "module-phase-000559",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005000000001018634,
          "startMs": 2328.8581,
          "spanId": "module-phase-000683",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Core::Monica.Modules.ModuleHostedService",
      "name": "ModuleHostedService",
      "fullTypeName": "Monica.Modules.ModuleHostedService",
      "assembly": "Monica.Core",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 720,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.3461000000002059,
      "serialCallbackMs": 0.3461000000002059,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleExecutionPipeline",
        "Monica.Core::Monica.Modules.ModuleObservableInstance"
      ],
      "dependents": [
        "Monica.Dapr::Monica.Modules.ModuleDaprClient",
        "Monica.Dapr::Monica.Modules.ModuleDaprEventBus",
        "Monica.DevOps::Monica.Modules.ModuleGit",
        "Monica.Framework::Monica.Modules.ModuleSeeder",
        "Monica.JobScheduler::Monica.Modules.ModuleJobScheduler",
        "Monica.Repository::Monica.Modules.ModuleRepository",
        "Monica.ServiceDiscovery::Monica.Modules.ModuleServiceDiscovery"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.02400000000000091,
          "startMs": 330.6467,
          "spanId": "module-phase-000086",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.03519999999997481,
          "startMs": 414.1243,
          "spanId": "module-phase-000160",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0015000000000213731,
          "startMs": 465.9341,
          "spanId": "module-phase-000255",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004599999999982174,
          "startMs": 470.1017,
          "spanId": "module-phase-000350",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0036999999999807187,
          "startMs": 1985.8603,
          "spanId": "module-phase-000450",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.2766999999998916,
          "startMs": 2069.7303,
          "spanId": "module-phase-000560",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.00040000000035433914,
          "startMs": 2328.8605,
          "spanId": "module-phase-000684",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Repository::Monica.Modules.ModuleRepository",
      "name": "ModuleRepository",
      "fullTypeName": "Monica.Modules.ModuleRepository",
      "assembly": "Monica.Repository",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 730,
      "isUI": false,
      "isWeb": false,
      "costMs": 3.6256000000004747,
      "serialCallbackMs": 3.6256000000004747,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleClock",
        "Monica.Core::Monica.Modules.ModuleExecutionPipeline",
        "Monica.Core::Monica.Modules.ModuleHostedService",
        "Monica.Core::Monica.Modules.ModuleObjectMapping",
        "Monica.EventBus::Monica.Modules.ModuleEventBus",
        "Monica.Repository::Monica.Modules.ModuleUnitOfWork"
      ],
      "dependents": [
        "Monica.Configuration.EfCore::Monica.Modules.ModuleConfigurationEfCore",
        "Monica.Repository.UI::Monica.Modules.ModuleRepositoryUI",
        "Monica.WebApi::Monica.Modules.ModuleWebApi"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.11719999999996844,
          "startMs": 324.4836,
          "spanId": "module-phase-000005",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.005899999999996908,
          "startMs": 414.162,
          "spanId": "module-phase-000161",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0015000000000213731,
          "startMs": 465.9371,
          "spanId": "module-phase-000256",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004700000000013915,
          "startMs": 470.1076,
          "spanId": "module-phase-000351",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002200000000129876,
          "startMs": 1985.8699,
          "spanId": "module-phase-000451",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.1649000000002161,
          "startMs": 2070.0535,
          "spanId": "module-phase-000561",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 1.1149000000000342,
          "startMs": 2070.2222,
          "spanId": "module-phase-000562",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 1.4039999999999964,
          "startMs": 2071.3427,
          "spanId": "module-phase-000563",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.22080000000005384,
          "startMs": 2072.7521,
          "spanId": "module-phase-000564",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.35609999999996944,
          "startMs": 2072.9754,
          "spanId": "module-phase-000565",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.10780000000022483,
          "startMs": 2073.3367,
          "spanId": "module-phase-000566",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.08910000000014406,
          "startMs": 2073.4481,
          "spanId": "module-phase-000567",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.027599999999893043,
          "startMs": 2073.5401,
          "spanId": "module-phase-000568",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.005400000000008731,
          "startMs": 2073.5692,
          "spanId": "module-phase-000569",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.0030999999999039574,
          "startMs": 2073.5755,
          "spanId": "module-phase-000570",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0003999999998995918,
          "startMs": 2328.8647,
          "spanId": "module-phase-000685",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Configuration.EfCore::Monica.Modules.ModuleConfigurationEfCore",
      "name": "ModuleConfigurationEfCore",
      "fullTypeName": "Monica.Modules.ModuleConfigurationEfCore",
      "assembly": "Monica.Configuration.EfCore",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 740,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.37649999999996453,
      "serialCallbackMs": 0.37649999999996453,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Configuration::Monica.Modules.ModuleConfiguration",
        "Monica.Repository::Monica.Modules.ModuleRepository"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.23589999999995825,
          "startMs": 324.2397,
          "spanId": "module-phase-000004",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.019200000000012096,
          "startMs": 414.1704,
          "spanId": "module-phase-000162",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013000000000147338,
          "startMs": 465.9412,
          "spanId": "module-phase-000257",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.0050999999999703505,
          "startMs": 470.1143,
          "spanId": "module-phase-000352",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0060000000000854925,
          "startMs": 1985.8759,
          "spanId": "module-phase-000452",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.10829999999987194,
          "startMs": 2073.5859,
          "spanId": "module-phase-000571",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0007000000000516593,
          "startMs": 2328.8684,
          "spanId": "module-phase-000686",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Dapr::Monica.Modules.ModuleDaprClient",
      "name": "ModuleDaprClient",
      "fullTypeName": "Monica.Modules.ModuleDaprClient",
      "assembly": "Monica.Dapr",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 750,
      "isUI": false,
      "isWeb": true,
      "costMs": 2.38880000000006,
      "serialCallbackMs": 2.38880000000006,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleHostedService",
        "Monica.Core::Monica.Modules.ModuleJsonSerialization",
        "Monica.HealthCheck::Monica.Modules.ModuleHealthCheck"
      ],
      "dependents": [
        "Monica.Dapr::Monica.Modules.ModuleDaprEventBus",
        "Monica.Dapr::Monica.Modules.ModuleDaprRpcClient",
        "Monica.Dapr::Monica.Modules.ModuleDaprStateStore"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.028099999999994907,
          "startMs": 324.899,
          "spanId": "module-phase-000018",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.2539000000000442,
          "startMs": 414.1953,
          "spanId": "module-phase-000163",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0033000000000242835,
          "startMs": 465.9437,
          "spanId": "module-phase-000258",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004799999999988813,
          "startMs": 470.1206,
          "spanId": "module-phase-000353",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.003200000000106229,
          "startMs": 1985.8853,
          "spanId": "module-phase-000453",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 2.058799999999792,
          "startMs": 2073.6988,
          "spanId": "module-phase-000572",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0004999999996471161,
          "startMs": 2328.8715,
          "spanId": "module-phase-000687",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.0022000000003572495,
          "startMs": 3171.0424,
          "spanId": "module-phase-000741",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.0340000000001055,
          "startMs": 3253.3949,
          "spanId": "module-phase-000780",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.JobScheduler::Monica.Modules.ModuleJobScheduler",
      "name": "ModuleJobScheduler",
      "fullTypeName": "Monica.Modules.ModuleJobScheduler",
      "assembly": "Monica.JobScheduler",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 760,
      "isUI": false,
      "isWeb": false,
      "costMs": 13.919700000000262,
      "serialCallbackMs": 13.919700000000262,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleHostedService",
        "Monica.HealthCheck::Monica.Modules.ModuleHealthCheck"
      ],
      "dependents": [
        "Monica.JobScheduler.UI::Monica.Modules.ModuleJobSchedulerUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.023700000000019372,
          "startMs": 324.933,
          "spanId": "module-phase-000019",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 2.4343999999999824,
          "startMs": 414.4554,
          "spanId": "module-phase-000164",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.022600000000011278,
          "startMs": 465.949,
          "spanId": "module-phase-000259",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.10750000000001592,
          "startMs": 470.1271,
          "spanId": "module-phase-000354",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0025000000000545697,
          "startMs": 1985.8926,
          "spanId": "module-phase-000454",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.0029999999997016857,
          "startMs": 2075.7685,
          "spanId": "module-phase-000573",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery / Type discovery commit",
          "durationMs": 10.454000000000178,
          "startMs": 2166.8012,
          "spanId": "module-phase-000616",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Type discovery commit"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.7494000000001506,
          "startMs": 2328.8749,
          "spanId": "module-phase-000688",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services / Registration contribution",
          "durationMs": 0.1226000000001477,
          "startMs": 2329.6313,
          "spanId": "module-phase-000689",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Registration contribution"
        }
      ]
    },
    {
      "id": "Monica.ServiceDiscovery::Monica.Modules.ModuleServiceDiscovery",
      "name": "ModuleServiceDiscovery",
      "fullTypeName": "Monica.Modules.ModuleServiceDiscovery",
      "assembly": "Monica.ServiceDiscovery",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 770,
      "isUI": false,
      "isWeb": true,
      "costMs": 2.207500000000266,
      "serialCallbackMs": 2.207500000000266,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleHostedService",
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.Core::Monica.Modules.ModuleResilience",
        "Monica.StateStore::Monica.Modules.ModuleStateStore"
      ],
      "dependents": [
        "Monica.JobScheduler.ServiceDiscovery::Monica.Modules.ModuleJobSchedulerServiceDiscovery",
        "Monica.ServiceDiscovery::Monica.Modules.ModuleServiceDiscoveryUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.1826000000000363,
          "startMs": 325.0918,
          "spanId": "module-phase-000021",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 1.5162000000000262,
          "startMs": 416.8945,
          "spanId": "module-phase-000165",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0835999999999899,
          "startMs": 465.9739,
          "spanId": "module-phase-000260",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.006400000000041928,
          "startMs": 470.2381,
          "spanId": "module-phase-000355",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002300000000104774,
          "startMs": 1985.8988,
          "spanId": "module-phase-000455",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.3317000000001826,
          "startMs": 2075.7762,
          "spanId": "module-phase-000574",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.05029999999987922,
          "startMs": 2076.1127,
          "spanId": "module-phase-000575",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.001900000000205182,
          "startMs": 2329.7663,
          "spanId": "module-phase-000690",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.0018999999997504347,
          "startMs": 3171.0484,
          "spanId": "module-phase-000742",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.030600000000049477,
          "startMs": 3253.435,
          "spanId": "module-phase-000781",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Dapr::Monica.Modules.ModuleDaprStateStore",
      "name": "ModuleDaprStateStore",
      "fullTypeName": "Monica.Modules.ModuleDaprStateStore",
      "assembly": "Monica.Dapr",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 780,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.1707999999998151,
      "serialCallbackMs": 0.1707999999998151,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Dapr::Monica.Modules.ModuleDaprClient",
        "Monica.StateStore::Monica.Modules.ModuleStateStore"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.026400000000023738,
          "startMs": 325.2851,
          "spanId": "module-phase-000023",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.1291999999999689,
          "startMs": 418.4175,
          "spanId": "module-phase-000166",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0024999999999977263,
          "startMs": 466.06,
          "spanId": "module-phase-000261",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004600000000039017,
          "startMs": 470.2458,
          "spanId": "module-phase-000356",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0020999999999276042,
          "startMs": 1985.903,
          "spanId": "module-phase-000456",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.0052999999998064595,
          "startMs": 2076.1698,
          "spanId": "module-phase-000576",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0007000000000516593,
          "startMs": 2329.7718,
          "spanId": "module-phase-000691",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Dapr::Monica.Modules.ModuleDaprEventBus",
      "name": "ModuleDaprEventBus",
      "fullTypeName": "Monica.Modules.ModuleDaprEventBus",
      "assembly": "Monica.Dapr",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 790,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.5419000000003393,
      "serialCallbackMs": 0.5419000000003393,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleHostedService",
        "Monica.Dapr::Monica.Modules.ModuleDaprClient",
        "Monica.EventBus::Monica.Modules.ModuleEventBus"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.01830000000001064,
          "startMs": 325.3356,
          "spanId": "module-phase-000025",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.11730000000000018,
          "startMs": 418.5504,
          "spanId": "module-phase-000167",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0018000000000029104,
          "startMs": 466.0635,
          "spanId": "module-phase-000262",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004500000000007276,
          "startMs": 470.2543,
          "spanId": "module-phase-000357",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018999999999778083,
          "startMs": 1985.9069,
          "spanId": "module-phase-000457",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.3976999999999862,
          "startMs": 2076.1776,
          "spanId": "module-phase-000577",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.00040000000035433914,
          "startMs": 2329.7753,
          "spanId": "module-phase-000692",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Framework::Monica.Modules.ModuleSeeder",
      "name": "ModuleSeeder",
      "fullTypeName": "Monica.Modules.ModuleSeeder",
      "assembly": "Monica.Framework",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 800,
      "isUI": false,
      "isWeb": false,
      "costMs": 7.043699999999774,
      "serialCallbackMs": 7.043699999999774,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleHostedService",
        "Monica.HealthCheck::Monica.Modules.ModuleHealthCheck"
      ],
      "dependents": [
        "Monica.Framework.UI::Monica.Modules.ModuleSeederUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.024600000000020827,
          "startMs": 325.7176,
          "spanId": "module-phase-000031",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.4533000000000129,
          "startMs": 418.6717,
          "spanId": "module-phase-000168",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0018000000000029104,
          "startMs": 466.0669,
          "spanId": "module-phase-000263",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.061900000000036925,
          "startMs": 470.26,
          "spanId": "module-phase-000358",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002100000000154978,
          "startMs": 1985.9115,
          "spanId": "module-phase-000458",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.006300000000010186,
          "startMs": 2076.586,
          "spanId": "module-phase-000578",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery / Type discovery commit",
          "durationMs": 6.036099999999806,
          "startMs": 2177.2622,
          "spanId": "module-phase-000617",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Type discovery commit"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.45759999999972933,
          "startMs": 2329.7784,
          "spanId": "module-phase-000693",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.WebApi::Monica.Modules.ModuleWebApi",
      "name": "ModuleWebApi",
      "fullTypeName": "Monica.Modules.ModuleWebApi",
      "assembly": "Monica.WebApi",
      "state": "Active",
      "depth": 4,
      "registrationOrder": 810,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.21250000000048885,
      "serialCallbackMs": 0.21250000000048885,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Authority::Monica.Modules.ModuleAuthentication",
        "Monica.AutoModel::Monica.Modules.ModuleAutoModel",
        "Monica.Core::Monica.Modules.ModuleExceptionHandling",
        "Monica.Core::Monica.Modules.ModuleMediator",
        "Monica.Core::Monica.Modules.ModuleObjectMapping",
        "Monica.DependencyInjection::Monica.Modules.ModuleDependencyInjection",
        "Monica.Repository::Monica.Modules.ModuleRepository",
        "Monica.WebApi::Monica.Modules.ModuleAutoControllers",
        "Monica.WebApi::Monica.Modules.ModuleSwagger"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.17219999999997526,
          "startMs": 325.7805,
          "spanId": "module-phase-000033",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.028999999999996362,
          "startMs": 419.1299,
          "spanId": "module-phase-000169",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0015000000000213731,
          "startMs": 466.0698,
          "spanId": "module-phase-000264",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004500000000007276,
          "startMs": 470.3241,
          "spanId": "module-phase-000359",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.00200000000018008,
          "startMs": 1985.9171,
          "spanId": "module-phase-000459",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.002100000000154978,
          "startMs": 2076.5958,
          "spanId": "module-phase-000579",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0012000000001535227,
          "startMs": 2330.2472,
          "spanId": "module-phase-000694",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Dapr::Monica.Modules.ModuleDaprRpcClient",
      "name": "ModuleDaprRpcClient",
      "fullTypeName": "Monica.Modules.ModuleDaprRpcClient",
      "assembly": "Monica.Dapr",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 820,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.12040000000047257,
      "serialCallbackMs": 0.12040000000047257,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Dapr::Monica.Modules.ModuleDaprClient",
        "Monica.WebApi::Monica.Modules.ModuleRpcClient"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.08840000000003556,
          "startMs": 326.0648,
          "spanId": "module-phase-000042",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.02240000000000464,
          "startMs": 419.1615,
          "spanId": "module-phase-000170",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0011999999999829924,
          "startMs": 466.0724,
          "spanId": "module-phase-000265",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004700000000013915,
          "startMs": 470.3298,
          "spanId": "module-phase-000360",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0015000000000782165,
          "startMs": 1985.9212,
          "spanId": "module-phase-000460",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.0018000000000029104,
          "startMs": 2076.6,
          "spanId": "module-phase-000580",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.00040000000035433914,
          "startMs": 2330.2517,
          "spanId": "module-phase-000695",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.DevOps::Monica.Modules.ModuleGit",
      "name": "ModuleGit",
      "fullTypeName": "Monica.Modules.ModuleGit",
      "assembly": "Monica.DevOps",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 830,
      "isUI": false,
      "isWeb": true,
      "costMs": 2.2701000000000704,
      "serialCallbackMs": 2.2701000000000704,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleHostedService",
        "Monica.EventBus::Monica.Modules.ModuleEventBus"
      ],
      "dependents": [
        "Monica.DevOps::Monica.Modules.ModuleGitUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.020099999999956708,
          "startMs": 326.1763,
          "spanId": "module-phase-000044",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 1.87560000000002,
          "startMs": 419.186,
          "spanId": "module-phase-000171",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0015000000000213731,
          "startMs": 466.0748,
          "spanId": "module-phase-000266",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004500000000007276,
          "startMs": 470.3359,
          "spanId": "module-phase-000361",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0020999999999276042,
          "startMs": 1985.9248,
          "spanId": "module-phase-000461",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.3202000000001135,
          "startMs": 2076.6052,
          "spanId": "module-phase-000581",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0009000000000014552,
          "startMs": 2330.2559,
          "spanId": "module-phase-000696",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.0059000000001105946,
          "startMs": 3171.0541,
          "spanId": "module-phase-000743",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.03929999999991196,
          "startMs": 3253.4715,
          "spanId": "module-phase-000782",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.ServiceDiscovery::Monica.Modules.ModuleServiceDiscoveryUI",
      "name": "ModuleServiceDiscoveryUI",
      "fullTypeName": "Monica.Modules.ModuleServiceDiscoveryUI",
      "assembly": "Monica.ServiceDiscovery",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 840,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.25230000000004793,
      "serialCallbackMs": 0.25230000000004793,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.ServiceDiscovery::Monica.Modules.ModuleServiceDiscovery",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.10969999999997526,
          "startMs": 326.3364,
          "spanId": "module-phase-000049",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.07859999999999445,
          "startMs": 421.0682,
          "spanId": "module-phase-000172",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0036000000000058208,
          "startMs": 466.0775,
          "spanId": "module-phase-000267",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004500000000007276,
          "startMs": 470.3415,
          "spanId": "module-phase-000362",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018999999999778083,
          "startMs": 1985.9289,
          "spanId": "module-phase-000462",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.05349999999998545,
          "startMs": 2076.9337,
          "spanId": "module-phase-000582",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005000000001018634,
          "startMs": 2330.2595,
          "spanId": "module-phase-000697",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Framework.UI::Monica.Modules.ModuleSeederUI",
      "name": "ModuleSeederUI",
      "fullTypeName": "Monica.Modules.ModuleSeederUI",
      "assembly": "Monica.Framework.UI",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 850,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.2112000000001899,
      "serialCallbackMs": 0.2112000000001899,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.Framework::Monica.Modules.ModuleSeeder",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.092899999999986,
          "startMs": 328.172,
          "spanId": "module-phase-000061",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.019299999999986994,
          "startMs": 421.1503,
          "spanId": "module-phase-000173",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0012000000000398359,
          "startMs": 466.0891,
          "spanId": "module-phase-000268",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004300000000000637,
          "startMs": 470.3472,
          "spanId": "module-phase-000363",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.005100000000084037,
          "startMs": 1985.9324,
          "spanId": "module-phase-000463",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.08779999999978827,
          "startMs": 2076.9922,
          "spanId": "module-phase-000583",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.000600000000304135,
          "startMs": 2330.263,
          "spanId": "module-phase-000698",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.JobScheduler.UI::Monica.Modules.ModuleJobSchedulerUI",
      "name": "ModuleJobSchedulerUI",
      "fullTypeName": "Monica.Modules.ModuleJobSchedulerUI",
      "assembly": "Monica.JobScheduler.UI",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 860,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.3572000000002049,
      "serialCallbackMs": 0.3572000000002049,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.JobScheduler::Monica.Modules.ModuleJobScheduler",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [
        "Monica.JobScheduler.ServiceDiscovery::Monica.Modules.ModuleJobSchedulerServiceDiscovery"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.12550000000004502,
          "startMs": 328.3753,
          "spanId": "module-phase-000063",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.04599999999999227,
          "startMs": 421.1715,
          "spanId": "module-phase-000174",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001699999999971169,
          "startMs": 466.0923,
          "spanId": "module-phase-000269",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.006399999999985084,
          "startMs": 470.3528,
          "spanId": "module-phase-000364",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0026000000000294676,
          "startMs": 1985.9428,
          "spanId": "module-phase-000464",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.1746000000002823,
          "startMs": 2077.089,
          "spanId": "module-phase-000584",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0003999999998995918,
          "startMs": 2330.2664,
          "spanId": "module-phase-000699",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.JobScheduler.ServiceDiscovery::Monica.Modules.ModuleJobSchedulerServiceDiscovery",
      "name": "ModuleJobSchedulerServiceDiscovery",
      "fullTypeName": "Monica.Modules.ModuleJobSchedulerServiceDiscovery",
      "assembly": "Monica.JobScheduler.ServiceDiscovery",
      "state": "Active",
      "depth": 4,
      "registrationOrder": 870,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.10319999999973106,
      "serialCallbackMs": 0.10319999999973106,
      "startupWorkMs": 0,
      "requires": [
        "Monica.JobScheduler.UI::Monica.Modules.ModuleJobSchedulerUI",
        "Monica.ServiceDiscovery::Monica.Modules.ModuleServiceDiscovery"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0332000000000221,
          "startMs": 328.5052,
          "spanId": "module-phase-000064",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.02019999999998845,
          "startMs": 421.221,
          "spanId": "module-phase-000175",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013000000000147338,
          "startMs": 466.096,
          "spanId": "module-phase-000270",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.0056000000000153705,
          "startMs": 470.3606,
          "spanId": "module-phase-000365",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0027999999999792635,
          "startMs": 1985.9494,
          "spanId": "module-phase-000465",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.03969999999981155,
          "startMs": 2077.2691,
          "spanId": "module-phase-000585",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0003999999998995918,
          "startMs": 2330.2703,
          "spanId": "module-phase-000700",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Repository.UI::Monica.Modules.ModuleRepositoryUI",
      "name": "ModuleRepositoryUI",
      "fullTypeName": "Monica.Modules.ModuleRepositoryUI",
      "assembly": "Monica.Repository.UI",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 880,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.17850000000009913,
      "serialCallbackMs": 0.17850000000009913,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.Repository::Monica.Modules.ModuleRepository",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.11119999999999663,
          "startMs": 329.0107,
          "spanId": "module-phase-000068",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.02800000000002001,
          "startMs": 421.2467,
          "spanId": "module-phase-000176",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0018999999999778083,
          "startMs": 466.0991,
          "spanId": "module-phase-000271",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.007000000000005002,
          "startMs": 470.3704,
          "spanId": "module-phase-000366",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002300000000104774,
          "startMs": 1985.9565,
          "spanId": "module-phase-000466",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.027700000000095315,
          "startMs": 2077.3136,
          "spanId": "module-phase-000586",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0003999999998995918,
          "startMs": 2330.2736,
          "spanId": "module-phase-000701",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.DevOps::Monica.Modules.ModuleGitUI",
      "name": "ModuleGitUI",
      "fullTypeName": "Monica.Modules.ModuleGitUI",
      "assembly": "Monica.DevOps",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 890,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.1269999999996685,
      "serialCallbackMs": 0.1269999999996685,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.DevOps::Monica.Modules.ModuleGit",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.09550000000001546,
          "startMs": 329.228,
          "spanId": "module-phase-000070",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.017800000000022465,
          "startMs": 421.2768,
          "spanId": "module-phase-000177",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013000000000147338,
          "startMs": 466.1021,
          "spanId": "module-phase-000272",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004500000000007276,
          "startMs": 470.3784,
          "spanId": "module-phase-000367",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018000000000029104,
          "startMs": 1985.9604,
          "spanId": "module-phase-000467",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.005599999999958527,
          "startMs": 2077.3446,
          "spanId": "module-phase-000587",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0004999999996471161,
          "startMs": 2330.2757,
          "spanId": "module-phase-000702",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.UI::Monica.Modules.ModuleStackTraceUI",
      "name": "ModuleStackTraceUI",
      "fullTypeName": "Monica.Modules.ModuleStackTraceUI",
      "assembly": "Monica.UI",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 900,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.05410000000006221,
      "serialCallbackMs": 0.05410000000006221,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.Framework.UI::Monica.Modules.ModuleObservableInstanceUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.00029999999998153726,
          "startMs": 330.7359,
          "spanId": "module-phase-000092",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.020200000000045293,
          "startMs": 421.2965,
          "spanId": "module-phase-000178",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013999999999896318,
          "startMs": 466.1044,
          "spanId": "module-phase-000273",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004300000000000637,
          "startMs": 470.3841,
          "spanId": "module-phase-000368",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0025000000000545697,
          "startMs": 1985.9659,
          "spanId": "module-phase-000468",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.024899999999888678,
          "startMs": 2077.3525,
          "spanId": "module-phase-000588",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005000000001018634,
          "startMs": 2330.2786,
          "spanId": "module-phase-000703",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Framework.UI::Monica.Modules.ModuleObservableInstanceUI",
      "name": "ModuleObservableInstanceUI",
      "fullTypeName": "Monica.Modules.ModuleObservableInstanceUI",
      "assembly": "Monica.Framework.UI",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 910,
      "isUI": true,
      "isWeb": true,
      "costMs": 0.3411999999996169,
      "serialCallbackMs": 0.3411999999996169,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.Core::Monica.Modules.ModuleObservableInstance",
        "Monica.UI::Monica.Modules.ModuleShellUI",
        "Monica.UI::Monica.Modules.ModuleStackTraceUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.2563000000000102,
          "startMs": 327.151,
          "spanId": "module-phase-000053",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.01799999999997226,
          "startMs": 421.3183,
          "spanId": "module-phase-000179",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001599999999996271,
          "startMs": 466.1069,
          "spanId": "module-phase-000274",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.0050999999999703505,
          "startMs": 470.3894,
          "spanId": "module-phase-000369",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0022999999998774,
          "startMs": 1985.9716,
          "spanId": "module-phase-000469",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.023299999999835563,
          "startMs": 2077.3802,
          "spanId": "module-phase-000589",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005999999998493877,
          "startMs": 2330.2808,
          "spanId": "module-phase-000704",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.001900000000205182,
          "startMs": 3171.0638,
          "spanId": "module-phase-000744",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.03209999999990032,
          "startMs": 3253.5169,
          "spanId": "module-phase-000783",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Core::Monica.Modules.ModuleSystem",
      "name": "ModuleSystem",
      "fullTypeName": "Monica.Modules.ModuleSystem",
      "assembly": "Monica.Core",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 920,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.2283000000001607,
      "serialCallbackMs": 0.2283000000001607,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.UI::Monica.Modules.ModuleSystemUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0002000000000066393,
          "startMs": 330.7373,
          "spanId": "module-phase-000093",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.09339999999997417,
          "startMs": 421.3378,
          "spanId": "module-phase-000180",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013000000000147338,
          "startMs": 466.1097,
          "spanId": "module-phase-000275",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004599999999982174,
          "startMs": 470.3955,
          "spanId": "module-phase-000370",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0017000000000280124,
          "startMs": 1985.9759,
          "spanId": "module-phase-000470",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.12669999999980064,
          "startMs": 2077.4059,
          "spanId": "module-phase-000590",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.00040000000035433914,
          "startMs": 2330.2843,
          "spanId": "module-phase-000705",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.UI::Monica.Modules.ModuleSystemUI",
      "name": "ModuleSystemUI",
      "fullTypeName": "Monica.Modules.ModuleSystemUI",
      "assembly": "Monica.UI",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 930,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.260899999999765,
      "serialCallbackMs": 0.260899999999765,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.Core::Monica.Modules.ModuleSystem",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.1874000000000251,
          "startMs": 327.8597,
          "spanId": "module-phase-000059",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.021099999999989905,
          "startMs": 421.437,
          "spanId": "module-phase-000181",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001099999999951251,
          "startMs": 466.112,
          "spanId": "module-phase-000276",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004399999999975535,
          "startMs": 470.4012,
          "spanId": "module-phase-000371",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018000000000029104,
          "startMs": 1985.9795,
          "spanId": "module-phase-000471",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.044499999999970896,
          "startMs": 2077.5371,
          "spanId": "module-phase-000591",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005999999998493877,
          "startMs": 2330.2863,
          "spanId": "module-phase-000706",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.DevOps::Monica.Modules.ModuleTerminal",
      "name": "ModuleTerminal",
      "fullTypeName": "Monica.Modules.ModuleTerminal",
      "assembly": "Monica.DevOps",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 940,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.07260000000007949,
      "serialCallbackMs": 0.07260000000007949,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.DevOps::Monica.Modules.ModuleTerminalUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0002000000000066393,
          "startMs": 330.7389,
          "spanId": "module-phase-000094",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.028999999999996362,
          "startMs": 421.4597,
          "spanId": "module-phase-000182",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013000000000147338,
          "startMs": 466.114,
          "spanId": "module-phase-000277",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004100000000050841,
          "startMs": 470.4068,
          "spanId": "module-phase-000372",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0036000000000058208,
          "startMs": 1985.983,
          "spanId": "module-phase-000472",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.03389999999990323,
          "startMs": 2077.5847,
          "spanId": "module-phase-000592",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005000000001018634,
          "startMs": 2330.2887,
          "spanId": "module-phase-000707",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.DevOps::Monica.Modules.ModuleTerminalUI",
      "name": "ModuleTerminalUI",
      "fullTypeName": "Monica.Modules.ModuleTerminalUI",
      "assembly": "Monica.DevOps",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 950,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.20169999999973243,
      "serialCallbackMs": 0.20169999999973243,
      "startupWorkMs": 0,
      "requires": [
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.DevOps::Monica.Modules.ModuleTerminal",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.17109999999996717,
          "startMs": 329.3269,
          "spanId": "module-phase-000071",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.018100000000004002,
          "startMs": 421.4901,
          "spanId": "module-phase-000183",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0014999999999645297,
          "startMs": 466.1195,
          "spanId": "module-phase-000278",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.006500000000016826,
          "startMs": 470.4117,
          "spanId": "module-phase-000373",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0020999999999276042,
          "startMs": 1985.9906,
          "spanId": "module-phase-000473",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.0019999999999527063,
          "startMs": 2077.6212,
          "spanId": "module-phase-000593",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0003999999998995918,
          "startMs": 2330.2908,
          "spanId": "module-phase-000708",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.Core::Monica.Modules.ModuleXmlDocumentation",
      "name": "ModuleXmlDocumentation",
      "fullTypeName": "Monica.Modules.ModuleXmlDocumentation",
      "assembly": "Monica.Core",
      "state": "Active",
      "depth": 0,
      "registrationOrder": 960,
      "isUI": false,
      "isWeb": true,
      "costMs": 0.11430000000018481,
      "serialCallbackMs": 0.11430000000018481,
      "startupWorkMs": 0,
      "requires": [],
      "dependents": [
        "Monica.AI::Monica.Modules.ModuleMcp",
        "Monica.AI::Monica.Modules.ModuleSkillSystem"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.0002000000000066393,
          "startMs": 330.7648,
          "spanId": "module-phase-000096",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.01999999999998181,
          "startMs": 421.5099,
          "spanId": "module-phase-000184",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001099999999951251,
          "startMs": 466.1222,
          "spanId": "module-phase-000279",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.004399999999975535,
          "startMs": 470.4191,
          "spanId": "module-phase-000374",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.003500000000030923,
          "startMs": 1985.9947,
          "spanId": "module-phase-000474",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.02179999999998472,
          "startMs": 2077.6253,
          "spanId": "module-phase-000594",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0003999999998995918,
          "startMs": 2330.2929,
          "spanId": "module-phase-000709",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.0016000000000531145,
          "startMs": 3171.0697,
          "spanId": "module-phase-000745",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 0.061300000000301225,
          "startMs": 3253.5575,
          "spanId": "module-phase-000784",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.AI::Monica.Modules.ModuleSkillSystem",
      "name": "ModuleSkillSystem",
      "fullTypeName": "Monica.Modules.ModuleSkillSystem",
      "assembly": "Monica.AI",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 970,
      "isUI": false,
      "isWeb": false,
      "costMs": 1.4061000000000945,
      "serialCallbackMs": 1.4061000000000945,
      "startupWorkMs": 0,
      "requires": [
        "Monica.AI::Monica.Modules.ModuleAI",
        "Monica.Core::Monica.Modules.ModuleXmlDocumentation"
      ],
      "dependents": [
        "Monica.AI.UI::Monica.Modules.ModuleAIUI",
        "Monica.AI::Monica.Modules.ModuleKnowledgeBase"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.08589999999998099,
          "startMs": 330.4297,
          "spanId": "module-phase-000084",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.8374999999999773,
          "startMs": 421.5324,
          "spanId": "module-phase-000185",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.002399999999965985,
          "startMs": 466.1247,
          "spanId": "module-phase-000280",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.1312000000000353,
          "startMs": 470.4247,
          "spanId": "module-phase-000375",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.00200000000018008,
          "startMs": 1986.0012,
          "spanId": "module-phase-000475",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.0020999999997002305,
          "startMs": 2077.6498,
          "spanId": "module-phase-000595",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery / Type discovery commit",
          "durationMs": 0.15239999999994325,
          "startMs": 2183.3048,
          "spanId": "module-phase-000618",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Type discovery commit"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.1926000000003114,
          "startMs": 2330.2967,
          "spanId": "module-phase-000710",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.AI::Monica.Modules.ModuleKnowledgeBase",
      "name": "ModuleKnowledgeBase",
      "fullTypeName": "Monica.Modules.ModuleKnowledgeBase",
      "assembly": "Monica.AI",
      "state": "Active",
      "depth": 2,
      "registrationOrder": 980,
      "isUI": false,
      "isWeb": false,
      "costMs": 0.2996000000001686,
      "serialCallbackMs": 0.2996000000001686,
      "startupWorkMs": 0,
      "requires": [
        "Monica.AI::Monica.Modules.ModuleSkillSystem",
        "Monica.Markdown::Monica.Modules.ModuleMarkdown"
      ],
      "dependents": [
        "Monica.AI.UI::Monica.Modules.ModuleAIUI",
        "Monica.AI.UI::Monica.Modules.ModuleKnowledgeBaseUI",
        "Monica.AI::Monica.Modules.ModuleRAG"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.023199999999974352,
          "startMs": 330.1687,
          "spanId": "module-phase-000080",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.07720000000000482,
          "startMs": 422.3764,
          "spanId": "module-phase-000186",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013999999999896318,
          "startMs": 466.128,
          "spanId": "module-phase-000281",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005699999999990268,
          "startMs": 470.5621,
          "spanId": "module-phase-000376",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002100000000154978,
          "startMs": 1986.0067,
          "spanId": "module-phase-000476",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.14809999999988577,
          "startMs": 2077.6554,
          "spanId": "module-phase-000596",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.023999999999887223,
          "startMs": 2077.8063,
          "spanId": "module-phase-000597",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.016900000000077853,
          "startMs": 2077.8319,
          "spanId": "module-phase-000598",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0010000000002037268,
          "startMs": 2330.4977,
          "spanId": "module-phase-000711",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.AI::Monica.Modules.ModuleRAG",
      "name": "ModuleRAG",
      "fullTypeName": "Monica.Modules.ModuleRAG",
      "assembly": "Monica.AI",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 990,
      "isUI": false,
      "isWeb": false,
      "costMs": 64.91790000000037,
      "serialCallbackMs": 64.91790000000037,
      "startupWorkMs": 0,
      "requires": [
        "Monica.AI::Monica.Modules.ModuleAI",
        "Monica.AI::Monica.Modules.ModuleKnowledgeBase",
        "Monica.Markdown::Monica.Modules.ModuleMarkdown"
      ],
      "dependents": [
        "Monica.AI.UI::Monica.Modules.ModuleKnowledgeBaseUI",
        "Monica.AI.UI::Monica.Modules.ModuleRAGUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.02159999999997808,
          "startMs": 330.1942,
          "spanId": "module-phase-000081",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.2095000000000482,
          "startMs": 422.4564,
          "spanId": "module-phase-000187",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.001699999999971169,
          "startMs": 466.1304,
          "spanId": "module-phase-000282",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005400000000008731,
          "startMs": 470.569,
          "spanId": "module-phase-000377",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0017000000000280124,
          "startMs": 1986.0115,
          "spanId": "module-phase-000477",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.2238999999999578,
          "startMs": 2077.8529,
          "spanId": "module-phase-000599",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 0.15259999999989304,
          "startMs": 2078.0796,
          "spanId": "module-phase-000600",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Configure services / Registration contribution",
          "durationMs": 64.30090000000018,
          "startMs": 2078.2353,
          "spanId": "module-phase-000601",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Registration contribution"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.000600000000304135,
          "startMs": 2330.5013,
          "spanId": "module-phase-000712",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.AI.UI::Monica.Modules.ModuleKnowledgeBaseUI",
      "name": "ModuleKnowledgeBaseUI",
      "fullTypeName": "Monica.Modules.ModuleKnowledgeBaseUI",
      "assembly": "Monica.AI.UI",
      "state": "Active",
      "depth": 4,
      "registrationOrder": 1000,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.17429999999961865,
      "serialCallbackMs": 0.17429999999961865,
      "startupWorkMs": 0,
      "requires": [
        "Monica.AI::Monica.Modules.ModuleKnowledgeBase",
        "Monica.AI::Monica.Modules.ModuleRAG",
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [
        "Monica.AI.UI::Monica.Modules.ModuleRAGUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.08819999999997208,
          "startMs": 330.3387,
          "spanId": "module-phase-000083",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.029899999999997817,
          "startMs": 422.6696,
          "spanId": "module-phase-000188",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0011999999999829924,
          "startMs": 466.1334,
          "spanId": "module-phase-000283",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.007300000000043383,
          "startMs": 470.5757,
          "spanId": "module-phase-000378",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018000000000029104,
          "startMs": 1986.0165,
          "spanId": "module-phase-000478",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.04529999999977008,
          "startMs": 2142.5644,
          "spanId": "module-phase-000602",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0005999999998493877,
          "startMs": 2330.5059,
          "spanId": "module-phase-000713",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.AI.UI::Monica.Modules.ModuleRAGUI",
      "name": "ModuleRAGUI",
      "fullTypeName": "Monica.Modules.ModuleRAGUI",
      "assembly": "Monica.AI.UI",
      "state": "Active",
      "depth": 5,
      "registrationOrder": 1010,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.18539999999984502,
      "serialCallbackMs": 0.18539999999984502,
      "startupWorkMs": 0,
      "requires": [
        "Monica.AI.UI::Monica.Modules.ModuleKnowledgeBaseUI",
        "Monica.AI::Monica.Modules.ModuleRAG",
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.11760000000003856,
          "startMs": 330.2177,
          "spanId": "module-phase-000082",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.02390000000002601,
          "startMs": 422.7012,
          "spanId": "module-phase-000189",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0014000000000464752,
          "startMs": 466.1355,
          "spanId": "module-phase-000284",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.0044999999999504325,
          "startMs": 470.5839,
          "spanId": "module-phase-000379",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.002200000000129876,
          "startMs": 1986.0201,
          "spanId": "module-phase-000479",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.03539999999975407,
          "startMs": 2142.6138,
          "spanId": "module-phase-000603",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.0003999999998995918,
          "startMs": 2330.509,
          "spanId": "module-phase-000714",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.AI::Monica.Modules.ModuleMcp",
      "name": "ModuleMcp",
      "fullTypeName": "Monica.Modules.ModuleMcp",
      "assembly": "Monica.AI",
      "state": "Active",
      "depth": 1,
      "registrationOrder": 1020,
      "isUI": false,
      "isWeb": true,
      "costMs": 180.07800000000003,
      "serialCallbackMs": 180.07800000000003,
      "startupWorkMs": 0,
      "requires": [
        "Monica.AI::Monica.Modules.ModuleAI",
        "Monica.Core::Monica.Modules.ModuleXmlDocumentation"
      ],
      "dependents": [
        "Monica.AI.UI::Monica.Modules.ModuleAIUI"
      ],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.01830000000001064,
          "startMs": 330.745,
          "spanId": "module-phase-000095",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 41.477300000000014,
          "startMs": 422.7271,
          "spanId": "module-phase-000190",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0011999999999829924,
          "startMs": 466.1377,
          "spanId": "module-phase-000285",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.07570000000004029,
          "startMs": 470.5893,
          "spanId": "module-phase-000380",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018999999999778083,
          "startMs": 1986.0239,
          "spanId": "module-phase-000480",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.002800000000206637,
          "startMs": 2142.6531,
          "spanId": "module-phase-000604",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery / Type discovery commit",
          "durationMs": 0.1428000000000793,
          "startMs": 2183.4616,
          "spanId": "module-phase-000619",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Type discovery commit"
        },
        {
          "name": "Post-configure services",
          "durationMs": 39.924099999999726,
          "startMs": 2330.5113,
          "spanId": "module-phase-000715",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure application builder",
          "durationMs": 0.002199999999902502,
          "startMs": 3171.0755,
          "spanId": "module-phase-000746",
          "kind": "Module callback",
          "modulePhase": "Configure application builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure endpoints",
          "durationMs": 98.43170000000009,
          "startMs": 3253.6248,
          "spanId": "module-phase-000785",
          "kind": "Module callback",
          "modulePhase": "Configure endpoints",
          "callbackKind": "Lifecycle"
        }
      ]
    },
    {
      "id": "Monica.AI.UI::Monica.Modules.ModuleAIUI",
      "name": "ModuleAIUI",
      "fullTypeName": "Monica.Modules.ModuleAIUI",
      "assembly": "Monica.AI.UI",
      "state": "Active",
      "depth": 3,
      "registrationOrder": 1030,
      "isUI": true,
      "isWeb": false,
      "costMs": 0.3503999999997518,
      "serialCallbackMs": 0.3503999999997518,
      "startupWorkMs": 0,
      "requires": [
        "Monica.AI::Monica.Modules.ModuleAI",
        "Monica.AI::Monica.Modules.ModuleKnowledgeBase",
        "Monica.AI::Monica.Modules.ModuleMcp",
        "Monica.AI::Monica.Modules.ModuleSkillSystem",
        "Monica.Core::Monica.Modules.ModuleLocalization",
        "Monica.UI::Monica.Modules.ModuleShellUI"
      ],
      "dependents": [],
      "phases": [
        {
          "name": "Describe",
          "durationMs": 0.203899999999976,
          "startMs": 329.9599,
          "spanId": "module-phase-000079",
          "kind": "Module callback",
          "modulePhase": "Describe",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Finalize options",
          "durationMs": 0.07010000000002492,
          "startMs": 464.2204,
          "spanId": "module-phase-000191",
          "kind": "Module callback",
          "modulePhase": "Finalize options",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare contracts",
          "durationMs": 0.0013000000000147338,
          "startMs": 466.1397,
          "spanId": "module-phase-000286",
          "kind": "Module callback",
          "modulePhase": "Declare contracts",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Declare type discovery",
          "durationMs": 0.005100000000027194,
          "startMs": 470.667,
          "spanId": "module-phase-000381",
          "kind": "Module callback",
          "modulePhase": "Declare type discovery",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure builder",
          "durationMs": 0.0018000000000029104,
          "startMs": 1986.0272,
          "spanId": "module-phase-000481",
          "kind": "Module callback",
          "modulePhase": "Configure builder",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Configure services",
          "durationMs": 0.06579999999985375,
          "startMs": 2142.6579,
          "spanId": "module-phase-000605",
          "kind": "Module callback",
          "modulePhase": "Configure services",
          "callbackKind": "Lifecycle"
        },
        {
          "name": "Post-configure services",
          "durationMs": 0.002399999999852298,
          "startMs": 2370.463,
          "spanId": "module-phase-000716",
          "kind": "Module callback",
          "modulePhase": "Post-configure services",
          "callbackKind": "Lifecycle"
        }
      ]
    }
  ],
  "moduleMeta": {
    "revision": 2363,
    "compositionId": "c4de0678879e4b34bf15821e8db20b4b",
    "phase": "Final",
    "outcome": "Succeeded",
    "captureTime": "2026-10-04T15:53:58.4487296+00:00",
    "totalMs": 3352.4989,
    "applicationStartupMs": 6243.1855,
    "serviceRegistrationMs": 2371.6041,
    "typeDiscoveryMs": 1346.4824999999996,
    "uiCount": 31,
    "webCount": 31,
    "maxDepth": 5,
    "findings": [],
    "criticalPath": [],
    "discovery": {
      "statistics": {
        "assemblyCount": 49,
        "enumeratedTypeCount": 13524,
        "excludedTypeCount": 2,
        "planCount": 13,
        "distinctQueryCount": 13,
        "matchCount": 13872,
        "commitCallbackCount": 14,
        "serviceRegistrations": {
          "addedCount": 273,
          "replacedCount": 0,
          "skippedCount": 75
        }
      },
      "stages": [
        {
          "name": "Discovery plan declaration",
          "stage": "Discovery plan declaration",
          "durationMs": 4.001100000000008,
          "spanId": "system-phase-000287"
        },
        {
          "name": "Assembly resolution",
          "stage": "Assembly resolution",
          "durationMs": 36.97030000000001,
          "spanId": "system-phase-000382"
        },
        {
          "name": "Type enumeration",
          "stage": "Type enumeration",
          "durationMs": 1007.0821999999998,
          "spanId": "system-phase-000383"
        },
        {
          "name": "Query evaluation",
          "stage": "Query evaluation",
          "durationMs": 261.4205000000002,
          "spanId": "system-phase-000384"
        },
        {
          "name": "Registration commit",
          "stage": "Registration commit",
          "durationMs": 37.00839999999971,
          "spanId": "system-phase-000606"
        }
      ],
      "queries": [
        {
          "queryId": "query-0001",
          "consumerModuleIds": [
            "Monica.Configuration::Monica.Modules.ModuleConfiguration"
          ],
          "matchCount": 25
        },
        {
          "queryId": "query-0002",
          "consumerModuleIds": [
            "Monica.DependencyInjection::Monica.Modules.ModuleDependencyInjection"
          ],
          "matchCount": 147
        },
        {
          "queryId": "query-0003",
          "consumerModuleIds": [
            "Monica.Core::Monica.Modules.ModuleObjectMapping"
          ],
          "matchCount": 5
        },
        {
          "queryId": "query-0004",
          "consumerModuleIds": [
            "Monica.EventBus::Monica.Modules.ModuleEventBus"
          ],
          "matchCount": 9
        },
        {
          "queryId": "query-0005",
          "consumerModuleIds": [
            "Monica.WebApi::Monica.Modules.ModuleAutoControllers"
          ],
          "matchCount": 12
        },
        {
          "queryId": "query-0006",
          "consumerModuleIds": [
            "Monica.ProjectUnits::Monica.Modules.ModuleProjectUnits"
          ],
          "matchCount": 13522
        },
        {
          "queryId": "query-0007",
          "consumerModuleIds": [
            "Monica.Core::Monica.Modules.ModuleLocalization"
          ],
          "matchCount": 32
        },
        {
          "queryId": "query-0008",
          "consumerModuleIds": [
            "Monica.WebApi::Monica.Modules.ModuleRpcClient"
          ],
          "matchCount": 26
        },
        {
          "queryId": "query-0009",
          "consumerModuleIds": [
            "Monica.Core::Monica.Modules.ModuleMediator"
          ],
          "matchCount": 75
        },
        {
          "queryId": "query-0010",
          "consumerModuleIds": [
            "Monica.JobScheduler::Monica.Modules.ModuleJobScheduler"
          ],
          "matchCount": 15
        },
        {
          "queryId": "query-0011",
          "consumerModuleIds": [
            "Monica.Framework::Monica.Modules.ModuleSeeder"
          ],
          "matchCount": 1
        },
        {
          "queryId": "query-0012",
          "consumerModuleIds": [
            "Monica.AI::Monica.Modules.ModuleSkillSystem",
            "Monica.AI::Monica.Modules.ModuleMcp"
          ],
          "matchCount": 3
        },
        {
          "queryId": "query-0013",
          "consumerModuleIds": [
            "Monica.AI::Monica.Modules.ModuleMcp"
          ],
          "matchCount": 0
        }
      ]
    },
    "traceSpanCount": 787,
    "directEdgeCount": 185
  },
  "configuration": {
    "definitions": [
      {
        "name": "MatchOptions",
        "label": "匹配电报参数配置",
        "key": null,
        "project": "AlarmService.Domain",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "AlarmOptions",
        "label": "告警相关判断配置",
        "key": null,
        "project": "AlarmService.Domain",
        "category": null,
        "version": 2,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "AtcFlightDataOptions",
        "label": "ATC连接配置",
        "key": null,
        "project": "DataExchangeService.AtcFlightDataAdaptor",
        "category": null,
        "version": 7,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "FlightAdaptorActiveMqOptions",
        "label": "飞行计划ActiveMQ消息队列（生产）参数配置",
        "key": null,
        "project": "DataExchangeService.FlightAdaptor",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "FlightAdaptorKafkaOptions",
        "label": "飞行计划Kafka消息队列（生产）参数配置",
        "key": null,
        "project": "DataExchangeService.FlightAdaptor",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "RouteDataOptions",
        "label": "航迹数据参数配置",
        "key": null,
        "project": "DataExchangeService.FlightRouteAdaptor",
        "category": null,
        "version": 12,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "UavTrackOptions",
        "label": "无人机航迹ActiveMQ消息队列（消费）参数配置",
        "key": null,
        "project": "DataExchangeService.GeneralFlightAdaptor",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "UavTrackICEOptions",
        "label": "无人机航迹数据交换平台参数配置",
        "key": null,
        "project": "DataExchangeService.GeneralFlightAdaptor",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "GeneralFlightOptions",
        "label": "通航ActiveMQ消息队列（消费）参数配置",
        "key": null,
        "project": "DataExchangeService.GeneralFlightAdaptor",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "OuterGeneralFlightOptions",
        "label": "通航ActiveMQ消息队列（生产）参数配置",
        "key": null,
        "project": "DataExchangeService.GeneralFlightAdaptor",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "GeneralFlightKafkaOptions",
        "label": "通航Kafka消息队列（消费）参数配置",
        "key": null,
        "project": "DataExchangeService.GeneralFlightAdaptor",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "GeneralFlightICEOptions",
        "label": "通航连接数据交换平台参数配置",
        "key": null,
        "project": "DataExchangeService.GeneralFlightAdaptor",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "MilitaryFlightAdaptorOptions",
        "label": "军航ActiveMQ消息队列（生产）参数配置",
        "key": null,
        "project": "DataExchangeService.MilitaryFlightAdaptor",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "NaipOptions",
        "label": "NAIP数据交换配置",
        "key": null,
        "project": "DataExchangeService.NaipAdaptor",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "TWRFlightDataOptions",
        "label": "TWR连接配置",
        "key": null,
        "project": "DataExchangeService.TWRFlightDataAdaptor",
        "category": null,
        "version": 7,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "OcrOptions",
        "label": "OCR参数配置",
        "key": null,
        "project": "FileService.Domain",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "CplOptions",
        "label": "CPL报组报配置",
        "key": null,
        "project": "FlightRouteService.Domain",
        "category": null,
        "version": 2,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "RemarkFlightRouteOptions",
        "label": "备注航路正则表达式参数",
        "key": null,
        "project": "FlightRouteService.Domain",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "FlightRouteOptions",
        "label": "航路参数配置",
        "key": null,
        "project": "FlightRouteService.Domain",
        "category": null,
        "version": 2,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "ControlAreaOptions",
        "label": "计算相关区域配置",
        "key": null,
        "project": "FlightRouteService.Domain",
        "category": null,
        "version": 2,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "LimitConditionOptions",
        "label": "限定条件判断参数配置",
        "key": null,
        "project": "FlightRouteService.Domain",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "TowerDepArrOptions",
        "label": "Tower场面状态起落判断参数",
        "key": "FlightService.Domain.Configurations.TowerDepArrOptions",
        "project": "FlightService.Domain",
        "category": null,
        "version": 1,
        "revision": 5,
        "origin": null,
        "reload": "Restart required",
        "fields": [
          {
            "path": "DeparturePreviousState",
            "label": "起飞前置状态",
            "description": null,
            "type": "string",
            "value": "TAK",
            "restart": true
          },
          {
            "path": "DepartureCurrentState",
            "label": "起飞完成状态",
            "description": null,
            "type": "string",
            "value": "FIN",
            "restart": true
          },
          {
            "path": "ArrivalPreviousState",
            "label": "落地前置状态",
            "description": null,
            "type": "string",
            "value": "TCH",
            "restart": true
          },
          {
            "path": "ArrivalCurrentStates",
            "label": "落地完成状态",
            "description": null,
            "type": "string[]",
            "value": [
              "V/H",
              "TAX",
              "FIN"
            ],
            "restart": true
          }
        ],
        "evidence": "Live Configuration State selected definition, schema v1 / revision 5; labels and types cross-checked with FlightService.Domain/Configurations/TowerDepArrOptions.cs.",
        "description": null
      },
      {
        "name": "DelayFlightOptions",
        "label": "延误航班原因相关参数",
        "key": null,
        "project": "FlightService.Domain",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "FlightOptions",
        "label": "航班相关参数",
        "key": null,
        "project": "FlightService.Domain",
        "category": null,
        "version": 7,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "DuplicateCheckOptions",
        "label": "重复性检查配置",
        "key": "FlightService.Domain.Configurations.DuplicateCheckOptions",
        "project": "FlightService.Domain",
        "category": null,
        "version": 1,
        "revision": 1,
        "origin": null,
        "reload": "Reload mode pending review",
        "fields": [
          {
            "path": "VipPFPFuzzyMatchLeft",
            "label": "VIP PFP模糊匹配SOBT左区间",
            "description": null,
            "type": "TimeSpan",
            "value": "2.00:00:00",
            "restart": true
          },
          {
            "path": "VipPFPFuzzyMatchRight",
            "label": "VIP PFP模糊匹配SOBT右区间",
            "description": null,
            "type": "TimeSpan",
            "value": "2.00:00:00",
            "restart": true
          },
          {
            "path": "PFPFuzzyMatchLeft",
            "label": "PFP模糊匹配SOBT左区间",
            "description": null,
            "type": "TimeSpan",
            "value": "02:00",
            "restart": true
          },
          {
            "path": "PFPFuzzyMatchRight",
            "label": "PFP模糊匹配SOBT右区间",
            "description": null,
            "type": "TimeSpan",
            "value": "04:00",
            "restart": true
          }
        ],
        "evidence": "Live Configuration State selected definition, schema v1 / revision 1; labels and types cross-checked with FlightService.Domain/Configurations/DuplicateCheckOptions.cs.",
        "description": "CK电报匹配时候PFP及FP匹配及修改或新增后重复计划检查区间设置"
      },
      {
        "name": "AIDCOptions",
        "label": "AIDC电报参数配置",
        "key": null,
        "project": "MessageService.Domain",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "MsgIgnoreOptions",
        "label": "判断AFTN报是否忽略告警配置",
        "key": null,
        "project": "MessageService.Domain",
        "category": null,
        "version": 2,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "OutMessageOptions",
        "label": "发报参数配置",
        "key": null,
        "project": "MessageService.Domain",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "CKMessageOptions",
        "label": "布置电报参数配置",
        "key": null,
        "project": "MessageService.Domain",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "TransitOptions",
        "label": "延伸服务转报参数配置",
        "key": null,
        "project": "MessageService.Domain",
        "category": null,
        "version": 2,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "AtcAddressOptions",
        "label": "空管发报地址参数配置",
        "key": null,
        "project": "MessageService.Domain",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "DepArrOptions",
        "label": "自动起飞落地报参数配置",
        "key": null,
        "project": "MessageService.Domain",
        "category": null,
        "version": 3,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "MilitaryFlightOptions",
        "label": "军航类型参数配置",
        "key": null,
        "project": "MilitaryFlightService.Domain",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "AreaIrrelevantFlightMatchOptions",
        "label": "AFTN区域无关航班匹配配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "FlightMatchOptions",
        "label": "AFTN航班匹配配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "AiModuleOptions",
        "label": "AI模块运行配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "DaprOptions",
        "label": "Dapr运行配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 5,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "JwtTokenOptions",
        "label": "JWT参数配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "K8SOptions",
        "label": "K8S运行配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "EventBusKafkaOptions",
        "label": "Kafka EventBus 控制台配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "KubernetesOptions",
        "label": "Kubernetes配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "ServicesOptions",
        "label": "全局微服务配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 2,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "ActivateAtcFPOptions",
        "label": "出港航班EST激活参数",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "CleanHistoryOptions",
        "label": "备份历史参数配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "AppOptions",
        "label": "微服务运行配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 26,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "SectorOptions",
        "label": "扇区配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "DatabaseOptions",
        "label": "数据库连接配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "BlobStoringOptions",
        "label": "文件存储配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "SystemInfoCustomLinksOptions",
        "label": "系统信息快捷链接配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "MsgPreProcessorForAtcOptions",
        "label": "自动化前置配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 3,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "FlightStatOptions",
        "label": "航班架次统计配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 6,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "FlightExtendOptions",
        "label": "航班通用相关参数",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 13,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "IgnoreAlarmByMsgOptions",
        "label": "虚警优化配置",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 2,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "ResendAtcFPLOptions",
        "label": "补发FPL报文作业参数",
        "key": null,
        "project": "Platform.Protocol",
        "category": null,
        "version": 2,
        "revision": null,
        "origin": null,
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "DataSyncOption",
        "label": "一二级节点数据同步设置",
        "key": null,
        "project": "SystemService.Domain",
        "category": null,
        "version": 4,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "SecondaryDataSyncPartitionOptions",
        "label": "二级节点数据同步分区设置",
        "key": null,
        "project": "SystemService.Domain",
        "category": null,
        "version": 7,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      },
      {
        "name": "FipsVersionOptions",
        "label": "Fips版本获取配置",
        "key": null,
        "project": "UserService.WebAPI",
        "category": null,
        "version": 1,
        "revision": null,
        "origin": "DB",
        "reload": null,
        "fields": [],
        "evidence": "Live configuration catalog: label, type name, publishing project, schema version and DB badge when shown."
      }
    ],
    "captureTime": "2026-10-04T15:58:08.239888+00:00",
    "hostUrl": "http://localhost:5294"
  },
  "provenance": {
    "sourceHost": "FIPS2022 / FlightService.API",
    "sourceLanguage": "en-US framework labels; original application configuration labels retained",
    "moduleCapture": "evidence/module-capture.json",
    "navigationCapture": "Live category dropdowns and Debug overflow",
    "configurationCapture": "57 live catalog definitions; field values captured for TowerDepArrOptions and DuplicateCheckOptions only",
    "systemCapture": "Live System dossier DOM",
    "behavior": "Captured data is immutable. Interaction belongs to the local prototype."
  }
};
