import { Component, OnInit } from '@angular/core';
import {
  GroupEventPermission,
  GroupPostPermission,
  GroupSettingsDto,
  UpdateGroupSettingsDto
} from '../groupsDTOs';
import { ActivatedRoute } from '@angular/router';
import { GroupService } from '../groupService';
import { firstValueFrom } from 'rxjs';

type SettingsModal = 'name' | 'description' | 'join' | 'post' | 'event' | null;

@Component({
  selector: 'app-groups-settings',
  standalone: false,
  templateUrl: './groups-settings.component.html',
  styleUrls: ['./groups-settings.component.scss']
})
export class GroupsSettingsComponent implements OnInit {
  groupId!: number;
  settings: GroupSettingsDto | null = null;

  loading = true;
  saving = false;
  error: string | null = null;
  saved = false;

  activeModal: SettingsModal = null;

  nameDraft = '';
  descriptionDraft: string = '';

  joinDraft: 'open' | 'closed' = 'open';
  postDraft: 'admins' | 'members' = 'members';
  eventDraft: 'admins' | 'members' = 'admins';

  GroupPostPermission = GroupPostPermission;
  GroupEventPermission = GroupEventPermission;

  constructor(
    private route: ActivatedRoute,
    private groupService: GroupService
  ) { }

  ngOnInit(): void {
    this.route.parent?.paramMap.subscribe(params => {
      const id = params.get('id');
      if (!id) return;
      this.groupId = +id;
      this.loadSettings();
    });
  }

  async loadSettings() {
    this.loading = true;
    this.error = null;
    this.saved = false;

    try {
      const s = await firstValueFrom(this.groupService.getSettings(this.groupId));
      this.settings = s ?? null;
    } catch (err: any) {
      this.error = err?.error ?? 'Failed to load settings';
    } finally {
      this.loading = false;
    }
  }

  openModal(kind: SettingsModal) {
    if (!this.settings) return;

    this.activeModal = kind;
    this.error = null;
    this.saved = false;

    if (kind === 'name') {
      this.nameDraft = this.settings.name;
    } else if (kind === 'description') {
      this.descriptionDraft = this.settings.description ?? '';
    } else if (kind === 'join') {
      this.joinDraft = this.settings.isOpen ? 'open' : 'closed';
    } else if (kind === 'post') {
      this.postDraft = this.settings.whoCanPost === GroupPostPermission.AdminsOnly ? 'admins' : 'members';
    } else if (kind === 'event') {
      this.eventDraft = this.settings.whoCanCreateEvent === GroupEventPermission.AdminsOnly ? 'admins' : 'members';
    }
  }

  closeModal() {
    this.activeModal = null;
  }

  private async doUpdate(partial: UpdateGroupSettingsDto) {
    if (!this.settings) return;
    this.saving = true;
    this.error = null;

    try {
      await firstValueFrom(this.groupService.updateSettings(this.groupId, partial));
      await this.loadSettings();
      this.saved = true;
      this.closeModal();
    } catch (err: any) {
      this.error = err?.error ?? 'Failed to save settings';
    } finally {
      this.saving = false;
    }
  }

  async saveName() {
    await this.doUpdate({ name: this.nameDraft });
  }

  async saveDescription() {
    await this.doUpdate({ description: this.descriptionDraft });
  }

  async saveJoinPermission() {
    const isOpen = this.joinDraft === 'open';
    await this.doUpdate({ isOpen });
  }

  async savePostPermission() {
    const whoCanPost =
      this.postDraft === 'admins'
        ? GroupPostPermission.AdminsOnly
        : GroupPostPermission.Members;

    await this.doUpdate({ whoCanPost });
  }

  async saveEventPermission() {
    const whoCanCreateEvent =
      this.eventDraft === 'admins'
        ? GroupEventPermission.AdminsOnly
        : GroupEventPermission.Members;

    await this.doUpdate({ whoCanCreateEvent });
  }
}
