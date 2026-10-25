import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogClose, MatDialogTitle } from '@angular/material/dialog';

import { BulkUpdateItemResult, BulkUpdateResult } from '../../../../models/bulk-update.model';
import { RequestDto, RequestStatus } from '../../../../models/request.model';
import { OUTCOME_CLASSES, OUTCOME_LABELS, STATUS_LABELS } from '../../../../shared/request-labels';

export interface BulkResultDialogData {
  result: BulkUpdateResult;
  newStatus: RequestStatus;
  /** The selected requests as they were sent (for titles and the status before the change). */
  requests: RequestDto[];
}

/** close: clear the selection. reload-failed: reload the list keeping the failed items selected. */
export type BulkResultChoice = 'close' | 'reload-failed';

interface ResultRow {
  id: number;
  label: string;
  cssClass: string;
  message: string;
}

/** Bulk status change result: summary counts and one row per item with its outcome. */
@Component({
  selector: 'app-bulk-result-dialog',
  imports: [MatDialogTitle, MatDialogClose],
  templateUrl: './bulk-result-dialog.html',
  styleUrl: './bulk-result-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BulkResultDialog {
  protected readonly data = inject<BulkResultDialogData>(MAT_DIALOG_DATA);
  protected readonly statusLabel = STATUS_LABELS[this.data.newStatus];
  protected readonly rows: ResultRow[] = this.data.result.results.map((result) => this.toRow(result));

  protected readonly close: BulkResultChoice = 'close';
  protected readonly reloadFailed: BulkResultChoice = 'reload-failed';

  private toRow(result: BulkUpdateItemResult): ResultRow {
    const request = this.data.requests.find((r) => r.id === result.id);
    return {
      id: result.id,
      label: OUTCOME_LABELS[result.outcome],
      cssClass: OUTCOME_CLASSES[result.outcome],
      message: this.message(result, request),
    };
  }

  private message(result: BulkUpdateItemResult, request: RequestDto | undefined): string {
    switch (result.outcome) {
      case 'Updated':
        return request?.title ?? '';
      case 'Conflict':
        return 'עודכנה בינתיים על ידי משתמש אחר';
      case 'NotFound':
        return 'הפנייה לא נמצאה';
      case 'InvalidTransition':
        return request
          ? `לא ניתן לעבור מ"${STATUS_LABELS[request.status]}" ל"${this.statusLabel}"`
          : (result.message ?? 'המעבר אינו מותר');
    }
  }
}
