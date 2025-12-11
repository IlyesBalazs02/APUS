import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import {
  ActivityCalendarMonthDto,
  ActivityCalendarDayDto,
  profiledto,
  TrainingPeriod,
  TrainingTimeSummaryDto
} from './ProfileDto';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { ActivityDto } from '../../activities/ActivityDto/ActivityDto';
import { ActivityService } from '../../../core/services/activityService';
import { Observable, Subscription } from 'rxjs';

interface PieSegment {
  label: string;
  value: number;
  percent: number;
  path: string;
}

interface CalendarCell {
  day?: number;
  hasActivity: boolean;
  isToday: boolean;
}

@Component({
  selector: 'app-user-profile',
  standalone: false,
  templateUrl: './user-profile.component.html',
  styleUrl: './user-profile.component.css'
})
export class UserProfileComponent implements OnInit, OnDestroy {
  profile?: profiledto;

  // Paged activities + state
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

  // Statistics & calendar
  trainingSummary?: TrainingTimeSummaryDto;
  calendarMonth?: ActivityCalendarMonthDto;

  selectedPeriod: TrainingPeriod = 'LastWeek';
  periods: TrainingPeriod[] = ['LastWeek', 'LastMonth', 'LastYear'];

  pieSegments: PieSegment[] = [];
  calendarWeeks: CalendarCell[][] = [];
  weekDayNames: string[] = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
  monthNames: string[] = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'
  ];
  calendarYear!: number;
  calendarMonthNumber!: number; // 1-12

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

        // Init calendar month/year to current
        const now = new Date();
        this.calendarYear = now.getFullYear();
        this.calendarMonthNumber = now.getMonth() + 1;

        // Load profile header
        const profileUrl = this.userId ? `/api/userprofile/${this.userId}` : `/api/userprofile/me`;
        this.http.get<profiledto>(profileUrl).subscribe({
          next: data => (this.profile = data),
          error: err => console.error('Failed to load profile', err)
        });

        // Load statistics & calendar
        this.loadStatistics();
        this.loadCalendar();

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

  // ---------- Infinite scroll ----------

  private setupObserver(): void {
    this.observer?.disconnect(); // in case of route changes
    if (!this.sentinelRef) {
      return;
    }

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
          this.skip += res.items.length;
          this.loading = false;
        },
        error: _ => { this.loading = false; }
      })
    );
  }

  private pickDataSource(skip: number, take: number): Observable<{ items: ActivityDto[]; hasMore: boolean }> {
    if (this.userId) {
      return this.activityService.getUserActivitiesPaged(this.userId, skip, take);
    } else {
      return this.activityService.getMyActivitiesPaged(skip, take);
    }
  }

  // ---------- Statistics ----------

  get totalSportsHours(): number {
    if (!this.trainingSummary?.sports?.length) return 0;
    return this.trainingSummary.sports.reduce((sum, s) => sum + s.totalHours, 0);
  }

  // convert decimal hours → "H:MM:SS"
  formatHoursToHms(hours: number | null | undefined): string {
    if (hours == null || isNaN(hours as number)) {
      return '0:00:00';
    }
    const totalSeconds = Math.round((hours as number) * 3600);
    const h = Math.floor(totalSeconds / 3600);
    const m = Math.floor((totalSeconds % 3600) / 60);
    const s = totalSeconds % 60;
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${h}:${pad(m)}:${pad(s)}`;
  }

  periodLabel(period: TrainingPeriod): string {
    switch (period) {
      case 'LastWeek': return 'Last 1 week';
      case 'LastMonth': return 'Last 1 month';
      case 'LastYear': return 'Last 1 year';
      default: return period;
    }
  }

  onPeriodChange(period: TrainingPeriod): void {
    if (this.selectedPeriod === period) return;
    this.selectedPeriod = period;
    this.loadStatistics();
  }

  private loadStatistics(): void {
    const userIdOrNull = this.userId ?? null;
    this.getTrainingTime(userIdOrNull, this.selectedPeriod).subscribe({
      next: summary => {
        this.trainingSummary = summary;
        this.buildPieSegments();
      },
      error: err => {
        console.error('Failed to load training stats', err);
        this.trainingSummary = undefined;
        this.pieSegments = [];
      }
    });
  }

  private buildPieSegments(): void {
    if (!this.trainingSummary || !this.trainingSummary.sports?.length) {
      this.pieSegments = [];
      return;
    }

    const total = this.totalSportsHours;
    if (total <= 0) {
      this.pieSegments = [];
      return;
    }

    const segments: PieSegment[] = [];
    const cx = 50;
    const cy = 50;
    const r = 45;

    let startAngle = 0; // radians, from +X axis, counter-clockwise

    for (const sport of this.trainingSummary.sports) {
      const value = sport.totalHours;
      if (value <= 0) continue;

      const fraction = value / total;
      const sliceAngle = 2 * Math.PI * fraction;
      const endAngle = startAngle + sliceAngle;

      const x1 = cx + r * Math.cos(startAngle);
      const y1 = cy + r * Math.sin(startAngle);
      const x2 = cx + r * Math.cos(endAngle);
      const y2 = cy + r * Math.sin(endAngle);

      const largeArcFlag = sliceAngle > Math.PI ? 1 : 0;

      const d = [
        `M ${cx} ${cy}`,
        `L ${x1} ${y1}`,
        `A ${r} ${r} 0 ${largeArcFlag} 1 ${x2} ${y2}`,
        'Z'
      ].join(' ');

      segments.push({
        label: sport.activityType,
        value,
        percent: fraction * 100,
        path: d
      });

      startAngle = endAngle;
    }

    this.pieSegments = segments;
  }

  private getTrainingTime(userId: string | null, period: TrainingPeriod) {
    const base = userId ? `/api/userprofile/${userId}` : `/api/userprofile/me`;
    return this.http.get<TrainingTimeSummaryDto>(`${base}/training-time`, {
      params: { period }
    });
  }

  // ---------- Calendar ----------

  private loadCalendar(): void {
    const userIdOrNull = this.userId ?? null;
    const year = this.calendarYear;
    const month = this.calendarMonthNumber;
    this.getCalendar(userIdOrNull, year, month).subscribe({
      next: calendar => {
        this.calendarMonth = calendar;
        this.buildCalendarWeeks(calendar);
      },
      error: err => {
        console.error('Failed to load calendar', err);
        this.calendarMonth = undefined;
        this.calendarWeeks = [];
      }
    });
  }

  private buildCalendarWeeks(calendar: ActivityCalendarMonthDto): void {
    if (!calendar || !calendar.days) {
      this.calendarWeeks = [];
      return;
    }

    const year = calendar.year;
    const month = calendar.month; // 1-12

    const firstOfMonth = new Date(Date.UTC(year, month - 1, 1));
    // JS: Sunday = 0, Monday = 1, ...; we want Monday as first column
    let firstWeekday = firstOfMonth.getUTCDay(); // 0-6
    if (firstWeekday === 0) {
      firstWeekday = 7;
    }

    const daysInMonth = new Date(Date.UTC(year, month, 0)).getUTCDate();

    const activityDays = new Set<number>();
    calendar.days.forEach((d: ActivityCalendarDayDto) => {
      activityDays.add(d.day);
    });


    const today = new Date();
    const todayYear = today.getFullYear();
    const todayMonth = today.getMonth() + 1;
    const todayDate = today.getDate();

    const weeks: CalendarCell[][] = [];
    let week: CalendarCell[] = [];

    // leading empty cells
    for (let i = 1; i < firstWeekday; i++) {
      week.push({ day: undefined, hasActivity: false, isToday: false });
    }

    for (let day = 1; day <= daysInMonth; day++) {
      const isToday = (day === todayDate && month === todayMonth && year === todayYear);
      const hasActivity = activityDays.has(day);

      week.push({ day, hasActivity, isToday });

      if (week.length === 7) {
        weeks.push(week);
        week = [];
      }
    }

    // trailing empty cells
    if (week.length > 0) {
      while (week.length < 7) {
        week.push({ day: undefined, hasActivity: false, isToday: false });
      }
      weeks.push(week);
    }

    this.calendarWeeks = weeks;
  }

  // navigate months (delta = -1 for previous, +1 for next)
  changeMonth(delta: number): void {
    let year = this.calendarYear;
    let month = this.calendarMonthNumber + delta;

    while (month < 1) {
      month += 12;
      year--;
    }
    while (month > 12) {
      month -= 12;
      year++;
    }

    this.calendarYear = year;
    this.calendarMonthNumber = month;
    this.loadCalendar();
  }

  private getCalendar(userId: string | null, year?: number, month?: number) {
    const base = userId ? `/api/userprofile/${userId}` : `/api/userprofile/me`;
    const params: any = {};
    if (year) params.year = year.toString();
    if (month) params.month = month.toString();
    return this.http.get<ActivityCalendarMonthDto>(`${base}/calendar`, { params });
  }
}
