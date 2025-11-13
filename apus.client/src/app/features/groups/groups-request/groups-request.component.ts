import { Component, OnInit } from '@angular/core';
import { GroupJoinRequestDto } from '../groupsDTOs';
import { ActivatedRoute } from '@angular/router';
import { GroupService } from '../groupService';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-groups-request',
  standalone: false,
  templateUrl: './groups-request.component.html',
  styleUrls: ['./groups-request.component.css']
})

export class GroupsRequestComponent implements OnInit {
  groupId!: number;
  requests: GroupJoinRequestDto[] = [];
  loading = false;
  error: string | null = null;

  decidingIds = new Set<number>(); // to disable buttons per-row

  constructor(
    private route: ActivatedRoute,
    private groupService: GroupService
  ) { }

  ngOnInit(): void {
    // parent route has :id
    this.groupId = Number(this.route.parent?.snapshot.paramMap.get('id'));
    this.loadRequests();
  }

  async loadRequests() {
    if (!this.groupId) { return; }
    this.loading = true;
    this.error = null;

    try {
      const res = await this.groupService.getRequests(this.groupId).toPromise();
      this.requests = res ?? [];
    } catch (err) {
      console.error(err);
      this.error = 'Failed to load requests.';
    } finally {
      this.loading = false;
    }
  }

  async decide(r: GroupJoinRequestDto, approve: boolean) {
    if (this.decidingIds.has(r.id)) return;

    this.decidingIds.add(r.id);
    try {
      await this.groupService.decide(r.id, approve).toPromise();
      this.requests = this.requests.filter(x => x.id !== r.id);
    } catch (err) {
      console.error(err);
      this.error = 'Failed to send decision.';
    } finally {
      this.decidingIds.delete(r.id);
    }
  }

  avatarUrl(m: GroupJoinRequestDto): string {
    if (!m.avatarUrl) {
      return `${environment.apiBase}/Perm/DefaultProfile.png`;
    }
    // if avatarUrl is already absolute (starts with http), just return it
    if (m.avatarUrl.startsWith('http')) return m.avatarUrl;

    return `${environment.apiBase}${m.avatarUrl}`;
  }
}
