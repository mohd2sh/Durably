import { Component, Input } from '@angular/core';

import { ExecutionStatus, TraceOutcome } from '../../core/models/execution.models';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `<span class="badge" [class]="toneClass">{{ label }}</span>`,
  styles: [
    `
      .badge {
        display: inline-flex;
        align-items: center;
        height: 1.5rem;
        padding: 0 0.55rem;
        border-radius: 999px;
        font-size: 0.72rem;
        font-weight: 600;
        letter-spacing: 0.03em;
        text-transform: uppercase;
        line-height: 1;
        border: 1px solid transparent;
        white-space: nowrap;
      }

      .tone-success {
        color: var(--success);
        background: var(--success-soft);
        border-color: color-mix(in srgb, var(--success) 25%, transparent);
      }

      .tone-danger {
        color: var(--danger);
        background: var(--danger-soft);
        border-color: color-mix(in srgb, var(--danger) 25%, transparent);
      }

      .tone-warning {
        color: var(--warning);
        background: var(--warning-soft);
        border-color: color-mix(in srgb, var(--warning) 25%, transparent);
      }

      .tone-info {
        color: var(--info);
        background: var(--info-soft);
        border-color: color-mix(in srgb, var(--info) 25%, transparent);
      }

      .tone-muted {
        color: var(--text-muted);
        background: var(--surface-muted);
        border-color: var(--border);
      }
    `
  ]
})
export class StatusBadgeComponent {
  @Input({ required: true }) label = '';

  @Input() executionStatus?: ExecutionStatus;

  @Input() traceOutcome?: TraceOutcome;

  get toneClass(): string {
    if (this.traceOutcome === TraceOutcome.Skipped) {
      return 'tone-muted';
    }

    if (this.traceOutcome === TraceOutcome.Failed) {
      return 'tone-warning';
    }

    if (this.traceOutcome === TraceOutcome.Succeeded) {
      return 'tone-success';
    }

    if (this.executionStatus === ExecutionStatus.Completed) {
      return 'tone-success';
    }

    if (this.executionStatus === ExecutionStatus.Failed) {
      return 'tone-danger';
    }

    return 'tone-info';
  }
}
