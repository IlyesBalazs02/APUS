import { Component, OnInit, OnDestroy } from '@angular/core';
import { profiledto } from './ProfileDto';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { ActivityDto } from '../../activities/ActivityDto/ActivityDto';
import { ActivityService } from '../../../core/services/activityService';
import { Subscription, switchMap } from 'rxjs';

@Component({
  selector: 'app-user-profile',
  standalone: false,
  templateUrl: './user-profile.component.html',
  styleUrl: './user-profile.component.css'
})
export class UserProfileComponent implements OnInit, OnDestroy {
  profile?: profiledto;
  activities?: ActivityDto[];
  private sub = new Subscription();

  constructor(
    private activityService: ActivityService,
    private http: HttpClient,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    this.sub.add(
      this.route.paramMap.subscribe(params => {
        const userId = params.get('id');
        const profileUrl = userId ? `/api/userprofile/${userId}` : `/api/userprofile/me`;

        // Load profile
        this.http.get<profiledto>(profileUrl).subscribe({
          next: (data) => (this.profile = data),
          error: (err) => console.error('Failed to load profile', err)
        });

        // Load activities for correct user
        if (userId)
          this.activityService
            .getUserActivitiesById(userId)
            .subscribe(acts => (this.activities = acts));
        else
          this.activityService
            .getUserActivities()
            .subscribe(acts => (this.activities = acts));
      })
    );
  }

  ngOnDestroy() {
    this.sub.unsubscribe();
  }
}
