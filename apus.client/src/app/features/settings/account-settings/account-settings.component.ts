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

  constructor(private accountService: AccountService, private authService: AuthService) { }

  ngOnInit(): void {
    this.email = this.authService.currentUserEmail();
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
    console.log('Password changed');
    this.closeModal();
  }

  saveGender(g: string) {
    this.gender = g;
    console.log('Gender set to:', g);
    this.closeModal();
  }
}
