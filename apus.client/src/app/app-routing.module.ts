import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { HomeComponentComponent } from './shared/components/home-component/home-component.component';
import { AuthGuard } from './core/guards/auth.guard';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { CreateRouteComponent } from './features/create-route/create-route.component';
import { SettingsComponent } from './features/settings/settings.component';
import { UserProfileComponent } from './features/user-features/user-profile/user-profile.component';
import { DisplayUsersComponent } from './features/user-features/display-users/display-users.component';

const routes: Routes = [
  // Public auth routes
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },

  // Non-lazy feature routes (if you really want them non-lazy)
  { path: 'createRoute', component: CreateRouteComponent, canActivate: [AuthGuard] },
  { path: 'userprofile', component: UserProfileComponent, canActivate: [AuthGuard] },
  { path: 'displayUser', component: DisplayUsersComponent, canActivate: [AuthGuard] },
  { path: 'profile/:id', component: UserProfileComponent, canActivate: [AuthGuard] },

  // Lazy-loaded feature modules (protected where needed)
  {
    path: 'activities',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./features/activities/activities.module')
        .then(m => m.ActivitiesModule)
  },
  {
    path: 'settings',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./features/settings/settings.module')
        .then(m => m.SettingsModule)
  },
  {
    path: 'search',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./features/search/search.module')
        .then(m => m.SearchModule)
  },
  {
    path: 'groups',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./features/groups/groups.module')
        .then(m => m.GroupsModule)
  },

  // Root: redirect to activities (no guard here!)
  { path: '', redirectTo: '/activities', pathMatch: 'full' },

  // Wildcard: also send to activities (or to 404 page if you add one later)
  { path: '**', redirectTo: '/activities' },
];


@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
