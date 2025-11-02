import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormControl } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { PrivacyDto, PrivacyService } from './privacy.service';

@Component({
  selector: 'app-privacy-settings',
  standalone: false,
  templateUrl: './privacy-settings.component.html',
  styleUrls: ['./privacy-settings.component.scss']
})
export class PrivacySettingsComponent implements OnInit {
  privacyForm!: FormGroup;
  isLoading = false;
  isSaving = false;
  hasError = false;
  successMessage = '';

  visibilityOptions = ['Everyone', 'Only followers', 'Only me'];

  constructor(
    private fb: FormBuilder,
    private privacyService: PrivacyService
  ) { }

  ngOnInit(): void {
    // Create disabled to avoid user edits before data arrives
    this.privacyForm = this.fb.group({
      allowFollow: new FormControl({ value: false, disabled: true }),
      activityVisibility: new FormControl({ value: 'Everyone', disabled: true }),
      profileVisibility: new FormControl({ value: 'Everyone', disabled: true })
    });

    this.loadPrivacySettings();
  }

  private loadPrivacySettings(): void {
    this.isLoading = true;
    this.hasError = false;

    this.privacyService.getMine()
      .pipe(finalize(() => {
        this.isLoading = false;
        // Enable after loading (even if there was an error, so user can retry)
        this.privacyForm.enable({ emitEvent: false });
      }))
      .subscribe({
        next: (data: PrivacyDto) => {
          this.privacyForm.patchValue({
            allowFollow: data.allowFollow,
            activityVisibility: this.normalizeVisibility(data.activityVisibility),
            profileVisibility: this.normalizeVisibility(data.profileVisibility)
          }, { emitEvent: false });
        },
        error: (err) => {
          console.error('Error loading privacy settings:', err);
          this.hasError = true;
        }
      });
  }

  onSave(): void {
    if (this.privacyForm.invalid) return;

    this.isSaving = true;
    this.successMessage = '';

    // Disable during save to prevent changes
    this.privacyForm.disable({ emitEvent: false });

    const dto: PrivacyDto = this.privacyForm.getRawValue(); // includes disabled values

    this.privacyService.updateMine(dto)
      .pipe(finalize(() => {
        this.isSaving = false;
        this.privacyForm.enable({ emitEvent: false });
      }))
      .subscribe({
        next: (resp) => {
          this.successMessage = 'Privacy settings saved successfully!';
          console.log('Saved:', resp);
        },
        error: (err) => {
          console.error('Error saving privacy settings:', err);
        }
      });
  }

  private normalizeVisibility(value: string): string {
    if (!value) return 'Everyone';
    const n = value.trim().toLowerCase();
    if (n === 'followers' || n === 'only followers') return 'Only followers';
    if (n === 'only me' || n === 'onlyme') return 'Only me';
    return 'Everyone';
  }
}
