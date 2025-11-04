import { NgModule } from "@angular/core";
import { FriendSearchComponent } from "./friend-search/friend-search.component";
import { UserSearchComponent } from "./user-search/user-search.component";
import { GroupSearchComponent } from "./group-search/group-search.component";
import { CommonModule } from "@angular/common";
import { RouterModule } from "@angular/router";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { SearchComponent } from "./search.component";
import { SearchRoutingModule } from "./search-routing.module";
import { FriendRequestsComponent } from "./friend-requests/friend-requests.component";

@NgModule({
    declarations: [
        SearchComponent,
        FriendSearchComponent,
        UserSearchComponent,
        GroupSearchComponent,
        FriendRequestsComponent
    ],
    imports: [
        CommonModule,
        RouterModule,
        SearchRoutingModule,
        ReactiveFormsModule,
        FormsModule
    ],
})

export class SearchModule { }