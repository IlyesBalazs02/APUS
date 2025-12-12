import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CreateGroupPostDto, GroupDto, GroupPostDto, GroupPostPermission, } from '../groupsDTOs';
import { GroupService } from '../groupService';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

type GroupPostVm = GroupPostDto & {
  _expanded: boolean;
  _commentsOpen: boolean;
};

@Component({
  selector: 'app-groups-post',
  standalone: false,
  templateUrl: './groups-post.component.html',
  styleUrls: ['./groups-post.component.scss']
})


export class GroupsPostComponent implements OnInit, AfterViewInit, OnDestroy {

  groupId!: number;
  group: GroupDto | null = null;

  posts: GroupPostVm[] = [];
  loading = false;
  loadingMore = false;
  hasMore = true;
  private pageSize = 10;
  private skip = 0;

  title = '';
  text = '';
  creating = false;
  createError: string | null = null;

  currentUserId: string | null = null;
  canPost = false;

  readonly maxTitleLength = 100;
  readonly maxTextLength = 2000;
  readonly maxPreviewLength = 300;

  deleteTargetId: number | null = null;

  commentsOpenPostId: number | null = null;

  // infinite scroll
  @ViewChild('sentinel') sentinelRef?: ElementRef<HTMLDivElement>;
  private io?: IntersectionObserver;

  constructor(
    private route: ActivatedRoute,
    private groupService: GroupService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.currentUserId = this.authService.currentUserId();

    this.group = this.route.parent?.snapshot.data['group'] as GroupDto | null ?? null;
    const idFromParam = this.route.parent?.snapshot.params['id'];
    this.groupId = this.group?.id ?? (idFromParam ? +idFromParam : 0);

    this.updateCanPost();
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
    }, {
      root: null,
      rootMargin: '0px',
      threshold: 0.1
    });

    this.io.observe(this.sentinelRef.nativeElement);
  }

  ngOnDestroy(): void {
    this.io?.disconnect();
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
      this.posts = resp.items.map(p => ({
        ...p,
        _expanded: false,
        _commentsOpen: false
      }));
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
      this.posts = this.posts.concat(
        resp.items.map(p => ({
          ...p,
          _expanded: false,
          _commentsOpen: false
        }))
      );
      this.hasMore = resp.hasMore;
      this.skip += resp.items.length;
    } finally {
      this.loadingMore = false;
    }
  }


  async createPost() {
    if (!this.groupId || !this.canPost || this.creating) return;

    let t = this.title.trim();
    let body = this.text.trim();
    if (!t || !body) return;

    if (t.length > this.maxTitleLength) t = t.slice(0, this.maxTitleLength);
    if (body.length > this.maxTextLength) body = body.slice(0, this.maxTextLength);

    this.creating = true;
    this.createError = null;
    const dto: CreateGroupPostDto = { title: t, text: body };

    try {
      const created = await this.groupService.createPost(this.groupId, dto).toPromise();
      if (created) {
        this.posts = [
          { ...created, _expanded: false, _commentsOpen: false },
          ...this.posts
        ];
        this.title = '';
        this.text = '';
      }

    } catch {
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

  openDeletePopup(post: GroupPostDto & { _expanded?: boolean }) {
    this.deleteTargetId = post.id;
  }

  closeDeletePopup() {
    this.deleteTargetId = null;
  }

  async confirmDelete() {
    if (this.deleteTargetId == null) return;

    await this.groupService.deletePost(this.deleteTargetId).toPromise();
    this.posts = this.posts.filter(p => p.id !== this.deleteTargetId);
    this.deleteTargetId = null;
  }

  toggleExpand(post: GroupPostVm) {
    post._expanded = !post._expanded;
  }


  // ----------- Comments --------------
  openCommentsModal(post: GroupPostDto) {
    this.commentsOpenPostId = post.id;
  }

  closeCommentsModal() {
    this.commentsOpenPostId = null;
  }

  commentsUrl(post: GroupPostDto): string {
    return `/api/groups/posts/${post.id}/comments`;
  }
}
