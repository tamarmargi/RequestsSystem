using FluentValidation;
using Microsoft.Extensions.Logging;
using Requests.BL.Caching;
using Requests.BL.Domain;
using Requests.BL.Dtos;
using Requests.BL.Mapping;
using Requests.BL.Results;
using Requests.BL.Validation;
using Requests.DAL.Entities;
using Requests.DAL.Repositories;
using ValidationException = Requests.BL.Exceptions.ValidationException;

namespace Requests.BL.Services;

public class RequestCommandService : IRequestCommandService
{
    private const string ConflictMessage = "The request was modified by another user.";

    private readonly IRequestRepository _repository;
    private readonly IValidator<UpdateStatusRequest> _validator;
    private readonly IValidator<BulkUpdateStatusRequest> _bulkValidator;
    private readonly IStatsCache _statsCache;
    private readonly ILogger<RequestCommandService> _logger;

    public RequestCommandService(
        IRequestRepository repository,
        IValidator<UpdateStatusRequest> validator,
        IValidator<BulkUpdateStatusRequest> bulkValidator,
        IStatsCache statsCache,
        ILogger<RequestCommandService> logger)
    {
        _repository = repository;
        _validator = validator;
        _bulkValidator = bulkValidator;
        _statsCache = statsCache;
        _logger = logger;
    }

    public async Task<UpdateStatusResult> UpdateStatusAsync(
        int id, UpdateStatusRequest request, string changedBy, CancellationToken ct)
    {
        // 1. Input.
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw ValidationException.From(validation);
        }

        UpdateStatusRequestValidator.TryDecodeVersion(request.Version, out var expectedVersion);

        // 2-5.
        var entity = await _repository.GetForUpdateAsync(id, ct);
        var previousStatus = entity?.Status;
        var result = await ApplyStatusChangeAsync(id, entity, expectedVersion, request.NewStatus!.Value, changedBy, ct);

        switch (result)
        {
            case UpdateStatusResult.Success:
                _logger.LogInformation(
                    "Request {RequestId} status changed from {PreviousStatus} to {NewStatus}",
                    id, previousStatus, request.NewStatus);
                _statsCache.Invalidate();
                break;
            case UpdateStatusResult.Conflict:
                _logger.LogWarning("Status update conflict for request {RequestId}", id);
                break;
        }

        return result;
    }

    public async Task<BulkUpdateResultDto> BulkUpdateStatusAsync(
        BulkUpdateStatusRequest request, string changedBy, CancellationToken ct)
    {
        // Request-level validation: on failure nothing is touched.
        var validation = await _bulkValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            throw ValidationException.From(validation);
        }

        var newStatus = request.NewStatus!.Value;
        var items = request.Items!;

        // One query for all items; then each item is checked and saved on its own, in input order.
        var entities = await _repository.GetManyForUpdateAsync(items.Select(i => i.Id).ToList(), ct);

        var results = new List<BulkItemResultDto>(items.Count);
        foreach (var item in items)
        {
            UpdateStatusRequestValidator.TryDecodeVersion(item.Version, out var expectedVersion);
            var result = await ApplyStatusChangeAsync(
                item.Id, entities.GetValueOrDefault(item.Id), expectedVersion, newStatus, changedBy, ct);
            results.Add(ToItemResult(item.Id, result));
        }

        var succeeded = results.Count(r => r.Outcome == BulkOutcome.Updated);
        _logger.LogInformation(
            "Bulk status update to {NewStatus}: {Total} items, {Succeeded} updated, {NotFound} not found, " +
            "{Conflict} conflicts, {InvalidTransition} invalid transitions",
            newStatus, results.Count, succeeded,
            results.Count(r => r.Outcome == BulkOutcome.NotFound),
            results.Count(r => r.Outcome == BulkOutcome.Conflict),
            results.Count(r => r.Outcome == BulkOutcome.InvalidTransition));

        // Only after the saves, and only if something changed.
        if (succeeded > 0)
        {
            _statsCache.Invalidate();
        }

        return new BulkUpdateResultDto(succeeded, results.Count - succeeded, results);
    }

    public static string DescribeInvalidTransition(RequestStatus from, RequestStatus to)
    {
        var allowed = StatusTransitions.GetAllowedTargets(from);
        var allowedText = allowed.Count == 0 ? $"none ({from} is final)" : string.Join(", ", allowed);
        return $"Cannot change status from {from} to {to}. Allowed from {from}: {allowedText}.";
    }

    /// <summary>
    /// Checks 2-5 of a status change, shared by the single and the bulk update: exists -> version -> transition -> save.
    /// </summary>
    /// <param name="entity">The tracked request, or null if it does not exist.</param>
    private async Task<UpdateStatusResult> ApplyStatusChangeAsync(
        int id, Request? entity, byte[] expectedVersion, RequestStatus newStatus, string changedBy, CancellationToken ct)
    {
        // 2. Existence.
        if (entity is null)
        {
            return new UpdateStatusResult.NotFound();
        }

        // 3. Version, before the transition rule: with stale data a transition error would be misleading.
        //    Only a shortcut; the database enforces the version again in the UPDATE.
        if (!entity.RowVersion.AsSpan().SequenceEqual(expectedVersion))
        {
            return new UpdateStatusResult.Conflict(entity.ToDto());
        }

        // 4. Workflow.
        if (!StatusTransitions.IsAllowed(entity.Status, newStatus))
        {
            return new UpdateStatusResult.InvalidTransition(DescribeInvalidTransition(entity.Status, newStatus));
        }

        // Taken into work without a handler -> the user becomes the handler. Set on the tracked entity, so it is
        // written in the same SaveChanges (and under the same RowVersion check) as the status and history row.
        entity.AssignedTo = AutoAssignment.HandlerAfterChange(entity.Status, newStatus, entity.AssignedTo, changedBy);

        // 5. Save (its own SaveChanges = its own transaction); false means someone else saved in between.
        if (!await _repository.SaveStatusChangeAsync(entity, expectedVersion, newStatus, changedBy, ct))
        {
            var current = await _repository.GetByIdAsync(id, ct);
            return current is null
                ? new UpdateStatusResult.NotFound()
                : new UpdateStatusResult.Conflict(current.ToDto());
        }

        return new UpdateStatusResult.Success(entity.ToDto());
    }

    private static BulkItemResultDto ToItemResult(int id, UpdateStatusResult result) => result switch
    {
        UpdateStatusResult.Success success => new BulkItemResultDto(id, BulkOutcome.Updated, Version: success.Request.Version),
        UpdateStatusResult.NotFound => new BulkItemResultDto(id, BulkOutcome.NotFound, $"Request {id} does not exist."),
        UpdateStatusResult.Conflict => new BulkItemResultDto(id, BulkOutcome.Conflict, ConflictMessage),
        UpdateStatusResult.InvalidTransition invalid => new BulkItemResultDto(id, BulkOutcome.InvalidTransition, invalid.Message),
        _ => throw new InvalidOperationException($"Unhandled result {result.GetType().Name}.")
    };
}
