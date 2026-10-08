# ProjectUnit Context Metadata

Use this reference before creating or changing any discovered ProjectUnit. The annotations form the agent-readable architecture catalog shown by Monica's status dashboard and typed APIs.

## Required Decisions

Resolve these facts from repository conventions and source requirements before writing code:

- `$Owner$`: the team, role, or capability accountable for the unit.
- `$RequirementId$`: a stable requirement identifier owned by the application or its workflow system.
- `$SubdomainTag$`: the bounded context or subdomain tag.
- `$FeatureTag$`: the capability or feature tag.
- `$Title$` and `$Description$`: a concise title and one-sentence responsibility specific to this unit, in the catalog language below.

If any fact cannot be discovered, ask one focused question. Do not invent ownership or requirement IDs.

## Catalog Language

Resolve the application's configured workspace language before writing human-readable metadata. It governs the title and `Description` even though the values are C# strings: `zh-CN` uses Chinese and `en-US` uses English. For Monica Workflow, read `language` in `.workflow/workspace.yml` or the resolved workspace context. If no workspace language is configured, follow the application's existing catalog convention. English requirements for conversation, code comments, XML documentation or framework guidance do not override the application catalog language.

Keep `Owner`, tags, requirement IDs, namespaces and CLR type names stable. Localize the human title and responsibility without translating these identities.

## Annotation Pattern

```csharp
using Monica.ProjectUnits.Annotations;

[ProjectUnitMetadata(
    "$Title$",
    Owner = "$Owner$",
    Description = "$Description$",
    Tags = ["$SubdomainTag$", "$FeatureTag$"])]
[ProjectUnitRequirement("$RequirementId$")]
public sealed class CommandHandlerApproveOrder : ApplicationService<CommandApproveOrder>
{
    // Implementation omitted.
}
```

Apply the attributes directly to every discovered class or record. Metadata does not flow from a base class. Add another `[ProjectUnitRequirement("...")]` for each independently traceable requirement.

## Normalization And Diagnostics

- Monica trims titles, owners, descriptions, tags, and requirement IDs.
- Duplicate tags and requirement IDs are removed case-insensitively.
- Monica does not validate a project-specific requirement ID pattern.
- Missing annotations are coverage debt, not startup failures.
- Explicit but empty titles, owners, tags, or requirement IDs produce catalog warnings.
- Keep titles concise because Serilog source enrichment and management UI use the normalized title.

## Requirement Navigation

Requirement links are host-owned. A host may implement `IProjectUnitRequirementLinkResolver` and register it with `UseRequirementLinkResolver<TResolver>()`. The resolver runs only when detail is requested; unresolved IDs remain visible and non-clickable.

Do not put paths or URLs into the annotation. Store stable IDs and let the host resolver map them to the current documentation system.
