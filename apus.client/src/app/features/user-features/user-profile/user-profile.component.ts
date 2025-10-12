import { Component, OnInit } from '@angular/core';
import { profiledto } from './ProfileDto';
import { HttpClient } from '@angular/common/http';
import { ActivityDto } from '../../activities/ActivityDto/ActivityDto';
import { ActivityService } from '../../../core/services/activityService';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-user-profile',
  standalone: false,
  templateUrl: './user-profile.component.html',
  styleUrl: './user-profile.component.css'
})

export class UserProfileComponent implements OnInit {
  profile?: profiledto;
  activities?: ActivityDto[];

  constructor(private activityService: ActivityService, private http: HttpClient, private route: ActivatedRoute) { }

  ngOnInit(): void {
    const userId = this.route.snapshot.paramMap.get('id');

    // Maybe dont redirect the current user's profile ?????
    const url = userId ? `/api/profile/${userId}` : `/api/profile/me`;

    this.http.get<profiledto>(url).subscribe({
      next: (data) => (this.profile = data),
      error: (err) => console.error('Failed to load profile', err)
    });

    this.activityService
      .getUserActivities()
      .subscribe((dtos: ActivityDto[]) => (this.activities = dtos));
  }

}
