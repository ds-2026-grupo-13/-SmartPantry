import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { ListService, LocalizationPipe, PagedResultDto } from '@abp/ng.core';
import { Confirmation, ConfirmationService } from '@abp/ng.theme.shared';
import {
  PantryItemDto,
  PantryItemStatus,
  PantryService,
  UpdatePantryItemDto,
} from '@proxy/pantry';
import { EMPTY } from 'rxjs';
import { catchError, finalize, tap } from 'rxjs/operators';

export function validExpirationDateValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  if (value === null || value === undefined || value === '') {
    return null;
  }

  if (typeof value !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return { invalidDate: true };
  }

  const [yearStr, monthStr, dayStr] = value.split('-');
  const year = Number(yearStr);
  const month = Number(monthStr);
  const day = Number(dayStr);

  if (year < 2000 || year > 2100 || month < 1 || month > 12 || day < 1 || day > 31) {
    return { invalidDate: true };
  }

  const parsed = new Date(Date.UTC(year, month - 1, day));
  if (
    isNaN(parsed.getTime()) ||
    parsed.getUTCFullYear() !== year ||
    parsed.getUTCMonth() + 1 !== month ||
    parsed.getUTCDate() !== day
  ) {
    return { invalidDate: true };
  }

  return null;
}

@Component({
  selector: 'app-pantry',
  standalone: true,
  templateUrl: './pantry.component.html',
  styleUrls: ['./pantry.component.scss'],
  providers: [ListService],
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LocalizationPipe, DatePipe],
})
export class PantryComponent implements OnInit {
  readonly list = inject(ListService);
  private readonly pantryService = inject(PantryService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly PantryItemStatus = PantryItemStatus;

  data: PagedResultDto<PantryItemDto> = { items: [], totalCount: 0 };
  isLoading = true;
  hasError = false;
  actionError = false;

  statusFilter: PantryItemStatus | null = null;

  editingItem: PantryItemDto | null = null;
  editForm: FormGroup;
  isSaving = false;
  editServerError = false;

  get items(): PantryItemDto[] {
    return this.data.items ?? [];
  }

  constructor() {
    this.editForm = this.fb.group({
      quantity: [null, [Validators.min(0)]],
      unit: ['', [Validators.maxLength(32)]],
      expirationDate: ['', [validExpirationDateValidator]],
    });
  }

  ngOnInit(): void {
    const streamCreator = (query: any) => {
      this.isLoading = true;
      this.hasError = false;
      this.actionError = false;
      this.cdr.markForCheck();

      return this.pantryService
        .getList(
          {
            ...query,
            status: this.statusFilter,
          },
          { skipHandleError: true }
        )
        .pipe(
          tap(response => {
            this.data = response ?? { items: [], totalCount: 0 };
            this.isLoading = false;
            this.hasError = false;
            this.cdr.markForCheck();
          }),
          catchError(() => {
            this.isLoading = false;
            this.hasError = true;
            this.data = { items: [], totalCount: 0 };
            this.cdr.markForCheck();
            return EMPTY;
          })
        );
    };

    this.list.hookToQuery(streamCreator).subscribe();
  }

  retry(): void {
    this.list.get();
  }

  onFilterChange(status: PantryItemStatus | null): void {
    this.statusFilter = status;
    this.list.get();
  }

  openEdit(item: PantryItemDto): void {
    this.editingItem = item;
    this.editServerError = false;
    this.isSaving = false;

    const formattedDate = item.expirationDate ? item.expirationDate.substring(0, 10) : '';

    this.editForm.reset({
      quantity: item.quantity ?? null,
      unit: item.unit ?? '',
      expirationDate: formattedDate,
    });
  }

  cancelEdit(): void {
    if (this.isSaving) {
      return;
    }
    this.editingItem = null;
    this.editServerError = false;
    this.editForm.reset();
  }

  saveEdit(): void {
    if (!this.editingItem || !this.editingItem.id) {
      return;
    }

    this.editForm.markAllAsTouched();
    if (this.editForm.invalid) {
      return;
    }

    const raw = this.editForm.getRawValue();
    const dto: UpdatePantryItemDto = {
      quantity:
        raw.quantity === '' || raw.quantity === null || raw.quantity === undefined
          ? null
          : Number(raw.quantity),
      unit: raw.unit && String(raw.unit).trim().length > 0 ? String(raw.unit).trim() : null,
      expirationDate:
        raw.expirationDate && String(raw.expirationDate).trim().length > 0
          ? `${String(raw.expirationDate).trim()}T00:00:00Z`
          : null,
    };

    this.isSaving = true;
    this.editServerError = false;

    this.pantryService
      .update(this.editingItem.id, dto, { skipHandleError: true })
      .pipe(
        finalize(() => {
          this.isSaving = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: () => {
          this.editingItem = null;
          this.editForm.reset();
          this.list.get();
        },
        error: () => {
          this.editServerError = true;
          this.cdr.markForCheck();
        },
      });
  }

  markAsConsumed(item: PantryItemDto): void {
    if (!item.id || item.status === PantryItemStatus.Consumed) {
      return;
    }

    this.actionError = false;
    this.pantryService.markAsConsumed(item.id, { skipHandleError: true }).subscribe({
      next: () => {
        this.list.get();
      },
      error: () => {
        this.actionError = true;
        this.cdr.markForCheck();
      },
    });
  }

  deleteItem(item: PantryItemDto): void {
    if (!item.id) {
      return;
    }

    this.actionError = false;
    this.confirmation
      .warn('::Pantry:DeleteConfirmMessage', '::Pantry:DeleteConfirmTitle', {
        messageLocalizationParams: [item.productName || ''],
      })
      .subscribe((status: Confirmation.Status) => {
        if (status === Confirmation.Status.confirm && item.id) {
          this.pantryService.delete(item.id, { skipHandleError: true }).subscribe({
            next: () => {
              this.list.get();
            },
            error: () => {
              this.actionError = true;
              this.cdr.markForCheck();
            },
          });
        }
      });
  }
}
