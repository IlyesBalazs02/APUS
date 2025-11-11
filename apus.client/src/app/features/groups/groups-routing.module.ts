import { RouterModule, Routes } from "@angular/router";
import { AuthGuard } from "../../core/guards/auth.guard";
import { NgModule } from "@angular/core";
import { GroupsComponent } from "./groups.component";
import { groupResolver } from "./group.resolver";
import { GroupsHomeComponent } from "./groups-home/groups-home.component";

const routes: Routes = [
    {
        path: '',
        canActivateChild: [AuthGuard],
        children: [
            { path: '', component: GroupsHomeComponent },
            { path: ':id', component: GroupsComponent, resolve: { group: groupResolver } }

        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class GroupsRoutingModule { }