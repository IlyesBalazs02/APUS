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

//angular material
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';


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
        FormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        MatCardModule,
        MatButtonModule,
        MatProgressSpinnerModule
    ],
})

export class SearchModule { }