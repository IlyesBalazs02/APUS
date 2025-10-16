import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';

@Component({
  selector: 'app-privacy-settings',
  standalone: false,
  templateUrl: './privacy-settings.component.html',
  styleUrls: ['./privacy-settings.component.scss']
})
export class PrivacySettingsComponent implements OnInit {
  privacyForm!: FormGroup;

  constructor(private fb: FormBuilder) { }

  ngOnInit(): void {
    this.privacyForm = this.fb.group({
      allowFollow: [false],
      activityVisibility: ['Everyone'],
      profileVisibility: ['Everyone']
    });

    // load user data from API here and patch form
  }

  onSave(): void {
    if (this.privacyForm.valid) {
      const updatedValues = this.privacyForm.value;
      console.log('Privacy settings to send:', updatedValues);
      // later: call  backend API, e.g.
      // this.settingsService.updatePrivacySettings(updatedValues).subscribe(...)
    }
  }
}