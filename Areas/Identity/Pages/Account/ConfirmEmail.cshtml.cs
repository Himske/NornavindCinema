// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using NornavindCinema.Data;

namespace NornavindCinema.Areas.Identity.Pages.Account;

public class ConfirmEmailModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConfirmEmailModel> _logger;

    public ConfirmEmailModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context, ILogger<ConfirmEmailModel> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [TempData]
    public string? StatusMessage { get; set; }
    public async Task<IActionResult> OnGetAsync(string userId, string code)
    {
        if (userId == null || code == null)
        {
            return RedirectToPage("/Index");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userId}'.");
        }

        code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var wasConfirmed = user.EmailConfirmed;
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var result = await _userManager.ConfirmEmailAsync(user, code);
        if (!result.Succeeded)
        {
            StatusMessage = "Error confirming your email.";
            return Page();
        }

        if (!wasConfirmed)
        {
            const string memberRole = "Member";
            if (!await _roleManager.RoleExistsAsync(memberRole))
            {
                _logger.LogError("Cannot confirm user {UserId}: the Member role does not exist.", userId);
                StatusMessage = "Error confirming your email. Please contact support.";
                return Page();
            }

            if (!await _userManager.IsInRoleAsync(user, memberRole))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, memberRole);
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Failed to assign the Member role to user {UserId}: {Errors}",
                        userId, string.Join(", ", roleResult.Errors.Select(error => error.Code)));
                    StatusMessage = "Error confirming your email. Please contact support.";
                    return Page();
                }
            }
        }

        await transaction.CommitAsync();
        StatusMessage = "Thank you for confirming your email.";
        return Page();
    }
}
