import { Routes } from '@angular/router';

import { ExecutionDetailComponent } from './features/executions/execution-detail/execution-detail.component';
import { ExecutionListComponent } from './features/executions/execution-list/execution-list.component';

export const routes: Routes = [
  { path: '', component: ExecutionListComponent },
  { path: 'executions/:flowName/:instanceId', component: ExecutionDetailComponent }
];
