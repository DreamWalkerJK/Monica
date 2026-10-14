namespace Monica.Profiling.TypeAllocation.Models;

/// <summary>
/// Source-specific count and byte evidence grouped by full type name.
/// </summary>
public class TypeAllocationInfo
{
    private long _allocationCount;
    private long _totalBytes;

    /// <summary>
    /// Full type name (e.g. "System.String", "MyNamespace.MyClass")
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Short display preview (e.g. "String", "MyClass"). Different full identities can share this preview.
    /// </summary>
    public string ShortTypeName => GetShortTypeName(TypeName);

    /// <summary>
    /// Observed ETW tick count or enumerated heap object count, according to the snapshot data source.
    /// </summary>
    public long AllocationCount
    {
        get => Interlocked.Read(ref _allocationCount);
        set => Interlocked.Exchange(ref _allocationCount, value);
    }

    /// <summary>
    /// ETW tick allocation volume attributed to this threshold-crossing type, or enumerated heap
    /// object bytes. ETW attribution is sampled evidence, not an exact per-type allocation total.
    /// </summary>
    public long TotalBytes
    {
        get => Interlocked.Read(ref _totalBytes);
        set => Interlocked.Exchange(ref _totalBytes, value);
    }

    /// <summary>
    /// Atomically adds to the source-specific count.
    /// </summary>
    public void AddAllocationCount(long value) => Interlocked.Add(ref _allocationCount, value);

    /// <summary>
    /// Atomically adds to the source-specific byte total.
    /// </summary>
    public void AddTotalBytes(long value) => Interlocked.Add(ref _totalBytes, value);

    /// <summary>
    /// Byte total divided by source-specific count. This is a heap object average only for heap
    /// evidence; for ETW it is attributed bytes per tick, not average allocated-object size.
    /// </summary>
    public double AverageSize => AllocationCount > 0 ? (double)TotalBytes / AllocationCount : 0;

    /// <summary>
    /// Percentage of the snapshot's complete source count: accepted ETW tick events or enumerated
    /// heap objects. Retained ETW rows exclude evicted ranking entries, so their shares may sum below 100.
    /// </summary>
    public double AllocationPercentage { get; set; }

    /// <summary>
    /// Percentage of the snapshot's complete source byte total, rather than a filtered or paged row total.
    /// ETW attribution is sampled volume; retained ranking shares are not a complete allocation distribution.
    /// </summary>
    public double BytesPercentage { get; set; }

    /// <summary>
    /// Legacy aggregate-average threshold heuristic. It does not prove an object's GC heap kind;
    /// in particular it must not classify ETW ticks as LOH allocations.
    /// </summary>
    public bool IsLargeObjectHeap => AverageSize >= 85000;

    /// <summary>
    /// Provider observation time in UTC. ETW records this type's latest accepted tick timestamp;
    /// heap census rows use enumeration completion time. It does not establish object allocation time or lifetime.
    /// </summary>
    public DateTime LastSeenUtc { get; set; }

    private static string GetShortTypeName(string fullName)
    {
        var suffixStart = FindTypeSuffixStart(fullName);
        if (suffixStart > 0 && suffixStart < fullName.Length)
        {
            return GetShortTypeName(fullName[..suffixStart]) + fullName[suffixStart..];
        }

        var nestedSeparator = FindNestedSeparator(fullName);
        if (nestedSeparator > 0 && nestedSeparator < fullName.Length - 1)
        {
            return GetShortTypeName(fullName[..nestedSeparator]) + "+" + GetShortTypeName(fullName[(nestedSeparator + 1)..]);
        }

        // Handle C#-style generic types (e.g., HashSet<Namespace.Type>)
        // Preserve generated <> prefixes while distinguishing their later generic-argument brackets.
        var angleBracketIndex = fullName.IndexOf('<');
        if (angleBracketIndex >= 0 && angleBracketIndex + 1 < fullName.Length && fullName[angleBracketIndex + 1] == '>')
        {
            angleBracketIndex = fullName.IndexOf('<', angleBracketIndex + 2);
        }
        if (angleBracketIndex > 0 && fullName[angleBracketIndex - 1] is not ('.' or '+'))
        {
            // Verify it's a real generic by checking for matching closing bracket
            var closingIndex = FindMatchingClosingBracket(fullName, angleBracketIndex, '<', '>');
            if (closingIndex == fullName.Length - 1)
            {
                // Get base type name before '<'
                var basePart = fullName[..angleBracketIndex];
                var lastDot = basePart.LastIndexOf('.');
                var shortBase = lastDot >= 0 ? basePart[(lastDot + 1)..] : basePart;

                // Process generic arguments recursively
                var genericArgs = fullName[(angleBracketIndex + 1)..^1]; // Content inside <>
                var shortArgs = ShortenGenericArguments(genericArgs);

                return $"{shortBase}<{shortArgs}>";
            }
        }

        // Handle CLR-style generic types with backtick (e.g., Dictionary`2[[...]])
        var genericIndex = fullName.IndexOf('`');
        if (genericIndex >= 0)
        {
            var baseName = fullName[..genericIndex];
            var lastDot = baseName.LastIndexOf('.');
            var shortBase = lastDot >= 0 ? baseName[(lastDot + 1)..] : baseName;

            var bracketStart = fullName.IndexOf('[', genericIndex);
            if (bracketStart >= 0)
            {
                return shortBase + fullName[bracketStart..];
            }
            return shortBase + fullName[genericIndex..];
        }

        // Handle square bracket generics without backtick (e.g., Tables[System.IntPtr,SkiaSharp.SKObject])
        var squareBracketIndex = fullName.IndexOf('[');
        if (squareBracketIndex > 0 && fullName.EndsWith(']'))
        {
            var basePart = fullName[..squareBracketIndex];
            var lastDot = basePart.LastIndexOf('.');
            var shortBase = lastDot >= 0 ? basePart[(lastDot + 1)..] : basePart;

            var argsContent = fullName[(squareBracketIndex + 1)..^1]; // Content inside []
            var shortArgs = ShortenSquareBracketArguments(argsContent);

            return $"{shortBase}[{shortArgs}]";
        }

        // Handle non-generic types
        var dot = fullName.LastIndexOf('.');
        return dot >= 0 ? fullName[(dot + 1)..] : fullName;
    }

