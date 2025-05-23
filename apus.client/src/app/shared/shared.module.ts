import { NgModule } from "@angular/core";
import { FooterComponent } from "./components/footer/footer.component";
import { NavigationComponent } from "./components/navigation/navigation.component";
import { CommonModule } from "@angular/common";
import { RouterModule } from "@angular/router";

@NgModule({
    declarations: [
        FooterComponent,
        NavigationComponent
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
    ]
})

export class SharedModule { }
