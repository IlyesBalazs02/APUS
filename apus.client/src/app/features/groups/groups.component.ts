import { Component } from '@angular/core';
import { GroupDto } from './groupsDTOs';
import { ActivatedRoute, Router } from '@angular/router';
import { GroupService } from './groupService';

@Component({
    selector: 'app-settings',
    standalone: false,
    templateUrl: './groups.component.html',
    styleUrl: './groups.component.scss'
})
export class GroupsComponent {
    group: GroupDto | null = null;
    joining = false;
    leaving = false;

    constructor(private route: ActivatedRoute, private groupService: GroupService, private router: Router) {
        this.group = this.route.snapshot.data['group'] ?? null;
    }

    async join() {
        if (!this.group) return;
        this.joining = true;
        try {
            await this.groupService.join(this.group.id).toPromise();
            // optimistic UI: increment members
            this.group = { ...this.group, memberCount: this.group.memberCount + 1 };
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
        } finally {
            this.leaving = false;
        }
    }
}
