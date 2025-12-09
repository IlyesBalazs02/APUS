import {
  Component, OnInit, OnDestroy, AfterViewInit, ViewChild, ElementRef
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CreateGroupEventDto, GroupDto, GroupEventDto, GroupEventParticipantDto, GroupEventPermission } from '../groupsDTOs';
import { GroupService } from '../groupService';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-groups-events',
  standalone: false,
  templateUrl: './groups-event.component.html',
  styleUrls: ['./groups-event.component.scss']
})
export class GroupsEventComponent implements OnInit, AfterViewInit, OnDestroy {
  group: GroupDto | null = null;
  groupId: number | null = null;

  events: GroupEventDto[] = [];
  loading = false;
  loadingMore = false;
  skip = 0;
  pageSize = 10;
  hasMore = false;

  // create event
  title = '';
  description = '';
  startsAtLocal = '';       // bound to <input type="datetime-local">
  trackActivityId = '';     // optional
  creating = false;
  createError: string | null = null;
  canCreateEvent = false;

  readonly maxTitleLength = 100;
  readonly maxDescriptionLength = 2000;

  private io?: IntersectionObserver;
  @ViewChild('sentinel', { static: false }) sentinelRef?: ElementRef<HTMLDivElement>;

  // participants modal
  participantsOpen = false;
  participantsEvent?: GroupEventDto;
  participants: GroupEventParticipantDto[] = [];
  loadingParticipants = false;

  joiningEventId: number | null = null;
  leavingEventId: number | null = null;

  constructor(
    private route: ActivatedRoute,
    private groupService: GroupService,
    private authService: AuthService
  ) {
    this.group = this.route.parent?.snapshot.data['group'] ?? null;
  }

  ngOnInit(): void {
    const idFromParam = this.route.parent?.snapshot.paramMap.get('id');
    this.groupId = idFromParam ? +idFromParam : null;
    this.group = this.route.parent?.snapshot.data['group'] ?? null;

    this.updateCanCreateEvent();
    this.loadInitial();
  }

  ngAfterViewInit(): void {
    if (!this.sentinelRef) return;

    this.io = new IntersectionObserver(entries => {
      for (const entry of entries) {
        if (entry.isIntersecting) {
          this.loadMore();
        }
      }
    });

    this.io.observe(this.sentinelRef.nativeElement);
  }

  ngOnDestroy(): void {
    this.io?.disconnect();
  }

  private async loadInitial() {
    if (!this.groupId) return;
    this.loading = true;
    this.skip = 0;
    try {
      const resp = await this.groupService
        .getEvents(this.groupId, this.skip, this.pageSize)
        .toPromise();
      if (!resp) return;

      this.events = resp.items;
      this.hasMore = resp.hasMore;
      this.skip += resp.items.length;
    } finally {
      this.loading = false;
    }
  }

  async loadMore() {
    if (!this.groupId || !this.hasMore || this.loadingMore) return;
    this.loadingMore = true;
    try {
      const resp = await this.groupService
        .getEvents(this.groupId, this.skip, this.pageSize)
        .toPromise();
      if (!resp) return;

      this.events = this.events.concat(resp.items);
      this.hasMore = resp.hasMore;
      this.skip += resp.items.length;
    } finally {
      this.loadingMore = false;
    }
  }

  private updateCanCreateEvent() {
    const g = this.group;
    if (!g || !g.isMember) {
      this.canCreateEvent = false;
      return;
    }

    if (g.whoCanCreateEvent === GroupEventPermission.AdminsOnly) {
      this.canCreateEvent = !!g.isAdmin;
    } else {
      this.canCreateEvent = true;
    }
  }

  async createEvent() {
    if (!this.groupId || !this.canCreateEvent || this.creating) return;

    let title = this.title.trim();
    let desc = this.description.trim();

    if (!title) return;

    if (title.length > this.maxTitleLength) {
      title = title.slice(0, this.maxTitleLength);
    }
    if (desc.length > this.maxDescriptionLength) {
      desc = desc.slice(0, this.maxDescriptionLength);
    }

    const dto: CreateGroupEventDto = {
      title,
      description: desc || null,
      startsAtUtc: this.startsAtLocal
        ? new Date(this.startsAtLocal).toISOString()
        : null,
      trackActivityId: this.trackActivityId.trim() || null
    };

    this.creating = true;
    this.createError = null;

    try {
      const created = await this.groupService
        .createEvent(this.groupId, dto)
        .toPromise();

      if (created) {
        // prepend new event
        this.events = [created, ...this.events];
        this.title = '';
        this.description = '';
        this.startsAtLocal = '';
        this.trackActivityId = '';
      }
    } catch {
      this.createError = 'Could not create event.';
    } finally {
      this.creating = false;
    }
  }


  // -------- Participants modal ----------

  async openParticipantsModal(ev: GroupEventDto) {
    this.participantsEvent = ev;
    this.participantsOpen = true;
    this.loadingParticipants = true;
    try {
      this.participants =
        (await this.groupService.getEventParticipants(ev.id).toPromise()) || [];
    } finally {
      this.loadingParticipants = false;
    }
  }

  closeParticipantsModal() {
    this.participantsOpen = false;
    this.participantsEvent = undefined;
    this.participants = [];
  }

  avatarUrl(e: GroupEventDto): string {
    const url = e.createdByAvatarUrl;
    if (!url) {
      return `${environment.apiBase}/Perm/DefaultProfile.png`;
    }
    if (url.startsWith('http')) return url;
    return `${environment.apiBase}${url}`;
  }

  // ------- join / leave ---------

  canJoin(e: GroupEventDto): boolean {
    // Only members can join, but **no special case for creator**,
    // so the creator can also join.
    return !!this.group?.isMember && !e.isJoinedByCurrentUser;
  }

  canLeave(e: GroupEventDto): boolean {
    return !!this.group?.isMember && e.isJoinedByCurrentUser;
  }

  async joinEvent(e: GroupEventDto) {
    if (!this.groupId || this.joiningEventId === e.id || !this.canJoin(e)) return;
    this.joiningEventId = e.id;
    try {
      await this.groupService.joinEvent(this.groupId, e.id).toPromise();
      e.isJoinedByCurrentUser = true;
      e.participantCount = (e.participantCount || 0) + 1;
    } finally {
      this.joiningEventId = null;
    }
  }

  async leaveEvent(e: GroupEventDto) {
    if (!this.groupId || this.leavingEventId === e.id || !this.canLeave(e)) return;
    this.leavingEventId = e.id;
    try {
      await this.groupService.leaveEvent(this.groupId, e.id).toPromise();
      e.isJoinedByCurrentUser = false;
      e.participantCount = Math.max(0, (e.participantCount || 0) - 1);
    } finally {
      this.leavingEventId = null;
    }
  }
}
