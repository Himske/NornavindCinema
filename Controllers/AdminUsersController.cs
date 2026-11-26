using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NornavindCinema.Data;
using NornavindCinema.ViewModels;

namespace NornavindCinema.Controllers {
    [Authorize(Policy = "Administrator")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AdminUsersController(ApplicationDbContext context, UserManager<ApplicationUser> users, ILogger<AdminUsersController> logger) : Controller {
        private static readonly string[] ManagedRoles = ["Guest", "Member", "Admin"];

        public async Task<IActionResult> Index() {
            var accounts = await users.Users.AsNoTracking().OrderBy(user => user.Email).ToListAsync();
            var roles = await (from membership in context.UserRoles
                               join role in context.Roles on membership.RoleId equals role.Id
                               select new { membership.UserId, role.Name }).ToListAsync();
            var model = accounts.Select(user => new AdminUserViewModel {
                Id = user.Id,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.Where(role => role.UserId == user.Id && role.Name != null).Select(role => role.Name!).ToList()
            }).ToList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRole(string id, string role, bool? enabled) {
            if (string.IsNullOrEmpty(id) || !ManagedRoles.Contains(role, StringComparer.Ordinal) || enabled is null) return BadRequest();
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var user = await users.FindByIdAsync(id);
            if (user is null) return NotFound();
            var hasRole = await users.IsInRoleAsync(user, role);
            if (hasRole == enabled.Value) return RedirectToAction(nameof(Index));
            if (!await context.Roles.AnyAsync(existing => existing.NormalizedName == role.ToUpperInvariant())) {
                logger.LogError("Cannot update user role: role {Role} does not exist.", role);
                TempData["AdminError"] = "The requested role does not exist. Please contact support.";
                return RedirectToAction(nameof(Index));
            }
            if (role == "Admin" && !enabled.Value && (id == users.GetUserId(User) || await IsLastAdminAsync())) {
                TempData["AdminError"] = "You cannot remove your own Admin role or remove the last administrator.";
                return RedirectToAction(nameof(Index));
            }

            var result = enabled.Value ? await users.AddToRoleAsync(user, role) : await users.RemoveFromRoleAsync(user, role);
            if (!result.Succeeded) return IdentityFailure(result);
            result = await users.UpdateSecurityStampAsync(user);
            if (!result.Succeeded) return IdentityFailure(result);
            await transaction.CommitAsync();
            TempData["AdminSuccess"] = "User role updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id) {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            var user = await users.FindByIdAsync(id);
            if (user is null) return NotFound();
            return View(new AdminUserViewModel { Id = user.Id, Email = user.Email });
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id) {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var user = await users.FindByIdAsync(id);
            if (user is null) return NotFound();
            if (id == users.GetUserId(User) || (await users.IsInRoleAsync(user, "Admin") && await IsLastAdminAsync())) {
                TempData["AdminError"] = "You cannot delete your own account or the last administrator.";
                return RedirectToAction(nameof(Index));
            }
            var result = await users.DeleteAsync(user);
            if (!result.Succeeded) return IdentityFailure(result);
            await transaction.CommitAsync();
            TempData["AdminSuccess"] = "User account deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> IsLastAdminAsync() {
            return await (from membership in context.UserRoles
                          join role in context.Roles on membership.RoleId equals role.Id
                          where role.NormalizedName == "ADMIN"
                          select membership.UserId).CountAsync() <= 1;
        }

        private IActionResult IdentityFailure(IdentityResult result) {
            logger.LogWarning("User administration failed: {Errors}", string.Join(", ", result.Errors.Select(error => error.Code)));
            TempData["AdminError"] = "The user could not be updated. Please try again or contact support.";
            return RedirectToAction(nameof(Index));
        }
    }
}
