import { NgModule } from "@angular/core";
import { FooterComponent } from "./components/footer/footer.component";
import { NavigationComponent } from "./components/navigation/navigation.component";
import { CommonModule } from "@angular/common";
import { RouterModule } from "@angular/router";
import { ActivityCardComponent } from "./activity-card/activity-card.component";
import { FriendSearchComponent } from "./components/friend-search/friend-search.component";

@NgModule({
    declarations: [
        FooterComponent,
        NavigationComponent,
        ActivityCardComponent,
    ],
    imports: [
        CommonModule,
        RouterModule,
        FriendSearchComponent,
    ],
    exports: [
        CommonModule,
        RouterModule,
        FooterComponent,
        NavigationComponent,
        ActivityCardComponent,
        FriendSearchComponent,
    ]
})

export class SharedModule { }
