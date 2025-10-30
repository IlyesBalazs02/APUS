import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ProfileDto, ProfileService } from './Profile.service';

@Component({
  selector: 'app-profile-settings',
  standalone: false,
  templateUrl: './profile-settings.component.html',
  styleUrls: ['./profile-settings.component.scss']
})
export class ProfileSettingsComponent implements OnInit {
  profileForm!: FormGroup;
  avatarPreview = '';
  isLoading = false;
  isSaving = false;

  constructor(private fb: FormBuilder,
    private profileService: ProfileService) { }

  ngOnInit(): void {
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.maxLength(100)]],
      lastName: ['', [Validators.maxLength(100)]],
      bio: ['', [Validators.maxLength(300)]],
    });

    this.loadProfile();
  }

  private loadProfile(): void {
    this.isLoading = true;
    this.profileService.getProfile().subscribe({
      next: (res: ProfileDto) => {
        this.profileForm.patchValue({
          firstName: res.firstName || '',
          lastName: res.lastName || '',
          bio: res.bio || '',
        });
      },
      error: (err) => {
        console.error('Failed to load profile', err?.error || err);
      },
      complete: () => (this.isLoading = false)
    });
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
    if (this.profileForm.invalid) return;

    const payload: ProfileDto = this.profileForm.value;
    this.isSaving = true;

    this.profileService.updateProfile(payload).subscribe({
      next: (res) => {
        console.log(res.message);
        // optional: show success toast
      },
      error: (err) => {
        console.error('Failed to update profile', err?.error || err);
        // optional: show error toast
      },
      complete: () => (this.isSaving = false)
    });
  }
}
