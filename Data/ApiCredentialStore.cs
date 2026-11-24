using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using NornavindCinema.Models;

namespace NornavindCinema.Data {
    public class ApiCredentialStore(ApplicationDbContext context, IDataProtectionProvider protectionProvider) {
        public const string OmdbApiKey = "OmdbApiKey";
        public const string TmdbApiKey = "TmdbApiKey";
        public const string TmdbReadAccessToken = "TmdbReadAccessToken";

        private static readonly string[] Names = [OmdbApiKey, TmdbApiKey, TmdbReadAccessToken];

        public async Task<HashSet<string>> GetConfiguredNamesAsync(CancellationToken cancellationToken = default) {
            var names = await context.ApiCredentials.AsNoTracking()
                .Where(credential => Names.Contains(credential.Name))
                .Select(credential => credential.Name)
                .ToListAsync(cancellationToken);
            return names.ToHashSet(StringComparer.Ordinal);
        }

        public async Task<string?> GetAsync(string name, CancellationToken cancellationToken = default) {
            ValidateName(name);
            var protectedValue = await context.ApiCredentials.AsNoTracking()
                .Where(credential => credential.Name == name)
                .Select(credential => credential.ProtectedValue)
                .SingleOrDefaultAsync(cancellationToken);
            return protectedValue is null ? null : CreateProtector(name).Unprotect(protectedValue);
        }

        public async Task SaveAsync(IReadOnlyDictionary<string, string?> changes, CancellationToken cancellationToken = default) {
            foreach (var name in changes.Keys) {
                ValidateName(name);
            }

            var existing = await context.ApiCredentials.Where(credential => changes.Keys.Contains(credential.Name))
                .ToDictionaryAsync(credential => credential.Name, cancellationToken);
            foreach (var (name, value) in changes) {
                if (value is null) {
                    if (existing.TryGetValue(name, out var credential)) {
                        context.ApiCredentials.Remove(credential);
                    }
                }
                else {
                    var protectedValue = CreateProtector(name).Protect(value);
                    if (existing.TryGetValue(name, out var credential)) {
                        credential.ProtectedValue = protectedValue;
                    }
                    else {
                        context.ApiCredentials.Add(new ApiCredential { Name = name, ProtectedValue = protectedValue });
                    }
                }
            }
            await context.SaveChangesAsync(cancellationToken);
        }

        private IDataProtector CreateProtector(string name) => protectionProvider.CreateProtector("NornavindCinema.ApiCredentials.v1", name);

        private static void ValidateName(string name) {
            if (!Names.Contains(name, StringComparer.Ordinal)) {
                throw new ArgumentException("Unknown API credential name.", nameof(name));
            }
        }
    }
}
