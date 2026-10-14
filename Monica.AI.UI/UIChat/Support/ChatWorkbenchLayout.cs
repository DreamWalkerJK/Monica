namespace Monica.AI.UI.UIChat.Support;

/// <summary>Reports available space owned by the conversation workbench, excluding the application shell.</summary>
internal sealed record ChatWorkbenchLayout(int OwnerWidth, int MainWidth, int Height, bool IsObserved = true);
