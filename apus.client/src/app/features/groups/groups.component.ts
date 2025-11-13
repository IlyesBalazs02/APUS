import { Component, OnInit } from '@angular/core';
import { GroupDto, GroupMembersDto } from './groupsDTOs';
import { ActivatedRoute, Router } from '@angular/router';
import { GroupService } from './groupService';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';

@Component({
    selector: 'app-settings',
    standalone: false,
    templateUrl: './groups.component.html',
    styleUrls: ['./groups.component.scss']
})
export class GroupsComponent implements OnInit {
    group: GroupDto | null = null;
    currentUserId: string | null = null;
    joining = false;
    leaving = false;

    // show members modal
    membersOpen = false;
    members: GroupMembersDto[] = [];
    loadingMembers = false;
    isMember = false;
    isAdmin = false;

    constructor(private route: ActivatedRoute, private groupService: GroupService, private router: Router, private authService: AuthService) {
        this.group = this.route.snapshot.data['group'] ?? null;
    }

    ngOnInit(): void {
        this.currentUserId = this.authService.currentUserId();

        this.route.data.subscribe(({ group }) => {
            if (!group) return;
            this.group = group as GroupDto;
            this.isMember = this.group.isMember;
            this.isAdmin = this.group.isAdmin;
        });
    }

    async openMembers() {
        if (!this.group) return;
        this.loadingMembers = true;
        this.membersOpen = true;
        try {
            this.members = await this.groupService.getMembers(this.group.id).toPromise() || [];
        } finally {
            this.loadingMembers = false;
        }
    }

    closeMembers() { this.membersOpen = false; }


    async join() {
        if (!this.group) return;
        this.joining = true;
        try {
            await this.groupService.join(this.group.id).toPromise();
            this.group = { ...this.group, memberCount: this.group.memberCount + 1 };
            this.isMember = true;
        } finally {
            this.joining = false;
        }
    }

    async leave() {
        if (!this.group) return;
        this.leaving = true;
        try {
            await this.groupService.leave(this.group.id).toPromise();
            this.group = { ...this.group, memberCount: Math.max(0, this.group.memberCount - 1) };
            this.isMember = false;
        } finally {
            this.leaving = false;
        }
    }

    avatarUrl(m: GroupMembersDto): string {
        if (!m.avatarUrl) {
            return `${environment.apiBase}/Perm/DefaultProfile.png`;
        }
        // if avatarUrl is already absolute (starts with http), just return it
        if (m.avatarUrl.startsWith('http')) return m.avatarUrl;

        return `${environment.apiBase}${m.avatarUrl}`;
    }

    async kick(m: GroupMembersDto) {
        if (!this.group) return;
        await this.groupService.kickMember(this.group.id, m.userId).toPromise();
        this.members = this.members.filter(x => x.userId !== m.userId);
        this.group = { ...this.group, memberCount: this.group.memberCount - 1 };
    }
}
