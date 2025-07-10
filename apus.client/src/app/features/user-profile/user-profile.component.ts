import { Component, OnInit } from '@angular/core';
import { profiledto } from './ProfileDto';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-user-profile',
  standalone: false,
  templateUrl: './user-profile.component.html',
  styleUrl: './user-profile.component.css'
})

export class UserProfileComponent implements OnInit {
  profile!: profiledto;

  constructor(private http: HttpClient) { }

  ngOnInit(): void {
    this.http.get<profiledto>('/api/profile/me') // replace with real ID or call to 'me'
      .subscribe({
        next: (data) => {
          this.profile = data;
        },
        error: (err) => {
          console.error('Failed to load profile', err);
        }
      });
  }

}
