import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AuthGuard } from '../../core/guards/auth.guard';
import { DisplayActivitiesComponent } from './components/display-activities/display-activities.component';
import { CreateActivityComponent } from './components/create-activity/create-activity.component';
import { UploadActivityComponent } from './components/upload-activity/upload-activity.component';
import { DisplayActivityComponent } from './components/display-activity/display-activity.component';
import { EditActivityComponent } from './components/edit-activity/edit-activity.component';
import { ActivityMapComponent } from './components/display-activity/activity-map/activity-map.component';

const routes: Routes = [
    {
        path: '', //   /activities
        canActivateChild: [AuthGuard],
        children: [
            { path: '', component: DisplayActivitiesComponent },
            { path: 'create', component: CreateActivityComponent },
            { path: 'upload', component: UploadActivityComponent },
            { path: ':id', component: DisplayActivityComponent },
            { path: ':id/edit', component: EditActivityComponent },
            { path: ':id/map', component: ActivityMapComponent }
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class ActivitiesRoutingModule { }
