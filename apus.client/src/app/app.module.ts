import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';

// COMPONENTS
import { ActivitySelectorComponent } from './activity-selector/activity-selector.component';
import { CreateActivityComponent } from './create-activity/create-activity.component';
import { FooterComponent } from './footer/footer.component';
import { NavigationComponent } from './navigation/navigation.component';
import { ListActivitiesComponent } from './list-activities/list-activities.component';

@NgModule({
  declarations: [
    AppComponent,
    ActivitySelectorComponent,
    CreateActivityComponent,
    FooterComponent,
    NavigationComponent,
    ListActivitiesComponent,
  ],
  imports: [
    BrowserModule, HttpClientModule,
    AppRoutingModule,
    FormsModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
