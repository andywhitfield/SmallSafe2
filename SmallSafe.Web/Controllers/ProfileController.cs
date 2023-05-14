using Dropbox.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SmallSafe.Web.Authorization;
using SmallSafe.Web.Data;
using SmallSafe.Web.Services;
using SmallSafe.Web.ViewModels.Profile;

namespace SmallSafe.Web.Controllers;

[Authorize(Policy = TwoFactorRequirement.PolicyName)]
public class ProfileController : Controller
{
    private readonly ILogger<ProfileController> _logger;
    private readonly IOptionsSnapshot<DropboxConfig> _dropboxConfig;
    private readonly IUserService _userService;

    public ProfileController(ILogger<ProfileController> logger, IOptionsSnapshot<DropboxConfig> dropboxConfig, IUserService userService)
    {
        _logger = logger;
        _dropboxConfig = dropboxConfig;
        _userService = userService;
    }

    [HttpGet("~/profile")]
    public async Task<IActionResult> Index()
    {
        var user = await _userService.GetUserAsync(User);
        return View(new IndexViewModel(HttpContext, user.IsConnectedToDropbox));
    }

    [HttpPost("~/profile/dropbox-connect")]
    [ValidateAntiForgeryToken]
    public IActionResult ConnectToDropbox()
    {
        var dropboxRedirect = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Code, _dropboxConfig.Value.SmallSafeAppKey, RedirectUri, tokenAccessType: TokenAccessType.Offline, scopeList: new[] {"files.content.write"});
        _logger.LogInformation($"Getting user token from Dropbox: {dropboxRedirect} (redirect={RedirectUri})");
        return Redirect(dropboxRedirect.ToString());
    }

    [HttpGet("~/profile/dropbox-authentication")]
    public async Task<ActionResult> DropboxAuthentication(string code, string state)
    {
        var response = await DropboxOAuth2Helper.ProcessCodeFlowAsync(code, _dropboxConfig.Value.SmallSafeAppKey, _dropboxConfig.Value.SmallSafeAppSecret, RedirectUri.ToString());
        _logger.LogInformation($"Got user tokens from Dropbox: {response.AccessToken} / {response.RefreshToken}");

        var user = await _userService.GetUserAsync(User);
        await _userService.UpdateUserDropboxAsync(user, response.AccessToken, response.RefreshToken);

        _logger.LogInformation($"Updating user {user.UserAccountId} dropbox connection");
        return Redirect("~/profile");
    }

    [HttpPost("~/profile/dropbox-disconnect")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DisconnectFromDropbox()
    {
        var user = await _userService.GetUserAsync(User);
        _logger.LogInformation($"Removing Dropbox connection from user {user.UserAccountId}");
        await _userService.UpdateUserDropboxAsync(user, null, null);
        return Redirect("~/profile");
    }

    private Uri RedirectUri
    {
        get
        {
            var uriBuilder = new UriBuilder();
            uriBuilder.Scheme = Request.Scheme;
            uriBuilder.Host = Request.Host.Host;
            if (Request.Host.Port.HasValue && Request.Host.Port != 443 && Request.Host.Port != 80)
                uriBuilder.Port = Request.Host.Port.Value;
            uriBuilder.Path = "profile/dropbox-authentication";
            return uriBuilder.Uri;
        }
    }
}
