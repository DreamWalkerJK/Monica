# FileOps workspace and host policy

Use FileOps for bounded emergency filesystem access in a host. Its UI composes the real FileOps facade; browser capabilities are observations, while each service operation checks the current runtime policy. Register explicit existing roots and review the host's page and API authorization separately.

```csharp
builder.AddMonica(monica =>
{
    monica.AddFileOps(options => options.RuntimeConfig = new()
    {
        AllowedRoots = [dataRoot],
        StartupPath = dataRoot,
        ReadOnlyPaths = [readOnlyDirectory],
        AllowWriteOperations = true,
        AllowDeleteOperations = false
    });
    monica.AddFileOpsUI();
});
```

The host creates those directories before registration. `AddFileOpsUI` requires FileOps, contributes `FileOpsResource` localization and registers shell pages. Both FileOps destinations retain the original registration without a page-specific access-policy type. This does not automatically apply `OperationalPageAccessOption`; the FileOps UI introduces no separate authentication or role model. Host page access and underlying API authorization remain separate concerns. Full web hosts also use `app.UseMonica()` and `app.MapMonica()`.

## Files and policy are different tasks

`/ui/file-ops` retains the original file workspace destination. `/ui/file-ops/policy` is a sibling host-policy editor. They share one contextual route rail rather than stacked local tabs. The UI's return context contains only passive directory, ordering and checked-path identities within the current circuit. Returning to Files reloads policy and revalidates the remembered location; it does not resurrect a controller, task or text draft.

Browsing and text inspection remain one continuous task. Checked batch paths are independent of the inspected file. Select-all and client sorting operate on the accepted displayed subset, not an undisclosed complete directory. The service returns `IsTruncated` and `EntryLimit`, with no total count or paging contract. A file absent from that bounded subset is not necessarily deleted.

The file editor binds a full target identity, encoding, accepted disk metadata and local draft. Replacing a dirty draft requires an explicit native discard decision. Cancel preserves the draft and starts no mutation. Refreshing the current directory preserves an unrelated draft; deleting an unrelated entry does not clear the inspector. Route-owned work is canceled where supported and actually drained before retirement; injected facades and service scopes remain borrowed.

Save replaces the complete supplied text using the requested encoding. There is no expected-version check, atomic replacement, backup, history or rollback. The UI rereads the exact target after a completed save to obtain fresh metadata. A later read failure does not undo persistence or erase a newer draft; command outcome and refresh failure remain separate.

## Runtime policy is host-wide and in memory

The singleton runtime configuration store returns detached normalized snapshots. Saving replaces the host-wide in-memory policy; it is neither persisted nor user-specific. Opening, editing or canceling a UI draft does not change it. Restart restores the host's configured starting policy.

An empty or entirely invalid allowed-root list normalizes to the working directory, executable base directory and process temporary directory. It does **not** deny all access. Supply explicit roots when a bounded scope is intended. An invalid, unavailable or out-of-scope startup path falls back to the first normalized root. Read-only paths outside allowed roots are discarded during normalization.

Defaults and normalization are owned by `FileOpsRuntimeConfig`:

| Setting | Default | Normalization |
| --- | --- | --- |
| Write operations | Enabled | Current policy remains authoritative |
| Delete operations | Disabled | Independent of write enablement |
| Hidden entries | Hidden | Dot-prefixed names and hidden attributes are excluded |
| Text size | 512 KiB | At least 4 KiB |
| Upload size | 20 MiB | At least 64 KiB |
| Per-file download size | 100 MiB | At least the normalized upload size |
| Displayed directory entries | 500 | Between 50 and 5,000 |

Read-only means configured path ancestry, rather than an OS permission probe. Allowed-root and read-only checks use lexical paths and OS-specific path identity. They do not establish physical symlink or junction containment. Allowed roots themselves cannot be deleted. Preview accepts decoded bounded text independently of the editable-extension allowlist; saving additionally requires an editable extension and writable target.

## Transfers and destructive results

The UI uploads one file and retains same-name replacement (`overwrite: true`). The final target's read-only policy is checked before it is opened, including an existing protected file under a writable parent. Successful upload metadata is sampled after destination stream closure. The service checks declared upload length; copy failure or cancellation can leave partial content. No rollback, resume or browser progress contract exists.

Single downloads return caller-owned streams. ZIP downloads recursively include visible selected descendants, preserve empty directories and disambiguate duplicate archive names. Policy limits each file; there is no total archive-size limit or recursive entry-count limit. The successful temporary archive stream uses delete-on-close ownership. Dispose every successful descriptor stream even if its UI visit retired before the producer returned.

Browser transfer interop retains the descriptor stream until `arrayBuffer()` finishes, then releases its stream reference and the caller closes the descriptor. A successful UI outcome means download initiation, not confirmation that the browser persisted the file.

Batch deletion uses one policy snapshot, deduplicates target paths in their original order, and deletes sequentially. It stops at the first exception. Earlier deletions remain applied, while a failed facade result carries no partial-success list. Refresh the captured directory after an attempted mutation, including failure, without inventing an exact partial count. Native destructive confirmation displays the full frozen targets; selection changes cannot retarget it.

## Developer ownership

`IFileOpsWorkspaceService` and `IFileOpsTransferService` expose the actual scoped filesystem and transfer boundaries. The module aliases them to its existing scoped concrete services, preserving one instance per scope. `FileOpsFacade` borrows these interfaces and projects exceptions into `Res` envelopes; controlled behavior tests can replace these external boundaries while retaining the real facade. Direct facade construction now supplies those interface contracts; existing concrete implementations implement them.

The workspace controller belongs to one routed visit. Native dialog ownership must observe late `ShowAsync` references, close them on retirement and join the actual result and close dispatch. Retire dialog owners before waiting for work that awaits their result. Download interop owns its imported module and `DotNetStreamReference`, while the descriptor's stream belongs to the invoking operation. Cancellation alone is not drainage; once a UI mutation starts, the visit joins it without promising cancellation or rollback.

Reserve the Files visit's New folder opener before Create admission publishes Busy. Keep only that native button focusable through its owned name-dialog/result chain, expose temporary unavailability with `aria-disabled`, and reject duplicate activation in both location and visit handlers. Other busy operations and missing Create capability retain native disabling. Release the reservation after the chain drains; route retirement still closes/drains native ownership and rejects late name adoption. MudBlazor restores focus on Cancel and Escape, without a second JavaScript focus owner.
