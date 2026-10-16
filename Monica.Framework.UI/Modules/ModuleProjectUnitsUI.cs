using Microsoft.Extensions.DependencyInjection;
using Monica.Core;
using Monica.Core.Modularity;
using Monica.Core.Modularity.Abstractions;
using Monica.Framework.UI.Localization;
using Monica.Framework.UI.Pages;
using Monica.Framework.UI.UIProjectUnits.Services;
using Monica.UI.Shell.Models;
using Monica.UI.Icons;
using MudBlazor;

// ReSharper disable once CheckNamespace
namespace Monica.Modules;

public static class ModuleProjectUnitsUIBuilderExtensions
{
    extension(IMonicaBuilder builder)
    {
        /// <summary>
        /// Registers the project-units UI module.
        /// </summary>
        public ModuleRegistration<ModuleProjectUnitsUI, ModuleProjectUnitsUIOption> AddProjectUnitsUI(
            Action<ModuleProjectUnitsUIOption>? action = null)
        {
            return builder.AddModule<ModuleProjectUnitsUI, ModuleProjectUnitsUIOption>(action);
        }
    }
}

/// <summary>
/// Project-units UI module.
/// </summary>
public class ModuleProjectUnitsUI : MonicaModule<ModuleProjectUnitsUIOption>, IUIModule
{
    /// <inheritdoc />
    public override void Describe(ModuleDescriptor module)
    {
        module.Require<ModuleProjectUnits, ModuleProjectUnitsOption>();
        module.Require<ModuleLocalization, ModuleLocalizationOption>(
            static option => option.AddResource<ProjectUnitsResource>());
        module.Require<ModuleShellUI, ModuleShellUIOption>(static option =>
            option.ConfigureNavigation(static registry =>
            {
                var category = registry.RegisterLocalizedCategory<ProjectUnitsResource>(
                    "Monica.Framework.UI.ProjectUnits",
                    "Pages:ProjectUnits:Title",
                    order: 20,
                    parentId: BuiltInNavigationCategoryIds.Monitor,
                    icon: MoIcons.Workflow,
                    descriptionKey: "Pages:ProjectUnits:Description");

                registry.RegisterLocalizedPage<UIProjectUnitsPage, ProjectUnitsResource>(
                    UIProjectUnitsPage.PAGE_URL,
                    "Pages:ProjectUnits:Overview:Title",
                    MoIcons.Dashboard,
                    category,
                    addToNav: true,
                    navOrder: 0,
                    descriptionKey: "Pages:ProjectUnits:Overview:Description");
                registry.RegisterLocalizedPage<UIProjectUnitsCatalogPage, ProjectUnitsResource>(
                    UIProjectUnitsCatalogPage.PAGE_URL,
                    "Pages:ProjectUnits:Catalog:Title",
                    MoIcons.Workflow,
                    category,
                    addToNav: true,
                    navOrder: 10,
                    descriptionKey: "Pages:ProjectUnits:Catalog:Description");
                registry.RegisterLocalizedPage<UIProjectUnitsGraphPage, ProjectUnitsResource>(
                    UIProjectUnitsGraphPage.PAGE_URL,
                    "Pages:ProjectUnits:Graph:Title",
                    MoIcons.Network,
                    category,
                    addToNav: true,
                    navOrder: 20,
                    descriptionKey: "Pages:ProjectUnits:Graph:Description");
                registry.RegisterLocalizedPage<UIProjectUnitsEventsPage, ProjectUnitsResource>(
                    UIProjectUnitsEventsPage.PAGE_URL,
                    "Pages:ProjectUnits:Events:Title",
                    MoIcons.Calendar,
                    category,
                    addToNav: true,
                    navOrder: 30,
                    descriptionKey: "Pages:ProjectUnits:Events:Description");
                registry.RegisterLocalizedPage<UIProjectUnitsRequestFiltersPage, ProjectUnitsResource>(
                    UIProjectUnitsRequestFiltersPage.PAGE_URL,
                    "Pages:ProjectUnits:RequestFilters:Title",
                    MoIcons.Filter,
                    category,
                    addToNav: true,
                    navOrder: 40,
                    descriptionKey: "Pages:ProjectUnits:RequestFilters:Description");
                registry.RegisterLocalizedPage<UIProjectUnitsEnumsPage, ProjectUnitsResource>(
                    UIProjectUnitsEnumsPage.PAGE_URL,
                    "Pages:ProjectUnits:Enums:Title",
                    MoIcons.Braces,
                    category,
                    addToNav: true,
                    navOrder: 50,
                    descriptionKey: "Pages:ProjectUnits:Enums:Description");
            }));
    }

    public override void ConfigureServices(ModuleContext<ModuleProjectUnitsUIOption> context)
    {
        context.Services.AddScoped<IProjectUnitsUiDataSource, ProjectUnitsUiDataSource>();
    }
}

/// <summary>
/// Project-units UI module options.
/// </summary>
public class ModuleProjectUnitsUIOption : ModuleOptions<ModuleProjectUnitsUI>
{
}
