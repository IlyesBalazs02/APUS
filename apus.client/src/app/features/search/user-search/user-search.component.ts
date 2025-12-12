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
  users: UserSearchDto[] = [];
  loading = false;
  hasMore = true;

  private pageSize = 30;
  private skip = 0;
  private io?: IntersectionObserver;
  private currentQuery = '';
  private requestToken = 0;

  friendStatus = new Map<string, FriendStatusDto>();
  requesting = new Set<string>();

  @ViewChild('sentinel', { static: true }) sentinelRef!: ElementRef<HTMLDivElement>;

  private trigger$ = new BehaviorSubject<void>(undefined);

  constructor(private api: UserSearchApi, private destroyRef: DestroyRef, private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        const q = (params.get('q') || '').trim();
        if (q !== this.currentQuery) {
          this.currentQuery = q;
          this.requestToken++;
          this.reset();
          if (this.currentQuery.length >= 3) {
            this.loadMore();
          }
        }
      });

    this.search.valueChanges.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      tap(value => {
        const term = (value ?? '').trim();

        if (term.length >= 3) {
          this.currentQuery = term.toLowerCase();
          this.requestToken++;
          this.reset();
          this.loadMore();
        } else {
          this.reset();
        }
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe();

    queueMicrotask(() => this.setupObserver());
  }

  private setupObserver() {
    this.io = new IntersectionObserver(entries => {
      const e = entries[0];
      if (e.isIntersecting) this.loadMore();
    }, { root: null, rootMargin: '400px', threshold: 0 });

    if (this.sentinelRef?.nativeElement) {
      this.io.observe(this.sentinelRef.nativeElement);
    }
  }

  private reset() {
    this.users = [];
    this.skip = 0;
    this.hasMore = true;
  }

  loadMore() {
    if (this.loading || !this.hasMore) return;
    this.loading = true;

    const term = (this.currentQuery ?? '').trim();
    const myToken = this.requestToken;

    this.api.search(term, this.skip, this.pageSize).pipe(
      tap(res => {
        if (myToken !== this.requestToken) return;
        if (term.length < 3) return;

        console.log('search result', res);
        this.users = [...this.users, ...res.items];
        this.hasMore = res.hasMore;
        this.skip += res.items.length;

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
