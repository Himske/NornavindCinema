using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NornavindCinema.Data;
using NornavindCinema.ViewModels;

namespace NornavindCinema.Controllers {
    [Authorize(Policy = "Administrator")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class ApiConfigurationController(ApiCredentialStore credentials) : Controller {
        public async Task<IActionResult> Index(CancellationToken cancellationToken) {
            var model = new ApiConfigurationViewModel();
            await PopulateStatusAsync(model, cancellationToken);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ApiConfigurationViewModel model, CancellationToken cancellationToken) {
            if (!ModelState.IsValid) {
                await PopulateStatusAsync(model, cancellationToken);
                return View(model);
            }

            var changes = new Dictionary<string, string?>();
            AddChange(changes, ApiCredentialStore.OmdbApiKey, model.OmdbApiKey, model.ClearOmdbApiKey);
            AddChange(changes, ApiCredentialStore.TmdbApiKey, model.TmdbApiKey, model.ClearTmdbApiKey);
            AddChange(changes, ApiCredentialStore.TmdbReadAccessToken, model.TmdbReadAccessToken, model.ClearTmdbReadAccessToken);
            await credentials.SaveAsync(changes, cancellationToken);
            TempData["ConfigurationSaved"] = "API configuration saved.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateStatusAsync(ApiConfigurationViewModel model, CancellationToken cancellationToken) {
            var names = await credentials.GetConfiguredNamesAsync(cancellationToken);
            model.HasOmdbApiKey = names.Contains(ApiCredentialStore.OmdbApiKey);
            model.HasTmdbApiKey = names.Contains(ApiCredentialStore.TmdbApiKey);
            model.HasTmdbReadAccessToken = names.Contains(ApiCredentialStore.TmdbReadAccessToken);
        }

        private static void AddChange(Dictionary<string, string?> changes, string name, string? value, bool clear) {
            if (clear) {
                changes[name] = null;
            }
            else if (!string.IsNullOrWhiteSpace(value)) {
                changes[name] = value.Trim();
            }
        }
    }
}
