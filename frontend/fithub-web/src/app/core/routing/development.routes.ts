import { Routes } from '@angular/router';

export const developmentRoutes: Routes = [
  {
    path: 'design-system',
    loadComponent: () => import('../../features/design-system/design-system-page').then(m => m.DesignSystemPage),
  },
];
