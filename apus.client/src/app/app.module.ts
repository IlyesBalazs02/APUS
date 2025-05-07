import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { CommonModule } from '@angular/common';
import { NgTemplateOutlet } from '@angular/common';
import { NgComponentOutlet } from '@angular/common';

// COMPONENTS
import { ActivitySelectorComponent } from './activities/components/activity-selector/activity-selector.component';
import { CreateActivityComponent } from './activities/components/create-activity/create-activity.component';
import { FooterComponent } from './footer/footer.component';
import { NavigationComponent } from './navigation/navigation.component';
import { ListActivitiesComponent } from './activities/components/list-activities/list-activities.component';
import { ListActivitiesDifferentApproachComponent } from './activities/components/list-activities-different-approach/list-activities-different-approach.component';
import { RunningActivityComponent } from './activities/components/list-activities-different-approach/ActivityCards/running-activity/running-activity.component';
import { ListActivitiesNewMethodComponent } from './activities/components/list-activities-new-method/list-activities-new-method.component';

// FORMS
import { ReactiveFormsModule } from '@angular/forms';
import { FormlyModule } from '@ngx-formly/core';
import { FormlyBootstrapModule } from '@ngx-formly/bootstrap';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

//SANDBOX
import { MapSandBoxComponent } from './Testing/map-sand-box/map-sand-box.component';
import { UploadGpxFileComponent } from './Testing/upload-gpx-file/upload-gpx-file.component';
import { UploadActivityComponent } from './activities/components/upload-activity/upload-activity.component';

@NgModule({
  declarations: [
    AppComponent,
    ActivitySelectorComponent,
    CreateActivityComponent,
    FooterComponent,
    NavigationComponent,
    ListActivitiesComponent,
    ListActivitiesDifferentApproachComponent,
    RunningActivityComponent,
    MapSandBoxComponent,
    ListActivitiesNewMethodComponent,
    UploadGpxFileComponent,
    UploadActivityComponent
  ],
  imports: [
    BrowserModule,

    ReactiveFormsModule,
    FormlyModule.forRoot(),       // ← core first
    FormlyBootstrapModule,        // ← then the Bootstrap UI
    FormsModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatInputModule,

    AppRoutingModule,
    HttpClientModule,
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
