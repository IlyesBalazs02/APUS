import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { profiledto } from './ProfileDto';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { ActivityDto } from '../../activities/ActivityDto/ActivityDto';
import { ActivityService } from '../../../core/services/activityService';
import { Observable, Subscription, switchMap } from 'rxjs';

@Component({
  selector: 'app-user-profile',
  standalone: false,
  templateUrl: './user-profile.component.html',
  styleUrl: './user-profile.component.css'
})
export class UserProfileComponent implements OnInit, OnDestroy {
  profile?: profiledto;

  // Paged list + state
  activities: ActivityDto[] = [];
  loading = false;
  hasMore = true;

  // Paging controls
  private pageSize = 10;
  private skip = 0;
  private requestToken = 0;
  private observer?: IntersectionObserver;
  private sub = new Subscription();
  private userId?: string;   // if undefined => "me" profile

  @ViewChild('sentinel', { static: true }) sentinelRef!: ElementRef<HTMLDivElement>;

  constructor(
    private activityService: ActivityService,
    private http: HttpClient,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    this.sub.add(
      this.route.paramMap.subscribe(params => {
        // Determine whose profile we are on
        this.userId = params.get('id') ?? undefined;

        // Reset paging when route changes
        this.activities = [];
        this.loading = false;
        this.hasMore = true;
        this.skip = 0;

        // Load profile header
        const profileUrl = this.userId ? `/api/userprofile/${this.userId}` : `/api/userprofile/me`;
        this.http.get<profiledto>(profileUrl).subscribe({
          next: data => (this.profile = data),
          error: err => console.error('Failed to load profile', err)
        });

        // Start infinite scrolling
        this.setupObserver();
        this.loadMore();
      })
    );
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    this.sub.unsubscribe();
  }

  private setupObserver(): void {
    this.observer?.disconnect(); // in case of route changes
    this.observer = new IntersectionObserver(entries => {
      if (entries.some(e => e.isIntersecting)) this.loadMore();
    }, { rootMargin: '400px 0px 400px 0px' });

    this.observer.observe(this.sentinelRef.nativeElement);
  }

  private loadMore(): void {
    if (this.loading || !this.hasMore) return;
    this.loading = true;
    const token = ++this.requestToken;

    const req$ = this.pickDataSource(this.skip, this.pageSize);
    this.sub.add(
      req$.subscribe({
        next: res => {
          if (token !== this.requestToken) return; // drop stale responses
          this.activities.push(...res.items);
          this.hasMore = res.hasMore;
          this.skip += res.items.length;          // same “skip + rendered count” process
          this.loading = false;
        },
        error: _ => { this.loading = false; }
      })
    );
  }

  private pickDataSource(skip: number, take: number): Observable<{ items: ActivityDto[]; hasMore: boolean }> {
    // When viewing someone else’s profile -> only their activities.
    // When viewing own profile (no :id) -> use /me/paged (your feed or your own list, per server impl).
    if (this.userId) {
      return this.activityService.getUserActivitiesPaged(this.userId, skip, take);
    } else {
      return this.activityService.getMyActivitiesPaged(skip, take);
    }
  }
}
