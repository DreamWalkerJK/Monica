namespace Monica.UI.Shell.Support;

/// <summary>
/// Local SVG fragments for shell navigation and actions, rendered by MudIcon in its 24-unit view box.
/// Category contributions may continue to supply their own icons.
/// </summary>
internal static class ShellIcons
{
    private const string Start = "<g fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\">";
    private const string End = "</g>";

    internal const string Sparkle = Start + """<path d="m12 3 2.5 6.5L21 12l-6.5 2.5L12 21l-2.5-6.5L3 12l6.5-2.5Z"/>""" + End;

    internal const string BookSearch = Start + """<path d="M3 4c4-1 6 0 9 2 3-2 5-3 9-2v8M12 6v7M3 4v14c4-1 6 0 9 2"/><circle cx="17" cy="16" r="3"/><path d="m19.2 18.2 2.3 2.3"/>""" + End;

    internal const string Book = Start + """<path d="M12 6v14M3 4c4-1 6 0 9 2 3-2 5-3 9-2v14c-4-1-6 0-9 2-3-2-5-3-9-2Z"/>""" + End;

    internal const string Sliders = Start + """<path d="M4 7h1m6 0h9M4 17h9m6 0h1"/><circle cx="8" cy="7" r="3"/><circle cx="16" cy="17" r="3"/>""" + End;

    internal const string Server = Start + """<rect x="3" y="3" width="18" height="7" rx="2"/><rect x="3" y="14" width="18" height="7" rx="2"/><path d="M7 6.5h.01M7 17.5h.01M12 10v4"/>""" + End;

    internal const string Cube = Start + """<path d="m12 3 9 5-9 5-9-5ZM3 8v9l9 5 9-5V8M12 13v9"/>""" + End;

    internal const string CalendarCheck = Start + """<rect x="4" y="5" width="16" height="16" rx="2"/><path d="M8 3v4m8-4v4M4 11h16m-11 5 2 2 4-4"/>""" + End;

    internal const string Pulse = Start + """<path d="M3 12h4l3-8 4 16 3-8h4"/>""" + End;

    internal const string Bug = Start + """<rect x="8" y="8" width="8" height="10" rx="2"/><path d="M5 11h3m8 0h3M5 16h3m8 0h3M9 4l3 4 3-4M12 8v10"/>""" + End;

    internal const string Folder = Start + """<path d="M3 7V5a2 2 0 0 1 2-2h5l2 3h7a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2Z"/>""" + End;

    internal const string Menu = Start + """<path d="M4 6h16M4 12h16M4 18h16"/>""" + End;

    internal const string ChevronDown = Start + """<path d="m6 9 6 6 6-6"/>""" + End;

    internal const string ChevronRight = Start + """<path d="m9 5 7 7-7 7"/>""" + End;

    internal const string ChevronLeft = Start + """<path d="m15 5-7 7 7 7"/>""" + End;

    internal const string Close = Start + """<path d="m6 6 12 12M18 6 6 18"/>""" + End;

    internal const string CollapseSidebar = Start + """<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M9 4v16m7-12-4 4 4 4"/>""" + End;

    internal const string ExpandSidebar = Start + """<rect x="3" y="4" width="18" height="16" rx="2"/><path d="M9 4v16m3-12 4 4-4 4"/>""" + End;

    internal const string Search = Start + """<circle cx="10.5" cy="10.5" r="6.5"/><path d="m15.2 15.2 5.3 5.3"/>""" + End;

    internal const string Bell = Start + """<path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4"/>""" + End;

    internal const string Palette = Start + """<path d="M12 3a9 9 0 1 0 0 18h1a2 2 0 0 0 1.4-3.4l-.4-.4a1.9 1.9 0 0 1 1.4-3.2H18a3 3 0 0 0 3-3 8 8 0 0 0-9-8Z"/><circle cx="7.5" cy="10" r=".75"/><circle cx="10" cy="6.5" r=".75"/><circle cx="14.5" cy="6.5" r=".75"/><circle cx="17.5" cy="10" r=".75"/>""" + End;

    internal const string Globe = Start + """<circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3c5 5 5 13 0 18-5-5-5-13 0-18Z"/>""" + End;

    internal const string UserCircle = Start + """<circle cx="12" cy="12" r="9"/><circle cx="12" cy="9" r="2.5"/><path d="M6.5 19v-1.5a5.5 5.5 0 0 1 11 0V19"/>""" + End;

    internal const string User = Start + """<circle cx="12" cy="8" r="4"/><path d="M4 21v-2a8 8 0 0 1 16 0v2"/>""" + End;

    internal const string Logout = Start + """<path d="M9 4H5a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h4M9 12h12m-4-4 4 4-4 4"/>""" + End;

    internal const string Check = Start + """<path d="m5 12 4 4L19 6"/>""" + End;
}
