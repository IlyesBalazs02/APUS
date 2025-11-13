import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CreateGroupPostDto, GroupDto, GroupPostDto, GroupPostPermission, } from '../groupsDTOs';
import { GroupService } from '../groupService';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';


@Component({
  selector: 'app-groups-post',
  standalone: false,
  templateUrl: './groups-post.component.html',
  styleUrls: ['./groups-post.component.scss']
})
export class GroupsPostComponent implements OnInit {
  groupId!: number;
  group: GroupDto | null = null;

  posts: GroupPostDto[] = [];
  loading = false;
  loadingMore = false;
  hasMore = true;
  private pageSize = 10;
  private skip = 0;

  // create form
  title = '';
  text = '';
  creating = false;
  createError: string | null = null;

  currentUserId: string | null = null;
  canPost = false;

  constructor(
    private route: ActivatedRoute,
    private groupService: GroupService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.currentUserId = this.authService.currentUserId();

    // group is resolved on parent route
    this.group = this.route.parent?.snapshot.data['group'] as GroupDto | null ?? null;
    const idFromParam = this.route.parent?.snapshot.params['id'];
    this.groupId = this.group?.id ?? (idFromParam ? +idFromParam : 0);

    this.updateCanPost();
    this.loadInitial();
  }

  private updateCanPost() {
    if (!this.group) {
      this.canPost = false;
      return;
    }

    if (!this.group.isMember) {
      this.canPost = false;
      return;
    }

    if (this.group.whoCanPost === GroupPostPermission.AdminsOnly) {
      this.canPost = this.group.isAdmin;
    } else {
      // Members
      this.canPost = this.group.isMember;
    }
  }

  private async loadInitial() {
    if (!this.groupId) return;
    this.loading = true;
    this.skip = 0;
    try {
      const resp = await this.groupService.getPosts(this.groupId, this.skip, this.pageSize).toPromise();
      if (!resp) return;
      this.posts = resp.items;
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
      const resp = await this.groupService.getPosts(this.groupId, this.skip, this.pageSize).toPromise();
      if (!resp) return;
      this.posts = this.posts.concat(resp.items);
      this.hasMore = resp.hasMore;
      this.skip += resp.items.length;
    } finally {
      this.loadingMore = false;
    }
  }

  async createPost() {
    if (!this.groupId || !this.canPost || this.creating) return;

    const t = this.title.trim();
    const body = this.text.trim();
    if (!t || !body) return;

    this.creating = true;
    this.createError = null;
    const dto: CreateGroupPostDto = { title: t, text: body };

    try {
      const created = await this.groupService.createPost(this.groupId, dto).toPromise();
      if (created) {
        // prepend newest
        this.posts = [created, ...this.posts];
        this.title = '';
        this.text = '';
        // keep hasMore/skip as-is, paging will still work
      }
    } catch (err: any) {
      this.createError = 'Could not create post.';
    } finally {
      this.creating = false;
    }
  }

  avatarUrl(post: GroupPostDto): string {
    const url = post.authorAvatarUrl;
    if (!url) {
      return `${environment.apiBase}/Perm/DefaultProfile.png`;
    }
    if (url.startsWith('http')) return url;
    return `${environment.apiBase}${url}`;
  }

  canDelete(post: GroupPostDto): boolean {
    if (!this.currentUserId) return false;
    if (post.authorUserId === this.currentUserId) return true;
    return !!this.group?.isAdmin;
  }

  async deletePost(post: GroupPostDto) {
    if (!this.canDelete(post)) return;
    if (!confirm('Delete this post?')) return;

    await this.groupService.deletePost(post.id).toPromise();
    this.posts = this.posts.filter(p => p.id !== post.id);
  }
}
