import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { HomeComponentComponent } from './shared/components/home-component/home-component.component';
import { AuthGuard } from './core/guards/auth.guard';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { CreateRouteComponent } from './features/create-route/create-route.component';

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },

  { path: 'createRoute', component: CreateRouteComponent, canActivate: [AuthGuard] },

  {
    path: 'activities',
    loadChildren: () =>
      import('./features/activities/activities.module')
        .then(m => m.ActivitiesModule)
  },

  { path: '', component: HomeComponentComponent, canActivate: [AuthGuard] },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
