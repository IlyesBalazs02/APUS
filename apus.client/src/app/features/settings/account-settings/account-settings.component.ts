import { Component } from '@angular/core';
import { AccountService } from './account.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-account-settings',
  standalone: false,
  templateUrl: './account-settings.component.html',
  styleUrls: ['./account-settings.component.scss']
})
export class AccountSettingsComponent {
  email: string | null = null;
  gender = '';
  showEmailModal = false;
  showPasswordModal = false;
  showGenderModal = false;
  selectedGender: string | null = null;

  constructor(private accountService: AccountService, private authService: AuthService) { }

  ngOnInit(): void {
    this.email = this.authService.currentUserEmail();

    this.accountService.getGender().subscribe({
      next: (res) => {
        if (res.gender) {
          this.gender = res.gender;
          this.selectedGender = res.gender; // auto-select in modal
        }
      },
      error: (err) => {
        console.error('Failed to load gender:', err);
      }
    });
  }


  // Modal control
  openModal(type: 'email' | 'password' | 'gender') {
    if (type === 'email') this.showEmailModal = true;
    if (type === 'password') this.showPasswordModal = true;
    if (type === 'gender') this.showGenderModal = true;
  }

  closeModal() {
    this.showEmailModal = false;
    this.showPasswordModal = false;
    this.showGenderModal = false;
  }

  saveEmail(newEmail: string, password: string) {
    if (!newEmail || !password) return;

    this.accountService.changeEmail(password, newEmail).subscribe({
      next: (res) => {
        console.log(res.message);
        this.email = newEmail;
        this.closeModal();
      },
      error: (err) => {
        console.error('Failed to change email:', err.error);
        alert(err.error || 'Something went wrong.');
      }
    });
  }

  savePassword(current: string, newPass: string, confirm: string) {
    if (newPass !== confirm || !current) return;

    this.accountService.changePassword(current, newPass).subscribe({
      next: (res) => {
        console.log(res.message);
        this.closeModal();
      },
      error: (err) => {
        console.error('Failed to change password', err.error);
        alert(err.error || 'Something went wrong');
      }
    });
  }

  saveGender(g: string) {
    this.gender = g;
    this.accountService.changeGender(g).subscribe({
      next: (res) => {
        console.log(res.message);
        this.closeModal();
      },
      error: (err) => {
        console.error('Failed to change the gender', err.error);
        alert(err.error || 'Something went wrong');
      }
    });
  }
}
