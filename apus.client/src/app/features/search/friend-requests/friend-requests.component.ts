import { Component, OnInit } from '@angular/core';
import { FriendRequestItemDto, FriendRequestsApi } from './friend-requests.service';

@Component({
  selector: 'app-friend-requests',
  standalone: false,
  templateUrl: './friend-requests.component.html',
  styleUrl: './friend-requests.component.css'
})
export class FriendRequestsComponent implements OnInit {
  loading = false;
  items: FriendRequestItemDto[] = [];
  acting = new Set<string>();

  constructor(private api: FriendRequestsApi) { }

  ngOnInit(): void {
    this.refresh();
  }

  refresh() {
    this.loading = true;
    this.api.getIncoming().subscribe({
      next: list => { this.items = list; this.loading = false; },
      error: _ => { this.loading = false; }
    });
  }

  accept(fromUserId: string) {
    if (this.acting.has(fromUserId)) return;
    this.acting.add(fromUserId);
    this.api.accept(fromUserId).subscribe({
      next: () => { this.items = this.items.filter(i => i.fromUserId !== fromUserId); this.acting.delete(fromUserId); },
      error: () => { this.acting.delete(fromUserId); }
    });
  }

  reject(fromUserId: string) {
    if (this.acting.has(fromUserId)) return;
    this.acting.add(fromUserId);
    this.api.reject(fromUserId).subscribe({
      next: () => { this.items = this.items.filter(i => i.fromUserId !== fromUserId); this.acting.delete(fromUserId); },
      error: () => { this.acting.delete(fromUserId); }
    });
  }
}
