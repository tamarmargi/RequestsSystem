import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogClose, MatDialogRef, MatDialogTitle } from '@angular/material/dialog';

import { USER_NAME_MAX_LENGTH } from '../../../../core/current-user.service';
import { Icon } from '../../../../shared/icon/icon';

export interface UserNameDialogData {
  userName: string;
}

/** Edit the name sent as X-User-Name (stored by the server as "changed by"). */
@Component({
  selector: 'app-user-name-dialog',
  imports: [ReactiveFormsModule, MatDialogTitle, MatDialogClose, Icon],
  template: `
    <div class="dialog__header">
      <div>
        <h2 mat-dialog-title class="dialog__title">שם המשתמש</h2>
        <p class="dialog__subtitle">השם יירשם בהיסטוריה של כל שינוי סטטוס שתבצעו.</p>
      </div>
      <button type="button" class="icon-btn icon-btn--ghost" mat-dialog-close aria-label="סגירה">
        <app-icon name="close" [size]="18" />
      </button>
    </div>

    <form class="dialog__body" id="user-name-form" (submit)="$event.preventDefault(); save()">
      <div class="field">
        <label class="field__label" for="user-name">שם להצגה</label>
        <input
          id="user-name"
          class="input"
          type="text"
          autocomplete="name"
          [formControl]="name"
          [maxlength]="maxLength"
          [attr.aria-invalid]="name.invalid && name.touched"
          aria-describedby="user-name-error"
        />
        @if (name.invalid && name.touched) {
          <p id="user-name-error" class="field__error">יש להזין שם.</p>
        }
      </div>
    </form>

    <div class="dialog__actions">
      <button type="submit" form="user-name-form" class="btn btn--primary">שמירה</button>
      <button type="button" class="btn btn--secondary" mat-dialog-close>ביטול</button>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserNameDialog {
  private readonly dialogRef = inject<MatDialogRef<UserNameDialog, string>>(MatDialogRef);

  protected readonly maxLength = USER_NAME_MAX_LENGTH;
  protected readonly name = new FormControl(inject<UserNameDialogData>(MAT_DIALOG_DATA).userName, {
    nonNullable: true,
    validators: [Validators.required, Validators.pattern(/\S/)],
  });

  protected save(): void {
    if (this.name.invalid) {
      this.name.markAsTouched();
      return;
    }
    this.dialogRef.close(this.name.value.trim());
  }
}
