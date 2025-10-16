import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

@Component({
  selector: 'app-account-settings',
  standalone: false,
  templateUrl: './account-settings.component.html',
  styleUrls: ['./account-settings.component.scss']
})
export class AccountSettingsComponent implements OnInit {
  accountForm!: FormGroup;

  constructor(private fb: FormBuilder) { }

  ngOnInit(): void {
    this.accountForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: [''],
      password: [''],
      gender: ['']
    });

    // later load user data here
    // this.settingsService.getAccountSettings().subscribe(data => {
    //   this.accountForm.patchValue(data);
    // });
  }

  onSave(): void {
    if (this.accountForm.valid) {
      console.log('Account settings to send:', this.accountForm.value);
      // this.settingsService.updateAccountSettings(this.accountForm.value).subscribe(...)
    }
  }
}
