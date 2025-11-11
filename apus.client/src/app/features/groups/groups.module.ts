import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { GroupsRoutingModule } from './groups-routing.module';
import { GroupsHomeComponent } from './groups-home/groups-home.component';
import { GroupsComponent } from './groups.component';


@NgModule({
    declarations: [
        GroupsHomeComponent,
        GroupsComponent
    ],
    imports: [
        CommonModule,
        RouterModule,
        GroupsRoutingModule,
        ReactiveFormsModule,
        FormsModule
    ],
})
export class GroupsModule { }