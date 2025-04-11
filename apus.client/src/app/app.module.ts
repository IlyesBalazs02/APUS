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
import { ActivitySelectorComponent } from './activity-selector/activity-selector.component';
import { CreateActivityComponent } from './create-activity/create-activity.component';
import { FooterComponent } from './footer/footer.component';
import { NavigationComponent } from './navigation/navigation.component';
import { ListActivitiesComponent } from './list-activities/list-activities.component';
import { RunningActivityComponent } from './list-activities-different-approach/ActivityCards/running-activity/running-activity.component';
import { ListActivitiesDifferentApproachComponent } from './list-activities-different-approach/list-activities-different-approach.component';

@NgModule({
  declarations: [
    AppComponent,
    ActivitySelectorComponent,
    CreateActivityComponent,
    FooterComponent,
    NavigationComponent,
    ListActivitiesComponent,
    RunningActivityComponent,
    ListActivitiesDifferentApproachComponent
  ],
  imports: [
    BrowserModule, HttpClientModule,
    AppRoutingModule,
    FormsModule,
    CommonModule,
    NgTemplateOutlet,
    NgComponentOutlet
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
