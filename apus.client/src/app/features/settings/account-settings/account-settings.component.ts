import { Component } from '@angular/core';

@Component({
  selector: 'app-account-settings',
  standalone: false,
  templateUrl: './account-settings.component.html',
  styleUrls: ['./account-settings.component.scss']
})
export class AccountSettingsComponent {
  email = 'ilyesbalazs32@gmail.com';
  gender = '';
  showEmailModal = false;
  showPasswordModal = false;
  showGenderModal = false;

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
    console.log('Email updated:', newEmail);
    this.email = newEmail;
    this.closeModal();
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
