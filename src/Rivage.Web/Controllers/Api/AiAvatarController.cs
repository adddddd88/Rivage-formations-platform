using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rivage.Domain.Interfaces;
using Rivage.Infrastructure.Data;

namespace Rivage.Web.Controllers.Api;

[ApiController]
[Route("api/ai-avatar")]
[Authorize]
public class AiAvatarController : ControllerBase
{
    private readonly IAiAvatarService _avatar;
    private readonly RivageDbContext _db;

    public AiAvatarController(IAiAvatarService avatar, RivageDbContext db)
    {
        _avatar = avatar;
        _db = db;
    }

    [HttpGet("status")]
    public IActionResult Status() => Ok(new
    {
        provider = _avatar.ProviderName,
        configured = _avatar.IsConfigured
    });

    [HttpPost("session/{moduleId:int}")]
    public async Task<IActionResult> CreateSession(int moduleId, CancellationToken cancellationToken)
    {
        var module = await _db.Modules.AsNoTracking()
            .Include(m => m.Formation)
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);

        if (module is null) return NotFound();

        var name = User.Identity?.Name;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = userId is null ? null : await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        var result = await _avatar.CreateSessionAsync(new AiAvatarSessionRequest(
            module.Title,
            module.Content,
            user?.FirstName ?? name,
            module.Formation.Title), cancellationToken);

        return Ok(result);
    }

    [HttpPost("ask/{moduleId:int}")]
    public async Task<IActionResult> Ask(int moduleId, [FromBody] AskBody body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.Question))
            return BadRequest(new { error = "Question requise." });

        var module = await _db.Modules.AsNoTracking().FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);
        if (module is null) return NotFound();

        var result = await _avatar.AskAsync(new AiAvatarAskRequest(body.Question.Trim(), module.Title, module.Content), cancellationToken);
        return Ok(result);
    }

    public record AskBody(string Question);
}