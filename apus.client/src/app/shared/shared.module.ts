import { NgModule } from "@angular/core";
import { FooterComponent } from "./components/footer/footer.component";
import { NavigationComponent } from "./components/navigation/navigation.component";
import { CommonModule } from "@angular/common";
import { RouterModule } from "@angular/router";
import { ActivityCardComponent } from "./activity-card/activity-card.component";

@NgModule({
    declarations: [
        FooterComponent,
        NavigationComponent,
        ActivityCardComponent
    ],
    imports: [
        CommonModule,
        RouterModule
    ],
    exports: [
        CommonModule,
        RouterModule,
        FooterComponent,
        NavigationComponent,
        ActivityCardComponent
    ]
})

export class SharedModule { }
