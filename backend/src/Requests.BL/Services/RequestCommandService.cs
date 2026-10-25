using FluentValidation;
using Microsoft.Extensions.Logging;
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
    private readonly IRequestRepository _repository;
    private readonly IValidator<UpdateStatusRequest> _validator;
    private readonly ILogger<RequestCommandService> _logger;

    public RequestCommandService(
        IRequestRepository repository,
        IValidator<UpdateStatusRequest> validator,
        ILogger<RequestCommandService> logger)
    {
        _repository = repository;
        _validator = validator;
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

        var newStatus = request.NewStatus!.Value;
        UpdateStatusRequestValidator.TryDecodeVersion(request.Version, out var expectedVersion);

        // 2. Existence.
        var entity = await _repository.GetForUpdateAsync(id, ct);
        if (entity is null)
        {
            return new UpdateStatusResult.NotFound();
        }

        // 3. Version, before the transition rule: with stale data a transition error would be misleading.
        //    Only a shortcut; the database enforces the version again in the UPDATE.
        if (!entity.RowVersion.AsSpan().SequenceEqual(expectedVersion))
        {
            _logger.LogWarning("Status update conflict for request {RequestId}", id);
            return new UpdateStatusResult.Conflict(entity.ToDto());
        }

        // 4. Workflow.
        var previousStatus = entity.Status;
        if (!StatusTransitions.IsAllowed(previousStatus, newStatus))
        {
            return new UpdateStatusResult.InvalidTransition(DescribeInvalidTransition(previousStatus, newStatus));
        }

        // 5. Save; false means someone else saved between our read and our UPDATE.
        if (!await _repository.SaveStatusChangeAsync(entity, expectedVersion, newStatus, changedBy, ct))
        {
            _logger.LogWarning("Status update conflict for request {RequestId}", id);
            var current = await _repository.GetByIdAsync(id, ct);
            return current is null
                ? new UpdateStatusResult.NotFound()
                : new UpdateStatusResult.Conflict(current.ToDto());
        }

        _logger.LogInformation(
            "Request {RequestId} status changed from {PreviousStatus} to {NewStatus}", id, previousStatus, newStatus);
        OnStatusChanged();

        return new UpdateStatusResult.Success(entity.ToDto());
    }

    public static string DescribeInvalidTransition(RequestStatus from, RequestStatus to)
    {
        var allowed = StatusTransitions.GetAllowedTargets(from);
        var allowedText = allowed.Count == 0 ? $"none ({from} is final)" : string.Join(", ", allowed);
        return $"Cannot change status from {from} to {to}. Allowed from {from}: {allowedText}.";
    }

    /// <summary>
    /// EXTENSION POINT (next step, T6.1): invalidate the stats cache here. Called only after a successful save.
    /// </summary>
    private static void OnStatusChanged()
    {
    }
}
