import { Component, DestroyRef, ElementRef, OnInit, ViewChild } from '@angular/core';
import { FormControl } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
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
  search = new FormControl<string>(''); // reactive input field for searching
  friends: FriendDto[] = [];  // list of friends to display

  // UI state flags
  loading = false;
  hasMore = true;

  // paging and internal tracking
  private pageSize = 30;
  private skip = 0;
  private io?: IntersectionObserver;
  private currentQuery = '';
  private requestToken = 0; // prevents stale async results

  @ViewChild('sentinel', { static: true }) sentinelRef!: ElementRef<HTMLDivElement>;

  constructor(
    private api: UserSearchApi,
    private destroyRef: DestroyRef,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    // React when ?q= in URL changes (from parent search bar)
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        const q = (params.get('q') || '').trim();
        if (q !== this.currentQuery) {
          this.currentQuery = q;
          this.requestToken++;  // cancel older loads
          this.reset();
          if (this.currentQuery.length >= 3) {
            this.loadMore();    // start first page
          }
        }
      });

    // React to local typing in this tab’s input
    this.search.valueChanges.pipe(
      debounceTime(250),
      distinctUntilChanged(),     // ignore duplicates
      tap(value => {
        const term = (value ?? '').trim();
        if (term.length >= 3) {
          this.currentQuery = term.toLowerCase();
          this.requestToken++;
          this.reset();
          this.loadMore();
        } else {
          this.reset(); // clear results if <3
        }
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe();

    // Set up infinite scroll observer
    queueMicrotask(() => this.setupObserver());
  }

  // Creates IntersectionObserver that triggers loadMore() when scrolled near bottom 
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

  // Loads the next page of friends from the backend
  loadMore() {
    if (this.loading || !this.hasMore) return;
    this.loading = true;

    const term = (this.currentQuery ?? '').trim();
    const myToken = this.requestToken;

    this.api.searchFriends(term, this.skip, this.pageSize).pipe(
      tap(res => {
        if (myToken !== this.requestToken) return; // ignore outdated or canceled requests

        // append new friends to existing list
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

  // Track function for *ngFor
  trackById = (_: number, f: FriendDto) => f.id;
}
