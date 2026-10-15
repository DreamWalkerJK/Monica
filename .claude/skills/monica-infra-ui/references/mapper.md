# Inspect registered object mappings

Use the Mapper workspace to find a registered source/destination pair and read the C# expression generated for that pair in the current host. For host composition, navigation and access policy, follow [shell and access](shell-and-access.md).

```csharp
builder.AddMonica(monica =>
{
    monica.AddObjectMapping().AddProfile<ApplicationMappingProfile>();
    monica.AddMapperUI();
});
```

`ApplicationMappingProfile` is the application's parameterless Mapster `IRegister` implementation. Explicit registration is one way to contribute mappings; the ObjectMapping module also discovers eligible profiles through its configured business-type discovery. `AddMapperUI()` requires ObjectMapping, contributes `MapperResource`, enables the shell's Markdown support, and registers `/mapper-debug` in the Debug navigation category. The UI does not require a separate diagnostic HTTP endpoint. Normal host and shell authorization still apply.

The workspace reads the real `ObjectMappingFacade.GetStatusAsync()` result. Its registry enumerates the host configuration's registered rule-map pairs; it is not a census of every conversion Mapster could create by convention. The inspector clones the configuration and generates each expression without mapping an operator-supplied object. Registry presence or successful expression generation does not establish eager compilation success, host readiness, or correctness for every input. One expression-generation failure fails the whole read rather than returning partial rows.

Search matches the full returned source and destination names. Select a pair to read its complete generated expression. Type names are readable qualified names rather than assembly identities or stable cross-read keys. The UI keeps selection local to the adopted read and does not match a new read by name.

A successful Refresh replaces the entire registry and clears selection. A failed Refresh retains the previous registry and expression, with a persistent failure and previous-read context. A failed first read is unavailable evidence; a successful empty read is a genuine zero-entry registry. Displayed read times are local UI receipt times, not backend capture timestamps. Search and line-wrapping preferences survive Refresh; changing either does not regenerate expressions.

The catalog has one bounded entry-list scroll owner. A compact chooser closes after selection and reveals the full expression in ordinary shell flow; the code has no separate fixed vertical cap. Wrapping is initially enabled. When disabled, long lines scroll horizontally inside the code owner. Generated code remains encoded and complete. Native text selection is available; this workspace does not execute, edit or save an expression.

The page owns detached read records and any geometry/focus work for its visit; it borrows the scoped facade. The facade's task signature does not provide cancellation or background progress. Do not manufacture either by wrapping the synchronous inspection in a background task. Leaving the page retires its UI work, and returning performs a new read.

To diagnose a missing pair, inspect actual profile registration and business-type discovery first. To diagnose a failed read, preserve the reported exception and inspect the implicated Mapster rule; an empty catalog is not a substitute for failed evidence. Do not infer a profile count, history, assembly provenance or per-pair failure status from fields that the facade does not expose.
