import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogClose, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';

import { RequestDto, RequestStatus } from '../../../../models/request.model';
import { Icon } from '../../../../shared/icon/icon';
import { ALLOWED_TRANSITIONS, STATUS_DESCRIPTIONS } from '../../../../shared/request-labels';
import { StatusBadge } from '../../../../shared/status-badge/status-badge';

export interface StatusChangeDialogData {
  request: RequestDto;
}

/** Choose the new status. Only the transitions allowed from the current status are offered. */
@Component({
  selector: 'app-status-change-dialog',
  imports: [MatDialogTitle, MatDialogClose, Icon, StatusBadge],
  templateUrl: './status-change-dialog.html',
  styleUrl: './status-change-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusChangeDialog {
  private readonly dialogRef = inject<MatDialogRef<StatusChangeDialog, RequestStatus>>(MatDialogRef);
  protected readonly request = inject<StatusChangeDialogData>(MAT_DIALOG_DATA).request;

  protected readonly options = ALLOWED_TRANSITIONS[this.request.status];
  protected readonly descriptions = STATUS_DESCRIPTIONS;
  protected readonly selected = signal<RequestStatus | null>(this.options[0] ?? null);

  protected confirm(): void {
    const status = this.selected();
    if (status) {
      this.dialogRef.close(status);
    }
  }
}
