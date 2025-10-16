import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';

@Component({
  selector: 'app-profile-settings',
  standalone: false,
  templateUrl: './profile-settings.component.html',
  styleUrls: ['./profile-settings.component.scss']
})
export class ProfileSettingsComponent implements OnInit {
  profileForm!: FormGroup;
  avatarPreview: string = 'assets/images/default-avatar.png';

  constructor(private fb: FormBuilder) { }

  ngOnInit(): void {
    this.profileForm = this.fb.group({
      bio: [''],
      avatar: [null]
    });

    // Load profile data later if needed:
    // this.settingsService.getProfileSettings().subscribe(data => this.profileForm.patchValue(data));
  }

  onAvatarChange(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.profileForm.patchValue({ avatar: file });
      const reader = new FileReader();
      reader.onload = () => (this.avatarPreview = reader.result as string);
      reader.readAsDataURL(file);
    }
  }

  onSave(): void {
    if (this.profileForm.valid) {
      console.log('Profile settings to send:', this.profileForm.value);
      // this.settingsService.updateProfileSettings(this.profileForm.value).subscribe(...)
    }
  }
}
