import { NgModule } from "@angular/core";
import { FooterComponent } from "./components/footer/footer.component";
import { NavigationComponent } from "./components/navigation/navigation.component";
import { CommonModule } from "@angular/common";
import { RouterModule } from "@angular/router";
import { ActivityCardComponent } from "./activity-card/activity-card.component";
import { SearchBarComponent } from "../features/search/search-bar/search-bar.component";
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { FormsModule } from "@angular/forms";
import { CommentsModalComponent } from "./comments-modal/comments-modal.component";

@NgModule({
    declarations: [
        FooterComponent,
        NavigationComponent,
        ActivityCardComponent,
        CommentsModalComponent
    ],
    imports: [
        CommonModule,
        RouterModule,
        SearchBarComponent,
        MatBadgeModule,
        MatButtonModule,
        FormsModule
    ],
    exports: [
        CommonModule,
        RouterModule,
        FooterComponent,
        NavigationComponent,
        ActivityCardComponent,
        SearchBarComponent,
        CommentsModalComponent
    ]
})

export class SharedModule { }
