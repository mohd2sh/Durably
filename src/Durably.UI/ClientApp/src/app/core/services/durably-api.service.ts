import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { DEFAULT_PAGE_SIZE, EXECUTIONS_API_PATH } from '../../constants/api-routes';
import {
  ExecutionDetail,
  ExecutionSearchParams,
  ExecutionSummary,
  PagedResult,
  TraceRecord
} from '../models/execution.models';

@Injectable({ providedIn: 'root' })
export class DurablyApiService {
  constructor(private readonly http: HttpClient) {}

  searchExecutions(params: ExecutionSearchParams): Observable<PagedResult<ExecutionSummary>> {
    let httpParams = new HttpParams()
      .set('skip', String(params.skip ?? 0))
      .set('take', String(params.take ?? DEFAULT_PAGE_SIZE));

    httpParams = this.appendOptional(httpParams, 'flowName', params.flowName);
    httpParams = this.appendOptional(httpParams, 'instanceId', params.instanceId);
    httpParams = this.appendOptional(httpParams, 'from', params.from);
    httpParams = this.appendOptional(httpParams, 'to', params.to);
    httpParams = this.appendOptional(httpParams, 'metadataKey', params.metadataKey);
    httpParams = this.appendOptional(httpParams, 'metadataValue', params.metadataValue);

    if (params.status !== undefined && params.status !== null) {
      httpParams = httpParams.set('status', String(params.status));
    }

    return this.http.get<PagedResult<ExecutionSummary>>(EXECUTIONS_API_PATH, { params: httpParams });
  }

  getExecution(flowName: string, instanceId: string): Observable<ExecutionDetail> {
    const encodedFlow = encodeURIComponent(flowName);
    const encodedInstance = encodeURIComponent(instanceId);
    return this.http.get<ExecutionDetail>(`${EXECUTIONS_API_PATH}/${encodedFlow}/${encodedInstance}`);
  }

  getTraces(flowName: string, instanceId: string): Observable<TraceRecord[]> {
    const encodedFlow = encodeURIComponent(flowName);
    const encodedInstance = encodeURIComponent(instanceId);
    return this.http.get<TraceRecord[]>(
      `${EXECUTIONS_API_PATH}/${encodedFlow}/${encodedInstance}/traces`
    );
  }

  private appendOptional(params: HttpParams, key: string, value?: string): HttpParams {
    if (!value) {
      return params;
    }

    return params.set(key, value);
  }
}
