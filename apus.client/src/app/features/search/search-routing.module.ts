import { RouterModule, Routes } from "@angular/router";
import { AuthGuard } from "../../core/guards/auth.guard";
import { SearchComponent } from "./search.component";
import { UserSearchComponent } from "./user-search/user-search.component";
import { FriendSearchComponent } from "./friend-search/friend-search.component";
import { GroupSearchComponent } from "./group-search/group-search.component";
import { NgModule } from "@angular/core";
import { FriendRequestsComponent } from "./friend-requests/friend-requests.component";

const routes: Routes = [
    {
        path: '',
        canActivateChild: [AuthGuard],
        component: SearchComponent,
        children: [
            { path: 'users', component: UserSearchComponent },
            { path: 'friends', component: FriendSearchComponent },
            { path: 'groups', component: GroupSearchComponent },
            { path: 'requests', component: FriendRequestsComponent },
            { path: '', redirectTo: 'users', pathMatch: 'full' },
        ],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class SearchRoutingModule { }