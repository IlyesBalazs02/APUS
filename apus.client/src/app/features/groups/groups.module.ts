import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { GroupsRoutingModule } from './groups-routing.module';
import { GroupsHomeComponent } from './groups-home/groups-home.component';
import { GroupsComponent } from './groups.component';
import { GroupsRequestComponent } from './groups-request/groups-request.component';
import { GroupsPostComponent } from './groups-post/groups-post.component';
import { GroupsSettingsComponent } from './groups-settings/groups-settings.component';

import { SharedModule } from '../../shared/shared.module';
import { GroupsEventComponent } from './groups-event/groups-event.component';

@NgModule({
    declarations: [
        GroupsHomeComponent,
        GroupsComponent,
        GroupsRequestComponent,
        GroupsPostComponent,
        GroupsSettingsComponent,
        GroupsEventComponent
    ],
    imports: [
        CommonModule,
        RouterModule,
        GroupsRoutingModule,
        ReactiveFormsModule,
        FormsModule,
        SharedModule
    ],
})
export class GroupsModule { }
