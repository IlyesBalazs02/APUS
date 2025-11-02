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
  isLoading = false;
  isSaving = false;

  // Avatar
  avatarPreview = '/Perm/DefaultProfile.png'; // default small circle
  private currentAvatarUrl: string | null | undefined;

  // Modal state (like account settings)
  showAvatarModal = false;
  selectedAvatarFile: File | null = null;
  isUploading = false;

  constructor(
    private fb: FormBuilder,
    private profileService: ProfileService
  ) { }

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
        this.currentAvatarUrl = res.avatarUrl || null;
        this.avatarPreview = this.currentAvatarUrl || '/Perm/DefaultProfile.png';
      },
      error: (err) => {
        console.error('Failed to load profile', err?.error || err);
      },
      complete: () => (this.isLoading = false),
    });
  }

  onSave(): void {
    if (this.profileForm.invalid) return;
    this.isSaving = true;
    const payload = this.profileForm.value as Pick<ProfileDto, 'firstName' | 'lastName' | 'bio'>;

    this.profileService.updateProfile(payload).subscribe({
      next: (res) => {
        console.log(res.message);
      },
      error: (err) => {
        console.error('Failed to update profile', err?.error || err);
      },
      complete: () => (this.isSaving = false),
    });
  }

  // ===== Avatar modal handlers =====
  openAvatarModal(): void {
    console.log('[avatar] openAvatarModal()');   // debug
    this.selectedAvatarFile = null;
    this.showAvatarModal = true;
  }

  closeAvatarModal(): void {
    this.showAvatarModal = false;
    this.selectedAvatarFile = null;
  }

  onAvatarFilePicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedAvatarFile = input.files?.[0] ?? null;
  }

  saveAvatar(): void {
    if (!this.selectedAvatarFile) return;

    this.isUploading = true;
    this.profileService.uploadAvatar(this.selectedAvatarFile).subscribe({
      next: () => {
        // Always reload from server so we never use local preview
        this.loadProfile();
        this.closeAvatarModal();
      },
      error: (err) => {
        console.error('Avatar upload failed', err?.error || err);
      },
      complete: () => (this.isUploading = false),
    });
  }

  deleteAvatar(): void {
    this.profileService.deleteAvatar().subscribe({
      next: () => {
        this.currentAvatarUrl = null;
        this.avatarPreview = ''; // server will give default on next load
        this.loadProfile();
        this.closeAvatarModal();
      },
      error: (err) => {
        console.error('Delete avatar failed', err?.error || err);
      }
    });
  }

}
