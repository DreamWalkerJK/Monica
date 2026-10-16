/* Isolated fixtures. This prototype never calls a real StateStore service. */
window.storeFixtures = {
  providers: [
    {id:"memory",name:"默认",type:"Memory",scope:"本地内存",tone:"info",icon:"memory",serviceKey:null,isDefault:true,scan:true,ttl:true,implementation:"Monica.StateStore.Providers.Memory.MemoryCacheProvider",description:"当前进程内的状态与短期缓存",options:{"UseDistributedProviderAsDefault":"false"}},
    {id:"redis",name:"flight-cache",type:"Redis",scope:"分布式",tone:"primary",icon:"database",serviceKey:"flight-cache",isDefault:false,scan:true,ttl:true,implementation:"Monica.StateStore.StackExchange.RedisStateStore",description:"航班、航路与运行时快照",options:{"DatabaseIndex":"0","KeyPrefix":"fips:dev:","DefaultTTL":"未设置"}},
    {id:"dapr",name:"workflow-state",type:"Dapr",scope:"分布式",tone:"secondary",icon:"layers",serviceKey:"workflow-state",isDefault:false,scan:false,ttl:false,implementation:"Monica.Dapr.Services.DaprStateStoreProvider",description:"工作流持久状态，通过完整 Key 查询",options:null}
  ],
  keys: {}
};
const routeFixtures = [
  ["CA1831","ZBAA","ZSSS","B738","B-7892"],
  ["MU5102","ZBAA","ZSSS","A333","B-6085"],
  ["CZ3102","ZBAA","ZGGG","A321","B-8910"],
  ["HU7601","ZBAA","ZSSS","B738","B-1543"],
  ["CA1501","ZBAA","ZSPD","A359","B-1085"],
  ["MU5129","ZSPD","ZBAA","A320","B-6632"],
  ["3U8835","ZUUU","ZBAA","A321","B-1838"],
  ["ZH9101","ZGSZ","ZBAA","B738","B-5373"]
];
window.storeFixtures.keys.redis = [
  ...routeFixtures.map(function(r,i){
    return {key:"Flight:Route:"+r[0]+":20261016",raw:JSON.stringify({
      flightNumber:r[0],flightDate:"2026-10-16",departure:r[1],arrival:r[2],
      aircraft:{type:r[3],registration:r[4]},
      route:{name:"B458",waypoints:["ELKUR","BIVOR","DALIM"]},
      status:"Scheduled",version:19+i
    },null,2),etag:String(19+i),ttl:3272+i*180};
  }),
  ...["ZBAA","ZSSS","ZSPD","ZGGG","ZGSZ"].map(function(id,i){
    return {key:"Airport:Profile:"+id,raw:JSON.stringify({icao:id,enabled:true,runways:i+2,timezone:"Asia/Shanghai"},null,2),etag:"8",ttl:null};
  }),
  ...["dispatch:overview","flight:today","route:active","airport:summary"].map(function(id,i){
    return {key:"Dashboard:Snapshot:"+id,raw:JSON.stringify({name:id,updatedAt:"2026-10-16T10:32:08+08:00",count:128+i*35},null,2),etag:String(i+1),ttl:1800};
  }),
  ...["dispatcher-01","dispatcher-02","viewer-01"].map(function(id,i){
    return {key:"Session:User:"+id,raw:JSON.stringify({role:i===2?"viewer":"dispatcher",workspace:"operations"},null,2),etag:null,ttl:3600};
  }),
  {key:"Configuration:Feature:route-validation",raw:"true",etag:"3",ttl:null},
  {key:"Configuration:Feature:flight-import",raw:"enabled",etag:null,ttl:null},
  {key:"Lock:Job:daily-flight-sync",raw:"scheduler-instance-01",etag:null,ttl:120},
  {key:"Migration:State:2026-10",raw:JSON.stringify({version:"2026.10",completed:true},null,2),etag:"1",ttl:null}
];
window.storeFixtures.keys.memory = [
  {key:"Runtime:Module:StateStore",raw:JSON.stringify({name:"StateStore",initialized:true,providerCount:3},null,2),etag:null,ttl:null},
  {key:"Runtime:Culture:current",raw:"zh-CN",etag:null,ttl:null},
  {key:"Cache:Navigation:main",raw:JSON.stringify({items:["存储提供者","数据浏览器"],version:2},null,2),etag:null,ttl:600},
  {key:"Cache:Theme:default",raw:JSON.stringify({theme:"Default",mode:"dark"},null,2),etag:null,ttl:1800}
];
window.storeFixtures.keys.dapr = [
  {key:"Workflow:Instance:demo-001",raw:JSON.stringify({id:"demo-001",state:"Running",currentStep:"ValidateFlight",version:7},null,2),etag:"7",ttl:undefined}
];
