import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { MainActivity, createActivity } from '../../_models/ActivityClasses';
import { HttpClient } from '@angular/common/http';
import { BreakpointObserver } from '@angular/cdk/layout';
import { ActivityDto } from '../../ActivityDto/ActivityDto';
import { ActivityService } from '../../../../core/services/activityService';
import { Subscription } from 'rxjs';
import { PagedResponse } from '../../../../shared/DTOs/PagedResponse';

@Component({
  selector: 'app-display-activities',
  standalone: false,
  templateUrl: './display-activities.component.html',
  styleUrl: './display-activities.component.css'
})

export class DisplayActivitiesComponent implements OnInit {
  activities: ActivityDto[] = [];
  loading = false;
  hasMore = true;

  private pageSize = 10;
  private skip = 0;
  private observer?: IntersectionObserver;
  private subs = new Subscription();
  private requestToken = 0;

  @ViewChild('sentinel', { static: true }) sentinelRef!: ElementRef<HTMLDivElement>;

  constructor(private activityService: ActivityService) { }

  ngOnInit(): void {
    this.setupObserver();
    this.loadMore();
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    this.subs.unsubscribe();
  }

  private setupObserver(): void {
    this.observer = new IntersectionObserver(entries => {
      if (entries.some(e => e.isIntersecting)) this.loadMore();
    }, { rootMargin: '400px 0px 400px 0px' }); // start a bit earlier for smoother UX

    this.observer.observe(this.sentinelRef.nativeElement);
  }

  private loadMore(): void {
    if (this.loading || !this.hasMore) return;
    this.loading = true;
    const token = ++this.requestToken;

    this.subs.add(
      this.activityService.getActivitiesPaged(this.skip, this.pageSize).subscribe({
        next: (res: PagedResponse<ActivityDto>) => {
          console.log(res);
          if (token !== this.requestToken) return; // drop stale responses
          this.activities.push(...res.items);
          this.hasMore = res.hasMore;
          this.skip += res.items.length; // same process as your friends/users list
          this.loading = false;
        },
        error: _ => { this.loading = false; }
      })
    );
  }

}
