// FIXTURE DATA. Module names and assemblies come from the Monica source tree;
// every duration, count, and relationship below is illustrative, not host evidence.
window.WORKBENCH_FIXTURE = {
  domainMs: 8000,
  readyMs: 7510,
  brackets: [
    { label: "Monica 组合 · 3.06 s", start: 620, end: 3680, kind: "compose" },
    { label: "启动任务 · 2.71 s", start: 3680, end: 6390, kind: "work" }
  ],
  lanes: [
    { id: "host", name: "宿主构建", kind: "host", start: 0, end: 620,
      note: "WebApplication.CreateBuilder 至首个 Monica 回调" },
    { id: "discovery", name: "程序集发现", kind: "compose", start: 620, end: 1460,
      note: "扫描 37 个程序集，枚举 13,585 个类型" },
    { id: "registration", name: "模块注册", kind: "compose", start: 1460, end: 2380,
      note: "54 个已声明模块，51 个启用" },
    { id: "configure", name: "服务配置", kind: "compose", start: 2380, end: 3110,
      note: "ConfigureServices 回调串行执行" },
    { id: "pipeline", name: "管道组装", kind: "compose", start: 3110, end: 3680,
      note: "中间件与终结点映射" },
    { id: "work", name: "启动任务", kind: "work", start: 3680, end: 6390,
      note: "5 段串行启动任务",
      segments: [
        { label: "Kafka", module: "ModuleEventBusKafka", ms: 1180.4, warn: true },
        { label: "JobScheduler", module: "ModuleJobScheduler", ms: 512.0 },
        { label: "Configuration", module: "ModuleConfiguration", ms: 398.1 },
        { label: "Seeder", module: "ModuleSeeder", ms: 340.0 },
        { label: "其他", module: null, ms: 279.5 }
      ] },
    { id: "listen", name: "Kestrel 监听", kind: "host", start: 6390, end: 7510,
      note: "托管服务启动至端口就绪" }
  ],
  // Ordered intervals that determine the ready instant.
  criticalPath: [
    { name: "宿主构建", sub: "CreateBuilder", kind: "host", ms: 620 },
    { name: "程序集发现", sub: "37 个程序集", kind: "compose", ms: 840 },
    { name: "模块注册", sub: "51 个启用模块", kind: "compose", ms: 920 },
    { name: "ModuleEventBusKafka", sub: "消费者预热 · 超出预算", kind: "work", ms: 1180.4, warn: true, module: "ModuleEventBusKafka" },
    { name: "Kestrel 监听", sub: "端口就绪", kind: "host", ms: 1120 }
  ],
  depth: [6, 11, 15, 12, 5, 2],
  declared: 54,
  modules: [
    { name: "ModuleEventBusKafka", asm: "Monica.EventBus.Kafka", caps: ["Web"], cb: 46.2, sw: 1180.4, depth: 3, budget: 800,
      requires: ["ModuleEventBus", "ModuleJsonSerialization", "ModuleHostedService"] },
    { name: "ModuleJobScheduler", asm: "Monica.JobScheduler", caps: [], cb: 38.7, sw: 512.0, depth: 3,
      requires: ["ModuleHostedService", "ModuleLocker", "ModuleStateStore"] },
    { name: "ModuleConfiguration", asm: "Monica.Configuration", caps: ["Web"], cb: 61.4, sw: 398.1, depth: 2,
      requires: ["ModuleJsonSerialization", "ModuleLocalization"] },
    { name: "ModuleSeeder", asm: "Monica.Framework", caps: [], cb: 12.3, sw: 340.0, depth: 4,
      requires: ["ModuleRepository", "ModuleUnitOfWork", "ModuleHostedService"] },
    { name: "ModuleRepository", asm: "Monica.Repository", caps: [], cb: 142.6, sw: 0, depth: 3,
      requires: ["ModuleUnitOfWork", "ModuleSnowflake", "ModuleObjectMapping"] },
    { name: "ModuleAutoControllers", asm: "Monica.WebApi", caps: ["Web host"], cb: 118.9, sw: 0, depth: 3,
      requires: ["ModuleControllers", "ModuleXmlDocumentation", "ModuleMediator"] },
    { name: "ModuleShellUI", asm: "Monica.UI", caps: ["UI", "Web host"], cb: 96.4, sw: 0, depth: 4,
      requires: ["ModuleLocalization", "ModuleAuthorization", "ModuleSystem"] },
    { name: "ModuleHealthCheck", asm: "Monica.HealthCheck", caps: ["Web"], cb: 22.1, sw: 96.3, depth: 2,
      requires: ["ModuleHostedService"] },
    { name: "ModuleSwagger", asm: "Monica.WebApi", caps: ["Web host"], cb: 88.0, sw: 0, depth: 4,
      requires: ["ModuleAutoControllers", "ModuleXmlDocumentation"] },
    { name: "ModuleObjectMapping", asm: "Monica.Core", caps: ["Web"], cb: 71.5, sw: 0, depth: 1,
      requires: ["ModuleSystem"] },
    { name: "ModuleOpenTelemetry", asm: "Monica.OpenTelemetry", caps: ["Web"], cb: 54.8, sw: 12.4, depth: 2,
      requires: ["ModuleLogging"] },
    { name: "ModuleLocalization", asm: "Monica.Core", caps: ["Web"], cb: 47.0, sw: 0, depth: 1,
      requires: ["ModuleSystem"] },
    { name: "ModuleXmlDocumentation", asm: "Monica.Core", caps: ["Web"], cb: 39.6, sw: 0, depth: 1,
      requires: ["ModuleSystem"] },
    { name: "ModuleMediator", asm: "Monica.Core", caps: [], cb: 33.2, sw: 0, depth: 1,
      requires: ["ModuleSystem"] },
    { name: "ModuleAuthentication", asm: "Monica.Authority", caps: ["Web host"], cb: 28.4, sw: 0, depth: 2,
      requires: ["ModuleSystem"] },
    { name: "ModuleSignalR", asm: "Monica.SignalR", caps: ["Web host"], cb: 21.7, sw: 4.1, depth: 2,
      requires: ["ModuleAuthentication"] },
    { name: "ModuleLogging", asm: "Monica.Logging", caps: ["Web"], cb: 19.5, sw: 0, depth: 1,
      requires: ["ModuleSystem"] },
    { name: "ModuleSeederUI", asm: "Monica.Framework.UI", caps: ["UI"], cb: 14.6, sw: 0, depth: 5,
      requires: ["ModuleSeeder", "ModuleShellUI"] },
    { name: "ModuleStateStore", asm: "Monica.StateStore", caps: [], cb: 9.2, sw: 0, depth: 1,
      requires: ["ModuleSystem"] },
    { name: "ModuleDaprEventBus", asm: "Monica.Dapr", caps: [], cb: 0, sw: 0, depth: 3, disabled: true,
      requires: ["ModuleEventBus", "ModuleDaprClient"] }
  ],
  // Aggregate for the modules intentionally left out of this fixture.
  remainder: { count: 34, ms: 281.7 }
};
