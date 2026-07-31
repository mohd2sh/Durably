export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  take: number;
}

export interface ExecutionSummary {
  flowName: string;
  instanceId: string;
  status: ExecutionStatus;
  currentStep: number;
  attempts: number;
  failedStep?: string | null;
  errorMessage?: string | null;
  createdAt: string;
  updatedAt: string;
  metadataJson?: string | null;
}

export interface ExecutionDetail extends ExecutionSummary {
  contextJson: string;
  version: number;
  lockedBy?: string | null;
  lockedUntil?: string | null;
}

export interface TraceRecord {
  flowName: string;
  instanceId: string;
  stepKey: string;
  attempt: number;
  outcome: TraceOutcome;
  inputJson?: string | null;
  outputJson?: string | null;
  durationMs: number;
  exceptionMessage?: string | null;
  timestamp: string;
}

export enum ExecutionStatus {
  Running = 0,
  Completed = 1,
  Failed = 2
}

export enum TraceOutcome {
  Succeeded = 0,
  Failed = 1,
  Skipped = 2
}

export interface ExecutionSearchParams {
  flowName?: string;
  status?: ExecutionStatus;
  instanceId?: string;
  from?: string;
  to?: string;
  metadataKey?: string;
  metadataValue?: string;
  skip?: number;
  take?: number;
}