    private static int FindTypeSuffixStart(string fullName)
    {
        var end = fullName.Length;
        while (end > 0)
        {
            if (fullName[end - 1] is '*' or '&')
            {
                end--;
                continue;
            }
            if (fullName[end - 1] != ']') break;
            var opening = fullName.LastIndexOf('[', end - 1);
            if (opening < 0) break;
            // Only array qualifiers are suffixes; square-bracket type arguments remain intact.
            var rankOnly = true;
            for (var i = opening + 1; i < end - 1; i++)
            {
                if (fullName[i] == ',' || (i == opening + 1 && i == end - 2 && fullName[i] == '*')) continue;
                rankOnly = false;
                break;
            }
            if (!rankOnly) break;
            end = opening;
        }
        return end;
    }

    private static int FindNestedSeparator(string fullName)
    {
        var angleDepth = 0;
        var squareDepth = 0;
        for (var i = 0; i < fullName.Length; i++)
        {
            switch (fullName[i])
            {
                case '<': angleDepth++; break;
                case '>': angleDepth--; break;
                case '[': squareDepth++; break;
                case ']': squareDepth--; break;
                case '+' when angleDepth == 0 && squareDepth == 0: return i;
            }
        }
        return -1;
    }

    private static int FindMatchingClosingBracket(string text, int openIndex, char open, char close)
    {
        var depth = 1;
        for (var i = openIndex + 1; i < text.Length; i++)
        {
            if (text[i] == open) depth++;
            else if (text[i] == close)
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private static string ShortenGenericArguments(string args)
    {
        var result = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < args.Length; i++)
        {
            var c = args[i];
            if (c == '<' || c == '[') depth++;
            else if (c == '>' || c == ']') depth--;
            else if (c == ',' && depth == 0)
            {
                result.Add(GetShortTypeName(args[start..i].Trim()));
                start = i + 1;
            }
        }

        // Add the last argument
        if (start < args.Length)
        {
            result.Add(GetShortTypeName(args[start..].Trim()));
        }

        return string.Join(", ", result);
    }

    private static string ShortenSquareBracketArguments(string args)
    {
        // Handle nested CLR format: [[Type1],[Type2]] or [[Type1, Assembly],[Type2, Assembly]]
        if (args.StartsWith('['))
        {
            var result = new List<string>();
            var depth = 0;
            var start = -1;

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == '[')
                {
                    if (depth == 0) start = i + 1;
                    depth++;
                }
                else if (args[i] == ']')
                {
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        var typeName = args[start..i];
                        // Remove assembly info if present (after comma not inside brackets)
                        var commaDepth = 0;
                        for (var j = 0; j < typeName.Length; j++)
                        {
                            if (typeName[j] == '[') commaDepth++;
                            else if (typeName[j] == ']') commaDepth--;
                            else if (typeName[j] == ',' && commaDepth == 0)
                            {
                                typeName = typeName[..j].Trim();
                                break;
                            }
                        }
                        result.Add(GetShortTypeName(typeName));
                        start = -1;
                    }
                }
            }

            return string.Join(", ", result);
        }

        // Simple comma-separated format: Type1,Type2
        return ShortenGenericArguments(args);
    }
}
