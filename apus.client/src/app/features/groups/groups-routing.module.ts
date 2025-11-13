import { RouterModule, Routes } from "@angular/router";
import { AuthGuard } from "../../core/guards/auth.guard";
import { NgModule } from "@angular/core";
import { GroupsComponent } from "./groups.component";
import { groupResolver } from "./group.resolver";
import { GroupsHomeComponent } from "./groups-home/groups-home.component";
import { GroupsRequestComponent } from "./groups-request/groups-request.component";
import { GroupsPostComponent } from "./groups-post/groups-post.component";
import { GroupsSettingsComponent } from "./groups-settings/groups-settings.component";

const routes: Routes = [
    {
        path: '',
        canActivateChild: [AuthGuard],
        children: [
            { path: '', component: GroupsHomeComponent },
            {
                path: ':id',
                component: GroupsComponent,
                resolve: { group: groupResolver },
                children: [
                    { path: 'requests', component: GroupsRequestComponent },
                    { path: 'posts', component: GroupsPostComponent },
                    { path: 'settings', component: GroupsSettingsComponent }
                ]
            }
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class GroupsRoutingModule { }