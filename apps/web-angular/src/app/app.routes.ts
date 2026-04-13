import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'engagements',
    pathMatch: 'full',
  },
  {
    path: 'engagements',
    loadComponent: () =>
      import('./features/engagements/engagement-list/engagement-list.component')
        .then(m => m.EngagementListComponent),
  },
  {
    path: 'engagements/:id',
    loadComponent: () =>
      import('./features/engagements/engagement-detail/engagement-detail.component')
        .then(m => m.EngagementDetailComponent),
  },
];
