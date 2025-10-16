// features/settings/settings.module.ts
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { SettingsComponent } from './settings.component';
import { SettingsRoutingModule } from './settings-routing.module';

import { AccountSettingsComponent } from './account-settings/account-settings.component';
import { ProfileSettingsComponent } from './profile-settings/profile-settings.component';
import { PrivacySettingsComponent } from './privacy-settings/privacy-settings.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';


@NgModule({
    declarations: [
        SettingsComponent,
        AccountSettingsComponent,
        ProfileSettingsComponent,
        PrivacySettingsComponent
    ],
    imports: [
        CommonModule,
        RouterModule,
        SettingsRoutingModule,
        ReactiveFormsModule,
        FormsModule
    ],
})
export class SettingsModule { }
