import { CreateActivityComponent } from "./components/create-activity/create-activity.component";
import { ActivityCardComponent } from "./components/display-activities/activity-card/activity-card.component";
import { DisplayActivitiesComponent } from "./components/display-activities/display-activities.component";
import { ActivityMapComponent } from "./components/display-activity/activity-map/activity-map.component";
import { DisplayActivityComponent } from "./components/display-activity/display-activity.component";
import { EditActivityComponent } from "./components/edit-activity/edit-activity.component";
import { UploadActivityComponent } from "./components/upload-activity/upload-activity.component";
import { ActivitiesRoutingModule } from "./activities-routing.module";
import { NgModule } from "@angular/core";
import { NgChartsModule } from 'ng2-charts';
import { MatExpansionModule } from "@angular/material/expansion";
import { FormsModule, NgModel, ReactiveFormsModule } from "@angular/forms";
import { FormlyModule } from "@ngx-formly/core";
import { FormlyBootstrapModule } from "@ngx-formly/bootstrap";
import { CommonModule } from "@angular/common";

@NgModule({
    declarations: [
        CreateActivityComponent,
        UploadActivityComponent,
        DisplayActivitiesComponent,
        DisplayActivityComponent,
        ActivityCardComponent,
        EditActivityComponent,
        ActivityMapComponent,
    ],
    imports: [
        CommonModule,
        FormlyModule.forChild(),
        FormlyBootstrapModule,
        ActivitiesRoutingModule,
        NgChartsModule,
        MatExpansionModule,
        FormsModule,
        ReactiveFormsModule
    ]
})
export class ActivitiesModule { }
