import { CommonModule } from '@angular/common';
import { AfterViewChecked, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import mermaid from 'mermaid';

import { DurablyApiService } from '../../../core/services/durably-api.service';
import {
  ExecutionDetail,
  ExecutionStatus,
  TraceOutcome,
  TraceRecord
} from '../../../core/models/execution.models';
import { JsonDiffComponent } from '../../../shared/json-diff/json-diff.component';
import { JsonViewerComponent } from '../../../shared/json-viewer/json-viewer.component';
import { StatusBadgeComponent } from '../../../shared/status-badge/status-badge.component';

type TracePayloadTab = 'diff' | 'output' | 'input';

@Component({
  selector: 'app-execution-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    StatusBadgeComponent,
    JsonViewerComponent,
    JsonDiffComponent
  ],
  templateUrl: './execution-detail.component.html',
  styleUrl: './execution-detail.component.scss'
})
export class ExecutionDetailComponent implements OnInit, AfterViewChecked {
  @ViewChild('graphContainer') graphContainer?: ElementRef<HTMLDivElement>;

  execution?: ExecutionDetail;
  traces: TraceRecord[] = [];
  isLoading = true;
  errorMessage = '';

  showGraph = true;
  showMetadata = true;
  showLease = false;
  selectedTrace?: TraceRecord;
  payloadTab: TracePayloadTab = 'diff';

  readonly ExecutionStatus = ExecutionStatus;
  readonly TraceOutcome = TraceOutcome;

  private graphNeedsRender = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: DurablyApiService
  ) {}

  ngOnInit(): void {
    mermaid.initialize({ startOnLoad: false, theme: 'base', securityLevel: 'loose' });

    const flowName = this.route.snapshot.paramMap.get('flowName') ?? '';
    const instanceId = this.route.snapshot.paramMap.get('instanceId') ?? '';

    this.api.getExecution(flowName, instanceId).subscribe({
      next: execution => {
        this.execution = execution;
        this.loadTraces(flowName, instanceId);
      },
      error: () => {
        this.errorMessage = 'Execution not found or access denied.';
        this.isLoading = false;
      }
    });
  }

  ngAfterViewChecked(): void {
    if (this.graphNeedsRender && this.showGraph && this.graphContainer) {
      this.graphNeedsRender = false;
      void this.renderGraph();
    }
  }

  selectTrace(trace: TraceRecord): void {
    this.selectedTrace = trace;
    this.payloadTab = this.hasBothStates(trace) ? 'diff' : 'output';
  }

  setPayloadTab(tab: TracePayloadTab): void {
    this.payloadTab = tab;
  }

  togglePanel(panel: 'graph' | 'metadata' | 'lease'): void {
    if (panel === 'graph') {
      this.showGraph = !this.showGraph;
      if (this.showGraph) {
        this.graphNeedsRender = true;
      }
      return;
    }

    if (panel === 'metadata') {
      this.showMetadata = !this.showMetadata;
      return;
    }

    this.showLease = !this.showLease;
  }

  statusLabel(status: ExecutionStatus): string {
    return ExecutionStatus[status] ?? 'Unknown';
  }

  traceOutcomeLabel(outcome: TraceOutcome): string {
    switch (outcome) {
      case TraceOutcome.Succeeded:
        return 'Succeeded';
      case TraceOutcome.Failed:
        return 'Failed';
      case TraceOutcome.Skipped:
        return 'Skipped';
      default:
        return 'Unknown';
    }
  }

  private outcomeGraphClass(outcome: TraceOutcome): string {
    switch (outcome) {
      case TraceOutcome.Succeeded:
        return 'success';
      case TraceOutcome.Skipped:
        return 'skipped';
      default:
        return 'failed';
    }
  }

  private hasBothStates(trace: TraceRecord): boolean {
    return Boolean(trace.inputJson) && Boolean(trace.outputJson);
  }

  private loadTraces(flowName: string, instanceId: string): void {
    this.api.getTraces(flowName, instanceId).subscribe({
      next: traces => {
        this.traces = traces;
        this.selectedTrace = traces[0];
        if (this.selectedTrace) {
          this.payloadTab = this.hasBothStates(this.selectedTrace) ? 'diff' : 'output';
        }
        this.isLoading = false;
        this.graphNeedsRender = true;
      },
      error: () => {
        this.traces = [];
        this.isLoading = false;
      }
    });
  }

  async renderGraph(): Promise<void> {
    if (!this.showGraph || !this.graphContainer || this.traces.length === 0) {
      return;
    }

    // Mermaid classDef cannot parse rgba()/hsl() — commas break the style grammar.
    const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
    const successFill = isDark ? '#16352a' : '#e8f5e9';
    const successStroke = isDark ? '#3ecf8e' : '#067647';
    const failedFill = isDark ? '#3a1a1a' : '#ffebee';
    const failedStroke = isDark ? '#f97066' : '#b42318';
    const skippedFill = isDark ? '#1e2732' : '#e8ecef';
    const skippedStroke = isDark ? '#8b9aab' : '#5b6b7c';
    const textColor = isDark ? '#e8eef4' : '#1a2330';

    const lines = ['flowchart LR'];
    for (let index = 0; index < this.traces.length; index++) {
      const trace = this.traces[index];
      const nodeId = `S${index}`;
      const outcomeClass = this.outcomeGraphClass(trace.outcome);
      lines.push(`${nodeId}["${trace.stepKey}\\n${trace.durationMs}ms"]`);
      lines.push(`class ${nodeId} ${outcomeClass}`);

      if (index > 0) {
        lines.push(`S${index - 1} --> ${nodeId}`);
      }
    }

    lines.push(
      `classDef success fill:${successFill},stroke:${successStroke},color:${textColor}`
    );
    lines.push(
      `classDef failed fill:${failedFill},stroke:${failedStroke},color:${textColor}`
    );
    lines.push(
      `classDef skipped fill:${skippedFill},stroke:${skippedStroke},color:${textColor}`
    );

    const graph = lines.join('\n');
    const { svg } = await mermaid.render(`durably-graph-${Date.now()}`, graph);
    this.graphContainer.nativeElement.innerHTML = svg;
  }
}
