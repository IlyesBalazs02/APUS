import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subject, debounceTime, switchMap, startWith, scan, BehaviorSubject, takeUntil } from 'rxjs';
import { CreateGroupDto, GroupDto } from '../groupsDTOs';
import { GroupService } from '../groupService';

@Component({
  selector: 'app-groups-home',
  standalone: false,
  templateUrl: './groups-home.component.html',
  styleUrls: ['./groups-home.component.css']
})
export class GroupsHomeComponent implements OnInit, OnDestroy {
  q = '';
  private search$ = new BehaviorSubject<string>('');
  private page$ = new BehaviorSubject<number>(0);
  private destroy$ = new Subject<void>();

  groups: GroupDto[] = [];
  pageSize = 20;
  loading = false;
  hasMore = true;

  // Create form (simple)
  newGroup: CreateGroupDto = { name: '', description: '', isOpen: true };
  creating = false;

  constructor(private groupService: GroupService) { }

  ngOnInit(): void {
    // search+paginate stream
    this.search$.pipe(
      debounceTime(200),
      switchMap(q => {
        this.page$.next(0);
        this.loading = true;
        return this.groupService.search(q, 0, this.pageSize);
      }),
      takeUntil(this.destroy$)
    ).subscribe(firstPage => {
      this.groups = firstPage;
      this.hasMore = firstPage.length === this.pageSize;
      this.loading = false;
    });

    // initial load
    this.search$.next('');
  }

  onSearchChange(v: string) {
    this.q = v;
    this.search$.next(v);
  }

  async loadMore() {
    if (!this.hasMore || this.loading) return;
    this.loading = true;
    const nextPage = this.page$.value + 1;
    const more = await this.groupService.search(this.q, nextPage * this.pageSize, this.pageSize).toPromise();
    this.groups = this.groups.concat(more || []);
    this.hasMore = (more?.length || 0) === this.pageSize;
    this.page$.next(nextPage);
    this.loading = false;
  }

  async create() {
    if (!this.newGroup.name.trim() || this.creating) return;
    this.creating = true;
    try {
      const g = await this.groupService.create(this.newGroup).toPromise();
      if (g) this.groups.unshift(g);
      // reset form
      this.newGroup = { name: '', description: '', isOpen: true };
    } finally {
      this.creating = false;
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
