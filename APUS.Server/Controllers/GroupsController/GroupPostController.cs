using APUS.Server.Core.Helpers;
using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Domain.DTOs.Groups;
using APUS.Server.Domain.Entities.Groups;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APUS.Server.Controllers.GroupsController
{
	[ApiController]
	[Route("api/groups")]
	[Authorize]
	public class GroupPostController : ControllerBase
	{
		private readonly IGroupService _svc;
		private readonly IGroupPostCommentRepository _commentRepository;

		public GroupPostController(IGroupService svc, IGroupPostCommentRepository commentRepository)
		{
			_svc = svc;
			_commentRepository = commentRepository;
		}

		[HttpGet("{groupId:long}/posts")]
		public async Task<ActionResult<PagedResponse<GroupPostDto>>> List(
			long groupId,
			[FromQuery] int skip = 0,
			[FromQuery] int take = 20,
			CancellationToken ct = default)
		{
			take = Math.Clamp(take, 1, 50);
			var userId = User.GetUserId();
			var result = await _svc.GetPostsAsync(userId, groupId, skip, take, ct);
			return Ok(result);
		}

		[HttpPost("{groupId:long}/posts")]
		public async Task<ActionResult<GroupPostDto>> Create(
			long groupId,
			[FromBody] CreateGroupPostDto dto,
			CancellationToken ct)
		{
			var userId = User.GetUserId();
			var post = await _svc.CreatePostAsync(userId, groupId, dto, ct);
			return CreatedAtAction(nameof(List), new { groupId, skip = 0, take = 1 }, post);
		}

		[HttpDelete("posts/{postId:long}")]
		public async Task<IActionResult> Delete(long postId, CancellationToken ct)
		{
			var userId = User.GetUserId();
			await _svc.DeletePostAsync(userId, postId, ct);
			return NoContent();
		}

		[HttpGet("posts/{postId:long}/comments")]
		public async Task<ActionResult<IEnumerable<CommentDto>>> GetComments(
	long postId,
	CancellationToken ct)
		{
			// For now we just load comments by post; GroupService already enforces access
			// for listing posts, and the UI only exposes this for members.

			var entities = await _commentRepository.GetByPostIdAsync(postId);

			var dtos = entities.Select(c => new CommentDto
			{
				Id = c.Id,
				AuthorUserId = c.AuthorUserId,
				AuthorFullName = c.AuthorUser.FirstName + " " + c.AuthorUser.LastName,
				AuthorAvatarUrl = c.AuthorUser.AvatarUrl,
				Text = c.Text,
				CreatedAtUtc = c.CreatedAtUtc
			}).ToList();

			return Ok(dtos);
		}

		[HttpPost("posts/{postId:long}/comments")]
		[ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<CommentDto>> AddComment(
	long postId,
	[FromBody] CreateCommentRequest request,
	CancellationToken ct)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var userId = User.GetUserId(); // or User.FindFirstValue(ClaimTypes.NameIdentifier)!

			// Just create the comment; FK constraint will protect invalid postId.
			var entity = new GroupPostComment
			{
				GroupPostId = postId,
				AuthorUserId = userId,
				Text = request.Text.Trim(),
				CreatedAtUtc = DateTime.UtcNow
			};

			entity = await _commentRepository.AddAsync(entity);

			var dto = new CommentDto
			{
				Id = entity.Id,
				AuthorUserId = entity.AuthorUserId,
				AuthorFullName = entity.AuthorUser.FirstName + " " + entity.AuthorUser.LastName,
				AuthorAvatarUrl = entity.AuthorUser.AvatarUrl,
				Text = entity.Text,
				CreatedAtUtc = entity.CreatedAtUtc
			};

			return Ok(dto);
		}


	}
}
