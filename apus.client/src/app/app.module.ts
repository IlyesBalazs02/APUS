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
import { UploadActivityComponent } from './activities/components/upload-activity/upload-activity.component';
import { DisplayActivityComponent } from './activities/components/display-activity/display-activity.component';
import { DisplayActivitiesComponent } from './activities/components/display-activities/display-activities.component';
import { ActivityCardComponent } from './activities/components/display-activities/activity-card/activity-card.component';

// FORMS
import { ReactiveFormsModule } from '@angular/forms';
import { FormlyModule } from '@ngx-formly/core';
import { FormlyBootstrapModule } from '@ngx-formly/bootstrap';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

// Card
import { MatCardModule } from '@angular/material/card';

//SANDBOX
import { MapSandBoxComponent } from './Testing/map-sand-box/map-sand-box.component';
import { UploadGpxFileComponent } from './Testing/upload-gpx-file/upload-gpx-file.component';
import { EditActivityComponent } from './activities/components/edit-activity/edit-activity.component';

@NgModule({
  declarations: [
    AppComponent,
    ActivitySelectorComponent,
    CreateActivityComponent,
    FooterComponent,
    NavigationComponent,
    UploadGpxFileComponent,
    UploadActivityComponent,
    DisplayActivityComponent,
    DisplayActivitiesComponent,
    ActivityCardComponent,
    EditActivityComponent
  ],
  imports: [
    BrowserModule,

    ReactiveFormsModule,
    FormlyModule.forRoot(),
    FormlyBootstrapModule,
    FormsModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatInputModule,
    AppRoutingModule,
    HttpClientModule,
    MapSandBoxComponent,

    MatCardModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
