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

import { DurablyApiService } from '../../../core/services/durably-api.service';
import { ExecutionStatus, ExecutionSummary } from '../../../core/models/execution.models';
import { StatusBadgeComponent } from '../../../shared/status-badge/status-badge.component';

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
    StatusBadgeComponent
  ],
  templateUrl: './execution-list.component.html',
  styleUrl: './execution-list.component.scss'
})
export class ExecutionListComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(DurablyApiService);

  readonly displayedColumns = [
    'instanceId',
    'runId',
    'flowName',
    'status',
    'currentStep',
    'attempts',
    'updatedAt',
    'actions'
  ];

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

  executions: ExecutionSummary[] = [];
  totalCount = 0;
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.search();
  }

  search(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const formValue = this.filterForm.getRawValue();
    this.api
      .searchExecutions({
        flowName: formValue.flowName || undefined,
        instanceId: formValue.instanceId || undefined,
        status: formValue.status ?? undefined,
        metadataKey: formValue.metadataKey || undefined,
        metadataValue: formValue.metadataValue || undefined
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

  statusLabel(status: ExecutionStatus): string {
    return ExecutionStatus[status] ?? 'Unknown';
  }
}
