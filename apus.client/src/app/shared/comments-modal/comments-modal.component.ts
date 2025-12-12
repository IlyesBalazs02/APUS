import { Component, EventEmitter, Input, Output } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface CommentDto {
  id: string;
  authorUserId: string;
  authorFullName: string;
  authorAvatarUrl?: string | null;
  text: string;
  createdAtUtc: string;
}

@Component({
  selector: 'app-comments-modal',
  standalone: false,
  templateUrl: './comments-modal.component.html',
  styleUrls: ['./comments-modal.component.scss']
})
export class CommentsModalComponent {
  @Input() isOpen = false;
  @Input() loadUrl!: string;
  @Input() postUrl!: string;
  @Input() title = 'Comments';

  @Output() closed = new EventEmitter<void>();

  comments: CommentDto[] = [];
  loading = false;
  posting = false;
  newComment = '';
  loadError: string | null = null;

  constructor(private http: HttpClient) { }

  ngOnChanges() {
    if (this.isOpen && this.comments.length === 0) {
      this.loadComments();
    }
  }

  close() {
    this.closed.emit();
  }

  private loadComments() {
    if (!this.loadUrl) return;

    this.loading = true;
    this.loadError = null;

    this.http.get<CommentDto[]>(this.loadUrl).subscribe({
      next: res => {
        this.comments = res;
        this.loading = false;
      },
      error: () => {
        this.loadError = 'Failed to load comments';
        this.loading = false;
      }
    });
  }

  submit() {
    const text = this.newComment.trim();
    if (!text || this.posting || !this.postUrl) return;

    this.posting = true;

    this.http
      .post<CommentDto>(this.postUrl, { text })
      .subscribe({
        next: c => {
          this.comments.push(c);
          this.newComment = '';
          this.posting = false;
        },
        error: () => {
          this.posting = false;
        }
      });
  }
}
