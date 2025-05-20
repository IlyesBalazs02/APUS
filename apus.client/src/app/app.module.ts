import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
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
import { CreateActivityComponent } from './activities/components/create-activity/create-activity.component';
import { FooterComponent } from './footer/footer.component';
import { NavigationComponent } from './navigation/navigation.component';
import { UploadActivityComponent } from './activities/components/upload-activity/upload-activity.component';
import { DisplayActivityComponent } from './activities/components/display-activity/display-activity.component';
import { DisplayActivitiesComponent } from './activities/components/display-activities/display-activities.component';
import { ActivityCardComponent } from './activities/components/display-activities/activity-card/activity-card.component';
import { ActivityMapComponent } from './activities/components/display-activity/activity-map/activity-map.component';

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
import { RegisterComponent } from './auth/register/register.component';
import { JwtInterceptor } from './interceptors/jwt.interceptor';
import { LoginComponent } from './auth/login/login.component';
import { HomeComponentComponent } from './home-component/home-component.component';

//DisplayActivity
import { NgChartsModule } from 'ng2-charts';
import { CreateRouteComponent } from './create-route/create-route.component';

@NgModule({
  declarations: [
    AppComponent,
    CreateActivityComponent,
    FooterComponent,
    NavigationComponent,
    UploadGpxFileComponent,
    UploadActivityComponent,
    DisplayActivityComponent,
    DisplayActivitiesComponent,
    ActivityCardComponent,
    EditActivityComponent,
    ActivityMapComponent,
    RegisterComponent,
    LoginComponent,
    HomeComponentComponent,
    CreateRouteComponent
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

    MatCardModule,

    NgChartsModule
  ],
  providers: [{ provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
