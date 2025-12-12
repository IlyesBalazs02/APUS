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

  selectedAvatarFile: File | null = null;
  selectedAvatarPreview: string | null = null;
  avatarUrl: string | null = null;
  isDefaultAvatar = false;
  private readonly DEFAULT_HINTS = ['/Perm/DefaultProfile.png', '/api/Profile/default-avatar'];

  showAvatarModal = false;
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

  openAvatarModal(): void {
    this.selectedAvatarFile = null;
    this.selectedAvatarPreview = null;
    this.showAvatarModal = true;
  }

  closeAvatarModal(): void {
    this.showAvatarModal = false;
    this.selectedAvatarFile = null;
    this.selectedAvatarPreview = null;
  }

  onAvatarFilePicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedAvatarFile = input.files?.[0] ?? null;

    this.selectedAvatarPreview = null;
    if (this.selectedAvatarFile) {
      const reader = new FileReader();
      reader.onload = () => (this.selectedAvatarPreview = reader.result as string);
      reader.readAsDataURL(this.selectedAvatarFile);
    }
  }

  saveAvatar(): void {
    if (!this.selectedAvatarFile) return;
    this.isUploading = true;

    this.profileService.uploadAvatar(this.selectedAvatarFile).subscribe({
      next: () => {
        this.loadProfile();
        this.closeAvatarModal();
      },
      error: (err) => console.error('Avatar upload failed', err?.error || err),
      complete: () => (this.isUploading = false),
    });
  }
  deleteAvatar(): void {
    this.profileService.deleteAvatar().subscribe({
      next: () => {
        this.loadProfile();
        this.closeAvatarModal();
      },
      error: (err) => console.error('Delete avatar failed', err?.error || err),
    });
  }

  private loadProfile(): void {
    this.isLoading = true;
    this.profileService.getProfile().subscribe({
      next: (res) => {
        this.profileForm.patchValue({
          firstName: res.firstName || '',
          lastName: res.lastName || '',
          bio: res.bio || '',
        });
        this.avatarUrl = res.avatarUrl || null;

        this.markDefaultFlag(this.avatarUrl);
      },
      error: (err) => console.error('Failed to load profile', err?.error || err),
      complete: () => (this.isLoading = false),
    });
  }

  private markDefaultFlag(url: string | null) {
    const path = this.getPathname(url);
    this.isDefaultAvatar = path.endsWith("/perm/defaultprofile.png");
  }

  private getPathname(url: string | null): string {
    if (!url) return "";
    try {
      const u = new URL(url, window.location.origin);
      return u.pathname.toLowerCase();
    } catch {
      return (url || "").toLowerCase();
    }
  }
}
