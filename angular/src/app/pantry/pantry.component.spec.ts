import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ListService, LocalizationService, PagedResultDto } from '@abp/ng.core';
import { Confirmation, ConfirmationService } from '@abp/ng.theme.shared';
import { PantryItemDto, PantryItemStatus, PantryService } from '@proxy/pantry';
import { Observable, Subject, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { PantryComponent } from './pantry.component';

class FakeListService {
  private callback?: (query: any) => Observable<any>;

  hookToQuery(streamCreatorCallback: (query: any) => Observable<any>): Observable<any> {
    this.callback = streamCreatorCallback;
    return this.callback({ skipCount: 0, maxResultCount: 10 });
  }

  get(): void {
    if (this.callback) {
      this.callback({ skipCount: 0, maxResultCount: 10 }).subscribe();
    }
  }
}

describe('PantryComponent', () => {
  let component: PantryComponent;
  let fixture: ComponentFixture<PantryComponent>;

  let pantryServiceMock: {
    getList: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    markAsConsumed: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };

  let confirmationServiceMock: {
    warn: ReturnType<typeof vi.fn>;
  };

  let localizationServiceMock: {
    instant: ReturnType<typeof vi.fn>;
    get: ReturnType<typeof vi.fn>;
    languageChange$: Subject<string>;
  };

  beforeEach(async () => {
    pantryServiceMock = {
      getList: vi.fn(),
      update: vi.fn(),
      markAsConsumed: vi.fn(),
      delete: vi.fn(),
    };

    confirmationServiceMock = {
      warn: vi.fn(),
    };

    localizationServiceMock = {
      instant: vi.fn((key: string) => key),
      get: vi.fn((key: string) => of(key)),
      languageChange$: new Subject<string>(),
    };

    await TestBed.configureTestingModule({
      imports: [PantryComponent],
      providers: [
        { provide: PantryService, useValue: pantryServiceMock },
        { provide: ConfirmationService, useValue: confirmationServiceMock },
        { provide: LocalizationService, useValue: localizationServiceMock },
      ],
    })
      .overrideComponent(PantryComponent, {
        set: {
          providers: [{ provide: ListService, useClass: FakeListService }],
        },
      })
      .compileComponents();
  });

  it('debe mostrar el estado de carga mientras la respuesta está pendiente y no mostrar lista vacía', () => {
    const pendingSubject = new Subject<PagedResultDto<PantryItemDto>>();
    pantryServiceMock.getList.mockReturnValue(pendingSubject.asObservable());

    fixture = TestBed.createComponent(PantryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(component.isLoading).toBe(true);
    expect(compiled.querySelector('[data-testid="pantry-loading"]')).not.toBeNull();
    expect(compiled.querySelector('[data-testid="pantry-empty"]')).toBeNull();
    expect(compiled.querySelector('[data-testid="pantry-results"]')).toBeNull();
  });

  it('debe mostrar el estado de error cuando falla el servicio y permitir reintentar', () => {
    pantryServiceMock.getList.mockReturnValueOnce(throwError(() => new Error('Network error')));

    fixture = TestBed.createComponent(PantryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(component.isLoading).toBe(false);
    expect(component.hasError).toBe(true);
    expect(compiled.querySelector('[data-testid="pantry-error"]')).not.toBeNull();
    expect(compiled.querySelector('[data-testid="pantry-empty"]')).toBeNull();

    // Al reintentar, el servicio responde con lista vacía
    pantryServiceMock.getList.mockReturnValueOnce(of({ items: [], totalCount: 0 }));
    const retryBtn = compiled.querySelector('[data-testid="retry-button"]') as HTMLButtonElement;
    retryBtn.click();
    fixture.detectChanges();

    expect(pantryServiceMock.getList).toHaveBeenCalledTimes(2);
    expect(component.hasError).toBe(false);
    expect(compiled.querySelector('[data-testid="pantry-empty"]')).not.toBeNull();
  });

  it('debe mostrar el estado de lista vacía cuando el usuario no tiene ítems', () => {
    pantryServiceMock.getList.mockReturnValue(of({ items: [], totalCount: 0 }));

    fixture = TestBed.createComponent(PantryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(component.isLoading).toBe(false);
    expect(component.hasError).toBe(false);
    expect(compiled.querySelector('[data-testid="pantry-empty"]')).not.toBeNull();
    expect(compiled.querySelector('[data-testid="pantry-loading"]')).toBeNull();
  });

  it('debe mostrar el estado con resultados e indicar textos localizados para datos faltantes (RF-09)', () => {
    const mockItems: PantryItemDto[] = [
      {
        id: 'item-1',
        productId: 'prod-1',
        productName: 'Leche Entera',
        productBrand: 'La Serenísima',
        quantity: 2,
        unit: 'L',
        expirationDate: '2026-11-10T00:00:00Z',
        status: PantryItemStatus.Active,
        isExpiringSoon: true,
      },
      {
        id: 'item-2',
        productId: 'prod-2',
        productName: 'Sal Fina',
        productBrand: 'Dos Anclas',
        quantity: null,
        unit: null,
        expirationDate: null,
        status: PantryItemStatus.Active,
        isExpiringSoon: false,
      },
    ];

    pantryServiceMock.getList.mockReturnValue(of({ items: mockItems, totalCount: 2 }));

    fixture = TestBed.createComponent(PantryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const rows = compiled.querySelectorAll('[data-testid="pantry-item-row"]');
    expect(rows.length).toBe(2);
    expect(compiled.textContent).toContain('Leche Entera');
    expect(compiled.textContent).toContain('Sal Fina');
    expect(compiled.querySelector('[data-testid="missing-quantity"]')).not.toBeNull();
    expect(compiled.querySelector('[data-testid="missing-unit"]')).not.toBeNull();
    expect(compiled.querySelector('[data-testid="missing-expiration"]')).not.toBeNull();
  });

  it('no debe invocar al servidor al editar con cantidad negativa o fecha inválida', () => {
    const item: PantryItemDto = {
      id: 'item-1',
      productId: 'prod-1',
      productName: 'Yogur Frutilla',
      productBrand: 'SanCor',
      quantity: 1,
      unit: 'u',
      expirationDate: '2026-11-20T00:00:00Z',
      status: PantryItemStatus.Active,
    };

    pantryServiceMock.getList.mockReturnValue(of({ items: [item], totalCount: 1 }));

    fixture = TestBed.createComponent(PantryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    component.openEdit(item);
    fixture.detectChanges();

    // 1. Cantidad negativa
    component.editForm.patchValue({
      quantity: -3,
      unit: 'u',
      expirationDate: '2026-11-20',
    });

    component.saveEdit();
    fixture.detectChanges();

    expect(component.editForm.invalid).toBe(true);
    expect(pantryServiceMock.update).not.toHaveBeenCalled();
    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="error-quantity-min"]')
    ).not.toBeNull();

    // 2. Fecha inválida
    component.editForm.patchValue({
      quantity: 2,
      unit: 'u',
      expirationDate: '1990-02-31',
    });

    component.saveEdit();
    fixture.detectChanges();

    expect(component.editForm.invalid).toBe(true);
    expect(pantryServiceMock.update).not.toHaveBeenCalled();
    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="error-invalid-date"]')
    ).not.toBeNull();
  });

  it('debe pedir confirmación previa antes de eliminar y cancelar sin llamar al servidor si el usuario rechaza', () => {
    const item: PantryItemDto = {
      id: 'item-delete-1',
      productId: 'prod-1',
      productName: 'Arroz Largo Fino',
      productBrand: 'Gallo',
      quantity: 1,
      unit: 'kg',
      expirationDate: null,
      status: PantryItemStatus.Active,
    };

    pantryServiceMock.getList.mockReturnValue(of({ items: [item], totalCount: 1 }));

    fixture = TestBed.createComponent(PantryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    // Caso 1: Usuario cancela la confirmación -> NO se llama a delete
    confirmationServiceMock.warn.mockReturnValueOnce(of(Confirmation.Status.reject));
    component.deleteItem(item);

    expect(confirmationServiceMock.warn).toHaveBeenCalledTimes(1);
    expect(pantryServiceMock.delete).not.toHaveBeenCalled();

    // Caso 2: Usuario confirma la eliminación -> SE llama a delete
    confirmationServiceMock.warn.mockReturnValueOnce(of(Confirmation.Status.confirm));
    pantryServiceMock.delete.mockReturnValueOnce(of(undefined));
    component.deleteItem(item);

    expect(confirmationServiceMock.warn).toHaveBeenCalledTimes(2);
    expect(pantryServiceMock.delete).toHaveBeenCalledWith('item-delete-1', {
      skipHandleError: true,
    });
  });
});

