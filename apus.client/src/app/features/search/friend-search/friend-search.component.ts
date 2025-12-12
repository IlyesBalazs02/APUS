import { Component, DestroyRef, ElementRef, OnInit, ViewChild } from '@angular/core';
import { FormControl } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { FriendDto, UserSearchApi } from '../user-search/user-search.service';
import { debounceTime, distinctUntilChanged, tap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-friend-search',
  standalone: false,
  templateUrl: './friend-search.component.html',
  styleUrls: ['./friend-search.component.css']
})
export class FriendSearchComponent implements OnInit {
  search = new FormControl<string>('');
  friends: FriendDto[] = [];

  loading = false;
  hasMore = true;

  private pageSize = 30;
  private skip = 0;
  private io?: IntersectionObserver;
  private currentQuery = '';
  private requestToken = 0;

  @ViewChild('sentinel', { static: true }) sentinelRef!: ElementRef<HTMLDivElement>;

  constructor(
    private api: UserSearchApi,
    private destroyRef: DestroyRef,
    private route: ActivatedRoute,
  ) { }

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
    this.friends = [];
    this.skip = 0;
    this.hasMore = true;
  }

  loadMore() {
    if (this.loading || !this.hasMore) return;
    this.loading = true;

    const term = (this.currentQuery ?? '').trim();
    const myToken = this.requestToken;

    this.api.searchFriends(term, this.skip, this.pageSize).pipe(
      tap(res => {
        if (myToken !== this.requestToken) return;

        this.friends = [...this.friends, ...res.items];
        this.hasMore = !!res.hasMore;
        this.skip += res.items.length;
      }),
      tap(() => (this.loading = false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      error: _ => { this.loading = false; }
    });
  }

  trackById = (_: number, f: FriendDto) => f.id;
}
