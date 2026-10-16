# MoContextMenu 右键菜单组件

使用方式、坐标约定、`OpeningRevision`、回调顺序及原生键盘行为统一维护在 [共享右键菜单契约](../../../../skills/monica-infra-ui/references/shell-and-access.md#use-shared-cursor-menus)。组件命名空间为 `Monica.UI.Shared.Components.ContextMenu`。

调用方应传入 `MouseEventArgs.PageX` / `PageY`，在每次接受右键事件时递增 `OpeningRevision`，并在 `OnClose` 中清除 `Visible`。快捷键文字仅是展示提示，不会注册键盘绑定。重要操作仍需提供可通过键盘访问的常规入口。

异步生命周期与原生菜单所有权参见 [Blazor 开发约定](../../../../skills/monica-ui-development/references/blazor-best-practices.md)，布局、焦点和主题检查参见 [UI 审核分类](../../../../skills/monica-ui-audit/references/audit-categories.md)。
