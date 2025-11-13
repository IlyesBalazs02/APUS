import { RouterModule, Routes } from "@angular/router";
import { AuthGuard } from "../../core/guards/auth.guard";
import { NgModule } from "@angular/core";
import { GroupsComponent } from "./groups.component";
import { groupResolver } from "./group.resolver";
import { GroupsHomeComponent } from "./groups-home/groups-home.component";
import { GroupsRequestComponent } from "./groups-request/groups-request.component";

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