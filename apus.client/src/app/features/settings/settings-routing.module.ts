import { RouterModule, Routes } from "@angular/router";
import { SettingsComponent } from "./settings.component";
import { AccountSettingsComponent } from "./account-settings/account-settings.component";
import { ProfileSettingsComponent } from "./profile-settings/profile-settings.component";
import { PrivacySettingsComponent } from "./privacy-settings/privacy-settings.component";
import { NgModule } from "@angular/core";
import { AuthGuard } from "../../core/guards/auth.guard";

const routes: Routes = [
    {
        path: '',
        canActivateChild: [AuthGuard],
        component: SettingsComponent,
        children: [
            { path: 'account', component: AccountSettingsComponent },
            { path: 'profile', component: ProfileSettingsComponent },
            { path: 'privacy', component: PrivacySettingsComponent },
            { path: '', redirectTo: 'account', pathMatch: 'full' },
        ],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class SettingsRoutingModule { }