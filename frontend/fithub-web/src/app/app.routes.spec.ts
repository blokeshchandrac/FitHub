import { Route } from '@angular/router';
import { createRoutes } from './app.routes';
import { developmentRoutes } from './core/routing/development.routes';
import { developmentRoutes as productionDevelopmentRoutes } from './core/routing/development.routes.production';
import { anonymousLandingGuard } from './core/routing/anonymous-landing.guard';
import { administratorGuard } from './core/authentication/authentication.guards';

describe('application routes', () => {
  it('does not expose the design-system preview in Production', () => {
    expect(findRoute(createRoutes(productionDevelopmentRoutes), 'design-system')).toBeUndefined();
  });

  it('exposes the design-system preview in Development', () => {
    expect(findRoute(createRoutes(developmentRoutes), 'design-system')).toBeDefined();
  });

  it('protects every lazy simulation page behind the Administrator guard', () => {
    const route = findRoute(createRoutes(productionDevelopmentRoutes), 'simulation');
    expect(route).toBeDefined();
    expect(route?.children?.find((child) => child.path === '')?.loadComponent).toBeTypeOf(
      'function',
    );
    expect(route?.children?.find((child) => child.path === 'equipment')?.loadComponent).toBeTypeOf(
      'function',
    );
    expect(route?.children?.find((child) => child.path === 'parking')?.loadComponent).toBeTypeOf(
      'function',
    );
    expect(route?.canActivate).toContain(administratorGuard);
    expect(route?.children?.map((child) => child.path)).toEqual(['', 'equipment', 'parking']);
  });

  it('uses a dedicated lazy public landing page at the root route', () => {
    const publicLayout = createRoutes(productionDevelopmentRoutes).find(
      (route) => route.path === '' && route.component,
    );
    const landing = publicLayout?.children?.find(
      (child) => child.path === '' && child.pathMatch === 'full',
    );
    expect(landing?.loadComponent).toBeTypeOf('function');
    expect(landing?.redirectTo).toBeUndefined();
    expect(landing?.canActivate).toContain(anonymousLandingGuard);
  });
});

function findRoute(routes: readonly Route[], path: string): Route | undefined {
  for (const route of routes) {
    if (route.path === path) return route;
    const child = route.children ? findRoute(route.children, path) : undefined;
    if (child) return child;
  }
  return undefined;
}
