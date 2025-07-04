using Serenity.Web;

namespace Serenity.Localization;

/// <summary>
/// Contains initialization method for adding local text keys implicitly defined by
/// DisplayName, Tab, Placeholder, Hint etc. attributes used in Form definitions
/// </summary>
public static class PropertyItemsLocalTextRegistration
{
    private static readonly Regex LocalTextKeyLike = new(@"^([A-Z][A-za-z0-9_]*\.)+[A-Z][A-za-z0-9_]*$", RegexOptions.Compiled);

    /// <summary>
    /// Adds local text translations defined implicitly by DisplayName, Tab, Placeholder,
    /// Hint etc. attributes used in Column/Form etc. definitions.
    /// </summary>
    /// <param name="typeSource">Type source to search for enumeration classes in</param>
    /// <param name="languageID">Language ID texts will be added (default is invariant language)</param>
    /// <param name="registry">Registry</param>
    public static void AddPropertyItemsTexts(this ILocalTextRegistry registry, ITypeSource typeSource,
        string languageID = LocalText.InvariantLanguageID)
    {
        if (typeSource == null)
            throw new ArgumentNullException(nameof(typeSource));

        if (registry is null)
            throw new ArgumentNullException(nameof(registry));

        foreach (var type in typeSource.GetTypes())
        {
            if (GetPropertyItemsTextPrefix(type) is not string textPrefix)
                continue;

            void addText(string text, string? suffix)
            {
                if (string.IsNullOrEmpty(text))
                    return;

                if (IsLocalTextKeyCandidate(text))
                {
                    if (registry.TryGet(languageID, text, false) is null)
                        registry.Add(languageID, text, null);
                }
                else if (suffix is not null)
                    registry.Add(languageID, textPrefix + suffix, text);
            }

            var addonParams = new Dictionary<string, object?>(StringComparer.Ordinal);

            foreach (var member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public))
            {
                if (member.GetCustomAttribute<CategoryAttribute>()?.Category is string category)
                    addText(category, "Categories." + category);

                if (member.GetCustomAttribute<TabAttribute>()?.Value is string tab)
                    addText(tab, "Tabs." + tab);

                if (member.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName is string displayName)
                    addText(displayName, member.Name);

                if (member.GetCustomAttribute<HintAttribute>()?.Hint is string hint)
                    addText(hint, member.Name + "_Hint");

                if (member.GetCustomAttribute<PlaceholderAttribute>()?.Value is string placeholder)
                    addText(placeholder, member.Name + "_Placeholder");

                foreach (var addonAttr in member.GetCustomAttributes<EditorAddonAttribute>())
                {
                    addonParams.Clear();
                    addonAttr.SetParams(addonParams);
                    foreach (var pair in addonParams)
                    {
                        if (pair.Value is string s &&
                            addonAttr.IsLocalizableOption(pair.Key))
                        {
                            addText(s, suffix: null);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets form/column local text key prefix for given type
    /// </summary>
    /// <param name="type">Type with form/column attribute</param>
    public static string? GetPropertyItemsTextPrefix(Type type)
    {
        var formAttr = type.GetAttribute<FormScriptAttribute>();
        string? itemsKey;
        if (formAttr is null)
        {
            var columnsAttr = type.GetAttribute<ColumnsScriptAttribute>();
            if (columnsAttr is null)
                return null;

            if (columnsAttr.LocalTextPrefix is not null)
                return columnsAttr.LocalTextPrefix;

            itemsKey = columnsAttr.Key;
        }
        else if (formAttr.LocalTextPrefix is not null)
            return formAttr.LocalTextPrefix;
        else
            itemsKey = formAttr.Key;

        if (string.IsNullOrEmpty(itemsKey))
            itemsKey = type.FullName;

        return (formAttr is null ? "Columns." : "Forms.") + itemsKey + ".";
    }

    /// <summary>
    /// Returns true if the text value can be a local text key
    /// that could be passed to the client side
    /// </summary>
    /// <param name="text">Key or text</param>
    /// <returns></returns>
    public static bool IsLocalTextKeyCandidate(string text)
    {
        return !string.IsNullOrEmpty(text) &&
            LocalTextKeyLike.IsMatch(text) &&
            LocalTextPackages.DefaultSitePackageIncludes.IsMatch(text);
    }
}