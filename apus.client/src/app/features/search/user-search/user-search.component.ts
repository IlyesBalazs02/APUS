import { Component, DestroyRef, ElementRef, OnInit, ViewChild } from '@angular/core';
import { FriendStatusDto, UserSearchApi, UserSearchDto } from './user-search.service';
import { FormControl } from '@angular/forms';
import { BehaviorSubject, debounceTime, distinctUntilChanged, tap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-user-search',
  standalone: false,
  templateUrl: './user-search.component.html',
  styleUrls: ['./user-search.component.css']
})

export class UserSearchComponent implements OnInit {
  search = new FormControl<string>('');
  users: UserSearchDto[] = [];  // Array of users shown in the results grid
  loading = false; // true while waiting for the backend
  hasMore = true;  // false if there are no more results (end of scroll)

  private pageSize = 30;   // how many users to load per request
  private skip = 0;        // current offset for pagination
  private io?: IntersectionObserver; // triggers infinite scroll
  private currentQuery = '';          // current search text
  private requestToken = 0;           // used to cancel old requests

  friendStatus = new Map<string, FriendStatusDto>(); // status per user id
  requesting = new Set<string>();

  @ViewChild('sentinel', { static: true }) sentinelRef!: ElementRef<HTMLDivElement>;

  private trigger$ = new BehaviorSubject<void>(undefined);

  constructor(private api: UserSearchApi, private destroyRef: DestroyRef, private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        const q = (params.get('q') || '').trim();
        // only act if changed
        if (q !== this.currentQuery) {
          this.currentQuery = q;
          this.requestToken++;      // invalidate in-flight requests
          this.reset();
          if (this.currentQuery.length >= 3) {
            this.loadMore();        // first page for this URL query
          }
        }
      });

    this.search.valueChanges.pipe(
      debounceTime(250),        // wait some time after typing stops
      distinctUntilChanged(),   // ignore if text hasn't changed
      tap(value => {
        const term = (value ?? '').trim();
        // Only search if at least 3 characters

        if (term.length >= 3) {
          this.currentQuery = term.toLowerCase();
          this.requestToken++;   // invalidate older requests
          this.reset();
          this.loadMore();
        } else {
          // Clear results if text shorter than 3 chars
          this.reset();
        }
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe();

    // Setup intersection observer for infinite scroll
    queueMicrotask(() => this.setupObserver());
  }

  // Creates an observer that loads more users when you scroll near the bottom
  private setupObserver() {
    this.io = new IntersectionObserver(entries => {
      const e = entries[0];
      if (e.isIntersecting) this.loadMore();
    }, { root: null, rootMargin: '400px', threshold: 0 }); // prefetch earlier

    if (this.sentinelRef?.nativeElement) {
      this.io.observe(this.sentinelRef.nativeElement);
    }
  }

  // Reset pagination and clear displayed users
  private reset() {
    this.users = [];
    this.skip = 0;
    this.hasMore = true;
  }

  loadMore() {
    if (this.loading || !this.hasMore) return;
    this.loading = true;

    const term = (this.currentQuery ?? '').trim(); // always string
    const myToken = this.requestToken;

    this.api.search(term, this.skip, this.pageSize).pipe(
      tap(res => {
        if (myToken !== this.requestToken) return;
        if (term.length < 3) return;

        console.log('search result', res);
        this.users = [...this.users, ...res.items];
        this.hasMore = res.hasMore;
        this.skip += res.items.length;

        // Fetch friend statuses for the new batch
        const ids = res.items.map(u => u.id);
        if (ids.length) {
          this.api.getFriendStatuses(ids).subscribe(map => {
            Object.values(map).forEach(s => this.friendStatus.set(s.userId, s));
          });
        }
      }),
      tap(() => (this.loading = false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      error: _ => { this.loading = false; }
    });
  }

  // Track function for *ngFor
  trackById = (_: number, u: UserSearchDto) => u.id;


  //#region friends
  canAdd(uId: string) {
    const s = this.friendStatus.get(uId);
    return !!s?.canRequest && !this.requesting.has(uId);
  }

  addFriend(uId: string) {
    if (!this.canAdd(uId)) return;
    this.requesting.add(uId);
    this.api.sendFriendRequest(uId).subscribe({
      next: () => {
        // After sending, reflect "Pending Outgoing"
        this.friendStatus.set(uId, {
          userId: uId,
          canRequest: false,
          reason: 'Request already sent',
          existingStatus: 'Pending',
          direction: 'Outgoing'
        });
        this.requesting.delete(uId);
      },
      error: () => {
        this.requesting.delete(uId);
      }
    });
  }

  statusLabel(uId: string): string | null {
    const s = this.friendStatus.get(uId);
    if (!s) return null;
    if (s.existingStatus === 'Accepted') return 'Friends';
    if (s.existingStatus === 'Pending') {
      return s.direction === 'Outgoing' ? 'Requested' : 'Respond';
    }
    return s.canRequest ? null : (s.reason ?? 'Unavailable');
  }
  //#endregion
}
