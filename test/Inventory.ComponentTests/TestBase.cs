using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Bunit;
using Inventory.Shared.Interfaces;
using Inventory.Shared.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Inventory.ComponentTests;

public abstract class ComponentTestBase : TestContext
{
    protected ComponentTestBase()
    {
        Services.AddSingleton<ICultureService>(new FakeCultureService());
        Services.AddSingleton<IStringLocalizer<SharedResources>>(new FakeStringLocalizer());
    }

    protected void AddSingletonService<TService>(TService service) where TService : class
        => Services.AddSingleton(service);

    private sealed class FakeCultureService : ICultureService
    {
        private CultureInfo _currentCulture = CultureInfo.InvariantCulture;

        public CultureInfo CurrentCulture => _currentCulture;

        public CultureInfo CurrentUICulture => _currentCulture;

        public event EventHandler<CultureInfo>? CultureChanged;

        public Task<string> GetPreferredCultureAsync() => Task.FromResult(CurrentCulture.Name);

        public IEnumerable<CultureInfo> GetSupportedCultures() => new[] { new CultureInfo("en-US"), new CultureInfo("ru-RU") };

        public Task SetCultureAsync(string culture)
        {
            var newCulture = new CultureInfo(culture);
            _currentCulture = newCulture;
            CultureChanged?.Invoke(this, newCulture);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStringLocalizer : IStringLocalizer<SharedResources>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(CultureInfo culture) => this;
    }
}
