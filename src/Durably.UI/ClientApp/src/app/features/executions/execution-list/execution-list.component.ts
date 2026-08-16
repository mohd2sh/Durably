import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';

import { DEFAULT_PAGE_SIZE } from '../../../constants/api-routes';
import { DurablyApiService } from '../../../core/services/durably-api.service';
import { ExecutionStatus, ExecutionSummary } from '../../../core/models/execution.models';
import { StatusBadgeComponent } from '../../../shared/status-badge/status-badge.component';
import { ColumnResizeHandleDirective } from '../../../shared/table/column-resize-handle.directive';
import { loadColumnWidths, saveColumnWidths } from '../../../shared/table/column-widths.storage';

type TableColumn =
  | 'instanceId'
  | 'runId'
  | 'flowName'
  | 'status'
  | 'currentStep'
  | 'attempts'
  | 'updatedAt'
  | 'actions';

const COLUMN_WIDTHS_STORAGE_KEY = 'durably.executionList.columnWidths.v1';

const DEFAULT_COLUMN_WIDTHS: Record<TableColumn, number> = {
  instanceId: 240,
  runId: 240,
  flowName: 180,
  status: 128,
  currentStep: 180,
  attempts: 140,
  updatedAt: 168,
  actions: 104
};

@Component({
  selector: 'app-execution-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatPaginatorModule,
    StatusBadgeComponent,
    ColumnResizeHandleDirective
  ],
  templateUrl: './execution-list.component.html',
  styleUrl: './execution-list.component.scss'
})
export class ExecutionListComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(DurablyApiService);

  readonly displayedColumns: TableColumn[] = [
    'instanceId',
    'runId',
    'flowName',
    'status',
    'currentStep',
    'attempts',
    'updatedAt',
    'actions'
  ];

  readonly columnWidths = loadColumnWidths(COLUMN_WIDTHS_STORAGE_KEY, DEFAULT_COLUMN_WIDTHS);

  get tableWidth(): number {
    return this.displayedColumns.reduce((sum, column) => sum + this.columnWidths[column], 0);
  }

  readonly statusOptions = [
    { label: 'Any', value: null },
    { label: 'Pending', value: ExecutionStatus.Pending },
    { label: 'Running', value: ExecutionStatus.Running },
    { label: 'Completed', value: ExecutionStatus.Completed },
    { label: 'Failed', value: ExecutionStatus.Failed }
  ];

  readonly filterForm = this.formBuilder.group({
    flowName: [''],
    instanceId: [''],
    status: [null as ExecutionStatus | null],
    metadataKey: [''],
    metadataValue: ['']
  });

  readonly pageSizeOptions = [25, 50, 100];

  executions: ExecutionSummary[] = [];
  totalCount = 0;
  pageIndex = 0;
  pageSize = DEFAULT_PAGE_SIZE;
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.search();
  }

  search(): void {
    this.pageIndex = 0;
    this.fetch();
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.fetch();
  }

  statusLabel(status: ExecutionStatus): string {
    return ExecutionStatus[status] ?? 'Unknown';
  }

  saveColumnWidths(): void {
    saveColumnWidths(COLUMN_WIDTHS_STORAGE_KEY, this.columnWidths);
  }

  private fetch(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const formValue = this.filterForm.getRawValue();
    this.api
      .searchExecutions({
        flowName: formValue.flowName || undefined,
        instanceId: formValue.instanceId || undefined,
        status: formValue.status ?? undefined,
        metadataKey: formValue.metadataKey || undefined,
        metadataValue: formValue.metadataValue || undefined,
        skip: this.pageIndex * this.pageSize,
        take: this.pageSize
      })
      .subscribe({
        next: result => {
          this.executions = result.items;
          this.totalCount = result.totalCount;
          this.isLoading = false;
        },
        error: () => {
          this.errorMessage = 'Unable to load executions.';
          this.isLoading = false;
        }
      });
  }
}
